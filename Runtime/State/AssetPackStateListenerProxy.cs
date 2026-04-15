#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> implementing
    /// <c>com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IStateUpdateCallback</c>.
    /// Wired by <see cref="AssetPackStateListenerController"/> when running on Android. Reconstructs
    /// an <see cref="AssetPackState"/> from the six primitive arguments and raises
    /// <see cref="OnStateUpdate"/> on the Unity main thread.
    /// </summary>
    internal sealed class AssetPackStateListenerProxy : AndroidJavaProxy
    {
        public event Action<string, AssetPackState> OnStateUpdate;

        public AssetPackStateListenerProxy()
            : base("com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IStateUpdateCallback")
        {
        }

        // Signature must match Java IStateUpdateCallback.onStateUpdate exactly.
        public void onStateUpdate(
            string packName,
            int    status,
            int    errorCode,
            long   bytesDownloaded,
            long   totalBytesToDownload,
            int    transferProgressPercentage)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var state = new AssetPackState(
                    name:                       packName,
                    status:                     (AssetPackStatus)status,
                    errorCode:                  (AssetPackErrorCode)errorCode,
                    bytesDownloaded:            bytesDownloaded,
                    totalBytesToDownload:       totalBytesToDownload,
                    transferProgressPercentage: transferProgressPercentage,
                    observedAtUtc:              DateTime.UtcNow);
                OnStateUpdate?.Invoke(packName, state);
            });
        }
    }
}
#endif
