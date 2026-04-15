#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Android JNI implementation of <see cref="IAssetDeliveryProvider"/>. Compiled only under
    /// <c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>. Every public method is stateless per-call:
    /// a fresh TCS + proxy pair is created for each invocation, then the corresponding Java bridge
    /// method is called with the proxy as the callback argument.
    /// </summary>
    /// <remarks>
    /// The static <see cref="GetBridge"/> helper initialises the Java-side
    /// <c>AssetDeliveryBridge</c> singleton exactly once per process lifetime via a
    /// double-checked lock. The bridge is intentionally never disposed — it is scoped to the
    /// process, not to any Unity scene or MonoBehaviour. The Activity reference is fetched freshly
    /// from <c>UnityPlayer.currentActivity</c> on each call that requires it (e.g.
    /// <c>showConfirmationDialog</c>) so no stale Activity pointer is ever cached.
    /// </remarks>
    internal sealed class AndroidAssetDeliveryProvider : IAssetDeliveryProvider, IAssetPackStateListener
    {
        // ── Static bridge (process-scoped singleton) ─────────────────────────────

        private static AndroidJavaObject _bridge;
        private static readonly object _bridgeLock = new object();

        /// <summary>
        /// Returns the shared <c>AssetDeliveryBridge</c> Java object, initializing it if needed.
        /// Returns <c>null</c> if initialization fails (error is logged).
        /// </summary>
        internal static AndroidJavaObject GetBridge()
        {
            if (_bridge != null) return _bridge;
            lock (_bridgeLock)
            {
                if (_bridge != null) return _bridge;
                try
                {
                    using (var bridgeClass = new AndroidJavaClass("com.bizsim.google.play.assetdelivery.AssetDeliveryBridge"))
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        if (currentActivity == null)
                        {
                            BizSimLogger.Error("UnityPlayer.currentActivity is null — cannot create AssetDeliveryBridge");
                            return null;
                        }
                        _bridge = bridgeClass.CallStatic<AndroidJavaObject>("init", currentActivity);
                    }
                }
                catch (AndroidJavaException aje)
                {
                    BizSimLogger.Error($"AssetDeliveryBridge.init failed: {aje.Message}");
                    _bridge = null;
                }
                catch (Exception ex)
                {
                    BizSimLogger.Error($"AssetDeliveryBridge.init failed (unexpected): {ex.Message}");
                    _bridge = null;
                }
                return _bridge;
            }
        }

        // ── IAssetDeliveryProvider ────────────────────────────────────────────────

        public ProviderKind Kind => ProviderKind.Real;

        public event Action<string, AssetPackState> OnStateUpdate;

        public void StartListening()  { /* delegated to AssetPackStateListenerController */ }
        public void StopListening()   { /* delegated to AssetPackStateListenerController */ }

        public Task FetchAsync(
            IReadOnlyList<string> packNames,
            TaskCompletionSource<AssetPackStates> tcs,
            float timeoutSeconds,
            CancellationToken ct)
        {
            var bridge = GetBridge();
            if (bridge == null)
            {
                tcs.TrySetException(BridgeNotInitializedException("FetchAsync"));
                return Task.CompletedTask;
            }

            var proxy = new FetchCallbackProxy(tcs);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    var names = ToArray(packNames);
                    bridge.Call("fetch", names, proxy);

                    if (timeoutSeconds > 0f)
                    {
                        _ = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct)
                            .ContinueWith(t =>
                            {
                                if (!t.IsCanceled)
                                    tcs.TrySetException(new AssetDeliveryException(
                                        new AssetDeliveryError(
                                            code:          AssetPackErrorCode.Timeout,
                                            message:       $"FetchAsync timed out after {timeoutSeconds:F1}s",
                                            packName:      string.Empty,
                                            occurredAtUtc: DateTime.UtcNow)));
                            }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                }
                catch (AndroidJavaException aje)
                {
                    tcs.TrySetException(new AssetDeliveryException(
                        new AssetDeliveryError(AssetPackErrorCode.InternalError,
                            $"fetch JNI call failed: {aje.Message}", string.Empty, DateTime.UtcNow)));
                }
            }

            return Task.CompletedTask;
        }

        public async Task<AssetPackStates> GetPackStatesAsync(
            IReadOnlyList<string> packNames,
            float timeoutSeconds,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var bridge = GetBridge();
            if (bridge == null)
                throw BridgeNotInitializedException("GetPackStatesAsync");

            var tcs = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);
            var proxy = new GetStatesCallbackProxy(tcs);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    bridge.Call("getPackStates", ToArray(packNames), proxy);
                }
                catch (AndroidJavaException aje)
                {
                    tcs.TrySetException(new AssetDeliveryException(
                        new AssetDeliveryError(AssetPackErrorCode.InternalError,
                            $"getPackStates JNI call failed: {aje.Message}", string.Empty, DateTime.UtcNow)));
                }

                if (timeoutSeconds > 0f)
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct);
                    var completed   = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);
                    if (completed == timeoutTask)
                    {
                        ct.ThrowIfCancellationRequested();
                        tcs.TrySetException(new AssetDeliveryException(
                            new AssetDeliveryError(AssetPackErrorCode.Timeout,
                                $"GetPackStatesAsync timed out after {timeoutSeconds:F1}s",
                                string.Empty, DateTime.UtcNow)));
                    }
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task<AssetPackLocation?> GetPackLocationAsync(string packName, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var bridge = GetBridge();
            if (bridge == null)
                throw BridgeNotInitializedException("GetPackLocationAsync");

            var tcs = new TaskCompletionSource<AssetPackLocation?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var proxy = new PackLocationCallbackProxy(tcs);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    // getPackLocation is synchronous on the Java side — the proxy is called before
                    // this bridge.Call returns.
                    bridge.Call("getPackLocation", packName, proxy);
                }
                catch (AndroidJavaException aje)
                {
                    tcs.TrySetResult(null);
                    BizSimLogger.Warning($"getPackLocation JNI call failed for '{packName}': {aje.Message}");
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task<IReadOnlyDictionary<string, AssetPackLocation>> GetPackLocationsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var bridge = GetBridge();
            if (bridge == null)
                throw BridgeNotInitializedException("GetPackLocationsAsync");

            // getPackLocations is not a direct Java API. We iterate using the bridge's
            // getPackLocation for each installed pack discovered via getPackStates with a broad query.
            // For production use, callers should pass specific pack names to GetPackLocationAsync.
            // This overload returns an empty dict — consumers should prefer GetPackLocationAsync.
            BizSimLogger.Warning("GetPackLocationsAsync: returns empty on Android; use GetPackLocationAsync per pack");
            return new Dictionary<string, AssetPackLocation>();
        }

        public Task CancelAsync(IReadOnlyList<string> packNames, CancellationToken ct)
        {
            var bridge = GetBridge();
            if (bridge == null)
            {
                BizSimLogger.Warning("CancelAsync: bridge not initialized — cancel is a no-op");
                return Task.CompletedTask;
            }

            try
            {
                bridge.Call("cancel", ToArray(packNames));
            }
            catch (AndroidJavaException aje)
            {
                BizSimLogger.Warning($"cancel JNI call failed: {aje.Message}");
            }

            return Task.CompletedTask;
        }

        public async Task RemovePackAsync(string packName, float timeoutSeconds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var bridge = GetBridge();
            if (bridge == null)
                throw BridgeNotInitializedException("RemovePackAsync");

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var proxy = new RemovePackCallbackProxy(tcs);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    bridge.Call("removePack", packName, proxy);
                }
                catch (AndroidJavaException aje)
                {
                    tcs.TrySetException(new AssetDeliveryException(
                        new AssetDeliveryError(AssetPackErrorCode.InternalError,
                            $"removePack JNI call failed: {aje.Message}", packName, DateTime.UtcNow)));
                }

                if (timeoutSeconds > 0f)
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct);
                    var completed   = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);
                    if (completed == timeoutTask)
                    {
                        ct.ThrowIfCancellationRequested();
                        tcs.TrySetException(new AssetDeliveryException(
                            new AssetDeliveryError(AssetPackErrorCode.Timeout,
                                $"RemovePackAsync timed out after {timeoutSeconds:F1}s",
                                packName, DateTime.UtcNow)));
                    }
                }

                await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task<ActivityResultCode> ShowConfirmationDialogAsync(float timeoutSeconds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var bridge = GetBridge();
            if (bridge == null)
                throw BridgeNotInitializedException("ShowConfirmationDialogAsync");

            var tcs = new TaskCompletionSource<ActivityResultCode>(TaskCreationOptions.RunContinuationsAsynchronously);
            var proxy = new ConfirmationDialogCallbackProxy(tcs);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    // Fetch the current Activity freshly — never cache it.
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        bridge.Call("showConfirmationDialog", currentActivity, proxy);
                    }
                }
                catch (AndroidJavaException aje)
                {
                    tcs.TrySetException(new AssetDeliveryException(
                        new AssetDeliveryError(AssetPackErrorCode.InternalError,
                            $"showConfirmationDialog JNI call failed: {aje.Message}",
                            string.Empty, DateTime.UtcNow)));
                }

                if (timeoutSeconds > 0f)
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct);
                    var completed   = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);
                    if (completed == timeoutTask)
                    {
                        ct.ThrowIfCancellationRequested();
                        tcs.TrySetException(new AssetDeliveryException(
                            new AssetDeliveryError(AssetPackErrorCode.Timeout,
                                $"ShowConfirmationDialogAsync timed out after {timeoutSeconds:F1}s",
                                string.Empty, DateTime.UtcNow)));
                    }
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task ReconcileStateAsync(IReadOnlyList<string> allDeclaredPackNames)
        {
            if (allDeclaredPackNames == null || allDeclaredPackNames.Count == 0)
                return;

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10f)))
                {
                    // Query states; packs that are FAILED or in an unknown transient state are
                    // logged. No automatic removal — policy is advisory-only at reconcile time.
                    var states = await GetPackStatesAsync(allDeclaredPackNames, 10f, cts.Token)
                        .ConfigureAwait(false);
                    foreach (var kv in states.Packs)
                    {
                        if (kv.Value.Status == AssetPackStatus.Failed)
                            BizSimLogger.Warning($"ReconcileStateAsync: pack '{kv.Key}' is in FAILED state (errorCode={kv.Value.ErrorCode})");
                    }
                }
            }
            catch (Exception ex)
            {
                BizSimLogger.Warning($"ReconcileStateAsync failed: {ex.Message}");
            }
        }

        public async Task<long> GetPackDownloadSizeBytesAsync(string packName, CancellationToken ct)
        {
            try
            {
                var states = await GetPackStatesAsync(
                    new[] { packName }, 10f, ct).ConfigureAwait(false);
                if (states.TryGet(packName, out var state))
                    return state.TotalBytesToDownload;
            }
            catch (Exception ex)
            {
                BizSimLogger.Warning($"GetPackDownloadSizeBytesAsync for '{packName}' failed: {ex.Message}");
            }
            return 0L;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string[] ToArray(IReadOnlyList<string> list)
        {
            var arr = new string[list.Count];
            for (int i = 0; i < list.Count; i++) arr[i] = list[i];
            return arr;
        }

        private static AssetDeliveryException BridgeNotInitializedException(string callerName)
        {
            return new AssetDeliveryException(
                new AssetDeliveryError(
                    code:          AssetPackErrorCode.BridgeNotInitialized,
                    message:       $"{callerName}: AssetDeliveryBridge failed to initialize (see previous log for details)",
                    packName:      string.Empty,
                    occurredAtUtc: DateTime.UtcNow));
        }
    }
}
#endif
