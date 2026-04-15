using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Verifies that AssetDeliveryController public methods throw InvalidOperationException
    /// when called from a background thread.
    /// </summary>
    public class AssetDeliveryControllerMainThreadTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("[test-ctrl]");
            _go.AddComponent<AssetDeliveryController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            AssetDeliveryTestInjector.Reset();
        }

        [Test]
        public async Task FetchAsync_FromBackgroundThread_ThrowsInvalidOperation()
        {
            var thrown = await Task.Run(() =>
            {
                try { _ = AssetDeliveryController.Instance.FetchAsync("level_1"); return (System.Exception)null; }
                catch (System.Exception ex) { return ex; }
            });
            Assert.IsInstanceOf<System.InvalidOperationException>(thrown);
            StringAssert.Contains("main thread", thrown.Message);
        }

        [Test]
        public async Task GetPackStatesAsync_FromBackgroundThread_ThrowsInvalidOperation()
        {
            var thrown = await Task.Run(() =>
            {
                try
                {
                    _ = AssetDeliveryController.Instance.GetPackStatesAsync(
                        new[] { "level_1" });
                    return (System.Exception)null;
                }
                catch (System.Exception ex) { return ex; }
            });
            Assert.IsInstanceOf<System.InvalidOperationException>(thrown);
        }

        [Test]
        public async Task GetPackLocationAsync_FromBackgroundThread_ThrowsInvalidOperation()
        {
            var thrown = await Task.Run(() =>
            {
                try { _ = AssetDeliveryController.Instance.GetPackLocationAsync("level_1"); return (System.Exception)null; }
                catch (System.Exception ex) { return ex; }
            });
            Assert.IsInstanceOf<System.InvalidOperationException>(thrown);
        }

        [Test]
        public async Task CancelAsync_FromBackgroundThread_ThrowsInvalidOperation()
        {
            var thrown = await Task.Run(() =>
            {
                try { _ = AssetDeliveryController.Instance.CancelAsync(new[] { "level_1" }); return (System.Exception)null; }
                catch (System.Exception ex) { return ex; }
            });
            Assert.IsInstanceOf<System.InvalidOperationException>(thrown);
        }

        [Test]
        public async Task ShowConfirmationDialogAsync_FromBackgroundThread_ThrowsInvalidOperation()
        {
            var thrown = await Task.Run(() =>
            {
                try { _ = AssetDeliveryController.Instance.ShowConfirmationDialogAsync(); return (System.Exception)null; }
                catch (System.Exception ex) { return ex; }
            });
            Assert.IsInstanceOf<System.InvalidOperationException>(thrown);
        }
    }
}
