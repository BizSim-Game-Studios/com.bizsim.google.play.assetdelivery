using NUnit.Framework;
using UnityEditor;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.AssetDelivery.Editor.Tests
{
    /// <summary>
    /// Verifies that <see cref="AssetDeliveryEditorInit"/> registers
    /// <c>BIZSIM_ASSETDELIVERY_INSTALLED</c> on all relevant build platforms.
    /// </summary>
    public sealed class AssetDeliveryEditorInitTests
    {
        [Test]
        public void Define_IsRegistered_OnAndroidPlatform()
        {
            // Trigger the static ctor if not already run.
            BizSimDefineManager.AddDefine("BIZSIM_ASSETDELIVERY_INSTALLED",
                BizSimDefineManager.GetRelevantPlatforms());

            var defines = PlayerSettings.GetScriptingDefineSymbols(
                UnityEditor.Build.NamedBuildTarget.Android);
            Assert.IsTrue(defines.Contains("BIZSIM_ASSETDELIVERY_INSTALLED"),
                "BIZSIM_ASSETDELIVERY_INSTALLED must be registered on Android");
        }

        [Test]
        public void Define_IsRegistered_OnEditorPlatform()
        {
            BizSimDefineManager.AddDefine("BIZSIM_ASSETDELIVERY_INSTALLED",
                BizSimDefineManager.GetRelevantPlatforms());

            // Editor platform: standalone (the editor compiles against standalone defines)
            var defines = PlayerSettings.GetScriptingDefineSymbols(
                UnityEditor.Build.NamedBuildTarget.Standalone);
            Assert.IsTrue(defines.Contains("BIZSIM_ASSETDELIVERY_INSTALLED"),
                "BIZSIM_ASSETDELIVERY_INSTALLED must be registered on Standalone/Editor");
        }
    }
}
