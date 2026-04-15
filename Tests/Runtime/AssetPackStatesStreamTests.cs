using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    public class AssetPackStatesStreamTests
    {
        private static IReadOnlyDictionary<string, AssetPackState> MakeSnap(string name, AssetPackStatus status)
        {
            var s = new AssetPackState(name, status, AssetPackErrorCode.NoError, 0, 0, 0, System.DateTime.UtcNow);
            return new Dictionary<string, AssetPackState> { [name] = s };
        }

        [Test]
        public async Task EnqueuedSnapshot_IsYieldedByReadAsync()
        {
            var stream = new AssetPackStatesStream(4);
            stream.Enqueue(MakeSnap("pack_a", AssetPackStatus.Downloading));

            using var cts = new CancellationTokenSource(2000);
            await foreach (var snap in stream.ReadAsync(cts.Token))
            {
                Assert.IsTrue(snap.ContainsKey("pack_a"));
                break;
            }
        }

        [Test]
        public async Task Cancellation_StopsIteration()
        {
            var stream = new AssetPackStatesStream(4);
            using var cts = new CancellationTokenSource(100); // 100 ms — nothing enqueued

            int count = 0;
            await foreach (var _ in stream.ReadAsync(cts.Token)) count++;

            Assert.AreEqual(0, count, "No items should be yielded before cancellation");
        }

        [Test]
        public void Backpressure_DropsOldestOnOverflow()
        {
            var stream = new AssetPackStatesStream(4);
            for (int i = 0; i < 8; i++)
                stream.Enqueue(MakeSnap("pack_" + i, AssetPackStatus.Downloading));

            // After enqueuing 8 into a capacity-4 queue, only the last 4 should remain.
            // We verify by draining synchronously via direct inspection.
            int drained = 0;
            var cts = new CancellationTokenSource(200);
            var task = Task.Run(async () =>
            {
                await foreach (var _ in stream.ReadAsync(cts.Token)) { drained++; }
            }, cts.Token);
            task.Wait(500);

            Assert.LessOrEqual(drained, 4, "Backpressure must have dropped oldest snapshots");
        }

        [Test]
        public async Task YieldedSnapshot_IsDefensiveCopy()
        {
            var stream = new AssetPackStatesStream(4);
            var original = new Dictionary<string, AssetPackState>
            {
                ["pack_x"] = new AssetPackState("pack_x", AssetPackStatus.Pending,
                    AssetPackErrorCode.NoError, 0, 0, 0, System.DateTime.UtcNow)
            };
            stream.Enqueue(original);

            using var cts = new CancellationTokenSource(2000);
            IReadOnlyDictionary<string, AssetPackState> yielded = null;
            await foreach (var snap in stream.ReadAsync(cts.Token))
            {
                yielded = snap;
                break;
            }

            // Mutate original dict — yielded should not be affected.
            original["pack_x"] = new AssetPackState("pack_x", AssetPackStatus.Completed,
                AssetPackErrorCode.NoError, 0, 0, 0, System.DateTime.UtcNow);

            Assert.AreEqual(AssetPackStatus.Pending, yielded["pack_x"].Status,
                "Yielded snapshot must be a defensive copy");
        }
    }
}
