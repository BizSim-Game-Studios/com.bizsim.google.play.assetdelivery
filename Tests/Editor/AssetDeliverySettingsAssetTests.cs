using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor.Tests
{
    /// <summary>
    /// Tests for <see cref="AssetDeliverySettingsAsset"/>: auto-create, round-trip, reset-to-defaults.
    /// Each test cleans up the created asset in TearDown.
    /// </summary>
    public sealed class AssetDeliverySettingsAssetTests
    {
        [TearDown]
        public void TearDown()
        {
            // Remove the test asset if it was created.
            if (AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                    AssetDeliverySettings.AssetDatabasePath) != null)
            {
                AssetDatabase.DeleteAsset(AssetDeliverySettings.AssetDatabasePath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void LoadOrCreate_AutoCreatesAssetAndFolders()
        {
            // Pre-condition: asset does not exist (cleared by TearDown of prior run).
            AssetDatabase.DeleteAsset(AssetDeliverySettings.AssetDatabasePath);
            AssetDatabase.Refresh();

            var settings = AssetDeliverySettingsAsset.LoadOrCreate();

            Assert.IsNotNull(settings, "LoadOrCreate must return a non-null settings object");
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                    AssetDeliverySettings.AssetDatabasePath),
                "Asset file must exist on disk after LoadOrCreate");
        }

        [Test]
        public void Save_RoundTripsLogLevel()
        {
            var settings = AssetDeliverySettingsAsset.LoadOrCreate();

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("LogLevel").enumValueIndex =
                (int)BizSimLogger.LogLevel.Warning;
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();

            // Re-load from disk
            AssetDatabase.ImportAsset(AssetDeliverySettings.AssetDatabasePath);
            var reloaded = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);

            Assert.AreEqual(BizSimLogger.LogLevel.Warning, reloaded.LogLevel,
                "LogLevel must survive a Save/Reload round-trip");
        }

        [Test]
        public void ResetToDefaults_RestoresOriginalValues()
        {
            var settings = AssetDeliverySettingsAsset.LoadOrCreate();

            // Mutate
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("LogsEnabled").boolValue = false;
            serialized.FindProperty("StateQueueCapacity").intValue = 99;
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();

            // Reset
            AssetDeliverySettingsAsset.ResetToDefaults();

            var reloaded = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);

            Assert.IsTrue(reloaded.LogsEnabled,
                "LogsEnabled must be restored to true after ResetToDefaults");
            Assert.AreEqual(32, reloaded.StateQueueCapacity,
                "StateQueueCapacity must be restored to 32 after ResetToDefaults");
        }
    }
}
