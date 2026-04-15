using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor.Tests
{
    /// <summary>
    /// Tests for <see cref="AssetDeliveryBuildValidator"/> r2 extensions.
    /// Uses structural checks to verify each check method exists and the
    /// validator implements the expected interface.
    /// </summary>
    public sealed class AssetDeliveryBuildValidatorTests
    {
        private AssetDeliveryBuildValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new AssetDeliveryBuildValidator();
            AssetDeliverySettingsAsset.LoadOrCreate();
        }

        // Verify IPreprocessBuildWithReport implementation.
        [Test]
        public void Validator_ImplementsIPreprocessBuildWithReport()
        {
            Assert.IsInstanceOf<IPreprocessBuildWithReport>(_validator,
                "AssetDeliveryBuildValidator must implement IPreprocessBuildWithReport");
        }

        // Verify IPostprocessBuildWithReport implementation (for WriteCurrent).
        [Test]
        public void Validator_ImplementsIPostprocessBuildWithReport()
        {
            Assert.IsInstanceOf<IPostprocessBuildWithReport>(_validator,
                "AssetDeliveryBuildValidator must implement IPostprocessBuildWithReport");
        }

        // callbackOrder must be 0 (before the metadata jar generator at 100).
        [Test]
        public void Validator_CallbackOrderIsZero()
        {
            Assert.AreEqual(0, _validator.callbackOrder,
                "AssetDeliveryBuildValidator.callbackOrder must be 0");
        }

        // Rename check: missing cache on first build does not throw.
        [Test]
        public void RenameCheck_MissingCache_DoesNotThrow()
        {
            AssetDeliveryPackManifestCache.DeleteCache();
            Assert.DoesNotThrow(() =>
                AssetDeliveryPackManifestCache.CheckForUnsafeRename(isRelease: false));
        }

        // Settings asset existence check: asset must be loadable.
        [Test]
        public void SettingsAsset_ExistsAfterLoadOrCreate()
        {
            var s = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);
            Assert.IsNotNull(s, "Settings asset must exist after LoadOrCreate");
        }

        // targetSdkVersion advisory: verify the PlayerSettings API is accessible.
        [Test]
        public void TargetSdk_ApiIsAccessible()
        {
            Assert.DoesNotThrow(() =>
            {
                int sdk = (int)PlayerSettings.Android.targetSdkVersion;
                // Just verify we can read it; actual value depends on project settings.
                _ = sdk;
            });
        }
    }
}
