// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// BizSimAddressablesInitialization — reads AddressablesPackMap.json from StreamingAssets
// and populates BizSimAddressablesRuntimeData before the first scene loads.

#if BIZSIM_ADDRESSABLES
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Reads <c>StreamingAssets/BizSim/AddressablesPackMap.json</c> at startup and
    /// populates <see cref="BizSimAddressablesRuntimeData.Instance"/>.
    /// If the file is missing (e.g. consumer has not run a BizSim Asset Delivery build yet),
    /// a Verbose warning is logged and the runtime data stays uninitialized — the bundle
    /// provider falls back gracefully to the base <c>AssetBundleProvider</c>.
    /// </summary>
    internal static class BizSimAddressablesInitialization
    {
        internal const string PackMapRelativePath = "BizSim/AddressablesPackMap.json";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var mapPath = Path.Combine(Application.streamingAssetsPath, PackMapRelativePath);
            if (!File.Exists(mapPath))
            {
                BizSimLogger.Verbose(
                    "[Addressables] AddressablesPackMap.json not found — Addressables integration " +
                    "is inactive. Run a BizSim Asset Delivery build to generate the map.");
                return;
            }

            try
            {
                var json = File.ReadAllText(mapPath);
                var payload = JsonUtility.FromJson<PackMapPayload>(json);
                var dict = new Dictionary<string, string>();
                if (payload?.entries != null)
                {
                    foreach (var e in payload.entries)
                        if (!string.IsNullOrEmpty(e.bundleName) && !string.IsNullOrEmpty(e.packId))
                            dict[e.bundleName] = e.packId;
                }
                BizSimAddressablesRuntimeData.Instance.Populate(dict);
                BizSimLogger.Info(
                    $"[Addressables] Pack map loaded — {dict.Count} bundle(s) mapped to asset packs.");
            }
            catch (Exception ex)
            {
                BizSimLogger.Error(
                    $"[Addressables] Failed to parse AddressablesPackMap.json: {ex.Message}");
            }
        }

        // ─── Serialization helpers ────────────────────────────────────────────────

        [Serializable]
        private sealed class PackMapPayload
        {
            public PackMapEntry[] entries;
        }

        [Serializable]
        private sealed class PackMapEntry
        {
            public string bundleName;
            public string packId;
        }
    }
}
#endif // BIZSIM_ADDRESSABLES
