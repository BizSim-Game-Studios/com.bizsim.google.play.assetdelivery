using UnityEditor;
using UnityEngine;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Editor configuration window for <c>com.bizsim.google.play.assetdelivery</c>.
    /// Opens via <c>BizSim → Google Play → Asset Delivery → Configuration</c>.
    /// Draws the project-wide <see cref="AssetDeliverySettings"/> asset with Apply/Revert/Reset
    /// buttons, Firebase Analytics integration status, and documentation links.
    /// </summary>
    public sealed class AssetDeliveryConfiguration : EditorWindow
    {
        private const string REPO_URL        = "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery";
        private const string DOCS_URL        = REPO_URL + "/blob/main/Documentation~/DATA_SAFETY.md";
        private const string BUILD_GRADLE_URL = REPO_URL + "/blob/main/Documentation~/BUILD_GRADLE_GUIDE.md";
        private const string LOCAL_DEV_URL   = REPO_URL + "/blob/main/Documentation~/LOCAL_DEV_WORKFLOW.md";

        private Vector2 _scroll;
        private SerializedObject _settingsSO;

        [MenuItem("BizSim/Google Play/Asset Delivery/Configuration", false, 100)]
        public static void ShowWindow()
        {
            var w = GetWindow<AssetDeliveryConfiguration>("BizSim Asset Delivery");
            w.minSize = new Vector2(480, 540);
            w.Show();
        }

        private void OnEnable()
        {
            var settings = AssetDeliverySettingsAsset.LoadOrCreate();
            _settingsSO = new SerializedObject(settings);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader();
            EditorGUILayout.Space(8);
            DrawSettingsSection();
            EditorGUILayout.Space(8);
            DrawEnterpriseSection();
            EditorGUILayout.Space(8);
            DrawFirebaseSection();
            EditorGUILayout.Space(8);
            DrawLinksSection();
            EditorGUILayout.EndScrollView();
        }

        // ─── Sections ─────────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("BizSim Google Play Asset Delivery", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Package version: {PackageVersion.Current}");
            EditorGUILayout.LabelField($"Play Core asset-delivery: {PackageVersion.PlayCoreVersion}");
            EditorGUILayout.LabelField($"Released: {PackageVersion.ReleaseDate}");
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings (project-wide defaults)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These values are stored in " + AssetDeliverySettings.AssetDatabasePath +
                " and become the controller's defaults at runtime.",
                MessageType.Info);

            if (_settingsSO == null) OnEnable();
            _settingsSO.Update();

            // r1 base fields
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("LogsEnabled"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("LogLevel"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("UseMockInDevelopmentBuild"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("EnableAnalyticsByDefault"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("StateQueueCapacity"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("DefaultTimeoutSeconds"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("AutoStartStateListener"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("InstallTimePackNames"), true);

            EditorGUILayout.HelpBox(
                "InstallTimePackNames should match the pack names declared as deliveryType = \"install-time\" " +
                "in your host project's build.gradle. See the Build Gradle Guide link below.",
                MessageType.None);

            EditorGUILayout.Space();
            DrawApplyRevertReset();
        }

        private void DrawEnterpriseSection()
        {
            EditorGUILayout.LabelField("Enterprise Settings (r2)", EditorStyles.boldLabel);
            if (_settingsSO == null) OnEnable();
            _settingsSO.Update();

            EditorGUILayout.PropertyField(_settingsSO.FindProperty("StallTimeoutSeconds"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("LruBudgetBytes"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("UseLocalHttpInDevelopmentBuild"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("LocalHttpBaseUrl"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("PackDescriptors"), true);
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("CacheBudgetBytes"));

            EditorGUILayout.HelpBox(
                "PackDescriptors.StablePackId must never be renamed without setting AllowUnsafeRename = true " +
                "on that descriptor first. Renaming without this flag set will hard-fail your release build " +
                "to protect existing installs (ADR-015).",
                MessageType.Warning);

            EditorGUILayout.Space();
            DrawApplyRevertReset();
        }

        private void DrawFirebaseSection()
        {
            EditorGUILayout.LabelField("Firebase Analytics Integration", EditorStyles.boldLabel);
            bool packageInstalled = BizSimDefineManager.IsFirebaseAnalyticsInstalled();
            string version = packageInstalled ? BizSimDefineManager.GetFirebaseAnalyticsVersion() : null;

            using (new EditorGUILayout.HorizontalScope())
            {
                var prev = GUI.color;
                GUI.color = packageInstalled ? Color.green : new Color(1f, 0.5f, 0f);
                EditorGUILayout.LabelField(
                    packageInstalled ? $"\u2713 Installed (v{version})" : "\u2717 Not installed",
                    EditorStyles.boldLabel);
                GUI.color = prev;
            }

            bool definePresent = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();
            EditorGUILayout.LabelField($"BIZSIM_FIREBASE define: {(definePresent ? "active" : "inactive")}");

            string msg = BizSimDefineManager.GetFirebaseStatusMessage(out var msgType);
            EditorGUILayout.HelpBox(msg, msgType);

            using (new EditorGUI.DisabledScope(!packageInstalled || definePresent))
                if (GUILayout.Button("Add BIZSIM_FIREBASE to all platforms"))
                    BizSimDefineManager.AddFirebaseDefineAllPlatforms();
            using (new EditorGUI.DisabledScope(!definePresent))
                if (GUILayout.Button("Remove BIZSIM_FIREBASE from all platforms"))
                    BizSimDefineManager.RemoveFirebaseDefineAllPlatforms();
        }

        private void DrawLinksSection()
        {
            EditorGUILayout.LabelField("Documentation & Support", EditorStyles.boldLabel);
            if (GUILayout.Button("Open GitHub Repository"))     Application.OpenURL(REPO_URL);
            if (GUILayout.Button("Open Data Safety Documentation")) Application.OpenURL(DOCS_URL);
            if (GUILayout.Button("Build Gradle Guide"))         Application.OpenURL(BUILD_GRADLE_URL);
            if (GUILayout.Button("Local Dev Workflow Guide"))   Application.OpenURL(LOCAL_DEV_URL);
        }

        // ─── Shared buttons ───────────────────────────────────────────────────────

        private void DrawApplyRevertReset()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply"))
                {
                    _settingsSO.ApplyModifiedProperties();
                    AssetDeliverySettingsAsset.Save();
                    BizSimLogger.InvalidateCache();
                }
                if (GUILayout.Button("Revert"))
                {
                    _settingsSO.Update();
                }
                if (GUILayout.Button("Reset to defaults"))
                {
                    AssetDeliverySettingsAsset.ResetToDefaults();
                    _settingsSO = new SerializedObject(AssetDeliverySettingsAsset.LoadOrCreate());
                    BizSimLogger.InvalidateCache();
                }
            }
        }
    }
}
