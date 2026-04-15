using System;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Base class for JNI bridges that wrap a single Java class. Handles bridge construction,
    /// callback proxy wiring, and disposal. Derived classes provide the Java FQN and a proxy factory.
    /// </summary>
    /// <remarks>
    /// Copied from <c>com.bizsim.google.play.games/Runtime/Core/JniBridgeBase.cs</c> with namespace +
    /// logger adjusted, the games-specific <c>JniConstants.UnityPlayer</c> reference inlined, and
    /// the games-specific <c>GamesNativeBridgeException</c> replaced with <see cref="InvalidOperationException"/>
    /// (assetdelivery does not ship a dedicated native-bridge exception type).
    /// </remarks>
    internal abstract class JniBridgeBase : IDisposable
    {
        private bool _disposed;

        protected AndroidJavaObject Bridge { get; private set; }

        protected abstract string JavaClassName { get; }

        protected abstract AndroidJavaProxy CreateCallbackProxy();

        protected void InitializeBridge()
        {
            try
            {
                string shortName = JavaClassName.Substring(JavaClassName.LastIndexOf('.') + 1);
                BizSimLogger.Info($"Initializing {shortName}...");

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    Bridge = new AndroidJavaObject(JavaClassName, activity);
                    Bridge.Call("setCallback", CreateCallbackProxy());
                    BizSimLogger.Info($"{shortName} initialized successfully");
                }
            }
            catch (Exception ex)
            {
                string shortName = JavaClassName.Substring(JavaClassName.LastIndexOf('.') + 1);
                BizSimLogger.Error($"Failed to initialize {shortName}: {ex}");
                throw new InvalidOperationException($"Failed to initialize {JavaClassName}: {ex.Message}", ex);
            }
        }

        protected void CallBridge(string method, params object[] args)
        {
            Bridge.Call(method, args);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            OnDispose();

#if UNITY_ANDROID && !UNITY_EDITOR
            Bridge?.Dispose();
            Bridge = null;
#endif
        }

        protected abstract void OnDispose();
    }
}
