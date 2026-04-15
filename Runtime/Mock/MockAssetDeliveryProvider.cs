using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Implements both IAssetDeliveryProvider and IAssetPackStateListener for Editor / non-Android builds.
    /// Uses a hidden MonoBehaviour (MockAssetPackStateReplayer) for Update-tick-driven replay.
    /// </summary>
    public sealed class MockAssetDeliveryProvider : IAssetDeliveryProvider, IAssetPackStateListener
    {
        public ProviderKind Kind => ProviderKind.Mock;

        private readonly AssetDeliveryMockConfig _config;
        private MockAssetPackStateReplayer       _replayer;
        private bool                             _listening;

        // Per-pack TCS so ResolveTcs can target a specific pack's initial ack.
        private readonly Dictionary<string, TaskCompletionSource<AssetPackStates>> _pendingTcs =
            new Dictionary<string, TaskCompletionSource<AssetPackStates>>();

        // Test-only observable field — CancellationPropagationTests reads this.
        public int CancelCallCount;

        // State listener event (IAssetPackStateListener).
        public event System.Action<string, AssetPackState> OnStateUpdate;

        // Simulated pack states.
        private readonly Dictionary<string, AssetPackState> _states =
            new Dictionary<string, AssetPackState>();

        // Confirmation dialog pending flag per pack.
        private readonly HashSet<string> _awaitingConfirmation = new HashSet<string>();

        public MockAssetDeliveryProvider(AssetDeliveryMockConfig config)
        {
            _config = config;
        }

        // ─── IAssetPackStateListener ────────────────────────────────────────────

        public void StartListening()
        {
            if (_listening) return;
            _listening = true;
            EnsureReplayer();
        }

        public void StopListening()
        {
            _listening = false;
            if (_replayer != null)
            {
                Object.Destroy(_replayer.gameObject);
                _replayer = null;
            }
        }

        // ─── IAssetDeliveryProvider ─────────────────────────────────────────────

        public Task FetchAsync(
            IReadOnlyList<string> packNames,
            TaskCompletionSource<AssetPackStates> tcs,
            float timeoutSeconds,
            CancellationToken ct)
        {
            EnsureReplayer();
            foreach (var name in packNames)
            {
                _pendingTcs[name] = tcs;
                var cfg = FindConfig(name);
                if (cfg != null)
                    _replayer.BeginPack(cfg);
                else
                {
                    // Unknown pack — emit immediate NotInstalled
                    var state = new AssetPackState(name, AssetPackStatus.NotInstalled,
                        AssetPackErrorCode.PackUnavailable, 0, 0, 0, System.DateTime.UtcNow);
                    DispatchStateUpdate(name, state);
                }
            }

            // Register cancellation — faults TCS immediately, then increments counter.
            ct.Register(() =>
            {
                tcs.TrySetCanceled(ct);
                Interlocked.Increment(ref CancelCallCount);
            });

            return Task.CompletedTask;
        }

        public Task<AssetPackStates> GetPackStatesAsync(
            IReadOnlyList<string> packNames,
            float timeoutSeconds,
            CancellationToken ct)
        {
            var dict = new Dictionary<string, AssetPackState>();
            foreach (var name in packNames)
            {
                if (_states.TryGetValue(name, out var s))
                    dict[name] = s;
                else
                    dict[name] = new AssetPackState(name, AssetPackStatus.Unknown,
                        AssetPackErrorCode.NoError, 0, 0, 0, System.DateTime.UtcNow);
            }
            var snapshot = new AssetPackStates(dict, System.DateTime.UtcNow);
            return Task.FromResult(snapshot);
        }

        public Task<AssetPackLocation?> GetPackLocationAsync(string packName, CancellationToken ct)
        {
            if (_states.TryGetValue(packName, out var s) && s.Status == AssetPackStatus.Completed)
            {
                var fakePath   = Application.temporaryCachePath + "/mock_assetpacks/" + packName;
                var loc        = new AssetPackLocation(packName, fakePath, fakePath + "/assets",
                    AssetPackStorageMethod.StorageFiles);
                return Task.FromResult<AssetPackLocation?>(loc);
            }
            // Install-time short-circuit
            var cfg = FindConfig(packName);
            if (cfg != null && cfg.IsInstallTime)
            {
                var fakePath = Application.temporaryCachePath + "/mock_assetpacks/" + packName;
                var loc      = new AssetPackLocation(packName, fakePath, fakePath + "/assets",
                    AssetPackStorageMethod.ApkAssets);
                return Task.FromResult<AssetPackLocation?>(loc);
            }
            return Task.FromResult<AssetPackLocation?>(null);
        }

        public Task<IReadOnlyDictionary<string, AssetPackLocation>> GetPackLocationsAsync(CancellationToken ct)
        {
            var result = new Dictionary<string, AssetPackLocation>();
            foreach (var kv in _states)
            {
                if (kv.Value.Status == AssetPackStatus.Completed)
                {
                    var p = Application.temporaryCachePath + "/mock_assetpacks/" + kv.Key;
                    result[kv.Key] = new AssetPackLocation(kv.Key, p, p + "/assets",
                        AssetPackStorageMethod.StorageFiles);
                }
            }
            return Task.FromResult<IReadOnlyDictionary<string, AssetPackLocation>>(result);
        }

        public Task CancelAsync(IReadOnlyList<string> packNames, CancellationToken ct)
        {
            Interlocked.Increment(ref CancelCallCount);
            foreach (var name in packNames)
            {
                var state = new AssetPackState(name, AssetPackStatus.Canceled,
                    AssetPackErrorCode.CancelledByCaller, 0, 0, 0, System.DateTime.UtcNow);
                DispatchStateUpdate(name, state);
            }
            return Task.CompletedTask;
        }

        public Task RemovePackAsync(string packName, float timeoutSeconds, CancellationToken ct)
        {
            _states.Remove(packName);
            var state = new AssetPackState(packName, AssetPackStatus.NotInstalled,
                AssetPackErrorCode.NoError, 0, 0, 0, System.DateTime.UtcNow);
            DispatchStateUpdate(packName, state);
            return Task.CompletedTask;
        }

        public async Task<ActivityResultCode> ShowConfirmationDialogAsync(float timeoutSeconds, CancellationToken ct)
        {
            float latency = _config != null ? _config.ConfirmationDialogLatencyMs : 200f;
            await Task.Delay((int)latency, ct);
            // Resume all packs awaiting confirmation.
            foreach (var name in _awaitingConfirmation)
                _replayer?.ConfirmPack(name);
            _awaitingConfirmation.Clear();
            return ActivityResultCode.Ok;
        }

        public Task ReconcileStateAsync(IReadOnlyList<string> allDeclaredPackNames)
        {
            // Mock: no-op (no stale sessions to clean up).
            return Task.CompletedTask;
        }

        public Task<long> GetPackDownloadSizeBytesAsync(string packName, CancellationToken ct)
        {
            var cfg = FindConfig(packName);
            return Task.FromResult(cfg?.TotalBytesToDownload ?? 0L);
        }

        // ─── Internal helpers ────────────────────────────────────────────────────

        internal void DispatchStateUpdate(string packName, AssetPackState state)
        {
            _states[packName] = state;
            OnStateUpdate?.Invoke(packName, state);

            if (state.RequiresConfirmation)
                _awaitingConfirmation.Add(packName);
        }

        /// <summary>Called by MockAssetPackStateReplayer when the first non-PENDING state fires.</summary>
        internal void ResolveTcs(string packName, AssetPackStatus firstNonPending)
        {
            if (_pendingTcs.TryGetValue(packName, out var tcs))
            {
                // Build a snapshot of current states for the TCS resolution.
                var snap = new Dictionary<string, AssetPackState>(_states);
                tcs.TrySetResult(new AssetPackStates(snap, System.DateTime.UtcNow));
                _pendingTcs.Remove(packName);
            }
        }

        private AssetDeliveryMockConfig.SimulatedPack FindConfig(string name)
        {
            if (_config == null) return null;
            foreach (var p in _config.Packs)
                if (p.Name == name) return p;
            return null;
        }

        private void EnsureReplayer()
        {
            if (_replayer != null) return;
            var go = new GameObject("[MockAssetPackStateReplayer]")
                { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            _replayer       = go.AddComponent<MockAssetPackStateReplayer>();
            _replayer.Owner = this;
        }
    }
}
