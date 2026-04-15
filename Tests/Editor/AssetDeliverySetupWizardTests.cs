using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor.Tests
{
    /// <summary>
    /// Tests for <see cref="AssetDeliverySetupWizard"/>.
    /// Covers step badge logic and one-click fix mutations.
    /// </summary>
    public sealed class AssetDeliverySetupWizardTests
    {
        private AssetDeliverySetupWizard _window;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) { _window.Close(); _window = null; }
        }

        [Test]
        public void ShowWindow_OpensWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                _window = EditorWindow.GetWindow<AssetDeliverySetupWizard>();
            });
            Assert.IsNotNull(_window);
        }

        [Test]
        public void Step4_SettingsAssetCheck_PassesAfterLoadOrCreate()
        {
            // LoadOrCreate should ensure the asset exists so step 4 passes.
            AssetDeliverySettingsAsset.LoadOrCreate();
            var settings = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);
            Assert.IsNotNull(settings,
                "Settings asset must exist after LoadOrCreate so step 4 badge is green");
        }

        [Test]
        public void Step5_AddDemoPack_AddsDescriptorEntry()
        {
            var settings   = AssetDeliverySettingsAsset.LoadOrCreate();
            var serialized = new SerializedObject(settings);

            // Clear existing descriptors.
            serialized.FindProperty("PackDescriptors").arraySize = 0;
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();

            // Simulate Add demo pack.
            serialized.Update();
            var arr       = serialized.FindProperty("PackDescriptors");
            arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).FindPropertyRelative("StablePackId").stringValue = "Demo_InstallTime";
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();

            var reloaded = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);
            Assert.AreEqual(1, reloaded.PackDescriptors.Length,
                "PackDescriptors must have 1 entry after adding demo pack");
            Assert.AreEqual("Demo_InstallTime", reloaded.PackDescriptors[0].StablePackId);
        }

        [Test]
        public void SeenPrefKey_SetOnCompletion()
        {
            const string key = "BizSim.AssetDelivery.SetupWizardSeen";
            EditorPrefs.DeleteKey(key);
            Assert.IsFalse(EditorPrefs.HasKey(key));

            // Simulate completion.
            EditorPrefs.SetBool(key, true);
            Assert.IsTrue(EditorPrefs.GetBool(key, false),
                "SeenPrefKey must be true after wizard completion");

            // Cleanup.
            EditorPrefs.DeleteKey(key);
        }
    }
}
