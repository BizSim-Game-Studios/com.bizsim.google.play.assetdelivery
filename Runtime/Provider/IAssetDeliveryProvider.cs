using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Abstraction over the Google Play Asset Delivery Java API.
    /// Implemented by <c>AndroidAssetDeliveryProvider</c> (Phase 4) for production
    /// and by <c>MockAssetDeliveryProvider</c> for Editor / non-Android builds.
    /// </summary>
    public interface IAssetDeliveryProvider
    {
        /// <summary>Kind of provider — used by B6 to gate analytics injection in the Pack Inspector.</summary>
        ProviderKind Kind { get; }

        /// <summary>
        /// Initiates a fetch for the listed pack names. Resolves the TCS on the initial
        /// acknowledgment snapshot (i.e. when the first non-PENDING state fires), NOT when
        /// all packs reach COMPLETED. Subsequent progress arrives via IAssetPackStateListener.OnStateUpdate.
        /// </summary>
        Task FetchAsync(
            IReadOnlyList<string> packNames,
            System.Threading.Tasks.TaskCompletionSource<AssetPackStates> tcs,
            float timeoutSeconds,
            CancellationToken ct);

        Task<AssetPackStates> GetPackStatesAsync(
            IReadOnlyList<string> packNames,
            float timeoutSeconds,
            CancellationToken ct);

        Task<AssetPackLocation?> GetPackLocationAsync(string packName, CancellationToken ct);

        Task<IReadOnlyDictionary<string, AssetPackLocation>> GetPackLocationsAsync(CancellationToken ct);

        /// <summary>
        /// Propagates cancellation to the Java-side AssetPackManager.cancel(List&lt;String&gt;).
        /// Per Q15, this is distinct from CancellationToken cancellation — both paths must fire.
        /// </summary>
        Task CancelAsync(IReadOnlyList<string> packNames, CancellationToken ct);

        Task RemovePackAsync(string packName, float timeoutSeconds, CancellationToken ct);

        Task<ActivityResultCode> ShowConfirmationDialogAsync(float timeoutSeconds, CancellationToken ct);

        /// <summary>
        /// Called by controller Awake to remove stale sessions left by a previous process death.
        /// Implementors query getPackStates for AllDeclaredPackNames and remove FAILED / orphaned sessions.
        /// </summary>
        Task ReconcileStateAsync(IReadOnlyList<string> allDeclaredPackNames);

        /// <summary>
        /// Returns download size information for a pack without fetching it. Used by CanFetchAsync.
        /// </summary>
        Task<long> GetPackDownloadSizeBytesAsync(string packName, CancellationToken ct);
    }

    /// <summary>Discriminates provider implementations; used by B6 inspector injection guard.</summary>
    public enum ProviderKind
    {
        /// <summary>Production provider — Google Play Core AndroidAssetDeliveryProvider.</summary>
        Real,
        /// <summary>Mock provider — MockAssetDeliveryProvider (Editor / non-Android builds).</summary>
        Mock,
        /// <summary>Local HTTP provider — LocalHttpAssetDeliveryProvider (dev builds / emulator iteration).</summary>
        LocalHttp,
    }
}
