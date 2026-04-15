using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Tests for FetchPreflightResult and related data types.
    /// CanFetchAsync integration tests require a running controller — structural tests here.
    /// </summary>
    public class CanFetchPreflightTests
    {
        [Test]
        public void FetchPreflightResult_Ok_HasCanFetchTrue()
        {
            var r = FetchPreflightResult.Ok(1000, 500_000_000);
            Assert.IsTrue(r.CanFetch);
            Assert.AreEqual(FetchPreflightReason.OK, r.Reason);
        }

        [Test]
        public void FetchPreflightResult_InsufficientStorage_HasCanFetchFalse()
        {
            var r = new FetchPreflightResult(false, FetchPreflightReason.InsufficientStorage,
                500_000_000, 100_000_000, "Not enough space");
            Assert.IsFalse(r.CanFetch);
            Assert.AreEqual(FetchPreflightReason.InsufficientStorage, r.Reason);
        }

        [Test]
        public void FetchPreflightResult_NoNetwork_HasCorrectReason()
        {
            var r = new FetchPreflightResult(false, FetchPreflightReason.NoNetwork, 0, 0, "No network");
            Assert.AreEqual(FetchPreflightReason.NoNetwork, r.Reason);
        }

        [Test]
        public void FetchPreflightResult_DataSaverOn_HasCorrectReason()
        {
            var r = new FetchPreflightResult(false, FetchPreflightReason.DataSaverOn, 0, 0, "Data saver");
            Assert.AreEqual(FetchPreflightReason.DataSaverOn, r.Reason);
        }

        [Test]
        public void FetchPreflightReason_AllExpectedValuesExist()
        {
            // Assert all documented reasons compile correctly
            _ = FetchPreflightReason.OK;
            _ = FetchPreflightReason.InsufficientStorage;
            _ = FetchPreflightReason.NoNetwork;
            _ = FetchPreflightReason.MeteredNetworkRefused;
            _ = FetchPreflightReason.PackNotDeclared;
            _ = FetchPreflightReason.DataSaverOn;
            Assert.Pass();
        }
    }
}
