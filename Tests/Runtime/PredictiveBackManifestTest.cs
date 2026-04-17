using System.IO;
using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// C5.2 drift guard (Plan E). For assetdelivery, expected value is "true" —
    /// cellular confirmation dialog is system-managed.
    /// </summary>
    public class PredictiveBackManifestTest
    {
        private const string ManifestPath =
            "Packages/com.bizsim.google.play.assetdelivery/Runtime/Plugins/Android/BizSimAssetDelivery.androidlib/AndroidManifest.xml";

        private const string FallbackPath =
            "Runtime/Plugins/Android/BizSimAssetDelivery.androidlib/AndroidManifest.xml";

        private static string ReadManifest()
        {
            if (File.Exists(ManifestPath)) return File.ReadAllText(ManifestPath);
            if (File.Exists(FallbackPath)) return File.ReadAllText(FallbackPath);
            Assert.Inconclusive("Manifest not found at " + ManifestPath + " or " + FallbackPath);
            return null;
        }

        [Test]
        public void Manifest_DeclaresPredictiveBackCallback_True()
        {
            var xml = ReadManifest();
            Assert.IsTrue(xml.Contains("enableOnBackInvokedCallback=\"true\""),
                "Per C5.2, assetdelivery's .androidlib manifest must declare " +
                "android:enableOnBackInvokedCallback=\"true\".");
        }
    }
}
