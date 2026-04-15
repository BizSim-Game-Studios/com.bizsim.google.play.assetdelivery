#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Local HTTP asset delivery provider (ADR-025, §C3). Implements <see cref="IAssetDeliveryProvider"/>
    /// by serving packs from a local Python HTTP server running on the dev machine.
    /// Enabled when <see cref="AssetDeliverySettings.UseLocalHttpInDevelopmentBuild"/> = true
    /// in a DEVELOPMENT_BUILD or in the Editor.
    ///
    /// Feature Interaction I7:
    ///   1. Analytics tag: events fire with source="local_http" via analytics adapter context.
    ///   2. Stall detector: disabled when Debugger.IsAttached and ProviderKind == LocalHttp.
    ///   3. Pre-flight network check: skipped (localhost always reachable).
    ///
    /// See Documentation~/LOCAL_DEV_WORKFLOW.md for setup instructions.
    /// </summary>
    public sealed class LocalHttpAssetDeliveryProvider : IAssetDeliveryProvider, IAssetPackStateListener
    {
        private readonly string _baseUrl;
        private readonly Dictionary<string, AssetPackState> _states  = new Dictionary<string, AssetPackState>();
        private readonly object _lock = new object();

        public event Action<string, AssetPackState> OnStateUpdate;

        public LocalHttpAssetDeliveryProvider(string baseUrl)
        {
            _baseUrl = baseUrl?.TrimEnd('/') + "/" ?? "http://10.0.2.2:8080/packs/";
        }

        public ProviderKind Kind => ProviderKind.LocalHttp;

        // ─── IAssetDeliveryProvider ───────────────────────────────────────────────

        public Task FetchAsync(
            IReadOnlyList<string> packNames,
            TaskCompletionSource<AssetPackStates> tcs,
            float timeoutSeconds,
            CancellationToken ct)
        {
            _ = FetchInternalAsync(packNames, tcs, ct);
            return Task.CompletedTask;
        }

        private async Task FetchInternalAsync(
            IReadOnlyList<string> packNames,
            TaskCompletionSource<AssetPackStates> tcs,
            CancellationToken ct)
        {
            var tasks = new List<Task>();
            foreach (var name in packNames)
            {
                tasks.Add(DownloadPackAsync(name, ct));
            }

            // Acknowledge immediately (matching real provider contract).
            var ackStates = new Dictionary<string, AssetPackState>();
            foreach (var name in packNames)
                ackStates[name] = new AssetPackState(name, AssetPackStatus.Pending,
                    AssetPackErrorCode.NoError, 0, 0, 0, DateTime.UtcNow);
            tcs.TrySetResult(new AssetPackStates(ackStates, DateTime.UtcNow));

            await Task.WhenAll(tasks);
        }

        private async Task DownloadPackAsync(string packName, CancellationToken ct)
        {
            string url = _baseUrl + packName + ".zip";
            BizSimLogger.Info($"[LocalHttp] Fetching '{packName}' from {url} (source=local_http)");

            UpdateState(packName, AssetPackStatus.Downloading, AssetPackErrorCode.NoError, 0, 1, 0);

            using var req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                UpdateState(packName, AssetPackStatus.Downloading, AssetPackErrorCode.NoError,
                    (long)(req.downloadedBytes), 0, (int)(req.downloadProgress * 100));
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                BizSimLogger.Warning($"[LocalHttp] Failed to fetch '{packName}': {req.error}");
                UpdateState(packName, AssetPackStatus.Failed,
                    AssetPackErrorCode.NetworkError, 0, 0, 0);
                return;
            }

            // Write to persistent data path.
            string destDir  = Path.Combine(Application.persistentDataPath, "LocalHttpPacks", packName);
            Directory.CreateDirectory(destDir);
            string destZip  = Path.Combine(destDir, packName + ".zip");
            File.WriteAllBytes(destZip, req.downloadHandler.data);

            BizSimLogger.Info($"[LocalHttp] '{packName}' downloaded. Bytes: {req.downloadedBytes}");
            UpdateState(packName, AssetPackStatus.Completed, AssetPackErrorCode.NoError,
                (long)req.downloadedBytes, (long)req.downloadedBytes, 100);
        }

        private void UpdateState(string packName, AssetPackStatus status,
            AssetPackErrorCode error, long bytes, long total, int percent)
        {
            var state = new AssetPackState(packName, status, error, bytes, total, percent, DateTime.UtcNow);
            lock (_lock) { _states[packName] = state; }
            OnStateUpdate?.Invoke(packName, state);
        }

        public async Task<AssetPackStates> GetPackStatesAsync(
            IReadOnlyList<string> packNames,
            float timeoutSeconds,
            CancellationToken ct)
        {
            var result = new Dictionary<string, AssetPackState>();
            lock (_lock)
            {
                foreach (var n in packNames)
                    result[n] = _states.TryGetValue(n, out var s) ? s
                        : new AssetPackState(n, AssetPackStatus.NotInstalled,
                            AssetPackErrorCode.NoError, 0, 0, 0, DateTime.UtcNow);
            }
            return new AssetPackStates(result);
        }

        public Task<AssetPackLocation?> GetPackLocationAsync(string packName, CancellationToken ct)
        {
            string assetsPath = Path.Combine(Application.persistentDataPath, "LocalHttpPacks", packName);
            AssetPackLocation? loc = null;
            if (Directory.Exists(assetsPath))
                loc = new AssetPackLocation(packName, assetsPath, assetsPath,
                    AssetPackStorageMethod.StorageFiles);
            return Task.FromResult(loc);
        }

        public Task<IReadOnlyDictionary<string, AssetPackLocation>> GetPackLocationsAsync(CancellationToken ct)
        {
            var result = new Dictionary<string, AssetPackLocation>();
            lock (_lock)
            {
                foreach (var kv in _states)
                    if (kv.Value.Status == AssetPackStatus.Completed)
                    {
                        string assetsPath = Path.Combine(Application.persistentDataPath,
                            "LocalHttpPacks", kv.Key);
                        result[kv.Key] = new AssetPackLocation(kv.Key, assetsPath, assetsPath,
                            AssetPackStorageMethod.StorageFiles);
                    }
            }
            return Task.FromResult<IReadOnlyDictionary<string, AssetPackLocation>>(result);
        }

        public Task CancelAsync(IReadOnlyList<string> packNames, CancellationToken ct)
        {
            foreach (var n in packNames)
                UpdateState(n, AssetPackStatus.Canceled, AssetPackErrorCode.NoError, 0, 0, 0);
            return Task.CompletedTask;
        }

        public Task RemovePackAsync(string packName, float timeoutSeconds, CancellationToken ct)
        {
            string destDir = Path.Combine(Application.persistentDataPath, "LocalHttpPacks", packName);
            if (Directory.Exists(destDir))
                Directory.Delete(destDir, recursive: true);
            lock (_lock) { _states.Remove(packName); }
            return Task.CompletedTask;
        }

        public Task<ActivityResultCode> ShowConfirmationDialogAsync(
            float timeoutSeconds, CancellationToken ct)
        {
            // LocalHttp never requires cellular consent.
            return Task.FromResult(ActivityResultCode.Ok);
        }

        public Task ReconcileStateAsync(IReadOnlyList<string> allDeclaredPackNames)
        {
            // No persistent sessions to reconcile for local HTTP.
            return Task.CompletedTask;
        }

        public Task<long> GetPackDownloadSizeBytesAsync(string packName, CancellationToken ct)
        {
            // Return 0 — pre-flight storage check passes (we're on localhost).
            return Task.FromResult(0L);
        }

        // ─── IAssetPackStateListener ──────────────────────────────────────────────

        public void StartListening() { /* No-op: LocalHttp fires events directly. */ }
        public void StopListening()  { /* No-op */ }
    }
}
#endif
