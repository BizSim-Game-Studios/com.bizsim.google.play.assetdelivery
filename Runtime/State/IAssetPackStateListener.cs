namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Receives push-mode state updates from the Google Play Asset Delivery listener.
    /// In the mock, both interfaces are implemented by MockAssetDeliveryProvider.
    /// On Android, a dedicated AndroidJavaProxy implementation raises the event on the main thread
    /// via UnityMainThreadDispatcher.
    /// </summary>
    public interface IAssetPackStateListener
    {
        /// <summary>Fired on the Unity main thread for every AssetPackState transition.</summary>
        event System.Action<string, AssetPackState> OnStateUpdate;

        void StartListening();
        void StopListening();
    }
}
