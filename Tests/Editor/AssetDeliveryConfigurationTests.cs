using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor.Tests
{
    /// <summary>
    /// Smoke tests for <see cref="AssetDeliveryConfiguration"/>: window opens without error
    /// and the Settings asset is reachable.
    /// </summary>
    public sealed class AssetDeliveryConfigurationTests
    {
        private AssetDeliveryConfiguration _window;

        [SetUp]
        public void SetUp()
        {
            // Ensure Settings asset exists so the window doesn't hard-null.
            AssetDeliverySettingsAsset.LoadOrCreate();
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
        }

        [Test]
        public void ShowWindow_OpensWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                _window = EditorWindow.GetWindow<AssetDeliveryConfiguration>();
            }, "AssetDeliveryConfiguration.ShowWindow must not throw");
            Assert.IsNotNull(_window, "Window instance must be non-null");
        }

        [Test]
        public void SettingsAsset_IsLoadable()
        {
            var settings = AssetDeliverySettingsAsset.LoadOrCreate();
            Assert.IsNotNull(settings, "Settings asset must be non-null after LoadOrCreate");
        }

        [Test]
        public void ResetToDefaults_CallsInvalidateCache_NoException()
        {
            // Verifies the Apply/Reset path that calls BizSimLogger.InvalidateCache() doesn't throw.
            Assert.DoesNotThrow(() =>
            {
                AssetDeliverySettingsAsset.ResetToDefaults();
                BizSimLogger.InvalidateCache();
            });
        }
    }
}
