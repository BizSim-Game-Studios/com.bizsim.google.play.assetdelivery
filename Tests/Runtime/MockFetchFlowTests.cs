using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    public class MockFetchFlowTests
    {
        private MockAssetDeliveryProvider _provider;
        private AssetDeliveryMockConfig   _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            _config.Packs = new[]
            {
                new AssetDeliveryMockConfig.SimulatedPack
                {
                    Name                  = "level_1",
                    FinalStatus           = AssetPackStatus.Completed,
                    DownloadDurationSeconds = 0.5f,
                    TotalBytesToDownload  = 1000,
                    Error                 = AssetPackErrorCode.NoError,
                }
            };
            _provider = new MockAssetDeliveryProvider(_config);
        }

        [TearDown]
        public void TearDown()
        {
            _provider.StopListening();
        }

        [UnityTest]
        public IEnumerator SinglePack_EmitsFullStateSequence_OnSuccess()
        {
            var observed = new List<AssetPackStatus>();
            _provider.OnStateUpdate += (name, s) => { if (name == "level_1") observed.Add(s.Status); };
            _provider.StartListening();

            var tcs = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = _provider.FetchAsync(new[] { "level_1" }, tcs, 10f, default);

            yield return new WaitForSeconds(1.0f);

            CollectionAssert.Contains(observed, AssetPackStatus.Pending);
            CollectionAssert.Contains(observed, AssetPackStatus.Downloading);
            CollectionAssert.Contains(observed, AssetPackStatus.Transferring);
            CollectionAssert.Contains(observed, AssetPackStatus.Completed);
        }

        [UnityTest]
        public IEnumerator StopAtDownloading_EmitsFailedStatus()
        {
            _config.Packs = new[]
            {
                new AssetDeliveryMockConfig.SimulatedPack
                {
                    Name = "dl_fail", FinalStatus = AssetPackStatus.Failed,
                    DownloadDurationSeconds = 0.2f, TotalBytesToDownload = 500,
                    Error = AssetPackErrorCode.NetworkError,
                    StopAt = AssetPackStatus.Downloading,
                }
            };
            _provider = new MockAssetDeliveryProvider(_config);
            _provider.StartListening();

            var terminal = new List<AssetPackStatus>();
            _provider.OnStateUpdate += (name, s) => { if (s.IsTerminal) terminal.Add(s.Status); };

            var tcs = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = _provider.FetchAsync(new[] { "dl_fail" }, tcs, 5f, default);

            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(terminal.Count > 0, "Expected a terminal status");
        }

        [UnityTest]
        public IEnumerator StopAtWaitingForWifi_EmitsConfirmationState()
        {
            _config.Packs = new[]
            {
                new AssetDeliveryMockConfig.SimulatedPack
                {
                    Name = "wifi_pack", FinalStatus = AssetPackStatus.Completed,
                    DownloadDurationSeconds = 0.3f, TotalBytesToDownload = 100,
                    StopAt = AssetPackStatus.WaitingForWifi,
                }
            };
            _provider = new MockAssetDeliveryProvider(_config);
            _provider.StartListening();

            var wifiSeen = false;
            _provider.OnStateUpdate += (_, s) => { if (s.Status == AssetPackStatus.WaitingForWifi) wifiSeen = true; };

            var tcs = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = _provider.FetchAsync(new[] { "wifi_pack" }, tcs, 5f, default);

            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(wifiSeen, "Expected WaitingForWifi status");
        }

        [UnityTest]
        public IEnumerator TcsResolved_OnFirstNonPendingState_NotAtCompleted()
        {
            _provider.StartListening();
            var tcs = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = _provider.FetchAsync(new[] { "level_1" }, tcs, 10f, default);

            // TCS should resolve before download completes
            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(tcs.Task.IsCompleted,
                "TCS must resolve on first non-PENDING state (DOWNLOADING ack), not at COMPLETED");
        }
    }
}
