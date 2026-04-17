using NUnit.Framework;
using BizSim.Google.Play.AssetDelivery;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>K8 PackageVersion schema drift guard (Plan G).</summary>
    public class PackageVersionSchemaTest
    {
        [Test]
        public void NativeSdkFields_ArePopulated()
        {
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkVersion));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkLabel));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkArtifactCoord));
        }

        [Test]
        public void NativeSdkArtifactCoord_EndsWithVersion()
        {
            Assert.IsTrue(PackageVersion.NativeSdkArtifactCoord.EndsWith(":" + PackageVersion.NativeSdkVersion));
        }

        [Test]
        public void NativeSdkFields_MatchExpectedAssetDeliveryValues()
        {
            Assert.AreEqual("2.3.0", PackageVersion.NativeSdkVersion);
            Assert.AreEqual("Play Core (asset-delivery)", PackageVersion.NativeSdkLabel);
            Assert.AreEqual("com.google.android.play:asset-delivery:2.3.0", PackageVersion.NativeSdkArtifactCoord);
        }

#pragma warning disable CS0618
        [Test]
        public void LegacyAlias_ResolvesToSameValue()
        {
            Assert.AreEqual(PackageVersion.NativeSdkVersion, PackageVersion.PlayCoreVersion);
        }
#pragma warning restore CS0618
    }
}
