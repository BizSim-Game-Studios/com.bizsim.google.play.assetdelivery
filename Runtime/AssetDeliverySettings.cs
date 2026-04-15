using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    public sealed class AssetDeliverySettings : ScriptableObject
    {
        // Path constants per CROSS-INVARIANTS §12.5 — keep the two in sync.
        public const string ResourcesLoadKey  = "BizSim/GooglePlay/AssetDeliverySettings";
        public const string AssetDatabasePath = "Assets/Resources/" + ResourcesLoadKey + ".asset";

        public bool LogsEnabled = true;
        public BizSimLogger.LogLevel LogLevel = BizSimLogger.LogLevel.Info;
        public bool UseMockInDevelopmentBuild = false;
        public bool EnableAnalyticsByDefault = false;
        public int StateQueueCapacity = 32;
        public float DefaultTimeoutSeconds = 120f;
        public bool AutoStartStateListener = true;
        public string[] InstallTimePackNames = new string[0];
    }
}
