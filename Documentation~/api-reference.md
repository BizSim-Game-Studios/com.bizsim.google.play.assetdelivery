# API Reference

Last reviewed: 2026-04-16

Namespace: `BizSim.Google.Play.AssetDelivery`

## AssetDeliveryController

MonoBehaviour singleton. Entry point for all asset delivery operations.

| Member | Type | Description |
|--------|------|-------------|
| `Instance` | `AssetDeliveryController` | Lazy singleton; creates a DontDestroyOnLoad GameObject |
| `FetchAsync(packNames, ct)` | `Task<AssetDeliveryResult>` | Fetches one or more asset packs |
| `GetPackStatesAsync(packNames, ct)` | `Task<AssetPackStates>` | Queries current states for specified packs |
| `GetPackLocationAsync(packName, ct)` | `Task<AssetPackLocation>` | Returns the on-disk location of a completed pack |
| `ShowConfirmationDialogAsync(ct)` | `Task<ConfirmationResult>` | Shows cellular consent dialog |
| `CancelAsync(packNames, ct)` | `Task` | Cancels in-flight downloads |
| `RemovePackAsync(packName, ct)` | `Task` | Deletes a downloaded pack from device storage |
| `OnPackStateChanged` | `event Action<AssetPackStates>` | Fired on each pack state transition |
| `SetAnalyticsAdapter(adapter)` | `void` | Injects an analytics adapter or null to disable |

## AssetPackStates

Dictionary-style container for pack state snapshots.

| Property | Type | Description |
|----------|------|-------------|
| `TotalBytes` | `long` | Total bytes across all queried packs |
| `PackStates` | `IReadOnlyDictionary<string, AssetPackState>` | Per-pack state dictionary |

## AssetPackState

Readonly struct representing the state of a single asset pack.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Pack name |
| `Status` | `AssetPackStatus` | Current delivery status |
| `ErrorCode` | `AssetPackErrorCode` | Error code if failed |
| `BytesDownloaded` | `long` | Bytes downloaded so far |
| `TotalBytesToDownload` | `long` | Total download size |
| `TransferProgressPercentage` | `int` | Download progress (0-100) |
| `IsCompleted` | `bool` | Classifier helper |
| `IsFailed` | `bool` | Classifier helper |
| `RequiresCellularConfirmation` | `bool` | Whether cellular consent dialog is needed |

## AssetPackLocation

Readonly struct representing where pack assets are stored on disk.

| Property | Type | Description |
|----------|------|-------------|
| `AssetsPath` | `string` | Absolute path to the assets directory |
| `PackStorageMethod` | `AssetPackStorageMethod` | How the pack is stored |

## Enums

| Enum | Key Values |
|------|------------|
| `AssetPackStatus` | `Unknown`, `Pending`, `Downloading`, `Transferring`, `Completed`, `Failed`, `Canceled`, `WaitingForWifi`, `NotInstalled` (29 values total) |
| `AssetPackErrorCode` | `NoError`, `AppUnavailable`, `PackUnavailable`, `InvalidRequest`, `DownloadNotFound`, `ApiNotAvailable`, `NetworkError`, `AccessDenied`, `InsufficientStorage`, `AppNotOwned`, `InternalError` |
| `AssetPackStorageMethod` | `None`, `ApkAssets`, `StorageFiles` |

## AssetDeliverySettings

ScriptableObject at `Assets/Resources/BizSim/GooglePlay/AssetDeliverySettings.asset`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogsEnabled` | `bool` | `true` | Master log switch |
| `LogLevel` | `LogLevel` | `Info` | Minimum log severity |
| `UseMockInDevelopmentBuild` | `bool` | `false` | Use mock provider in Development Builds |
| `EnableAnalyticsByDefault` | `bool` | `false` | Auto-enable analytics adapter |
| `InstallTimePackNames` | `string[]` | `[]` | Names of install-time asset packs |

## AssetDeliveryMockConfig

ScriptableObject for editor testing. Ships with 14 preset scenarios.

| Field | Type | Description |
|-------|------|-------------|
| `SimulatedStates` | `MockPackState[]` | Per-pack state sequences to simulate |
| `SimulatedCellularResult` | `ConfirmationResult` | Result for `ShowConfirmationDialogAsync` |
| `SimulatedDelayMs` | `int` | Artificial delay between state transitions |
