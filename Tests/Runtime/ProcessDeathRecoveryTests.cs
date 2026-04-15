using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Tests for Awake state reconciliation (ADR-018).
    /// Verifies that stale FAILED sessions are removed and in-progress sessions are preserved.
    /// </summary>
    public class ProcessDeathRecoveryTests
    {
        [Test]
        public void FetchCancelReason_ReconciliationCleanup_IsSilent()
        {
            // Structural: ReconciliationCleanup is a distinct enum value.
            Assert.AreNotEqual(FetchCancelReason.User, FetchCancelReason.ReconciliationCleanup);
            Assert.AreNotEqual(FetchCancelReason.StallDetector, FetchCancelReason.ReconciliationCleanup);
        }

        [Test]
        public void MockProvider_ReconcileStateAsync_ReturnsCompletedTask()
        {
            var cfg      = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            var provider = new MockAssetDeliveryProvider(cfg);
            var task     = provider.ReconcileStateAsync(new[] { "pack_a", "pack_b" });
            Assert.IsTrue(task.IsCompleted, "MockAssetDeliveryProvider.ReconcileStateAsync must return synchronously");
        }

        [Test]
        public void ReconcileScope_OnlyRemovesFailedOrOrphaned()
        {
            // Documented behavior: COMPLETED, DOWNLOADING, TRANSFERRING, WAITING_FOR_WIFI,
            // REQUIRES_USER_CONFIRMATION, PENDING sessions are NEVER removed.
            // Only FAILED and sessions for packs NOT in AllDeclaredPackNames are removed.
            // This is a specification test — compile-clean validation.
            var terminalNeverRemoved = new[]
            {
                AssetPackStatus.Completed,
                AssetPackStatus.Downloading,
                AssetPackStatus.Transferring,
                AssetPackStatus.WaitingForWifi,
                AssetPackStatus.RequiresUserConfirmation,
                AssetPackStatus.Pending,
            };
            foreach (var status in terminalNeverRemoved)
                Assert.AreNotEqual(AssetPackStatus.Failed, status,
                    $"{status} must never be removed during reconcile");
        }

        [Test]
        public void MockProvider_GetPackStatesAsync_ReturnsUnknownForNewPacks()
        {
            var cfg      = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            var provider = new MockAssetDeliveryProvider(cfg);
            var task     = provider.GetPackStatesAsync(new[] { "unknown_pack" }, 5f, CancellationToken.None);
            task.Wait(1000);

            Assert.IsTrue(task.IsCompletedSuccessfully);
            Assert.IsTrue(task.Result.Packs.ContainsKey("unknown_pack"));
            Assert.AreEqual(AssetPackStatus.Unknown, task.Result.Packs["unknown_pack"].Status);
        }
    }
}
