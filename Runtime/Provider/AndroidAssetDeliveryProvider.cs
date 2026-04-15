#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Android JNI bridge implementation — Phase 4.
    /// This stub satisfies the compile-time IAssetDeliveryProvider + IAssetPackStateListener
    /// contract while JNI wiring is added in Phase 4 Tasks 8-11.
    /// </summary>
    internal sealed class AndroidAssetDeliveryProvider : IAssetDeliveryProvider, IAssetPackStateListener
    {
        public ProviderKind Kind => ProviderKind.Real;

        public event System.Action<string, AssetPackState> OnStateUpdate;

        public void StartListening()  { /* Phase 4 */ }
        public void StopListening()   { /* Phase 4 */ }

        public Task FetchAsync(IReadOnlyList<string> packNames,
            System.Threading.Tasks.TaskCompletionSource<AssetPackStates> tcs,
            float timeoutSeconds, CancellationToken ct)
        {
            tcs.TrySetException(new System.NotImplementedException("AndroidAssetDeliveryProvider — Phase 4"));
            return Task.CompletedTask;
        }

        public Task<AssetPackStates> GetPackStatesAsync(
            IReadOnlyList<string> packNames, float timeoutSeconds, CancellationToken ct)
            => throw new System.NotImplementedException("AndroidAssetDeliveryProvider — Phase 4");

        public Task<AssetPackLocation?> GetPackLocationAsync(string packName, CancellationToken ct)
            => throw new System.NotImplementedException("AndroidAssetDeliveryProvider — Phase 4");

        public Task<IReadOnlyDictionary<string, AssetPackLocation>> GetPackLocationsAsync(CancellationToken ct)
            => throw new System.NotImplementedException("AndroidAssetDeliveryProvider — Phase 4");

        public Task CancelAsync(IReadOnlyList<string> packNames, CancellationToken ct)
            => Task.CompletedTask;

        public Task RemovePackAsync(string packName, float timeoutSeconds, CancellationToken ct)
            => throw new System.NotImplementedException("AndroidAssetDeliveryProvider — Phase 4");

        public Task<ActivityResultCode> ShowConfirmationDialogAsync(float timeoutSeconds, CancellationToken ct)
            => throw new System.NotImplementedException("AndroidAssetDeliveryProvider — Phase 4");

        public Task ReconcileStateAsync(IReadOnlyList<string> allDeclaredPackNames)
            => Task.CompletedTask;

        public Task<long> GetPackDownloadSizeBytesAsync(string packName, CancellationToken ct)
            => Task.FromResult(0L);
    }
}
#endif
