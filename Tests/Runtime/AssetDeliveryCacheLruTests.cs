using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Tests for IAssetDeliveryCache / CachedPack data types.
    /// Full LRU eviction integration tests require a provider — structural tests here.
    /// TODO(v1.1): add integration tests against a mock that tracks RemovePackAsync calls — ADR-029.
    /// </summary>
    public class AssetDeliveryCacheLruTests
    {
        [Test]
        public void CachedPack_Struct_StoresAllFields()
        {
            var t = System.DateTime.UtcNow;
            var p = new CachedPack("my_pack", 1024 * 1024, t);
            Assert.AreEqual("my_pack", p.PackId);
            Assert.AreEqual(1024 * 1024, p.SizeBytes);
            Assert.AreEqual(t, p.LastAccessedUtc);
        }

        [Test]
        public void RemovePackOptions_DefaultForceUnload_IsFalse()
        {
            var opts = RemovePackOptions.Default;
            Assert.IsFalse(opts.ForceUnload);
        }

        [Test]
        public void RemovePackOptions_ForceUnloadTrue_IsTrue()
        {
            var opts = new RemovePackOptions(forceUnload: true);
            Assert.IsTrue(opts.ForceUnload);
        }

        [Test]
        public void LocalizationKey_GetForErrorCode_HasCorrectPrefix()
        {
            var key = AssetDeliveryErrorMessages.Get(AssetPackErrorCode.NetworkError);
            StringAssert.StartsWith("pad.error.", key.Key);
            StringAssert.Contains("networkerror", key.Key);
        }

        [Test]
        public void LocalizationKey_GetForTimeout_HasCorrectPrefix()
        {
            var key = AssetDeliveryErrorMessages.Get(AssetPackErrorCode.Timeout);
            StringAssert.StartsWith("pad.error.", key.Key);
        }

        [Test]
        public void AssetPackYieldable_FaultedTask_ReturnsDefaultResult()
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<AssetPackHandle>(
                System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
            tcs.TrySetException(new System.Exception("test error"));
            var y = new AssetPackYieldable(tcs.Task);

            // keepWaiting is false for a faulted task
            Assert.IsFalse(y.keepWaiting);
            Assert.IsNull(y.Result);
            Assert.IsNotNull(y.Error);
            Assert.IsTrue(y.IsFaulted);
            Assert.IsFalse(y.IsCanceled);
            Assert.IsFalse(y.IsCompletedSuccessfully);
        }

        [Test]
        public void AssetPackYieldable_CancelledTask_ReturnsDefault()
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<AssetPackHandle>(
                System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
            tcs.TrySetCanceled();
            var y = new AssetPackYieldable(tcs.Task);

            Assert.IsFalse(y.keepWaiting);
            Assert.IsNull(y.Result);
            Assert.IsInstanceOf<System.OperationCanceledException>(y.Error);
            Assert.IsTrue(y.IsCanceled);
            Assert.IsFalse(y.IsFaulted);
        }
    }
}
