using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Per-pack BytesDownloaded watchdog (ADR-016, §A2).
    /// Guards R-PAD-3. Fires HandleStallAsync when a pack's BytesDownloaded has not advanced
    /// for StallTimeoutSeconds while Status == DOWNLOADING.
    ///
    /// Feature Interaction contracts:
    ///   I1 (B1): sets _cancelReasons[pack] = StallDetector before cancelling.
    ///   I2 (B4): honors _isPausedCallback — does NOT fire while cellular dialog is up.
    ///   I9:      ReArmForPack seeds _lastAdvanceTime so the watchdog gets a fresh grace period
    ///            on reconcile / retry / foreground-resume.
    ///
    /// Tick() is called from AssetDeliveryUpdateHandler.Update (MonoBehaviour.Update proxy).
    /// All time values are injected via _nowProvider for testability — no real time.clock calls inside.
    /// </summary>
    internal sealed class AssetDeliveryStallDetector
    {
        private readonly Func<float>             _nowProvider;
        private readonly Func<string, Task>      _cancelCallback;
        private readonly Func<string, bool>      _isPausedCallback;

        private readonly ConcurrentDictionary<string, float> _lastAdvanceTime = new();
        private readonly ConcurrentDictionary<string, long>  _lastBytes       = new();
        private readonly HashSet<string>                      _stallFired      = new HashSet<string>();

        private float _stallTimeoutSeconds = 30f;

        public AssetDeliveryStallDetector(
            Func<float>        nowProvider,
            Func<string, Task> cancelCallback,
            Func<string, bool> isPausedCallback)
        {
            _nowProvider      = nowProvider;
            _cancelCallback   = cancelCallback;
            _isPausedCallback = isPausedCallback;
        }

        /// <summary>Configures the stall timeout (from settings, range 5-300).</summary>
        public void Configure(float stallTimeoutSeconds)
            => _stallTimeoutSeconds = Math.Max(5f, Math.Min(300f, stallTimeoutSeconds));

        /// <summary>Called by state listener on each DOWNLOADING state update.</summary>
        public void Track(string packName, long bytesDownloaded)
        {
            float now = _nowProvider();
            // If bytes have advanced, refresh the advance time.
            if (_lastBytes.TryGetValue(packName, out var prev) && bytesDownloaded > prev)
                _lastAdvanceTime[packName] = now;
            _lastBytes[packName] = bytesDownloaded;
            if (!_lastAdvanceTime.ContainsKey(packName))
                _lastAdvanceTime[packName] = now;
        }

        /// <summary>Seed a fresh grace period — called by reconcile, retry, and foreground-resume (I9).</summary>
        public void ReArmForPack(string packName)
        {
            _lastAdvanceTime[packName] = _nowProvider();
            _stallFired.Remove(packName);
        }

        /// <summary>Stop watching a pack (called when pack reaches terminal state).</summary>
        public void StopTracking(string packName)
        {
            _lastAdvanceTime.TryRemove(packName, out _);
            _lastBytes.TryRemove(packName, out _);
            _stallFired.Remove(packName);
        }

        /// <summary>Number of packs currently being tracked (test helper).</summary>
        public int Count => _lastAdvanceTime.Count;

        /// <summary>Called from MonoBehaviour.Update each frame.</summary>
        public void Tick()
        {
            float now = _nowProvider();
            foreach (var kv in _lastAdvanceTime)
            {
                var packName = kv.Key;
                // I2: Honor pause contract — skip if paused (cellular dialog up etc.)
                if (_isPausedCallback(packName)) continue;
                // Already fired for this stall episode
                if (_stallFired.Contains(packName)) continue;

                float elapsed = now - kv.Value;
                if (elapsed >= _stallTimeoutSeconds)
                {
                    _stallFired.Add(packName);
                    BizSimLogger.Warning(
                        $"Stall detected for '{packName}': no progress for {elapsed:F1}s. Cancelling.");
                    _ = _cancelCallback(packName); // fire-and-forget; controller handles cleanup
                }
            }
        }
    }
}
