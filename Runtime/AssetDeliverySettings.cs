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

        // All declared pack names (both install-time and on-demand).
        // Used by Awake reconciliation (ADR-018) to scope which sessions to clean up.
        public string[] AllDeclaredPackNames = new string[0];

        // Enterprise subsystem settings (r2 / ADR-016 stall detector).
        [UnityEngine.Range(5f, 300f)]
        public float StallTimeoutSeconds = 30f;

        // LRU cache disk budget in bytes. 0 = unlimited (ADR-029).
        public long LruBudgetBytes = 0L;

        // Development / local HTTP provider (ADR-025).
        public bool   UseLocalHttpInDevelopmentBuild = false;
        public string LocalHttpBaseUrl = "http://10.0.2.2:8080/packs/";
    }
}
