# MockPresets Sample — BizSim Google Play Asset Delivery

Materializes 14 `AssetDeliveryMockConfig` ScriptableObject assets that cover every
major lifecycle branch. Use these presets in Editor play mode without a device.

## Creating the presets

Run **BizSim > Google Play > Asset Delivery > Create Mock Presets** (or
**Assets > Create > BizSim > Google Play > AssetDelivery > Mock Presets**).

The menu action creates all 14 assets under `Assets/BizSim/AssetDelivery/MockPresets/`.
Assign any preset to the `AssetDeliveryController._mockConfig` serialized field.

## Preset reference

| # | Name | Simulated scenario | Key knobs |
|---|------|--------------------|-----------|
| 1 | `Mock_InstallTimeOnly` | `core_assets` already COMPLETED (install-time short-circuit Q4) | `IsInstallTime = true` |
| 2 | `Mock_FastFollow_Success` | `tutorial_pack` TRANSFERRING → COMPLETED in 3 s (post-install auto-download) | `DownloadDurationSeconds = 3` |
| 3 | `Mock_OnDemand_Success_Small` | `level_1` full sequence in 2 s — default for editor iteration | `DownloadDurationSeconds = 2`, 10 MB |
| 4 | `Mock_OnDemand_Success_Large` | `hd_textures` same sequence over 8 s — tests progress UI | `DownloadDurationSeconds = 8`, 400 MB |
| 5 | `Mock_OnDemand_NetworkError_Retryable` | `level_2` halts at DOWNLOADING with NetworkError (-6) | `StopAt = Downloading`, `Error = NetworkError` |
| 6 | `Mock_OnDemand_InsufficientStorage` | `level_3` fails with InsufficientStorage (-10) | `Error = InsufficientStorage` |
| 7 | `Mock_OnDemand_PackUnavailable` | `level_4` fails immediately with PackUnavailable (-2) | `Error = PackUnavailable` |
| 8 | `Mock_OnDemand_StuckAtZero` | `stuck_pack` frozen at 0 bytes → stall detector fires (-203) | `DownloadDurationSeconds = 999` (progress never advances) |
| 9 | `Mock_OnDemand_SessionNotFound` | `renamed_pack` fails with InvalidRequest (-3) — rename-guard repro (ADR-015) | `Error = InvalidRequest` |
| 10 | `Mock_WaitingForWifi_Accepted` | `hd_textures` → WaitingForWifi, user accepts → resumes | `StopAt = WaitingForWifi`, `ConfirmationDialogLatencyMs = 500` |
| 11 | `Mock_WaitingForWifi_Denied` | `hd_textures` → WaitingForWifi, user denies → canceled | `FinalStatus = Canceled`, `StopAt = WaitingForWifi` |
| 12 | `Mock_ProcessDeath_MidDownload` | App killed mid-download; Awake reconciles on next launch (ADR-018) | `FinalStatus = NotInstalled` |
| 13 | `Mock_AppNotOwned_PreLaunchReview` | Fails with AppNotOwned (-13) during pre-launch review (R-PAD-10) | `Error = AppNotOwned` |
| 14 | `Mock_ApiNotAvailable_Emulator` | Fails with ApiNotAvailable (-5) on emulator / sideload | `Error = ApiNotAvailable` |

## When to use each preset

| Scenario | Recommended preset(s) |
|----------|-----------------------|
| Default editor iteration | `Mock_OnDemand_Success_Small` |
| Progress bar / UI stress test | `Mock_OnDemand_Success_Large` |
| Retry logic | `Mock_OnDemand_NetworkError_Retryable` |
| Storage full UI | `Mock_OnDemand_InsufficientStorage` |
| Stall detector alert | `Mock_OnDemand_StuckAtZero` |
| Cellular consent (happy) | `Mock_WaitingForWifi_Accepted` |
| Cellular consent (deny) | `Mock_WaitingForWifi_Denied` |
| Cold-start reconciliation | `Mock_ProcessDeath_MidDownload` |
| Pre-launch review devices | `Mock_AppNotOwned_PreLaunchReview` |
| Emulator / CI | `Mock_ApiNotAvailable_Emulator` |

## Localized error messages

`ErrorMessages/error-messages.tsv` seeds the error string catalog:
**10 languages × 12 error codes = 120 entries**.

Columns: `error_code | en | es | de | fr | pt | ja | ko | zh-CN | ar | ru`

Error codes covered: `APP_UNAVAILABLE`, `PACK_UNAVAILABLE`, `INVALID_REQUEST`,
`DOWNLOAD_NOT_FOUND`, `API_NOT_AVAILABLE`, `NETWORK_ERROR`, `ACCESS_DENIED`,
`INSUFFICIENT_STORAGE`, `APP_NOT_OWNED`, `CONFIRMATION_NOT_REQUIRED`,
`INTERNAL_ERROR`, `STUCK_AT_ZERO`.

To integrate with Unity Localization (com.unity.localization), import the TSV
into a **String Table Collection** via the Localization Tables window. Each row
becomes one table entry; the `error_code` column maps to the key.

Non-English entries are machine-translated placeholders. Review native-speaker
translations before shipping. Lines marked `# TRANSLATE` in the TSV need manual
review.

## Architecture notes

The mock provider (`MockAssetDeliveryProvider`) drives state transitions via the
`MockAssetPackStateReplayer` tick-driver MonoBehaviour. Key simulation knobs on
each `SimulatedPack`:

- `FinalStatus` — terminal status the replayer emits after `DownloadDurationSeconds`
- `StopAt` — pause at this status and await `ShowConfirmationDialogAsync` (for consent flows) or just stop (for error flows)
- `Error` — reported when `FinalStatus == Failed`
- `IsInstallTime` — bypasses the download sequence entirely
- `DownloadDurationSeconds` — controls how fast bytes advance; set to 999 to freeze

The stall detector (ADR-016) watches `BytesDownloaded` across ticks. A pack
with `DownloadDurationSeconds = 999` causes progress to advance by ~0.1% per
second — slow enough that the 30 s stall threshold fires within 30 s of entering
DOWNLOADING.
