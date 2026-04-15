using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// ScriptableObject that drives the mock provider for Editor / non-Android builds.
    /// Assign via AssetDeliveryController._mockConfig serialized field.
    /// </summary>
    [CreateAssetMenu(menuName = "BizSim/Google Play/Asset Delivery Mock Config", fileName = "AssetDeliveryMockConfig")]
    public sealed class AssetDeliveryMockConfig : ScriptableObject
    {
        [System.Serializable]
        public sealed class SimulatedPack
        {
            public string Name = "pack_name";

            [Tooltip("Final status the simulated download reaches.")]
            public AssetPackStatus FinalStatus = AssetPackStatus.Completed;

            [Tooltip("How long (real seconds) the simulated download takes.")]
            public float DownloadDurationSeconds = 2f;

            [Tooltip("Simulated total bytes.")]
            public long TotalBytesToDownload = 10_000_000L;

            [Tooltip("Error code to report when FinalStatus == Failed.")]
            public AssetPackErrorCode Error = AssetPackErrorCode.NoError;

            [Tooltip("If set, the pack halts at this status.")]
            public AssetPackStatus StopAt = AssetPackStatus.Unknown;

            [Tooltip("Simulated .bundle file names inside AssetsPath (for ADR-021 multi-bundle support).")]
            public string[] SimulatedBundleFileNames = System.Array.Empty<string>();

            [Tooltip("If true, this pack is treated as install-time.")]
            public bool IsInstallTime;
        }

        [Tooltip("Packs to simulate.")]
        public SimulatedPack[] Packs = System.Array.Empty<SimulatedPack>();

        [Tooltip("Additional latency added to GetPackStatesAsync (ms).")]
        public float GetStatesLatencyMs = 50f;

        [Tooltip("Latency added to ShowConfirmationDialogAsync result (ms).")]
        public float ConfirmationDialogLatencyMs = 200f;

        [Tooltip("When true, inject a B6 error via the Pack Inspector error-injection path.")]
        [HideInInspector] public bool SuppressAnalytics;
    }
}
