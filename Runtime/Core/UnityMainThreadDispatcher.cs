using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Marshals background-thread work (Java bridge callbacks, Task continuations) back to the
    /// Unity main thread so consumer event handlers always fire on main thread. Auto-initialized
    /// at <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>.
    /// </summary>
    /// <remarks>
    /// Copied from <c>com.bizsim.google.play.games/Runtime/Core/UnityMainThreadDispatcher.cs</c>
    /// with namespace + logger adjusted for the assetdelivery package. Keep in sync with sibling
    /// packages — breaking drift across packages is a workspace-wide reliability risk.
    /// </remarks>
    [DefaultExecutionOrder(-1000)]
    internal class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static volatile UnityMainThreadDispatcher _instance;
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
        private static bool _isQuitting;
        private static int _mainThreadId;

        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        public static void Enqueue(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (_isQuitting) return;

            _executionQueue.Enqueue(action);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null || _isQuitting) return;

            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _isQuitting = false;
            Application.quitting += () => _isQuitting = true;

            var go = new GameObject("[UnityMainThreadDispatcher]");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            while (_executionQueue.TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    BizSimLogger.Error($"Main thread callback error: {e}");
                }
            }
        }
    }
}
