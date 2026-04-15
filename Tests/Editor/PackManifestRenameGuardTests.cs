using NUnit.Framework;
using UnityEditor.Build;

namespace BizSim.Google.Play.AssetDelivery.Editor.Tests
{
    /// <summary>
    /// Tests for <see cref="AssetDeliveryPackManifestCache"/> rename guard.
    /// Covers: happy path (no rename), rename blocked, rename allowed with opt-in,
    /// missing cache on first build (no error).
    /// </summary>
    public sealed class PackManifestRenameGuardTests
    {
        [SetUp]
        public void SetUp()
        {
            AssetDeliveryPackManifestCache.DeleteCache();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDeliveryPackManifestCache.DeleteCache();
        }

        // Happy path: no rename — no error.
        [Test]
        public void HappyPath_NoRename_NoError()
        {
            // Write a cache with the same IDs that are in settings.
            AssetDeliveryPackManifestCache.Write(new AssetDeliveryPackManifestCache.PackManifest
            {
                StablePackIds = new[] { "pack_a", "pack_b" }
            });

            // Settings has the same IDs — no rename.
            var settings   = AssetDeliverySettingsAsset.LoadOrCreate();
            var serialized = new UnityEditor.SerializedObject(settings);
            var arr        = serialized.FindProperty("PackDescriptors");
            arr.arraySize  = 2;
            arr.GetArrayElementAtIndex(0).FindPropertyRelative("StablePackId").stringValue = "pack_a";
            arr.GetArrayElementAtIndex(1).FindPropertyRelative("StablePackId").stringValue = "pack_b";
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();

            Assert.DoesNotThrow(() =>
                AssetDeliveryPackManifestCache.CheckForUnsafeRename(isRelease: true),
                "No rename should not throw BuildFailedException");
        }

        // Rename blocked on release build.
        [Test]
        public void RenameBlocked_OnReleaseBuild_ThrowsBuildFailedException()
        {
            AssetDeliveryPackManifestCache.Write(new AssetDeliveryPackManifestCache.PackManifest
            {
                StablePackIds = new[] { "pack_old" }
            });

            // Settings has a NEW pack ID — simulates rename.
            var settings   = AssetDeliverySettingsAsset.LoadOrCreate();
            var serialized = new UnityEditor.SerializedObject(settings);
            var arr        = serialized.FindProperty("PackDescriptors");
            arr.arraySize  = 1;
            var el         = arr.GetArrayElementAtIndex(0);
            el.FindPropertyRelative("StablePackId").stringValue = "pack_new";
            el.FindPropertyRelative("AllowUnsafeRename").boolValue = false;
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();

            Assert.Throws<BuildFailedException>(() =>
                AssetDeliveryPackManifestCache.CheckForUnsafeRename(isRelease: true),
                "Release build must throw BuildFailedException on unsafe rename");
        }

        // Rename allowed with AllowUnsafeRename = true.
        [Test]
        public void RenameAllowed_WithOptIn_DoesNotThrow()
        {
            AssetDeliveryPackManifestCache.Write(new AssetDeliveryPackManifestCache.PackManifest
            {
                StablePackIds = new[] { "pack_old" }
            });

            var settings   = AssetDeliverySettingsAsset.LoadOrCreate();
            var serialized = new UnityEditor.SerializedObject(settings);
            var arr        = serialized.FindProperty("PackDescriptors");
            arr.arraySize  = 1;
            var el         = arr.GetArrayElementAtIndex(0);
            el.FindPropertyRelative("StablePackId").stringValue = "pack_new";
            el.FindPropertyRelative("AllowUnsafeRename").boolValue = true; // opt-in
            serialized.ApplyModifiedProperties();
            AssetDeliverySettingsAsset.Save();

            Assert.DoesNotThrow(() =>
                AssetDeliveryPackManifestCache.CheckForUnsafeRename(isRelease: true),
                "Rename with AllowUnsafeRename = true must NOT throw");
        }

        // Missing cache (first build) — no error.
        [Test]
        public void MissingCache_OnFirstBuild_NoError()
        {
            // Cache was deleted in SetUp — this simulates a fresh checkout with no prior build.
            Assert.DoesNotThrow(() =>
                AssetDeliveryPackManifestCache.CheckForUnsafeRename(isRelease: true),
                "Missing cache (first build) must not throw");
        }
    }
}
