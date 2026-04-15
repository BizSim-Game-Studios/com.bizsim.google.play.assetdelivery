using System.Collections.Generic;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Hidden MonoBehaviour tick-driver for the mock provider.
    /// Advances each SimulatedPack through its timeline on Update.
    /// </summary>
    [AddComponentMenu("")]  // Hide from Add Component menu
    internal sealed class MockAssetPackStateReplayer : MonoBehaviour
    {
        // Wired by MockAssetDeliveryProvider after Instantiation.
        internal MockAssetDeliveryProvider Owner;

        private sealed class PackTimeline
        {
            internal readonly AssetDeliveryMockConfig.SimulatedPack Config;
            internal float StartTime;
            internal AssetPackStatus CurrentStatus = AssetPackStatus.Unknown;
            internal bool TcsResolved;
            internal bool WaitingForConfirmation;

            internal PackTimeline(AssetDeliveryMockConfig.SimulatedPack cfg, float now)
            {
                Config    = cfg;
                StartTime = now;
            }
        }

        private readonly Dictionary<string, PackTimeline> _timelines = new Dictionary<string, PackTimeline>();
        private readonly List<string> _toRemove = new List<string>();

        internal void BeginPack(AssetDeliveryMockConfig.SimulatedPack cfg)
        {
            _timelines[cfg.Name] = new PackTimeline(cfg, Time.unscaledTime);
        }

        internal void ConfirmPack(string packName)
        {
            if (_timelines.TryGetValue(packName, out var tl))
            {
                tl.WaitingForConfirmation = false;
                tl.StartTime             = Time.unscaledTime; // reset clock so download resumes
            }
        }

        private void Update()
        {
            if (Owner == null) return;
            _toRemove.Clear();
            float now = Time.unscaledTime;

            foreach (var kv in _timelines)
            {
                var tl  = kv.Value;
                var cfg = tl.Config;
                if (tl.WaitingForConfirmation) continue;

                float elapsed = now - tl.StartTime;
                float dur     = Mathf.Max(cfg.DownloadDurationSeconds, 0.05f);
                float xfer    = Mathf.Max(dur * 0.1f, 0.1f); // 10 % of download time for Transferring phase

                // --- State machine ---
                if (tl.CurrentStatus == AssetPackStatus.Unknown)
                {
                    // Emit PENDING immediately
                    Emit(cfg.Name, AssetPackStatus.Pending, 0, cfg.TotalBytesToDownload, 0);
                    tl.CurrentStatus = AssetPackStatus.Pending;
                }
                else if (tl.CurrentStatus == AssetPackStatus.Pending && elapsed >= 0.05f)
                {
                    // Move to DOWNLOADING (TCS resolved here — initial ack snapshot per spec)
                    long bytes = 0;
                    Emit(cfg.Name, AssetPackStatus.Downloading, bytes, cfg.TotalBytesToDownload, 0);
                    tl.CurrentStatus = AssetPackStatus.Downloading;

                    if (!tl.TcsResolved)
                    {
                        tl.TcsResolved = true;
                        Owner.ResolveTcs(cfg.Name, AssetPackStatus.Downloading);
                    }

                    // Check StopAt
                    if (cfg.StopAt == AssetPackStatus.Downloading)
                    {
                        Emit(cfg.Name, cfg.FinalStatus, bytes, cfg.TotalBytesToDownload, 0, cfg.Error);
                        _toRemove.Add(cfg.Name);
                        continue;
                    }
                    if (cfg.StopAt == AssetPackStatus.WaitingForWifi ||
                        cfg.StopAt == AssetPackStatus.RequiresUserConfirmation)
                    {
                        Emit(cfg.Name, cfg.StopAt, bytes, cfg.TotalBytesToDownload, 0);
                        tl.CurrentStatus         = cfg.StopAt;
                        tl.WaitingForConfirmation = true;
                        continue;
                    }
                }
                else if (tl.CurrentStatus == AssetPackStatus.Downloading)
                {
                    // Progress during download phase
                    float t = Mathf.Clamp01(elapsed / dur);
                    long  bytes = (long)(cfg.TotalBytesToDownload * t);
                    Emit(cfg.Name, AssetPackStatus.Downloading, bytes, cfg.TotalBytesToDownload, 0);

                    if (t >= 1f)
                    {
                        // Start transfer phase
                        Emit(cfg.Name, AssetPackStatus.Transferring, cfg.TotalBytesToDownload, cfg.TotalBytesToDownload, 0);
                        tl.CurrentStatus = AssetPackStatus.Transferring;
                        tl.StartTime     = now; // reset for transfer phase
                    }
                }
                else if (tl.CurrentStatus == AssetPackStatus.Transferring)
                {
                    float t = Mathf.Clamp01(elapsed / xfer);
                    int pct = (int)(t * 100);
                    Emit(cfg.Name, AssetPackStatus.Transferring, cfg.TotalBytesToDownload, cfg.TotalBytesToDownload, pct);

                    if (t >= 1f)
                    {
                        Emit(cfg.Name, cfg.FinalStatus == AssetPackStatus.Completed
                                ? AssetPackStatus.Completed
                                : cfg.FinalStatus,
                            cfg.TotalBytesToDownload, cfg.TotalBytesToDownload, 100, cfg.Error);
                        _toRemove.Add(cfg.Name);
                    }
                }
            }

            foreach (var k in _toRemove) _timelines.Remove(k);
        }

        private void Emit(string name, AssetPackStatus status, long bytes, long total, int pct,
            AssetPackErrorCode error = AssetPackErrorCode.NoError)
        {
            var state = new AssetPackState(name, status, error, bytes, total, pct, System.DateTime.UtcNow);
            Owner?.DispatchStateUpdate(name, state);
        }
    }
}
