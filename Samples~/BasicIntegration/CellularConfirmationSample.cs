using System;
using UnityEngine;
using UnityEngine.UI;
using BizSim.Google.Play.AssetDelivery;

namespace BizSim.Google.Play.AssetDelivery.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates the WaitingForWifi / RequiresUserConfirmation handling flow.
    ///
    /// When a large pack requires cellular consent, this sample:
    ///   1. Detects the WaitingForWifi / RequiresUserConfirmation status.
    ///   2. Calls ShowConfirmationDialogAsync (Google's native dialog).
    ///   3. Logs the result (Ok = user allowed; Canceled = user denied).
    ///
    /// Setup:
    ///   1. Set _largePackName to a pack that triggers the cellular-consent flow.
    ///   2. Use Mock_WaitingForWifi_Accepted or Mock_WaitingForWifi_Denied preset.
    ///   3. Wire _fetchButton, _statusLabel, and optionally _resultLabel.
    /// </summary>
    public sealed class CellularConfirmationSample : MonoBehaviour
    {
        [SerializeField] private string _largePackName = "hd_textures";
        [SerializeField] private Button _fetchButton;
        [SerializeField] private Text   _statusLabel;
        [SerializeField] private Text   _resultLabel;

        // Re-entry guard: OnPackStateChanged fires multiple times for WaitingForWifi
        // as Play Core refreshes state. Without this guard we'd open duplicate dialogs.
        private bool _confirmationInProgress;

        private void Awake()
        {
            AssetDeliveryController.Instance.OnPackStateChanged += OnState;
            if (_fetchButton != null)
                _fetchButton.onClick.AddListener(StartFetch);
        }

        private void OnDestroy()
        {
            if (AssetDeliveryController.Instance != null)
                AssetDeliveryController.Instance.OnPackStateChanged -= OnState;
        }

        private async void StartFetch()
        {
            SetStatus($"Fetching {_largePackName}...");
            try
            {
                // Kick off the fetch — the state stream handles consent flow.
                await AssetDeliveryController.Instance
                    .FetchAsync(new[] { _largePackName });
                SetStatus($"{_largePackName}: fetch complete.");
            }
            catch (OperationCanceledException)
            {
                SetStatus($"{_largePackName}: fetch canceled (user denied consent).");
            }
            catch (Exception ex)
            {
                SetStatus($"Fetch failed: {ex.Message}");
            }
        }

        private async void OnState(string name, AssetPackState state)
        {
            if (name != _largePackName) return;

            SetStatus($"{name}: {state.Status}");

            if ((state.Status == AssetPackStatus.WaitingForWifi ||
                 state.Status == AssetPackStatus.RequiresUserConfirmation) &&
                !_confirmationInProgress)
            {
                _confirmationInProgress = true;
                try
                {
                    // Show a custom UI here before calling the system dialog, e.g.:
                    //   await MyUiDialog.ShowAsync("Download over cellular? (~500 MB)");
                    // For sample brevity we call the system dialog immediately.
                    var result = await AssetDeliveryController.Instance
                        .ShowConfirmationDialogAsync();

                    var msg = result == ActivityResultCode.Ok
                        ? "User ALLOWED cellular download."
                        : "User DENIED cellular download.";

                    if (_resultLabel != null) _resultLabel.text = msg;
                    Debug.Log($"[CellularConfirmationSample] {msg}");
                }
                catch (Exception ex)
                {
                    // IMPORTANT: ShowConfirmationDialogAsync can fault its Task
                    // (e.g., CONFIRMATION_NOT_REQUIRED if no packs await consent,
                    // or NETWORK_UNRESTRICTED if the connection changed to WiFi
                    // between the state event and the dialog call). We're in
                    // `async void` so an unhandled exception becomes an
                    // UnobservedTaskException — try/catch is MANDATORY here.
                    Debug.LogWarning(
                        $"[CellularConfirmationSample] ShowConfirmationDialogAsync faulted: {ex.Message}");
                }
                finally
                {
                    _confirmationInProgress = false;
                }
            }
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null)
                _statusLabel.text = msg;
            Debug.Log($"[CellularConfirmationSample] {msg}");
        }
    }
}
