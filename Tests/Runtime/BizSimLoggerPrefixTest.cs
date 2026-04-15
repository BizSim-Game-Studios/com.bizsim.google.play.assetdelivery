using NUnit.Framework;
using BizSim.Google.Play.AssetDelivery;

public class BizSimLoggerPrefixTest
{
    [Test]
    public void Prefix_IsExactlyBizSimAssetDelivery()
    {
        Assert.AreEqual("[BizSim.AssetDelivery] ", BizSimLogger.Prefix,
            "Per CROSS-PACKAGE-INVARIANTS.md §12.3, the per-package log prefix is a hard convention.");
    }
}
