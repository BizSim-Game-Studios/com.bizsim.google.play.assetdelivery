// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.

#if BIZSIM_ADDRESSABLES
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor.EditorTests
{
    /// <summary>
    /// Tests for <see cref="BuildScriptBizSimAssetDelivery"/>.
    /// </summary>
    [TestFixture]
    public sealed class BuildScriptBizSimAssetDeliveryTests
    {
        // ─── Name property ────────────────────────────────────────────────────────

        [Test]
        public void Name_Returns_BizSimAssetDelivery()
        {
            var script = ScriptableObject.CreateInstance<BuildScriptBizSimAssetDelivery>();
            Assert.AreEqual("BizSim Asset Delivery", script.Name);
            Object.DestroyImmediate(script);
        }

        // ─── CreateAssetMenu attribute presence ───────────────────────────────────

        [Test]
        public void CreateAssetMenu_Attribute_IsPresent()
        {
            var attr = typeof(BuildScriptBizSimAssetDelivery)
                .GetCustomAttribute<CreateAssetMenuAttribute>();
            Assert.IsNotNull(attr, "CreateAssetMenuAttribute must be present on BuildScriptBizSimAssetDelivery.");
        }

        [Test]
        public void CreateAssetMenu_MenuName_ContainsBizSim()
        {
            var attr = typeof(BuildScriptBizSimAssetDelivery)
                .GetCustomAttribute<CreateAssetMenuAttribute>();
            Assert.IsNotNull(attr);
            StringAssert.Contains("BizSim", attr.menuName,
                "Menu name must contain 'BizSim' so it is discoverable.");
        }

        // ─── Constants ────────────────────────────────────────────────────────────

        [Test]
        public void PackMapDirectory_IsUnderStreamingAssets()
        {
            StringAssert.StartsWith("Assets/StreamingAssets",
                BuildScriptBizSimAssetDelivery.PackMapDirectory);
        }

        [Test]
        public void PackMapFileName_IsAddressablesPackMapJson()
        {
            Assert.AreEqual("AddressablesPackMap.json",
                BuildScriptBizSimAssetDelivery.PackMapFileName);
        }
    }
}
#endif // BIZSIM_ADDRESSABLES
