// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// BizSimAddressablesRuntimeData — pack map singleton populated at startup.
// Conceptually mirrors Unity's internal PlayAssetDeliveryRuntimeData but is
// fully public so consumers can inspect routing state in custom tooling.

#if BIZSIM_ADDRESSABLES
using System.Collections.Generic;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Singleton holding the Addressables-to-AssetPack routing map, populated by
    /// <see cref="BizSimAddressablesInitialization"/> at startup from
    /// <c>StreamingAssets/BizSim/AddressablesPackMap.json</c>.
    /// </summary>
    public sealed class BizSimAddressablesRuntimeData
    {
        // ─── Singleton ────────────────────────────────────────────────────────────

        private static BizSimAddressablesRuntimeData _instance;

        /// <summary>Singleton accessor — never null; returns empty/uninitialized instance before startup.</summary>
        public static BizSimAddressablesRuntimeData Instance
            => _instance ??= new BizSimAddressablesRuntimeData();

        // ─── Public data ──────────────────────────────────────────────────────────

        /// <summary>
        /// Maps AssetBundle file-name (no extension) to the StablePackId that contains it.
        /// Populated by <see cref="BizSimAddressablesInitialization"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> AssetBundleNameToStablePackId
            => _bundleToPackId;

        /// <summary>
        /// Maps StablePackId to the on-device directory path after the pack has been fetched.
        /// Populated at runtime as packs are fetched successfully.
        /// </summary>
        public Dictionary<string, string> StablePackIdToLocalPath { get; } = new Dictionary<string, string>();

        /// <summary>True once the JSON map has been loaded successfully.</summary>
        public bool Initialized { get; private set; }

        // ─── Internal (populated by BizSimAddressablesInitialization) ─────────────

        private readonly Dictionary<string, string> _bundleToPackId = new Dictionary<string, string>();

        // ─── Package-internal API ─────────────────────────────────────────────────

        /// <summary>
        /// Registers bundle-to-pack mappings read from the JSON manifest.
        /// Called once by <see cref="BizSimAddressablesInitialization"/>.
        /// </summary>
        internal void Populate(Dictionary<string, string> bundleToPackId)
        {
            _bundleToPackId.Clear();
            if (bundleToPackId != null)
            {
                foreach (var kv in bundleToPackId)
                    _bundleToPackId[kv.Key] = kv.Value;
            }
            Initialized = true;
        }

        /// <summary>Resets all data (used in tests).</summary>
        internal static void Reset()
        {
            _instance = null;
        }
    }
}
#endif // BIZSIM_ADDRESSABLES
