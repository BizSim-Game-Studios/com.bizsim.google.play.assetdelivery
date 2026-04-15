using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Regression guard for the IAssetDeliveryAnalyticsAdapter interface (ADR-027).
    /// Asserts EXACTLY 12 methods and that no payload struct contains user/device/ad ID fields.
    /// Changing the method count or adding PII fields fails this test, forcing a deliberate SemVer bump.
    /// </summary>
    public class AnalyticsEventCatalogTests
    {
        [Test]
        public void IAssetDeliveryAnalyticsAdapter_HasExactly12Methods()
        {
            var methods = typeof(IAssetDeliveryAnalyticsAdapter)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName)
                .ToArray();
            Assert.AreEqual(12, methods.Length,
                "IAssetDeliveryAnalyticsAdapter must have EXACTLY 12 methods (ADR-027 version lock). " +
                $"Found: {string.Join(", ", methods.Select(m => m.Name))}");
        }

        [Test]
        public void AllEventMethodNames_StartWithPad_Underscore()
        {
            var methods = typeof(IAssetDeliveryAnalyticsAdapter)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName);
            foreach (var m in methods)
                StringAssert.StartsWith("pad_", m.Name,
                    $"All analytics method names must start with 'pad_' (ADR-027). Found: {m.Name}");
        }

        private static readonly string[] ForbiddenFieldNames =
        {
            "UserId", "DeviceId", "AdId", "AdvertisingId", "UserEmail"
        };

        [Test]
        public void PayloadStructs_ContainNoPiiFields()
        {
            var payloadTypes = new[]
            {
                typeof(PadFetchStartedEvent),
                typeof(PadFetchSucceededEvent),
                typeof(PadFetchFailedEvent),
                typeof(PadFetchCanceledEvent),
                typeof(PadRetryEvent),
                typeof(PadStallDetectedEvent),
                typeof(PadPreflightBlockedEvent),
                typeof(PadCellularConsentShownEvent),
                typeof(PadCellularConsentAcceptedEvent),
                typeof(PadCellularConsentDeniedEvent),
                typeof(PadPackRemovedEvent),
                typeof(PadBundleLoadedEvent),
            };

            foreach (var t in payloadTypes)
            {
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    foreach (var forbidden in ForbiddenFieldNames)
                        Assert.AreNotEqual(forbidden, f.Name,
                            $"PII field '{forbidden}' found in {t.Name} — privacy-by-design violation (ADR-027).");
                }
            }
        }

        [Test]
        public void AllTwelveEventNames_AreDistinct()
        {
            var methods = typeof(IAssetDeliveryAnalyticsAdapter)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.Name)
                .ToArray();
            var distinct = methods.Distinct().ToArray();
            Assert.AreEqual(methods.Length, distinct.Length, "Event method names must be distinct");
        }
    }
}
