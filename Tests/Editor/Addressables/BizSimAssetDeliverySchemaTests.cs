// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.

#if BIZSIM_ADDRESSABLES
using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Editor.EditorTests
{
    /// <summary>
    /// Tests for <see cref="BizSimAssetDeliverySchema"/>.
    /// </summary>
    [TestFixture]
    public sealed class BizSimAssetDeliverySchemaTests
    {
        // ─── Default value tests ──────────────────────────────────────────────────

        [Test]
        public void DefaultDeliveryMode_IsOnDemand()
        {
            var schema = UnityEngine.ScriptableObject.CreateInstance<BizSimAssetDeliverySchema>();
            Assert.AreEqual(BizSimAddressablesDeliveryMode.OnDemand, schema.DeliveryMode);
            UnityEngine.Object.DestroyImmediate(schema);
        }

        [Test]
        public void DefaultUsePlayCoreFetch_IsTrue()
        {
            var schema = UnityEngine.ScriptableObject.CreateInstance<BizSimAssetDeliverySchema>();
            Assert.IsTrue(schema.UsePlayCoreFetch);
            UnityEngine.Object.DestroyImmediate(schema);
        }

        [Test]
        public void DefaultPackName_IsEmpty()
        {
            var schema = UnityEngine.ScriptableObject.CreateInstance<BizSimAssetDeliverySchema>();
            Assert.IsEmpty(schema.PackName);
            UnityEngine.Object.DestroyImmediate(schema);
        }

        // ─── Pack name sanitization tests ────────────────────────────────────────

        [TestCase("My Group",          "MyGroup")]
        [TestCase("Group-123",         "Group123")]
        [TestCase("123Group",          "Group123Group")]
        [TestCase("",                  "Group")]
        [TestCase("valid_name",        "valid_name")]
        [TestCase("!@#",               "Group")]
        [TestCase("_underscore",       "Group_underscore")]  // leading underscore → digit-like, prepend Group
        public void SanitizePackName_ReturnsValidIdentifier(string input, string expected)
        {
            var result = BizSimAssetDeliverySchema.SanitizePackName(input);
            Assert.AreEqual(expected, result);
            Assert.IsNotEmpty(result);
            Assert.IsTrue(char.IsLetter(result[0]),
                $"Pack name '{result}' must start with a letter.");
        }

        // ─── ResolvedPackName tests ───────────────────────────────────────────────

        [Test]
        public void ResolvedPackName_WhenPackNameEmpty_UsesGroupName()
        {
            var schema = UnityEngine.ScriptableObject.CreateInstance<BizSimAssetDeliverySchema>();
            schema.PackName = "";
            var resolved = schema.ResolvedPackName("My Group");
            Assert.AreEqual("MyGroup", resolved);
            UnityEngine.Object.DestroyImmediate(schema);
        }

        [Test]
        public void ResolvedPackName_WhenPackNameSet_UsesThatName()
        {
            var schema = UnityEngine.ScriptableObject.CreateInstance<BizSimAssetDeliverySchema>();
            schema.PackName = "customPackName";
            var resolved = schema.ResolvedPackName("My Group");
            Assert.AreEqual("customPackName", resolved);
            UnityEngine.Object.DestroyImmediate(schema);
        }
    }
}
#endif // BIZSIM_ADDRESSABLES
