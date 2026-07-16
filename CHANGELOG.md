# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.2] - 2026-06-03

### Fixed
- **CS0618 obsolete-symbol warning** in `AssetDeliveryConfiguration` and `AssetDeliveryMetadataJarGenerator` — both read `PackageVersion.PlayCoreVersion` (an `[Obsolete]` alias); now resolved via a reflection helper preferring the canonical `NativeSdkVersion`, matching the games/review/appupdate siblings. The metadata JAR's `playCoreVersion=` artifact key is **unchanged** (legacy key preserved per `unity-version-compatibility.md` Rule 3); only the obsolete symbol reference is removed.

### Changed
- **`AssetDeliverySettings` CreateAssetMenu path** unified to `BizSim/Google Play Service/AssetDelivery Settings`, matching the games/review/appupdate sibling convention. No effect on existing serialized assets.
- **`.androidlib/build.gradle` Android 15+ hardening (ADR-030).** Added `packagingOptions.jniLibs.useLegacyPackaging = false` for 16 KB native-library page alignment (R-PAD-8), plus a documented `enableUncompressedNativeLibs` marker for the build validator regex.

### Added
- Missing `.meta` files for `PackageVersionSchemaTest` and `PredictiveBackManifestTest`.

## [1.1.1] - 2026-04-17

### Fixed
- **C5.2 compliance (Plan E).** `Runtime/Plugins/Android/BizSimAssetDelivery.androidlib/AndroidManifest.xml` now explicitly declares `android:enableOnBackInvokedCallback="true"`. Cellular confirmation dialog is a system-managed Activity; predictive-back animations (Android 14+ / API 34+) are handled by the system. Added `PredictiveBackManifestTest` drift guard. See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/05-predictive-back-audit.md`.

## [1.1.0] - 2026-04-17

### Added
- **K8 PackageVersion schema unification (Plan G).** Three new `public const string` fields on `PackageVersion`: `NativeSdkVersion` (`"2.3.0"`), `NativeSdkLabel` (`"Play Core (asset-delivery)"`), `NativeSdkArtifactCoord` (`"com.google.android.play:asset-delivery:2.3.0"`). See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/06-package-version-schema.md`.
- `PackageVersionSchemaTest` drift guard.

### Deprecated
- `PackageVersion.PlayCoreVersion` — now `[Obsolete]` alias of `NativeSdkVersion`. Removed in 2.0.0 per ADR-009.

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
