// Test-only injection helpers — internal visibility.
// InternalsVisibleTo grants access to the test assembly declared in AssemblyInfo.cs.
namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Editor / test-only bridge that allows NUnit tests to inject settings and read the
    /// current provider from AssetDeliveryController without touching serialized fields.
    /// In player builds these methods are compile-time no-ops (the test assembly is not included).
    /// </summary>
    internal static class AssetDeliveryTestInjector
    {
        private static AssetDeliverySettings  _injectedSettings;
        private static IAssetDeliveryProvider _injectedProvider;

        internal static void SetSettings(AssetDeliverySettings s) => _injectedSettings = s;
        internal static AssetDeliverySettings GetSettings()        => _injectedSettings;
        internal static void ClearSettings()                        => _injectedSettings = null;

        internal static void SetProvider(IAssetDeliveryProvider p) => _injectedProvider = p;
        internal static IAssetDeliveryProvider GetCurrentProvider() => _injectedProvider;
        internal static void ClearProvider()                         => _injectedProvider = null;

        internal static void Reset()
        {
            _injectedSettings = null;
            _injectedProvider = null;
        }
    }
}
