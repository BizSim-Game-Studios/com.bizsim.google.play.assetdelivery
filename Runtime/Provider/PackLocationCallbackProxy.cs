#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> implementing
    /// <c>com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IPackLocationCallback</c>.
    /// Used by <see cref="AndroidAssetDeliveryProvider.GetPackLocationAsync"/>. Note that
    /// <c>getPackLocation</c> on the Java side is synchronous — the callback is invoked
    /// synchronously on the calling thread (not via mainHandler.post) so that the TCS resolves
    /// before the C# caller's await returns.
    /// </summary>
    internal sealed class PackLocationCallbackProxy : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<AssetPackLocation?> _tcs;

        public PackLocationCallbackProxy(TaskCompletionSource<AssetPackLocation?> tcs)
            : base("com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$IPackLocationCallback")
        {
            _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        }

        public void onLocationReceived(string packName, string path, string assetsPath, int storageMethod)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var loc = new AssetPackLocation(
                    packName:      packName,
                    path:          path ?? "",
                    assetsPath:    assetsPath ?? "",
                    storageMethod: (AssetPackStorageMethod)storageMethod);
                _tcs.TrySetResult(loc);
            });
        }

        public void onNotInstalled(string packName)
        {
            UnityMainThreadDispatcher.Enqueue(() => _tcs.TrySetResult(null));
        }
    }
}
#endif
