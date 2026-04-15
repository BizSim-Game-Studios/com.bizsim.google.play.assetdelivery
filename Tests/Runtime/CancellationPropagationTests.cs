using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    public class CancellationPropagationTests
    {
        [Test]
        public async Task MockProvider_CancelAsync_IncrementsCounter()
        {
            var cfg = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            cfg.Packs = new[]
            {
                new AssetDeliveryMockConfig.SimulatedPack
                {
                    Name = "level_1", FinalStatus = AssetPackStatus.Completed,
                    DownloadDurationSeconds = 5f, TotalBytesToDownload = 1000,
                }
            };
            var provider = new MockAssetDeliveryProvider(cfg);
            provider.StartListening();

            using var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<AssetPackStates>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = provider.FetchAsync(new[] { "level_1" }, tcs, 10f, cts.Token);

            // Cancel after a brief moment
            await Task.Delay(50);
            cts.Cancel();

            // TCS should be cancelled now
            try { await tcs.Task; Assert.Fail("Expected OperationCanceledException"); }
            catch (OperationCanceledException) { /* expected */ }

            // Verify the mock's CancelCallCount was incremented by the ct.Register callback
            Assert.GreaterOrEqual(provider.CancelCallCount, 1,
                "CancelCallCount must increment when CancellationToken fires");

            provider.StopListening();
        }

        [Test]
        public async Task MockProvider_CancelAsync_EmitsCanceledState()
        {
            var cfg = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            cfg.Packs = new[]
            {
                new AssetDeliveryMockConfig.SimulatedPack
                {
                    Name = "level_2", FinalStatus = AssetPackStatus.Completed,
                    DownloadDurationSeconds = 5f, TotalBytesToDownload = 500,
                }
            };
            var provider = new MockAssetDeliveryProvider(cfg);
            provider.StartListening();

            var canceledSeen = false;
            provider.OnStateUpdate += (name, s) =>
            {
                if (name == "level_2" && s.Status == AssetPackStatus.Canceled)
                    canceledSeen = true;
            };

            await provider.CancelAsync(new[] { "level_2" }, CancellationToken.None);

            Assert.IsTrue(canceledSeen, "CancelAsync must dispatch a CANCELED state event");
            provider.StopListening();
        }
    }
}
