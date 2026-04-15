using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// 6-step guided setup wizard (ADR-024, §C2).
    /// Opens via <c>BizSim → Google Play → Asset Delivery → Getting Started</c>.
    /// On completion marks <c>EditorPrefs["BizSim.AssetDelivery.SetupWizardSeen"]</c> = true
    /// so it does not auto-open on subsequent editor launches.
    /// </summary>
    public sealed class AssetDeliverySetupWizard : EditorWindow
    {
        private const string SeenPrefKey = "BizSim.AssetDelivery.SetupWizardSeen";
        private const string ManifestPath = "Packages/manifest.json";
        private const string OpenUPMScope = "com.google.external-dependency-manager";
        private const string DemoPackId   = "Demo_InstallTime";

        private Vector2 _scroll;

        [MenuItem("BizSim/Google Play/Asset Delivery/Getting Started", false, 300)]
        public static void ShowWindow()
        {
            var w = GetWindow<AssetDeliverySetupWizard>("Asset Delivery Setup");
            w.minSize = new Vector2(540, 500);
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Asset Delivery — Getting Started", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Follow these steps to integrate Google Play Asset Delivery in your project.",
                MessageType.Info);
            EditorGUILayout.Space(8);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawStep(1, "Android build target",
                IsAndroidPlatform(),
                "Active build target is Android.",
                "Active build target is NOT Android.",
                "Switch to Android",
                () => EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android));

            DrawStep(2, "OpenUPM scoped registry",
                HasOpenUpmRegistry(),
                "OpenUPM scoped registry is in Packages/manifest.json.",
                "OpenUPM scoped registry is missing from Packages/manifest.json.",
                "Add registry",
                AddOpenUpmRegistry);

            DrawStep(3, "EDM4U installed",
                HasEdm4U(),
                "External Dependency Manager (EDM4U) is installed.",
                "EDM4U not detected. Install it from OpenUPM or the Google APIs for Unity page.",
                "Install EDM4U",
                InstallEdm4U);

            DrawStep(4, "Settings asset exists",
                HasSettingsAsset(),
                "AssetDeliverySettings.asset exists.",
                "AssetDeliverySettings.asset not found.",
                "Create Settings",
                () => AssetDeliverySettingsAsset.LoadOrCreate());

            DrawStep(5, "Pack declared",
                HasAtLeastOnePack(),
                "At least one AssetPackDescriptor is declared.",
                "No packs declared in PackDescriptors.",
                "Create demo pack",
                AddDemoPack);

            DrawStep(6, "Sample imported",
                IsSampleImported(),
                "BasicIntegration sample is imported.",
                "BasicIntegration sample is not imported. Import it via Package Manager.",
                "Import sample",
                ImportSample);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(8);

            if (AllStepsPassed())
            {
                EditorGUILayout.HelpBox("All setup steps complete! Your project is ready.", MessageType.None);
                if (GUILayout.Button("Mark as Done"))
                {
                    EditorPrefs.SetBool(SeenPrefKey, true);
                    Close();
                }
            }
        }

        // ─── Step renderer ────────────────────────────────────────────────────────

        private static void DrawStep(
            int stepNumber,
            string title,
            bool pass,
            string passMsg,
            string failMsg,
            string fixLabel,
            System.Action fix)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var prev = GUI.color;
                    GUI.color = pass ? Color.green : new Color(0.9f, 0.3f, 0.3f);
                    EditorGUILayout.LabelField(pass ? "\u2713" : "\u2717",
                        EditorStyles.boldLabel, GUILayout.Width(24));
                    GUI.color = prev;
                    EditorGUILayout.LabelField($"Step {stepNumber}: {title}", EditorStyles.boldLabel);
                }

                EditorGUILayout.LabelField(pass ? passMsg : failMsg, EditorStyles.wordWrappedLabel);

                if (!pass)
                {
                    if (GUILayout.Button(fixLabel, GUILayout.Width(160)))
                        fix?.Invoke();
                }
            }
        }

        // ─── Step checks ──────────────────────────────────────────────────────────

        private static bool IsAndroidPlatform()
            => EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;

        private static bool HasOpenUpmRegistry()
        {
            if (!File.Exists(ManifestPath)) return false;
            return File.ReadAllText(ManifestPath).Contains(OpenUPMScope);
        }

        private static bool HasEdm4U()
        {
            var type = System.Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
            return type != null;
        }

        private static bool HasSettingsAsset()
            => AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                   AssetDeliverySettings.AssetDatabasePath) != null;

        private static bool HasAtLeastOnePack()
        {
            var s = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);
            return s != null && s.PackDescriptors != null && s.PackDescriptors.Length > 0;
        }

        private static bool IsSampleImported()
            => AssetDatabase.IsValidFolder("Assets/Samples/BizSim Google Play Asset Delivery");

        private bool AllStepsPassed()
            => IsAndroidPlatform() && HasOpenUpmRegistry() && HasEdm4U()
            && HasSettingsAsset() && HasAtLeastOnePack() && IsSampleImported();

        // ─── One-click fixes ──────────────────────────────────────────────────────

        private static void AddOpenUpmRegistry()
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogWarning(BizSimLogger.Prefix + "Packages/manifest.json not found.");
                return;
            }

            string content = File.ReadAllText(ManifestPath);
            if (content.Contains(OpenUPMScope))
            {
                Debug.Log(BizSimLogger.Prefix + "OpenUPM registry already present.");
                return;
            }

            string registryBlock =
                "\"scopedRegistries\": [\n" +
                "    {\n" +
                "      \"name\": \"package.openupm.com\",\n" +
                "      \"url\": \"https://package.openupm.com\",\n" +
                "      \"scopes\": [\n" +
                "        \"com.google.external-dependency-manager\"\n" +
                "      ]\n" +
                "    }\n" +
                "  ],\n  ";

            // Insert before the "dependencies" key.
            content = Regex.Replace(content, @"""dependencies""", registryBlock + "\"dependencies\"");
            File.WriteAllText(ManifestPath, content);
            AssetDatabase.Refresh();
            Debug.Log(BizSimLogger.Prefix + "OpenUPM registry added to Packages/manifest.json.");
        }

        private static void InstallEdm4U()
        {
            UnityEditor.PackageManager.Client.Add(
                "https://github.com/googlesamples/unity-jar-resolver.git?path=upm#v1.2.187");
        }

        private static void AddDemoPack()
        {
            var settings = AssetDeliverySettingsAsset.LoadOrCreate();
            var serialized = new SerializedObject(settings);
            var arr = serialized.FindProperty("PackDescriptors");
            arr.arraySize++;
            var el = arr.GetArrayElementAtIndex(arr.arraySize - 1);
            el.FindPropertyRelative("StablePackId").stringValue  = DemoPackId;
            el.FindPropertyRelative("DisplayName").stringValue   = "Demo Install-Time Pack";
            el.FindPropertyRelative("DeliveryMode").enumValueIndex = (int)DeliveryMode.InstallTime;
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();
            Debug.Log(BizSimLogger.Prefix + $"Added demo pack '{DemoPackId}' to PackDescriptors.");
        }

        private static void ImportSample()
        {
            // Open Package Manager window focused on this package's samples.
            UnityEditor.PackageManager.UI.Window.Open("com.bizsim.google.play.assetdelivery");
        }
    }
}
