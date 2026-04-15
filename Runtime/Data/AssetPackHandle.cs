using System.Collections.Generic;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Result of a successful pack download. Carries the pack name, its on-device path,
    /// storage method, per-pack progress snapshot, and the list of .bundle files discovered
    /// inside the pack (ADR-021 multi-bundle-per-pack first-class modeling).
    /// </summary>
    public sealed class AssetPackHandle
    {
        public string PackName { get; }
        public AssetPackLocation Location { get; }
        public AssetPackStorageMethod StorageMethod { get; }
        public AssetPackState LastKnownState { get; }

        /// <summary>
        /// List of *.bundle files found inside Location.AssetsPath via Directory.GetFiles.
        /// Populated by the provider at handle creation time (after COMPLETED fires).
        /// Mock populates from AssetDeliveryMockConfig.SimulatedPack.SimulatedBundleFileNames.
        /// </summary>
        public IReadOnlyList<string> AssetBundleFileNames { get; }

        /// <summary>Convenience accessor for Location.AssetsPath.</summary>
        public string AssetsPath => Location.AssetsPath;

        public AssetPackHandle(
            string packName,
            AssetPackLocation location,
            AssetPackState lastKnownState,
            IReadOnlyList<string> assetBundleFileNames)
        {
            PackName              = packName;
            Location              = location;
            StorageMethod         = location.StorageMethod;
            LastKnownState        = lastKnownState;
            AssetBundleFileNames  = assetBundleFileNames ?? System.Array.Empty<string>();
        }
    }
}
