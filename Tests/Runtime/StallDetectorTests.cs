using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Tests for AssetDeliveryStallDetector.
    /// All tests use a mock clock (Func&lt;float&gt; nowProvider) — no real time dependency.
    /// </summary>
    public class StallDetectorTests
    {
        private float _mockNow;
        private readonly List<string> _cancelledPacks = new List<string>();
        private AssetDeliveryStallDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _mockNow = 0f;
            _cancelledPacks.Clear();
            _detector = new AssetDeliveryStallDetector(
                nowProvider:      () => _mockNow,
                cancelCallback:   p => { _cancelledPacks.Add(p); return Task.CompletedTask; },
                isPausedCallback: _ => false);
            _detector.Configure(30f);
        }

        [Test]
        public void HappyPath_BytesAdvancing_NoCancelFired()
        {
            _detector.Track("pack_a", 0);
            _mockNow = 10f;
            _detector.Track("pack_a", 1000);
            _mockNow = 29f; // just under threshold
            _detector.Tick();

            Assert.IsEmpty(_cancelledPacks, "Should not cancel when bytes are advancing");
        }

        [Test]
        public void StallAtZero_ExceedsThreshold_CancelFired()
        {
            _detector.Track("pack_a", 0);
            _mockNow = 31f; // past 30s threshold
            _detector.Tick();

            Assert.Contains("pack_a", _cancelledPacks);
        }

        [Test]
        public void PauseActive_NoCancelFired_EvenAfterThreshold()
        {
            bool paused = true;
            var det = new AssetDeliveryStallDetector(
                nowProvider:      () => _mockNow,
                cancelCallback:   p => { _cancelledPacks.Add(p); return Task.CompletedTask; },
                isPausedCallback: _ => paused);
            det.Configure(30f);

            det.Track("pack_b", 0);
            _mockNow = 300f; // 5 minutes — way over threshold
            det.Tick();

            Assert.IsEmpty(_cancelledPacks, "Must not cancel while paused (e.g. cellular dialog up)");
        }

        [Test]
        public void ReArm_AfterPauseLift_RefreshesGracePeriod()
        {
            _detector.Track("pack_c", 0);
            _mockNow = 20f;
            _detector.ReArmForPack("pack_c"); // simulate pause-lift re-arm
            _mockNow = 45f; // 25 s since re-arm — still under threshold
            _detector.Tick();

            Assert.IsEmpty(_cancelledPacks, "ReArm must refresh the grace period");
        }

        [Test]
        public void ReArm_OnRetry_ResetsStallFired()
        {
            _detector.Track("pack_d", 0);
            _mockNow = 35f;
            _detector.Tick(); // first stall fires
            Assert.Contains("pack_d", _cancelledPacks);

            // Now retry — re-arm should reset
            _cancelledPacks.Clear();
            _detector.ReArmForPack("pack_d");
            _mockNow = 60f; // only 25 s since re-arm
            _detector.Tick();

            Assert.IsEmpty(_cancelledPacks, "Re-arm on retry must reset the stall-fired flag");
        }

        [Test]
        public void StopTracking_RemovesPack_NoCancelAfterStop()
        {
            _detector.Track("pack_e", 0);
            _detector.StopTracking("pack_e");
            _mockNow = 60f;
            _detector.Tick();

            Assert.IsEmpty(_cancelledPacks, "Stopped pack must not fire cancel");
        }
    }
}
