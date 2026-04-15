#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> implementing
    /// <c>com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IGetStatesCallback</c>.
    /// Used by <see cref="AndroidAssetDeliveryProvider.GetPackStatesAsync"/>. Method names and
    /// signatures MUST match the Java interface verbatim (case-sensitive).
    /// </summary>
    internal sealed class GetStatesCallbackProxy : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<AssetPackStates> _tcs;

        public GetStatesCallbackProxy(TaskCompletionSource<AssetPackStates> tcs)
            : base("com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IGetStatesCallback")
        {
            _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        }

        public void onStatesReceived(
            string[] packNames,
            int[]    statuses,
            int[]    errorCodes,
            long[]   bytesDownloaded,
            long[]   totalBytesToDownload)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var packs = new Dictionary<string, AssetPackState>(packNames.Length);
                    for (int i = 0; i < packNames.Length; i++)
                    {
                        packs[packNames[i]] = new AssetPackState(
                            name:                       packNames[i],
                            status:                     (AssetPackStatus)statuses[i],
                            errorCode:                  (AssetPackErrorCode)errorCodes[i],
                            bytesDownloaded:            bytesDownloaded[i],
                            totalBytesToDownload:       totalBytesToDownload[i],
                            transferProgressPercentage: 0,
                            observedAtUtc:              DateTime.UtcNow);
                    }
                    _tcs.TrySetResult(new AssetPackStates(packs, DateTime.UtcNow));
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            });
        }

        public void onStatesError(int errorCode, string message)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                _tcs.TrySetException(new AssetDeliveryException(
                    new AssetDeliveryError(
                        code:          (AssetPackErrorCode)errorCode,
                        message:       message ?? "",
                        packName:      string.Empty,
                        occurredAtUtc: DateTime.UtcNow)));
            });
        }
    }
}
#endif
