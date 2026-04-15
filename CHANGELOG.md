# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-04-15

### Added
- Addressables integration via `BuildScriptBizSimAssetDelivery` custom build script — consumers can deliver Addressables groups as asset packs routed through BizSim's enterprise runtime (stall detector, retry policy, Pack Inspector, analytics catalog, 14 mock presets). See `Documentation~/ADDRESSABLES_INTEGRATION.md`.
- Initial release of the Google Play Asset Delivery bridge for Unity.
- `AssetDeliveryController` singleton with `FetchAsync`, `GetPackStatesAsync`, `GetPackLocationAsync`, `GetPackLocationsAsync`, `CancelAsync`, `RemovePackAsync`, `ShowConfirmationDialogAsync`.
- Per-pack install state stream via `OnPackStateChanged` event + `ReadStatesAsync(ct)` async iteration yielding full-dictionary snapshots.
- C# enums mirroring Google's constants one-to-one (`AssetPackStatus`, `AssetPackErrorCode`, `AssetPackStorageMethod`, `ActivityResultCode`) — 29 parity TestCase rows.
- `AssetPackState`, `AssetPackStates`, `AssetPackLocation` value-type structs with classifier helpers.
- Install-time pack short-circuit (`FetchAsync` on install-time packs synthesizes `COMPLETED` without JNI round-trip).
- Mock provider with 14 ScriptableObject presets covering all delivery modes + cellular confirmation + error paths.
- Cellular confirmation via `showConfirmationDialog(Activity)` — **no fragment shim required, any Activity works**.
- Cancellation propagation to Java-side `AssetPackManager.cancel(List<String>)`.
- Client-side pack-name regex validation.
- Optional Firebase Analytics adapter guarded by `BIZSIM_FIREBASE` with pack-name SHA1-prefix redaction.
- Optional UniTask support guarded by `BIZSIM_UNITASK`.
- `editor.core` integration for Firebase define management.
- `BIZSIM_ASSETDELIVERY_INSTALLED` define auto-registered at editor load.
