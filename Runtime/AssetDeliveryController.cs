using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Top-level singleton for Google Play Asset Delivery. All public methods are main-thread only.
    /// Singleton created lazily on first Instance access — place in scene via Prefab for explicit control,
    /// or rely on the lazy factory for fire-and-forget usage.
    ///
    /// Awake strict ordering (I9):
    ///   1. Load settings
    ///   2. Select provider
    ///   3. ReconcileStateAsync
    ///   4. CreateInScene update handler
    ///   5. StartListening if AutoStartStateListener
    ///   6. ReArm stall detector for any in-flight pack found by reconcile
    /// </summary>
    public sealed class AssetDeliveryController : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────

        private static AssetDeliveryController _instance;

        public static AssetDeliveryController Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (!Application.isPlaying) return null;
                var go = new GameObject("[AssetDeliveryController]");
                _instance = go.AddComponent<AssetDeliveryController>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        // ─── Inspector-serialized fields ─────────────────────────────────────────

        [SerializeField] private AssetDeliveryMockConfig _mockConfig;

        // ─── Private state ────────────────────────────────────────────────────────

        private AssetDeliverySettings              _settings;
        private IAssetDeliveryProvider             _provider;
        private IAssetPackStateListener            _stateListener;
        private IAssetDeliveryAnalyticsAdapter     _analytics;
        private AssetPackStatesStream              _statesStream;
        private AssetDeliveryStallDetector         _stallDetector;
        private int                                _mainThreadId;

        private readonly Dictionary<string, AssetPackState>      _tracked        = new Dictionary<string, AssetPackState>();
        private readonly HashSet<string>                          _inFlightFetches = new HashSet<string>();
        private readonly Dictionary<string, FetchCancelReason>    _cancelReasons   = new Dictionary<string, FetchCancelReason>();
        // B2: per-pack cellular consent in-flight tracking
        private readonly Dictionary<string, bool>                 _cellularConsentInFlight = new Dictionary<string, bool>();
        // B3: loaded bundles weak-reference tracking
        private readonly Dictionary<string, List<WeakReference<UnityEngine.AssetBundle>>> _loadedBundles =
            new Dictionary<string, List<WeakReference<UnityEngine.AssetBundle>>>();
        // B4: stall detector pause tracking (dialog in flight)
        private readonly Dictionary<string, bool>                 _confirmationDialogInFlight = new Dictionary<string, bool>();

        private readonly object _stateLock = new object();

        // ─── Public events ────────────────────────────────────────────────────────

        /// <summary>Fired on the main thread for every state transition of any in-flight pack.</summary>
        public event Action<string, AssetPackState>      OnPackStateChanged;
        /// <summary>Fired when a fetch fails fatally.</summary>
        public event Action<AssetDeliveryError>          OnError;
        /// <summary>
        /// Fired when any pack enters WaitingForWifi or RequiresUserConfirmation.
        /// Consumer shows custom UI then calls request.AcceptConsent() or request.DenyConsent().
        /// If no handler is subscribed within 100 ms, the controller auto-invokes Google's native dialog.
        /// </summary>
        public event Action<CellularConsentRequest>      OnCellularConsentRequired;

        // ─── Public read-only ─────────────────────────────────────────────────────

        public IReadOnlyDictionary<string, AssetPackState> TrackedStates
        {
            get { lock (_stateLock) { return new Dictionary<string, AssetPackState>(_tracked); } }
        }

        /// <summary>
        /// Kind of the active provider. Used by the Pack Inspector to gate error injection
        /// (B6 — only allowed when Mock or LocalHttp).
        /// </summary>
        public ProviderKind ProviderKind => _provider?.Kind ?? ProviderKind.Mock;

#if UNITY_EDITOR
        /// <summary>
        /// Test-only: when true, analytics adapter calls are skipped for the NEXT state transition
        /// (one-shot semantics). Set by the Pack Inspector Inject Error action before injection.
        /// Clears automatically after the first post-injection state-changed event.
        /// </summary>
        internal bool SuppressAnalytics { get; set; }
#endif

        // ─── Awake ────────────────────────────────────────────────────────────────

        private async void Awake()
        {
            // Singleton guard
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            // Step 1 — Load settings (test injector wins if set)
            _settings = AssetDeliveryTestInjector.GetSettings()
                        ?? Resources.Load<AssetDeliverySettings>(AssetDeliverySettings.ResourcesLoadKey)
                        ?? ScriptableObject.CreateInstance<AssetDeliverySettings>();

            int  queueCap          = _settings.StateQueueCapacity;
            bool autoStartListener = _settings.AutoStartStateListener;
            _statesStream = new AssetPackStatesStream(maxCapacity: queueCap);

            // Step 2 — Select provider
#if UNITY_ANDROID && !UNITY_EDITOR
#if DEVELOPMENT_BUILD
            if (_settings.UseMockInDevelopmentBuild)
            {
                var mock         = new MockAssetDeliveryProvider(_mockConfig);
                _provider        = mock;
                _stateListener   = mock;
                BizSimLogger.Info("Development build: using MockAssetDeliveryProvider per settings");
            }
            else
#endif
            {
                // Android real provider — Phase 4
                var android      = new AndroidAssetDeliveryProvider();
                _provider        = android;
                _stateListener   = android;
            }
#else
            var mockProvider = new MockAssetDeliveryProvider(_mockConfig);
            _provider      = mockProvider;
            _stateListener = mockProvider;
#endif

            // Register test injector so tests can retrieve the current provider.
            AssetDeliveryTestInjector.SetProvider(_provider);

            // Step 3 — Awake state reconciliation (ADR-018)
            var allDeclared = _settings.AllDeclaredPackNames ?? Array.Empty<string>();
            await _provider.ReconcileStateAsync(allDeclared);

            // Step 4 — Create update handler MonoBehaviour (ADR-017)
            var updateHandler = AssetDeliveryUpdateHandler.CreateInScene(this);
            // Wire is called after Step 6 (stall detector init) below; we store a ref for now.
            // Wiring happens at end of Awake after all subsystems are ready.

            // Step 5 — Wire state listener + optionally start
            _stateListener.OnStateUpdate += HandleState;
            if (autoStartListener)
                _stateListener.StartListening();

            // Step 6 — Init stall detector (ADR-016)
            _stallDetector = new AssetDeliveryStallDetector(
                nowProvider: () => Time.unscaledTime,
                cancelCallback: HandleStallAsync,
                isPausedCallback: StallDetectorPaused);

            updateHandler.Wire(_provider, _stateListener, _stallDetector);
            _stallDetector.Configure(_settings.StallTimeoutSeconds);

            BizSimLogger.Info($"Initialized v{PackageVersion.Current}");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Opt-in for AutoStartStateListener = false consumers.</summary>
        public void StartStateListener()
        {
            EnsureMainThread();
            _stateListener?.StartListening();
        }

        /// <summary>
        /// Downloads one pack. Thin wrapper over FetchGroupAsync — projects the single pack handle.
        /// </summary>
        public async Task<AssetPackHandle> FetchAsync(
            string packName,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            EnsureMainThread();
            AssetPackNameValidator.ThrowIfInvalid(packName);
            var result = await FetchGroupAsync(new[] { packName }, null, ct);
            return result.PackHandles.TryGetValue(packName, out var h) ? h : null;
        }

        /// <summary>
        /// Downloads one or more packs concurrently. Primary method — FetchAsync delegates here.
        /// </summary>
        public async Task<AssetPackGroupResult> FetchGroupAsync(
            IEnumerable<string> packNames,
            FetchOptions options = null,
            CancellationToken ct = default)
        {
            EnsureMainThread();
            if (packNames == null) throw new ArgumentNullException(nameof(packNames));

            var nameList = new List<string>(packNames);
            if (nameList.Count == 0) throw new ArgumentException("packNames must be non-empty", nameof(packNames));

            // Validate and check duplicates
            var seen = new HashSet<string>();
            foreach (var n in nameList)
            {
                AssetPackNameValidator.ThrowIfInvalid(n);
                if (!seen.Add(n)) throw new ArgumentException($"Duplicate pack name '{n}' in group.", nameof(packNames));
            }

            // Partition install-time vs fetchable
            var installTimeSet = new HashSet<string>(_settings.InstallTimePackNames ?? Array.Empty<string>());
            var installTimePacks = new List<string>();
            var fetchPacks       = new List<string>();
            foreach (var n in nameList)
            {
                if (installTimeSet.Contains(n)) installTimePacks.Add(n);
                else                             fetchPacks.Add(n);
            }

            // Synthesize states for install-time packs
            var allStates  = new Dictionary<string, AssetPackState>();
            var allHandles = new Dictionary<string, AssetPackHandle>();
            foreach (var n in installTimePacks)
            {
                var state = new AssetPackState(n, AssetPackStatus.Completed,
                    AssetPackErrorCode.NoError, 0, 0, 100, DateTime.UtcNow);
                allStates[n] = state;
                BizSimLogger.Info($"Install-time pack '{n}' short-circuited to COMPLETED");
            }

            if (fetchPacks.Count == 0)
            {
                return new AssetPackGroupResult(allStates, allHandles, 1f, 0, 0f);
            }

            // Pre-flight (unless SkipPreflight)
            if (options?.SkipPreflight != true)
            {
                var preflight = await CanFetchAsyncInternal(fetchPacks, ct);
                if (!preflight.CanFetch)
                {
                    if (preflight.Reason == FetchPreflightReason.InsufficientStorage
                        && options?.AutoEvictLruOnInsufficientStorage == true)
                    {
                        // Try LRU eviction then re-check once
                        long needed = preflight.EstimatedBytesRequired
                                      - preflight.AvailableBytesOnStorage
                                      + 200L * 1024 * 1024;
                        await Cache.EvictLruAsync(needed, _inFlightFetches, _loadedBundles,
                            installTimeSet, _provider);
                        var recheck = await CanFetchAsyncInternal(fetchPacks, ct);
                        if (!recheck.CanFetch)
                            throw new InvalidOperationException(
                                $"Insufficient storage after LRU eviction: need {recheck.EstimatedBytesRequired}, have {recheck.AvailableBytesOnStorage}.");
                    }
                    else
                    {
                        SafeInvokeAnalytics(a => a.pad_preflight_blocked(new PadPreflightBlockedEvent(
                            preflight.Reason, preflight.EstimatedBytesRequired, preflight.AvailableBytesOnStorage)));
                        throw new InvalidOperationException(
                            $"Pre-flight check failed: {preflight.Reason}. {preflight.HumanReadableMessage}");
                    }
                }
            }

            // In-flight guard
            foreach (var n in fetchPacks)
            {
                if (_inFlightFetches.Contains(n))
                    throw new InvalidOperationException($"Fetch already in progress for pack '{n}'");
            }
            foreach (var n in fetchPacks) _inFlightFetches.Add(n);

            // Register cancellation — fault TCS first, then propagate to Java side (Q15).
            var groupTcs = new TaskCompletionSource<AssetPackStates>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var ctReg = ct.Register(() =>
            {
                groupTcs.TrySetCanceled(ct);
                foreach (var n in fetchPacks)
                    _cancelReasons[n] = FetchCancelReason.User;
                _provider.CancelAsync(fetchPacks, CancellationToken.None);
            });

            SafeInvokeAnalytics(a => a.pad_fetch_started(
                new PadFetchStartedEvent(fetchPacks.Count, false, 1)));

            try
            {
                float resolvedTimeout = ResolveTimeout(options?.TimeoutSeconds ?? -1f);

                // Retry loop (ADR-026)
                int maxAttempts = options?.RetryMaxAttempts ?? 1;
                float baseDelay  = options?.RetryBaseDelaySeconds ?? 2f;
                float jitter     = options?.RetryJitterFraction ?? 0.25f;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    // Reset cellular consent in-flight flags for each attempt (I3 / B2)
                    foreach (var n in fetchPacks) _cellularConsentInFlight[n] = false;
                    // Re-arm stall detector for each retry attempt (I1)
                    if (attempt > 1)
                        foreach (var n in fetchPacks) _stallDetector?.ReArmForPack(n);

                    var attemptTcs = new TaskCompletionSource<AssetPackStates>(
                        TaskCreationOptions.RunContinuationsAsynchronously);

                    await _provider.FetchAsync(fetchPacks, attemptTcs, resolvedTimeout, ct);

                    // Track stall detector for each pack
                    foreach (var n in fetchPacks) _stallDetector?.ReArmForPack(n);

                    AssetPackStates ackStates;
                    try
                    {
                        ackStates = await attemptTcs.Task;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // User cancel — propagate
                        throw;
                    }

                    // Wait for all packs to reach terminal state via state stream
                    var inFlightSet = new HashSet<string>(fetchPacks);
                    AssetPackErrorCode lastError = AssetPackErrorCode.NoError;

                    using var streamCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    await foreach (var snapshot in _statesStream.ReadAsync(streamCts.Token))
                    {
                        bool anyFailed = false;
                        foreach (var n in inFlightSet.ToSnapshot())
                        {
                            if (!snapshot.TryGetValue(n, out var s)) continue;
                            if (s.Status == AssetPackStatus.Completed)
                            {
                                inFlightSet.Remove(n);
                                var loc  = await _provider.GetPackLocationAsync(n, CancellationToken.None);
                                var locV = loc ?? new AssetPackLocation(n, "", "", AssetPackStorageMethod.StorageFiles);
                                allHandles[n] = new AssetPackHandle(n, locV, s,
                                    ScanBundleFiles(locV.AssetsPath));
                                allStates[n] = s;
                            }
                            else if (s.IsTerminal)
                            {
                                lastError = s.ErrorCode;
                                anyFailed = true;
                                allStates[n] = s;
                            }
                            else if (s.RequiresConfirmation)
                            {
                                HandleConsentRequired(fetchPacks, s);
                            }
                        }

                        if (inFlightSet.Count == 0) { streamCts.Cancel(); break; }
                        if (anyFailed)
                        {
                            streamCts.Cancel();
                            // Consult retry policy
                            bool retryable = AssetDeliveryError.IsRetryable(lastError)
                                && attempt < maxAttempts;
                            if (retryable)
                            {
#if UNITY_EDITOR
                                // When SuppressAnalytics == true (injected error in Pack Inspector)
                                // the developer wants to see the error screen, not a retry spinner.
                                if (SuppressAnalytics)
                                {
                                    SuppressAnalytics = false;
                                    throw new AssetDeliveryException(
                                        $"Pack fetch failed with {lastError} (retry suppressed by SuppressAnalytics)",
                                        lastError);
                                }
#endif
                                SafeInvokeAnalytics(a => a.pad_fetch_failed(
                                    new PadFetchFailedEvent(lastError, attempt, true)));
                                float delay = ComputeBackoff(baseDelay, attempt, jitter);
                                OnFetchRetrying?.Invoke(new FetchRetryEvent(attempt, maxAttempts,
                                    delay, lastError, FetchCancelReason.User));
                                SafeInvokeAnalytics(a => a.pad_fetch_retrying(
                                    new PadRetryEvent(attempt, maxAttempts, delay, lastError, FetchCancelReason.User)));
                                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                                break; // break inner loop → next outer attempt
                            }
                            else
                            {
                                SafeInvokeAnalytics(a => a.pad_fetch_failed(
                                    new PadFetchFailedEvent(lastError, attempt, false)));
                                throw new AssetDeliveryException(
                                    $"Pack fetch failed with {lastError}", lastError);
                            }
                        }

                        if (inFlightSet.Count == 0) break;
                    }

                    if (inFlightSet.Count == 0) break; // success
                }

                SafeInvokeAnalytics(a => a.pad_fetch_succeeded(new PadFetchSucceededEvent(
                    fetchPacks.Count, 0, 0)));

                return new AssetPackGroupResult(allStates, allHandles, 1f, 0, 0f);
            }
            finally
            {
                ctReg.Dispose();
                foreach (var n in fetchPacks)
                {
                    _inFlightFetches.Remove(n);
                    _cancelReasons.Remove(n);
                    _stallDetector?.StopTracking(n);
                }
            }
        }

        public Task<AssetPackStates> GetPackStatesAsync(
            IReadOnlyList<string> packNames,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            EnsureMainThread();
            foreach (var n in packNames) AssetPackNameValidator.ThrowIfInvalid(n);
            return _provider.GetPackStatesAsync(packNames, ResolveTimeout(timeoutSeconds), ct);
        }

        public Task<AssetPackLocation?> GetPackLocationAsync(
            string packName, CancellationToken ct = default)
        {
            EnsureMainThread();
            AssetPackNameValidator.ThrowIfInvalid(packName);
            UpdateLastAccessed(packName);
            return _provider.GetPackLocationAsync(packName, ct);
        }

        public Task<IReadOnlyDictionary<string, AssetPackLocation>> GetPackLocationsAsync(
            CancellationToken ct = default)
        {
            EnsureMainThread();
            return _provider.GetPackLocationsAsync(ct);
        }

        public Task CancelAsync(
            IReadOnlyList<string> packNames,
            CancellationToken ct = default)
        {
            EnsureMainThread();
            foreach (var n in packNames) AssetPackNameValidator.ThrowIfInvalid(n);
            foreach (var n in packNames) _cancelReasons[n] = FetchCancelReason.User;
            return _provider.CancelAsync(packNames, ct);
        }

        /// <summary>
        /// Removes a downloaded pack. Throws if live bundles exist unless ForceUnload = true (B3).
        /// </summary>
        public Task RemovePackAsync(
            string packName,
            RemovePackOptions options = default,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            EnsureMainThread();
            AssetPackNameValidator.ThrowIfInvalid(packName);

            // B3: loaded bundles check
            if (_loadedBundles.TryGetValue(packName, out var bundles))
            {
                CompactBundleRefs(packName);
                if (_loadedBundles.TryGetValue(packName, out bundles) && bundles.Count > 0)
                {
                    if (!options.ForceUnload)
                        throw new InvalidOperationException(
                            $"Pack '{packName}' has {bundles.Count} AssetBundle(s) still loaded. " +
                            "Call AssetBundle.Unload(true) first or pass RemovePackOptions { ForceUnload = true }.");
                    foreach (var wr in bundles)
                        if (wr.TryGetTarget(out var b)) b.Unload(true);
                    _loadedBundles.Remove(packName);
                }
            }

            return RemovePackInternalAsync(packName, options, ct, timeoutSeconds);
        }

        private async Task RemovePackInternalAsync(
            string packName, RemovePackOptions options, CancellationToken ct, float timeoutSeconds)
        {
            await _provider.RemovePackAsync(packName, ResolveTimeout(timeoutSeconds), ct);
            _loadedBundles.Remove(packName);
            _inFlightFetches.Remove(packName);
            PlayerPrefs.DeleteKey($"BizSim.AssetDelivery.LastAccess.{packName}");
            SafeInvokeAnalytics(a => a.pad_pack_removed(new PadPackRemovedEvent(options.ForceUnload)));
        }

        public Task<ActivityResultCode> ShowConfirmationDialogAsync(
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            EnsureMainThread();
            return _provider.ShowConfirmationDialogAsync(ResolveTimeout(timeoutSeconds), ct);
        }

        public Task<FetchPreflightResult> CanFetchAsync(
            IEnumerable<string> packNames,
            CancellationToken ct = default)
        {
            EnsureMainThread();
            var list = new List<string>(packNames);
            foreach (var n in list) AssetPackNameValidator.ThrowIfInvalid(n);
            return CanFetchAsyncInternal(list, ct);
        }

        public async Task<UnityEngine.AssetBundle> LoadAssetBundleAsync(
            AssetPackHandle handle,
            string bundleFileName,
            CancellationToken ct = default)
        {
            EnsureMainThread();
            if (handle == null) throw new ArgumentNullException(nameof(handle));

            // B5: Duplicate bundle name guard
            string fullPath = System.IO.Path.Combine(handle.AssetsPath, bundleFileName);
            var existing    = UnityEngine.AssetBundle.GetAllLoadedAssetBundles();
            foreach (var b in existing)
                if (b.name == bundleFileName)
                    throw new InvalidOperationException(
                        $"AssetBundle '{bundleFileName}' is already loaded; Unity does not permit two bundles with the same internal name.");

            float startTime = Time.unscaledTime;
            var createRequest = UnityEngine.AssetBundle.LoadFromFileAsync(fullPath);
            var loadTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            createRequest.completed += _ => loadTcs.TrySetResult(true);
            await loadTcs.Task;

            var bundle = createRequest.assetBundle;
            if (bundle == null)
                throw new AssetDeliveryException(
                    $"AssetBundle.LoadFromFileAsync returned null for '{fullPath}'",
                    AssetPackErrorCode.InternalError);

            // B3: Track loaded bundle
            if (!_loadedBundles.TryGetValue(handle.PackName, out var list))
            {
                list = new List<WeakReference<UnityEngine.AssetBundle>>();
                _loadedBundles[handle.PackName] = list;
            }
            list.Add(new WeakReference<UnityEngine.AssetBundle>(bundle));
            CompactBundleRefs(handle.PackName);

            UpdateLastAccessed(handle.PackName);
            SafeInvokeAnalytics(a => a.pad_bundle_loaded(
                new PadBundleLoadedEvent(Time.unscaledTime - startTime)));

            return bundle;
        }

        /// <summary>
        /// QA / support escape hatch — clears all declared packs silently.
        /// Per ADR-018 reconcile spec.
        /// </summary>
        public async Task ClearAllPacksAsync()
        {
            EnsureMainThread();
            var all = _settings.AllDeclaredPackNames ?? Array.Empty<string>();
            foreach (var n in all)
            {
                _loadedBundles.Remove(n);
                _inFlightFetches.Remove(n);
                _cancelReasons[n] = FetchCancelReason.User;
                try { await _provider.RemovePackAsync(n, ResolveTimeout(-1f), CancellationToken.None); }
                catch (Exception ex) { BizSimLogger.Warning($"ClearAllPacksAsync: RemovePack('{n}') threw: {ex.Message}"); }
                PlayerPrefs.DeleteKey($"BizSim.AssetDelivery.LastAccess.{n}");
            }
        }

        public void SetAnalyticsAdapter(IAssetDeliveryAnalyticsAdapter adapter) => _analytics = adapter;

        // ─── Yieldable companions ──────────────────────────────────────────────

        public AssetPackYieldable FetchAssetPackYieldable(string packName, CancellationToken ct = default)
            => new AssetPackYieldable(FetchAsync(packName, ct));

        public AssetPackGroupYieldable FetchGroupYieldable(
            IEnumerable<string> packNames, FetchOptions options = null, CancellationToken ct = default)
            => new AssetPackGroupYieldable(FetchGroupAsync(packNames, options, ct));

        public AssetBundleYieldable LoadAssetBundleYieldable(
            AssetPackHandle handle, string bundleFileName, CancellationToken ct = default)
            => new AssetBundleYieldable(LoadAssetBundleAsync(handle, bundleFileName, ct));

        public AssetPackPreflightYieldable CanFetchYieldable(
            IEnumerable<string> packNames, CancellationToken ct = default)
            => new AssetPackPreflightYieldable(CanFetchAsync(packNames, ct));

        // ─── Async iteration ──────────────────────────────────────────────────

        public async IAsyncEnumerable<IReadOnlyDictionary<string, AssetPackState>> ReadStatesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var snapshot in _statesStream.ReadAsync(ct))
                yield return snapshot;
        }

        // ─── State event dispatching (internal, called by update handler) ─────

        public event Action<FetchRetryEvent> OnFetchRetrying;

        internal void HandleStateFromListener(string packName, AssetPackState state)
            => HandleState(packName, state);

        // ─── Internal helpers ──────────────────────────────────────────────────

        private void HandleState(string packName, AssetPackState s)
        {
            // Stall detector tracking
            if (s.Status == AssetPackStatus.Downloading)
                _stallDetector?.Track(packName, s.BytesDownloaded);

            lock (_stateLock) { _tracked[packName] = s; }
            _statesStream.Enqueue(SnapshotLocked());

            switch (s.Status)
            {
                case AssetPackStatus.Completed:
                case AssetPackStatus.Failed:
                case AssetPackStatus.Canceled:
                case AssetPackStatus.NotInstalled:
                    // Terminal — stop stall detector
                    _stallDetector?.StopTracking(packName);
                    break;
                case AssetPackStatus.WaitingForWifi:
                case AssetPackStatus.RequiresUserConfirmation:
                    HandleConsentRequired(new[] { packName }, s);
                    break;
                default:
                    BizSimLogger.Verbose($"HandleState: {packName} => {s.Status}");
                    break;
            }

#if UNITY_EDITOR
            // One-shot suppress: skip analytics for the next event after an injected error,
            // then clear the flag so subsequent real events fire normally.
            bool suppressed = SuppressAnalytics;
            if (suppressed) SuppressAnalytics = false;
            if (!suppressed)
#endif
            SafeInvokeAnalytics(a => a.pad_fetch_started(new PadFetchStartedEvent(1, false, 1))); // placeholder — real analytics in FetchGroupAsync
            try { OnPackStateChanged?.Invoke(packName, s); }
            catch (Exception ex) { BizSimLogger.Warning($"OnPackStateChanged handler threw: {ex.Message}"); }

            if (s.Status == AssetPackStatus.Failed)
            {
                var err = new AssetDeliveryError(s.ErrorCode, s.ErrorCode.ToString(), packName, DateTime.UtcNow);
                try { OnError?.Invoke(err); }
                catch (Exception ex) { BizSimLogger.Warning($"OnError handler threw: {ex.Message}"); }
            }
        }

        private void HandleConsentRequired(IEnumerable<string> packNames, AssetPackState s)
        {
            var packList = new List<string>(packNames);
            foreach (var n in packList)
            {
                if (_cellularConsentInFlight.TryGetValue(n, out var v) && v) return; // re-entry guard
                _cellularConsentInFlight[n]        = true;
                _confirmationDialogInFlight[n]      = true;
            }

            long totalBytes = 0;
            foreach (var n in packList)
            {
                if (_tracked.TryGetValue(n, out var st)) totalBytes += st.TotalBytesToDownload;
            }

            void Accept()
            {
                foreach (var n in packList) { _cellularConsentInFlight[n] = false; _confirmationDialogInFlight[n] = false; }
                _ = _provider.ShowConfirmationDialogAsync(ResolveTimeout(-1f), CancellationToken.None);
            }
            void Deny()
            {
                foreach (var n in packList) { _cellularConsentInFlight[n] = false; _confirmationDialogInFlight[n] = false; }
                _ = _provider.CancelAsync(packList, CancellationToken.None);
            }

            var request = new CellularConsentRequest(
                packList.ToArray(), totalBytes, true, Accept, Deny);

            if (OnCellularConsentRequired != null)
            {
                try { OnCellularConsentRequired(request); }
                catch (Exception ex) { BizSimLogger.Warning($"OnCellularConsentRequired handler threw: {ex.Message}"); }
            }
            else
            {
                // 100 ms fallback — auto-invoke native dialog
                _ = AutoConsentFallbackAsync(request);
            }

            SafeInvokeAnalytics(a => a.pad_cellular_consent_shown(
                new PadCellularConsentShownEvent(packList.Count, totalBytes, 1)));
        }

        private async Task AutoConsentFallbackAsync(CellularConsentRequest request)
        {
            await Task.Delay(100);
            if (OnCellularConsentRequired == null)
                request.AcceptConsent();
        }

        private async Task HandleStallAsync(string packName)
        {
            _cancelReasons[packName] = FetchCancelReason.StallDetector;
            var state = _tracked.TryGetValue(packName, out var s) ? s : default;
            SafeInvokeAnalytics(a => a.pad_stall_detected(
                new PadStallDetectedEvent(_settings.StallTimeoutSeconds, state.BytesDownloaded)));
            await _provider.CancelAsync(new[] { packName }, CancellationToken.None);
        }

        private bool StallDetectorPaused(string packName)
        {
            if (_tracked.TryGetValue(packName, out var s)
                && (s.Status == AssetPackStatus.WaitingForWifi
                 || s.Status == AssetPackStatus.RequiresUserConfirmation))
                return true;
            if (_cellularConsentInFlight.TryGetValue(packName, out var v) && v) return true;
            if (_confirmationDialogInFlight.TryGetValue(packName, out var v2) && v2) return true;
            return false;
        }

        private async Task<FetchPreflightResult> CanFetchAsyncInternal(
            IList<string> packNames, CancellationToken ct)
        {
            // Sum estimated download sizes
            long totalBytes = 0;
            var declaredSet = new HashSet<string>(_settings.AllDeclaredPackNames ?? Array.Empty<string>());
            foreach (var n in packNames)
            {
                if (!declaredSet.Contains(n) && declaredSet.Count > 0)
                    return new FetchPreflightResult(false, FetchPreflightReason.PackNotDeclared,
                        0, 0, $"Pack '{n}' is not declared in AssetDeliverySettings.AllDeclaredPackNames.");
                totalBytes += await _provider.GetPackDownloadSizeBytesAsync(n, ct);
            }

            // Storage check
            long available = 0;
            try
            {
                var drives = System.IO.DriveInfo.GetDrives();
                if (drives.Length > 0) available = drives[0].AvailableFreeSpace;
            }
            catch { available = long.MaxValue; } // Can't check — optimistic

            long safeguard = 200L * 1024 * 1024; // 200 MB margin
            if (totalBytes > 0 && available < totalBytes + safeguard)
                return new FetchPreflightResult(false, FetchPreflightReason.InsufficientStorage,
                    totalBytes, available,
                    $"Need ~{totalBytes / 1048576} MB but only {available / 1048576} MB free (200 MB safety margin applied).");

            // Network check
            var reachability = Application.internetReachability;
            if (reachability == NetworkReachability.NotReachable)
                return new FetchPreflightResult(false, FetchPreflightReason.NoNetwork,
                    totalBytes, available, "No network connection available.");

            return FetchPreflightResult.Ok(totalBytes, available);
        }

        private IReadOnlyDictionary<string, AssetPackState> SnapshotLocked()
        {
            lock (_stateLock)
                return new Dictionary<string, AssetPackState>(_tracked);
        }

        private float ResolveTimeout(float caller)
            => caller > 0f ? caller : _settings.DefaultTimeoutSeconds;

        private void EnsureMainThread([CallerMemberName] string caller = "")
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
                throw new InvalidOperationException(
                    $"AssetDeliveryController.{caller} must be called from the main thread");
        }

        private void SafeInvokeAnalytics(Action<IAssetDeliveryAnalyticsAdapter> action)
        {
            if (_analytics == null) return;
            try { action(_analytics); }
            catch (Exception ex) { BizSimLogger.Warning($"Analytics adapter threw: {ex.Message}"); }
        }

        private void CompactBundleRefs(string packName)
        {
            if (!_loadedBundles.TryGetValue(packName, out var list)) return;
            list.RemoveAll(wr => !wr.TryGetTarget(out _));
            if (list.Count == 0) _loadedBundles.Remove(packName);
        }

        private void UpdateLastAccessed(string packName)
        {
            PlayerPrefs.SetString($"BizSim.AssetDelivery.LastAccess.{packName}",
                DateTime.UtcNow.ToBinary().ToString());
        }

        private static IReadOnlyList<string> ScanBundleFiles(string assetsPath)
        {
            if (string.IsNullOrEmpty(assetsPath)) return Array.Empty<string>();
            try
            {
                if (!System.IO.Directory.Exists(assetsPath)) return Array.Empty<string>();
                return System.IO.Directory.GetFiles(assetsPath, "*.bundle",
                    System.IO.SearchOption.AllDirectories);
            }
            catch { return Array.Empty<string>(); }
        }

        private static float ComputeBackoff(float baseDelay, int attempt, float jitter)
        {
            float raw    = baseDelay * Mathf.Pow(2f, attempt - 1);
            float factor = 1f + UnityEngine.Random.Range(-jitter, jitter);
            return raw * Mathf.Max(0f, factor);
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void OnApplicationQuit()
        {
            _stateListener?.StopListening();
            _statesStream?.Dispose();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            _stateListener?.StopListening();
            _statesStream?.Dispose();
            AssetDeliveryTestInjector.ClearProvider();
        }
    }

    // ─── Helper extension ────────────────────────────────────────────────────────

    internal static class HashSetExtensions
    {
        /// <summary>Returns a snapshot list so we can mutate the set while iterating.</summary>
        internal static List<T> ToSnapshot<T>(this HashSet<T> set)
            => new List<T>(set);
    }
}
