#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> implementing
    /// <c>com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IConfirmationDialogCallback</c>.
    /// Used by <see cref="AndroidAssetDeliveryProvider.ShowConfirmationDialogAsync"/>. Per Q9 /
    /// ADR-005, no fragment shim is required — the dialog is shown via a plain <c>Activity</c>
    /// reference fetched from <c>UnityPlayer.currentActivity</c>.
    /// </summary>
    internal sealed class ConfirmationDialogCallbackProxy : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<ActivityResultCode> _tcs;

        public ConfirmationDialogCallbackProxy(TaskCompletionSource<ActivityResultCode> tcs)
            : base("com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IConfirmationDialogCallback")
        {
            _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        }

        // resultCode: Activity.RESULT_OK (-1) or Activity.RESULT_CANCELED (0)
        public void onDialogResult(int resultCode)
        {
            UnityMainThreadDispatcher.Enqueue(() => _tcs.TrySetResult((ActivityResultCode)resultCode));
        }

        public void onDialogError(int errorCode, string message)
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
