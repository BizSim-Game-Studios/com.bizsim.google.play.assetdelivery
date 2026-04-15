using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Hidden MonoBehaviour (ADR-017, §A3). Subscribes to OnApplicationPause.
    /// On pause: stops state listener.
    /// On resume: re-registers listener, force-polls in-flight pack states,
    /// replays any events queued during the poll window (race guard).
    ///
    /// Also ticks the stall detector every frame via Update.
    ///
    /// Internal — consumers observe the effect but do not invoke the handler directly.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class AssetDeliveryUpdateHandler : MonoBehaviour
    {
        private AssetDeliveryController _controller;
        private IAssetDeliveryProvider  _provider;
        private IAssetPackStateListener _listener;
        private AssetDeliveryStallDetector _stallDetector;

        // In-flight pack names at the time of resume — set by the controller.
        private IReadOnlyCollection<string> _inFlightPackNames = System.Array.Empty<string>();

        private bool  _gettingPackStates;
        private float _pausedAt;

        // Events that arrived while _gettingPackStates == true.
        // Latest-value-wins per pack (newest state per pack wins).
        private readonly ConcurrentDictionary<string, AssetPackState> _stateUpdatesSinceGetPackStates
            = new ConcurrentDictionary<string, AssetPackState>();

        internal static AssetDeliveryUpdateHandler CreateInScene(AssetDeliveryController ctrl)
        {
            var go  = new GameObject("[AssetDeliveryUpdateHandler]")
                { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            var h              = go.AddComponent<AssetDeliveryUpdateHandler>();
            h._controller      = ctrl;
            return h;
        }

        internal void Wire(IAssetDeliveryProvider provider, IAssetPackStateListener listener,
            AssetDeliveryStallDetector stallDetector)
        {
            _provider       = provider;
            _listener       = listener;
            _stallDetector  = stallDetector;
        }

        internal void SetInFlightPackNames(IReadOnlyCollection<string> names)
            => _inFlightPackNames = names;

        private void Update()
        {
            _stallDetector?.Tick();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Pause path — stop listener, record time.
                _listener?.StopListening();
                _pausedAt = Time.unscaledTime;
                BizSimLogger.Verbose("App paused — state listener stopped.");
            }
            else
            {
                // Resume path — async to allow await
                _ = OnResumeAsync();
            }
        }

        private async Task OnResumeAsync()
        {
            BizSimLogger.Verbose("App resumed — re-registering listener + reconciling state.");

            // Step 1: Re-register listener.
            _listener?.StartListening();

            // Step 2: Poll for in-flight pack states.
            _gettingPackStates = true;
            _stateUpdatesSinceGetPackStates.Clear();

            var packList = new List<string>(_inFlightPackNames);
            if (packList.Count > 0 && _provider != null)
            {
                AssetPackStates poll;
                try
                {
                    poll = await _provider.GetPackStatesAsync(packList, 30f, CancellationToken.None);
                }
                catch (System.Exception ex)
                {
                    BizSimLogger.Warning($"OnResumeAsync: GetPackStatesAsync threw: {ex.Message}");
                    _gettingPackStates = false;
                    return;
                }

                _gettingPackStates = false;

                // Step 5: Process poll results.
                foreach (var n in packList)
                {
                    if (!poll.Packs.TryGetValue(n, out var s)) continue;

                    if (s.IsTerminal)
                    {
                        _controller?.HandleStateFromListener(n, s);
                    }
                    else if (s.Status == AssetPackStatus.Downloading
                          || s.Status == AssetPackStatus.Transferring)
                    {
                        _stallDetector?.ReArmForPack(n);
                    }
                    else if (s.RequiresConfirmation)
                    {
                        _controller?.HandleStateFromListener(n, s);
                    }
                    else if (s.Status == AssetPackStatus.Failed)
                    {
                        _controller?.HandleStateFromListener(n, s);
                    }
                }

                // Step 6: Drain the race-window queue (latest-wins).
                foreach (var kv in _stateUpdatesSinceGetPackStates)
                {
                    if (poll.Packs.TryGetValue(kv.Key, out var pollState)
                        && kv.Value.BytesDownloaded >= pollState.BytesDownloaded)
                    {
                        _controller?.HandleStateFromListener(kv.Key, kv.Value);
                    }
                }
                _stateUpdatesSinceGetPackStates.Clear();
            }
            else
            {
                _gettingPackStates = false;
            }
        }

        /// <summary>
        /// Called by the state listener proxy during the _gettingPackStates window to absorb events.
        /// Events are queued for drain after the poll completes.
        /// </summary>
        internal bool TryAbsorbStateUpdate(string packName, AssetPackState state)
        {
            if (!_gettingPackStates) return false;
            // Latest-value-wins per pack.
            _stateUpdatesSinceGetPackStates.AddOrUpdate(packName, state,
                (_, existing) => state.BytesDownloaded >= existing.BytesDownloaded ? state : existing);
            return true;
        }
    }
}
