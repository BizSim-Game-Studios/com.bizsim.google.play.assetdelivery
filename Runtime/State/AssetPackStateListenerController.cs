namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Manages the lifetime of the Java-side <c>AssetPackStateUpdateListener</c> and forwards
    /// state-update events to subscribers. On non-Android builds this is a no-op stub; the Android
    /// branch wires <see cref="AssetPackStateListenerProxy"/> to the JNI bridge.
    /// </summary>
    internal sealed class AssetPackStateListenerController : IAssetPackStateListener
    {
        public event System.Action<string, AssetPackState> OnStateUpdate;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool _started;
        private static readonly object _startLock = new object();
        private AssetPackStateListenerProxy _proxy;

        public void StartListening()
        {
            lock (_startLock)
            {
                if (_started) return;

                var bridge = AndroidAssetDeliveryProvider.GetBridge();
                if (bridge == null)
                {
                    BizSimLogger.Warning("AssetPackStateListenerController: bridge null — cannot register state listener");
                    return;
                }

                _proxy = new AssetPackStateListenerProxy();
                _proxy.OnStateUpdate += (packName, state) => OnStateUpdate?.Invoke(packName, state);
                bridge.Call("setStateUpdateCallback", _proxy);
                _started = true;
                BizSimLogger.Verbose("AssetPackStateListenerController: state listener registered");
            }
        }

        public void StopListening()
        {
            lock (_startLock)
            {
                if (!_started) return;

                var bridge = AndroidAssetDeliveryProvider.GetBridge();
                bridge?.Call("unregisterStateListener");
                _proxy = null;
                _started = false;
                BizSimLogger.Verbose("AssetPackStateListenerController: state listener unregistered");
            }
        }
#else
        public void StartListening()
        {
            BizSimLogger.Verbose("AssetPackStateListenerController.StartListening (stub)");
        }

        public void StopListening()
        {
            BizSimLogger.Verbose("AssetPackStateListenerController.StopListening (stub)");
        }
#endif
    }
}
