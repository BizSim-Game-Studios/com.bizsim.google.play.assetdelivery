// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.

#if BIZSIM_ADDRESSABLES
using System.Collections.Generic;
using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Tests for <see cref="BizSimAddressablesRuntimeData"/>.
    /// </summary>
    [TestFixture]
    public sealed class BizSimAddressablesRuntimeDataTests
    {
        [SetUp]
        public void SetUp()
        {
            BizSimAddressablesRuntimeData.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            BizSimAddressablesRuntimeData.Reset();
        }

        // ─── Empty / uninitialized state ──────────────────────────────────────────

        [Test]
        public void Instance_BeforePopulate_IsNotInitialized()
        {
            Assert.IsFalse(BizSimAddressablesRuntimeData.Instance.Initialized);
        }

        [Test]
        public void Instance_BeforePopulate_HasEmptyBundleMap()
        {
            Assert.AreEqual(0,
                BizSimAddressablesRuntimeData.Instance.AssetBundleNameToStablePackId.Count);
        }

        // ─── After Populate ───────────────────────────────────────────────────────

        [Test]
        public void Populate_SetsInitializedTrue()
        {
            BizSimAddressablesRuntimeData.Instance.Populate(new Dictionary<string, string>());
            Assert.IsTrue(BizSimAddressablesRuntimeData.Instance.Initialized);
        }

        [Test]
        public void Populate_WithEntries_BundleLookupSucceeds()
        {
            var map = new Dictionary<string, string>
            {
                { "assets_all_9c4c6d7e2f3b1a00", "myOnDemandPack" },
                { "catalog_2026",                 "installTimePack" },
            };
            BizSimAddressablesRuntimeData.Instance.Populate(map);

            var lookup = BizSimAddressablesRuntimeData.Instance.AssetBundleNameToStablePackId;
            Assert.AreEqual("myOnDemandPack",  lookup["assets_all_9c4c6d7e2f3b1a00"]);
            Assert.AreEqual("installTimePack", lookup["catalog_2026"]);
        }

        [Test]
        public void Populate_Twice_OverwritesPreviousEntries()
        {
            BizSimAddressablesRuntimeData.Instance.Populate(
                new Dictionary<string, string> { { "bundleA", "packOld" } });
            BizSimAddressablesRuntimeData.Instance.Populate(
                new Dictionary<string, string> { { "bundleB", "packNew" } });

            var lookup = BizSimAddressablesRuntimeData.Instance.AssetBundleNameToStablePackId;
            Assert.IsFalse(lookup.ContainsKey("bundleA"), "Old entry must be cleared on re-populate.");
            Assert.AreEqual("packNew", lookup["bundleB"]);
        }

        [Test]
        public void Populate_NullDictionary_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                BizSimAddressablesRuntimeData.Instance.Populate(null));
            Assert.IsTrue(BizSimAddressablesRuntimeData.Instance.Initialized);
        }

        // ─── StablePackIdToLocalPath ──────────────────────────────────────────────

        [Test]
        public void StablePackIdToLocalPath_InitiallyEmpty()
        {
            Assert.AreEqual(0,
                BizSimAddressablesRuntimeData.Instance.StablePackIdToLocalPath.Count);
        }

        [Test]
        public void StablePackIdToLocalPath_CanBePopulatedAtRuntime()
        {
            var data = BizSimAddressablesRuntimeData.Instance;
            data.StablePackIdToLocalPath["myPack"] = "/sdcard/myPack";
            Assert.AreEqual("/sdcard/myPack", data.StablePackIdToLocalPath["myPack"]);
        }
    }
}
#endif // BIZSIM_ADDRESSABLES
