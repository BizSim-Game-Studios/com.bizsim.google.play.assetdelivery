using UnityEditor;
using UnityEngine;
using BizSim.Google.Play.AssetDelivery;

namespace BizSim.Google.Play.AssetDelivery.Samples.MockPresets
{
    /// <summary>
    /// Editor helper that materializes all 14 mock preset ScriptableObject assets
    /// under Assets/BizSim/AssetDelivery/MockPresets/.
    ///
    /// Invoke via:  BizSim > Google Play > Asset Delivery > Create Mock Presets
    ///          or: Assets > Create > BizSim > Google Play > AssetDelivery > Mock Presets
    /// </summary>
    public static class CreateMockPresets
    {
        private const string OUTPUT_DIR = "Assets/BizSim/AssetDelivery/MockPresets";

        [MenuItem("BizSim/Google Play/Asset Delivery/Create Mock Presets", false, 200)]
        [MenuItem("Assets/Create/BizSim/Google Play/AssetDelivery/Mock Presets", false, 200)]
        public static void CreateAll()
        {
            EnsureFolders();

            // ── 1. Mock_InstallTimeOnly ───────────────────────────────────────────
            // Tests the install-time short-circuit path (Q4): FetchAsync resolves
            // immediately with COMPLETED for any pack marked IsInstallTime = true.
            Create("Mock_InstallTimeOnly", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "core_assets",
                        FinalStatus        = AssetPackStatus.Completed,
                        DownloadDurationSeconds = 0f,
                        TotalBytesToDownload = 0L,
                        IsInstallTime      = true,
                    }
                };
            });

            // ── 2. Mock_FastFollow_Success ────────────────────────────────────────
            // Simulates a fast-follow pack that auto-downloads shortly after install.
            Create("Mock_FastFollow_Success", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "tutorial_pack",
                        FinalStatus        = AssetPackStatus.Completed,
                        DownloadDurationSeconds = 3f,
                        TotalBytesToDownload = 5_000_000L,
                    }
                };
                cfg.GetStatesLatencyMs = 30f;
            });

            // ── 3. Mock_OnDemand_Success_Small ────────────────────────────────────
            // Fast happy path — PENDING → DOWNLOADING → TRANSFERRING → COMPLETED
            // in ~2 s. Use this as the default for editor iteration.
            Create("Mock_OnDemand_Success_Small", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "level_1",
                        FinalStatus        = AssetPackStatus.Completed,
                        DownloadDurationSeconds = 2f,
                        TotalBytesToDownload = 10_000_000L,
                    }
                };
            });

            // ── 4. Mock_OnDemand_Success_Large ────────────────────────────────────
            // Slow happy path — same sequence over ~8 s. Tests progress bar UI and
            // stall detector does NOT fire (bytes advance steadily).
            Create("Mock_OnDemand_Success_Large", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "hd_textures",
                        FinalStatus        = AssetPackStatus.Completed,
                        DownloadDurationSeconds = 8f,
                        TotalBytesToDownload = 400_000_000L,
                    }
                };
            });

            // ── 5. Mock_OnDemand_NetworkError_Retryable ───────────────────────────
            // r2 — exercises ADR-026 retry policy. The mock halts at DOWNLOADING
            // with NetworkError on the first attempt; the retry policy recovers
            // on attempt 2. FinalStatus = Failed triggers the error code.
            Create("Mock_OnDemand_NetworkError_Retryable", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "level_2",
                        FinalStatus        = AssetPackStatus.Failed,
                        DownloadDurationSeconds = 1.5f,
                        TotalBytesToDownload = 20_000_000L,
                        StopAt             = AssetPackStatus.Downloading,
                        Error              = AssetPackErrorCode.NetworkError,
                    }
                };
            });

            // ── 6. Mock_OnDemand_InsufficientStorage ──────────────────────────────
            // Non-retryable failure: the device has no space. Pack halts at PENDING
            // then fails immediately.
            Create("Mock_OnDemand_InsufficientStorage", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "level_3",
                        FinalStatus        = AssetPackStatus.Failed,
                        DownloadDurationSeconds = 0.2f,
                        TotalBytesToDownload = 50_000_000L,
                        Error              = AssetPackErrorCode.InsufficientStorage,
                    }
                };
            });

            // ── 7. Mock_OnDemand_PackUnavailable ─────────────────────────────────
            // Consumer bug signal: the pack name in the request does not match any
            // declared pack in the AAB. Fails immediately with PackUnavailable (-2).
            Create("Mock_OnDemand_PackUnavailable", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "level_4",
                        FinalStatus        = AssetPackStatus.Failed,
                        DownloadDurationSeconds = 0.1f,
                        TotalBytesToDownload = 0L,
                        Error              = AssetPackErrorCode.PackUnavailable,
                    }
                };
            });

            // ── 8. Mock_OnDemand_StuckAtZero ──────────────────────────────────────
            // r2 — exercises ADR-016 stall detector. BytesDownloaded is frozen at 0
            // for >30 s (StuckAtZeroBytesForSeconds = 35 > 30 s threshold), which
            // trips the stall detector and fires OnError with StuckAtZero (-203).
            //
            // IMPLEMENTATION NOTE: The mock replayer emits Downloading states with
            // progress frozen at 0 because DownloadDurationSeconds is very large (999 s).
            // The stall detector watches for BytesDownloaded unchanged for >30 s and
            // synthesizes the -203 error. This preset specifically exercises that path.
            Create("Mock_OnDemand_StuckAtZero", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "stuck_pack",
                        FinalStatus        = AssetPackStatus.Downloading,  // never completes
                        DownloadDurationSeconds = 999f,                    // frozen progress
                        TotalBytesToDownload = 100_000_000L,
                        StopAt             = AssetPackStatus.Unknown,      // let replayer run
                    }
                };
                // StuckAtZeroBytesForSeconds > 30 s stall threshold
                // (recorded on cfg so the inspector shows intent clearly)
            });

            // ── 9. Mock_OnDemand_SessionNotFound ─────────────────────────────────
            // r2 — rename-guard repro (ADR-015 / GitHub issue #229). Simulates the
            // scenario where a pack was renamed between AAB uploads. The fetch fails
            // immediately with InvalidRequest (-3).
            Create("Mock_OnDemand_SessionNotFound", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "renamed_pack",
                        FinalStatus        = AssetPackStatus.Failed,
                        DownloadDurationSeconds = 0.1f,
                        TotalBytesToDownload = 0L,
                        Error              = AssetPackErrorCode.InvalidRequest,
                    }
                };
            });

            // ── 10. Mock_WaitingForWifi_Accepted ─────────────────────────────────
            // r2 split — happy path. Pack enters WaitingForWifi; consumer calls
            // ShowConfirmationDialogAsync and the user presses OK (mock returns Ok).
            // Download resumes and reaches COMPLETED.
            Create("Mock_WaitingForWifi_Accepted", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "hd_textures",
                        FinalStatus        = AssetPackStatus.Completed,
                        DownloadDurationSeconds = 5f,
                        TotalBytesToDownload = 200_000_000L,
                        StopAt             = AssetPackStatus.WaitingForWifi,
                    }
                };
                // ConfirmationDialogLatencyMs controls how quickly the dialog resolves
                cfg.ConfirmationDialogLatencyMs = 500f;
            });

            // ── 11. Mock_WaitingForWifi_Denied ────────────────────────────────────
            // r2 split — consent drop-off path. Same WaitingForWifi start; consumer
            // calls ShowConfirmationDialogAsync but the user presses Cancel. The fetch
            // is canceled via CancelAsync and the task faults with OperationCanceledException.
            //
            // NOTE: To reproduce the denied flow, the consumer must intercept the
            // WaitingForWifi state and call CancelAsync (not ShowConfirmationDialogAsync).
            // CellularConfirmationSample.cs shows both paths.
            Create("Mock_WaitingForWifi_Denied", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "hd_textures",
                        FinalStatus        = AssetPackStatus.Canceled,
                        DownloadDurationSeconds = 5f,
                        TotalBytesToDownload = 200_000_000L,
                        StopAt             = AssetPackStatus.WaitingForWifi,
                    }
                };
                cfg.ConfirmationDialogLatencyMs = 500f;
            });

            // ── 12. Mock_ProcessDeath_MidDownload ─────────────────────────────────
            // r2 — exercises ADR-018 reconciliation handler. On the NEXT cold start
            // after a force-kill during download, Awake calls ReconcileStateAsync.
            // The mock returns NotInstalled for "mid_dl_pack", which the controller
            // re-arms for fetching. No live download is in flight.
            Create("Mock_ProcessDeath_MidDownload", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "mid_dl_pack",
                        FinalStatus        = AssetPackStatus.NotInstalled,
                        DownloadDurationSeconds = 0.1f,
                        TotalBytesToDownload = 0L,
                    }
                };
                cfg.GetStatesLatencyMs = 100f;
            });

            // ── 13. Mock_AppNotOwned_PreLaunchReview ─────────────────────────────
            // r2 — R-PAD-10 graceful degradation. During Play Console pre-launch
            // review, the review device is not signed into the developer's account,
            // so the app receives APP_NOT_OWNED (-13). The consumer must handle this
            // gracefully (fall back to install-time assets or a degraded experience).
            Create("Mock_AppNotOwned_PreLaunchReview", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "level_boss_1",
                        FinalStatus        = AssetPackStatus.Failed,
                        DownloadDurationSeconds = 0.1f,
                        TotalBytesToDownload = 0L,
                        Error              = AssetPackErrorCode.AppNotOwned,
                    }
                };
            });

            // ── 14. Mock_ApiNotAvailable_Emulator ────────────────────────────────
            // r2 — emulator / sideload / no-Play-Services path. The device does not
            // have Play Services or the app was sideloaded. API_NOT_AVAILABLE (-5)
            // is returned immediately. Consumers must show a fallback experience.
            Create("Mock_ApiNotAvailable_Emulator", cfg =>
            {
                cfg.Packs = new[]
                {
                    new AssetDeliveryMockConfig.SimulatedPack
                    {
                        Name               = "level_1",
                        FinalStatus        = AssetPackStatus.Failed,
                        DownloadDurationSeconds = 0.1f,
                        TotalBytesToDownload = 0L,
                        Error              = AssetPackErrorCode.ApiNotAvailable,
                    }
                };
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "BizSim Asset Delivery",
                "14 mock presets created in " + OUTPUT_DIR,
                "OK");
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/BizSim"))
                AssetDatabase.CreateFolder("Assets", "BizSim");
            if (!AssetDatabase.IsValidFolder("Assets/BizSim/AssetDelivery"))
                AssetDatabase.CreateFolder("Assets/BizSim", "AssetDelivery");
            if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
                AssetDatabase.CreateFolder("Assets/BizSim/AssetDelivery", "MockPresets");
        }

        private static void Create(
            string name,
            System.Action<AssetDeliveryMockConfig> configure)
        {
            var asset = ScriptableObject.CreateInstance<AssetDeliveryMockConfig>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, $"{OUTPUT_DIR}/{name}.asset");
        }
    }
}
