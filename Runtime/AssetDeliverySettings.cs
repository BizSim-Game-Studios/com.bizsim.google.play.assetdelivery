using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Project-wide settings ScriptableObject for <c>com.bizsim.google.play.assetdelivery</c>.
    /// Loaded by <see cref="AssetDeliveryController"/> and <see cref="BizSimLogger"/> via
    /// <c>Resources.Load</c> at runtime. Edit via
    /// <c>BizSim → Google Play → Asset Delivery → Configuration</c> in the Unity editor.
    /// </summary>
    /// <remarks>
    /// Field defaults are load-bearing. They are inherited by <c>ScriptableObject.CreateInstance</c>
    /// fallbacks in <see cref="BizSimLogger"/> and <see cref="AssetDeliveryController.Awake"/> when
    /// the asset is missing at runtime.
    /// </remarks>
    [CreateAssetMenu(
        menuName  = "BizSim/Google Play/AssetDelivery Settings",
        fileName  = "AssetDeliverySettings",
        order     = 0)]
    public sealed class AssetDeliverySettings : ScriptableObject
    {
        // ─── Path constants ───────────────────────────────────────────────────────
        // Per CROSS-INVARIANTS §12.5 — keep the two in sync.
        public const string ResourcesLoadKey  = "BizSim/GooglePlay/AssetDeliverySettings";
        public const string AssetDatabasePath = "Assets/Resources/" + ResourcesLoadKey + ".asset";

        // ─── Logging ──────────────────────────────────────────────────────────────

        [Header("Logging")]
        [Tooltip("Master switch. When false all log calls are no-ops regardless of LogLevel.")]
        public bool LogsEnabled = true;

        [Tooltip("Minimum severity level that will be printed to the Unity Console.")]
        public BizSimLogger.LogLevel LogLevel = BizSimLogger.LogLevel.Info;

        // ─── Mock / Development ───────────────────────────────────────────────────

        [Header("Mock / Development")]
        [Tooltip("When true in a DEVELOPMENT_BUILD, use MockAssetDeliveryProvider instead of Play Core.")]
        public bool UseMockInDevelopmentBuild = false;

        [Tooltip("Whether the analytics adapter is enabled at runtime by default.")]
        public bool EnableAnalyticsByDefault = false;

        // ─── Runtime Behaviour ────────────────────────────────────────────────────

        [Header("Runtime Behaviour")]
        [Tooltip("Capacity of the internal AssetPackStates ring buffer. Raise if you observe dropped state events.")]
        [Range(1, 256)]
        public int StateQueueCapacity = 32;

        [Tooltip("Default operation timeout in seconds. -1 sentinel means 'use this value'.")]
        public float DefaultTimeoutSeconds = 120f;

        [Tooltip("When true, StartListening() is called automatically in Awake.")]
        public bool AutoStartStateListener = true;

        [Tooltip("Pack names declared as install-time in your host project's build.gradle. " +
                 "FetchAsync short-circuits to COMPLETED for these.")]
        public string[] InstallTimePackNames = new string[0];

        // All declared pack names (both install-time and on-demand).
        // Used by Awake reconciliation (ADR-018) to scope which sessions to clean up.
        [Tooltip("ALL declared pack names (install-time + on-demand). Used for Awake reconciliation.")]
        public string[] AllDeclaredPackNames = new string[0];

        // ─── Enterprise / Stall Detector ─────────────────────────────────────────

        [Header("Enterprise — Stall Detector (ADR-016)")]
        [Tooltip("Seconds with no BytesDownloaded progress before the stall detector cancels the fetch.")]
        [UnityEngine.Range(5f, 300f)]
        public float StallTimeoutSeconds = 30f;

        // ─── LRU Cache ────────────────────────────────────────────────────────────

        [Header("Enterprise — LRU Cache (ADR-029)")]
        [Tooltip("Maximum disk budget for the LRU pack cache in bytes. 0 = unlimited.")]
        public long LruBudgetBytes = 0L;

        // ─── Local HTTP Dev Server (ADR-025) ─────────────────────────────────────

        [Header("Development — Local HTTP Provider (ADR-025)")]
        [Tooltip("When true in a DEVELOPMENT_BUILD, use the local HTTP provider instead of Play Core.")]
        public bool UseLocalHttpInDevelopmentBuild = false;

        [Tooltip("Base URL of the local pack HTTP server. The ADB reverse-forwarded address 10.0.2.2:8080 " +
                 "is the Android emulator default for localhost on the host machine.")]
        public string LocalHttpBaseUrl = "http://10.0.2.2:8080/packs/";

        // ─── Pack Descriptors (ADR-015 rename guard + Pack Inspector) ─────────────

        [Header("Pack Descriptors (ADR-015 Rename Guard)")]
        [Tooltip("One entry per asset pack declared in your host project. " +
                 "StablePackId must never be renamed without setting AllowUnsafeRename = true first.")]
        public AssetPackDescriptor[] PackDescriptors = new AssetPackDescriptor[0];

        // ─── Cache Budget ─────────────────────────────────────────────────────────

        [Header("Cache Budget")]
        [Tooltip("Disk budget hint for the consumer's asset cache. Default 500 MB. " +
                 "Used by the Setup Wizard size advisory check.")]
        public long CacheBudgetBytes = 500L * 1024L * 1024L;
    }

    // ─── Nested serializable types ────────────────────────────────────────────────

    /// <summary>
    /// Descriptor for a single asset pack. Lives in <see cref="AssetDeliverySettings.PackDescriptors"/>.
    /// The <c>StablePackId</c> is the Gradle pack name — renaming it bricks existing installs (ADR-015).
    /// </summary>
    [System.Serializable]
    public sealed class AssetPackDescriptor
    {
        [Tooltip("The pack name as declared in your build.gradle assetPack { packName }. " +
                 "NEVER rename without setting AllowUnsafeRename = true.")]
        public string StablePackId = "";

        [Tooltip("Human-readable label for the Pack Inspector.")]
        public string DisplayName = "";

        [Tooltip("Delivery mode declared in your build.gradle.")]
        public DeliveryMode DeliveryMode = DeliveryMode.OnDemand;

        [Tooltip("Set to true if you have deliberately renamed this pack's StablePackId. " +
                 "The build validator will otherwise hard-fail to protect existing installs.")]
        public bool AllowUnsafeRename = false;

        [Tooltip("AssetBundle file names expected inside this pack's assetsPath " +
                 "(used by the Pack Inspector for bundle enumeration).")]
        public string[] AssetBundleFileNames = new string[0];
    }

    /// <summary>Delivery mode for an asset pack descriptor.</summary>
    public enum DeliveryMode
    {
        InstallTime = 0,
        FastFollow  = 1,
        OnDemand    = 2,
    }
}
