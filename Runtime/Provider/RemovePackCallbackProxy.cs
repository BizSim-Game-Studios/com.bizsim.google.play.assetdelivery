#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> implementing
    /// <c>com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IRemovePackCallback</c>.
    /// Used by <see cref="AndroidAssetDeliveryProvider.RemovePackAsync"/>.
    /// </summary>
    internal sealed class RemovePackCallbackProxy : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<bool> _tcs;

        public RemovePackCallbackProxy(TaskCompletionSource<bool> tcs)
            : base("com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IRemovePackCallback")
        {
            _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        }

        public void onRemoveInvoked(string packName)
        {
            UnityMainThreadDispatcher.Enqueue(() => _tcs.TrySetResult(true));
        }

        public void onRemoveError(int errorCode, string message)
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
