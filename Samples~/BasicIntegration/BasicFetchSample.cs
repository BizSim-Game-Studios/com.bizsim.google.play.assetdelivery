using System;
using UnityEngine;
using UnityEngine.UI;
using BizSim.Google.Play.AssetDelivery;

namespace BizSim.Google.Play.AssetDelivery.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates on-demand pack fetching with per-pack progress bar.
    ///
    /// Setup:
    ///   1. Add this component to a GameObject in the scene.
    ///   2. Wire _progressBar, _statusLabel, _fetchButton, _cancelButton, _removeButton.
    ///   3. Set _packName to the pack declared in your host project's build.gradle.
    ///   4. Assign a MockConfig (e.g., Mock_OnDemand_Success_Small) to AssetDeliveryController.
    /// </summary>
    public sealed class BasicFetchSample : MonoBehaviour
    {
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text   _statusLabel;
        [SerializeField] private Button _fetchButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _removeButton;

        [SerializeField] private string _packName = "level_boss_1";

        private bool _fetching;

        private void Awake()
        {
            if (_fetchButton  != null) _fetchButton.onClick.AddListener(OnFetch);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(OnCancel);
            if (_removeButton != null) _removeButton.onClick.AddListener(OnRemove);

            AssetDeliveryController.Instance.OnPackStateChanged += OnPackState;
            SetStatus("Ready — press Fetch to start.");
        }

        private void OnDestroy()
        {
            if (AssetDeliveryController.Instance != null)
                AssetDeliveryController.Instance.OnPackStateChanged -= OnPackState;
        }

        // ─── Button handlers ──────────────────────────────────────────────────────

        private async void OnFetch()
        {
            if (_fetching) return;
            _fetching = true;
            SetStatus($"Fetching {_packName}...");
            if (_progressBar != null) _progressBar.value = 0f;

            try
            {
                var snapshot = await AssetDeliveryController.Instance
                    .FetchAsync(new[] { _packName });

                if (snapshot.AllCompleted)
                    SetStatus($"{_packName}: ready!");
                else
                    SetStatus($"{_packName}: fetch ended — check state.");
            }
            catch (OperationCanceledException)
            {
                SetStatus($"{_packName}: fetch canceled.");
            }
            catch (Exception ex)
            {
                SetStatus($"Fetch failed: {ex.Message}");
            }
            finally
            {
                _fetching = false;
            }
        }

        private async void OnCancel()
        {
            try
            {
                await AssetDeliveryController.Instance
                    .CancelAsync(new[] { _packName });
                SetStatus($"{_packName}: canceled.");
            }
            catch (Exception ex)
            {
                SetStatus($"Cancel failed: {ex.Message}");
            }
        }

        private async void OnRemove()
        {
            try
            {
                await AssetDeliveryController.Instance
                    .RemovePackAsync(_packName);
                SetStatus($"{_packName}: removed from device.");
                if (_progressBar != null) _progressBar.value = 0f;
            }
            catch (Exception ex)
            {
                SetStatus($"Remove failed: {ex.Message}");
            }
        }

        // ─── State callback ───────────────────────────────────────────────────────

        private void OnPackState(string name, AssetPackState state)
        {
            if (name != _packName) return;

            if (_progressBar != null)
                _progressBar.value = state.DownloadProgress;

            SetStatus($"{_packName}: {state.Status}" +
                (state.Error != AssetPackErrorCode.NoError
                    ? $" (error {state.Error})"
                    : string.Empty));
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null)
                _statusLabel.text = msg;
            Debug.Log($"[BasicFetchSample] {msg}");
        }
    }
}
