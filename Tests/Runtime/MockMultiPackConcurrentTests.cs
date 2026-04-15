using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    public class MockMultiPackConcurrentTests
    {
        [UnityTest]
        public IEnumerator ThreePacksConcurrent_AllReachCompleted()
        {
            var cfg = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            cfg.Packs = new[]
            {
                new AssetDeliveryMockConfig.SimulatedPack { Name = "p1", FinalStatus = AssetPackStatus.Completed, DownloadDurationSeconds = 0.3f, TotalBytesToDownload = 100 },
                new AssetDeliveryMockConfig.SimulatedPack { Name = "p2", FinalStatus = AssetPackStatus.Completed, DownloadDurationSeconds = 0.5f, TotalBytesToDownload = 200 },
                new AssetDeliveryMockConfig.SimulatedPack { Name = "p3", FinalStatus = AssetPackStatus.Completed, DownloadDurationSeconds = 0.8f, TotalBytesToDownload = 300 },
            };
            var p = new MockAssetDeliveryProvider(cfg);
            p.StartListening();

            var tcs = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = p.FetchAsync(new[] { "p1", "p2", "p3" }, tcs, 10f, default);

            yield return new WaitForSeconds(1.2f);

            var statesTask = p.GetPackStatesAsync(new[] { "p1", "p2", "p3" }, 5f, default);
            while (!statesTask.IsCompleted) yield return null;
            var states = statesTask.Result;
            Assert.AreEqual(AssetPackStatus.Completed, states.Packs["p1"].Status);
            Assert.AreEqual(AssetPackStatus.Completed, states.Packs["p2"].Status);
            Assert.AreEqual(AssetPackStatus.Completed, states.Packs["p3"].Status);
            p.StopListening();
        }

        [UnityTest]
        public IEnumerator DifferentPacks_CanBeFetchedConcurrently()
        {
            var cfg = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            cfg.Packs = new[]
            {
                new AssetDeliveryMockConfig.SimulatedPack { Name = "a1", FinalStatus = AssetPackStatus.Completed, DownloadDurationSeconds = 0.2f, TotalBytesToDownload = 50 },
                new AssetDeliveryMockConfig.SimulatedPack { Name = "a2", FinalStatus = AssetPackStatus.Completed, DownloadDurationSeconds = 0.2f, TotalBytesToDownload = 50 },
            };
            var p = new MockAssetDeliveryProvider(cfg);
            p.StartListening();

            var tcs1 = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcs2 = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Fetch p1 and p2 concurrently — must not throw
            Assert.DoesNotThrow(() => _ = p.FetchAsync(new[] { "a1" }, tcs1, 5f, default));
            Assert.DoesNotThrow(() => _ = p.FetchAsync(new[] { "a2" }, tcs2, 5f, default));

            yield return new WaitForSeconds(0.5f);
            p.StopListening();
        }
    }
}
