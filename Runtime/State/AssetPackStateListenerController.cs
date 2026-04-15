namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Non-Android stub for IAssetPackStateListener (Android implementation is in Phase 4).
    /// Used on non-Android build targets to satisfy the compile-time contract.
    /// </summary>
    internal sealed class AssetPackStateListenerController : IAssetPackStateListener
    {
        public event System.Action<string, AssetPackState> OnStateUpdate;

        public void StartListening()
        {
            BizSimLogger.Verbose("AssetPackStateListenerController.StartListening (stub — Phase 4 wires JNI)");
        }

        public void StopListening()
        {
            BizSimLogger.Verbose("AssetPackStateListenerController.StopListening (stub)");
        }
    }
}
