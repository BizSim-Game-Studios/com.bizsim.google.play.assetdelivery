using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Provider for players with no Play Asset Delivery (iOS, standalone, WebGL). No pack is ever present;
    /// fetches fail with <see cref="AssetPackErrorCode.ApiNotAvailable"/> and nothing is simulated.
    /// </summary>
    internal sealed class UnsupportedAssetDeliveryProvider : IAssetDeliveryProvider, IAssetPackStateListener
    {
        public ProviderKind Kind => ProviderKind.Unsupported;

        public event Action<string, AssetPackState> OnStateUpdate;

        public void StartListening() { }
        public void StopListening() { }

        public Task FetchAsync(IReadOnlyList<string> packNames, TaskCompletionSource<AssetPackStates> tcs,
            float timeoutSeconds, CancellationToken ct)
        {
            var dict = new Dictionary<string, AssetPackState>();
            foreach (string name in packNames)
            {
                var state = Failed(name);
                dict[name] = state;
                OnStateUpdate?.Invoke(name, state);
            }
            tcs.TrySetResult(new AssetPackStates(dict, DateTime.UtcNow));
            return Task.CompletedTask;
        }

        public Task<AssetPackStates> GetPackStatesAsync(IReadOnlyList<string> packNames, float timeoutSeconds, CancellationToken ct)
        {
            var dict = new Dictionary<string, AssetPackState>();
            foreach (string name in packNames)
                dict[name] = new AssetPackState(name, AssetPackStatus.NotInstalled, AssetPackErrorCode.NoError, 0, 0, 0, DateTime.UtcNow);
            return Task.FromResult(new AssetPackStates(dict, DateTime.UtcNow));
        }

        public Task<AssetPackLocation?> GetPackLocationAsync(string packName, CancellationToken ct)
            => Task.FromResult<AssetPackLocation?>(null);

        public Task<IReadOnlyDictionary<string, AssetPackLocation>> GetPackLocationsAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<string, AssetPackLocation>>(new Dictionary<string, AssetPackLocation>());

        public Task CancelAsync(IReadOnlyList<string> packNames, CancellationToken ct) => Task.CompletedTask;

        public Task RemovePackAsync(string packName, float timeoutSeconds, CancellationToken ct) => Task.CompletedTask;

        public Task<ActivityResultCode> ShowConfirmationDialogAsync(float timeoutSeconds, CancellationToken ct)
            => Task.FromResult(ActivityResultCode.Canceled);

        public Task ReconcileStateAsync(IReadOnlyList<string> allDeclaredPackNames) => Task.CompletedTask;

        public Task<long> GetPackDownloadSizeBytesAsync(string packName, CancellationToken ct) => Task.FromResult(0L);

        static AssetPackState Failed(string name)
            => new AssetPackState(name, AssetPackStatus.Failed, AssetPackErrorCode.ApiNotAvailable, 0, 0, 0, DateTime.UtcNow);
    }
}
