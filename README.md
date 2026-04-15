# BizSim Google Play Asset Delivery Bridge

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity)](https://unity.com/releases/unity-6)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![Version 1.0.0](https://img.shields.io/badge/Version-1.0.0-blue)](CHANGELOG.md)

A Unity bridge for the [Google Play Asset Delivery API](https://developer.android.com/guide/playcore/asset-delivery) (`com.google.android.play:asset-delivery:2.3.0`). Supports **on-demand**, **fast-follow**, and **install-time** asset pack delivery modes, exposes a per-pack state stream, and ships with a mock provider so you can iterate in the Editor without a Play Store install.

> ⚠️ **Unofficial.** This package is maintained by BizSim Game Studios. It is not an official Google product and is not affiliated with Google.

## Features

- **Java-to-C# bridge** for `com.google.android.play:asset-delivery:2.3.0`
- **Three delivery modes** — on-demand, fast-follow, and install-time asset packs
- **Per-pack state stream** — `OnPackStateChanged` event plus `ReadStatesAsync(ct)` async iteration yielding full-dictionary snapshots
- **`AssetPackState` classifier helpers** — `IsCompleted`, `IsFailed`, `RequiresCellularConfirmation`
- **Mock provider** with 14 ScriptableObject presets covering all delivery modes + cellular confirmation + error paths
- **Cellular confirmation** via `showConfirmationDialog(Activity)` — no fragment shim required, any Activity works
- **Optional Firebase Analytics adapter** guarded by `BIZSIM_FIREBASE`
- **Optional UniTask support** guarded by `BIZSIM_UNITASK`
- **Editor integration** via `editor.core` with the `BIZSIM_ASSETDELIVERY_INSTALLED` define auto-registered at editor load

## Installation

This package depends on Google's [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver), which is published to the OpenUPM scoped registry. Add EDM4U's registry to your project's `Packages/manifest.json` once, then add this package as a Git URL — UPM will auto-install EDM4U on first import.

**Step 1 — Add the OpenUPM scoped registry (one-time per project):**

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.external-dependency-manager"
      ]
    }
  ]
}
```

If you already have other OpenUPM-distributed packages, you may already have this registry — just add `com.google.external-dependency-manager` to the existing `scopes` array.

**Step 2 — Install this package via Git URL:**

```json
{
  "dependencies": {
    "com.bizsim.google.play.assetdelivery": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery.git#v1.0.0"
  }
}
```

After the package imports, EDM4U is automatically resolved by UPM — no manual `.unitypackage` import required. EDM4U then resolves the Android Maven dependencies declared in `Editor/Dependencies.xml` (`com.google.android.play:asset-delivery:2.3.0`) at the next Android build, or immediately via `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

## Known integration steps

1. **Asset pack declaration** in your host project's `android/app/build.gradle` (and per-pack subproject `build.gradle`). See [Documentation~/BUILD_GRADLE_GUIDE.md](Documentation~/BUILD_GRADLE_GUIDE.md).
2. **Install-time pack names** must be listed in `AssetDeliverySettings` (open `BizSim → Google Play → Asset Delivery → Configuration` and populate the `InstallTimePackNames` field).
3. **`UnityPlayerActivity` compatibility** — this package uses `showConfirmationDialog(Activity)` which accepts ANY Activity. Classic Unity's `UnityPlayerActivity` and Unity 6's `GameActivity` both work without modification.

## Quick Start

```csharp
using BizSim.Google.Play.AssetDelivery;

// 1. Fetch an on-demand asset pack
var result = await AssetDeliveryController.Instance.FetchAsync("myOnDemandPack");
if (result.ErrorCode != AssetPackErrorCode.NoError) return;

// 2. Get the location to load assets from
var location = await AssetDeliveryController.Instance.GetPackLocationAsync("myOnDemandPack");
var path = location.AssetsPath;

// 3. Subscribe to per-pack state changes during download
AssetDeliveryController.Instance.OnPackStateChanged += states =>
{
    foreach (var kvp in states.PackStates)
        Debug.Log($"{kvp.Key}: {kvp.Value.Status} {kvp.Value.BytesDownloaded}/{kvp.Value.TotalBytesToDownload}");
};
```

## Delivery mode semantics

The package exposes all three delivery modes but does **not** auto-choose. The consumer declares pack delivery modes in their `build.gradle` and fetches accordingly:

- **Install-time** — asset packs bundled with the initial APK/AAB download. `FetchAsync` on install-time packs is a no-op that synthesizes `COMPLETED` without a JNI round-trip.
- **Fast-follow** — asset packs downloaded automatically just after install completes, before the user opens the app. Available offline once downloaded.
- **On-demand** — asset packs downloaded at runtime on request. May require cellular confirmation if the user is on a metered connection.

**Cellular confirmation.** If `AssetPackStatus.WaitingForWifi` is reported, call `ShowConfirmationDialogAsync()` to show the Play Store cellular consent dialog. No fragment shim is required — any Activity host works.

## Requirements

- **Unity 6000.0+**
- **Android** target platform
- **EDM4U** (auto-resolved via OpenUPM scoped registry — see Installation)
- **Google Play Asset Delivery library** `2.3.0` (resolved automatically via `Editor/Dependencies.xml`)

## Google Play Data Safety

**No data collected by this package.** Play Core handles all communication with the Play Store directly; this bridge only relays method calls and result codes. The package has no PlayerPrefs persistence — every `FetchAsync` call queries Play Store fresh.

When filling out your app's [Play Store Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469), this package does not require any new declarations. Full input text lives in [`Documentation~/DATA_SAFETY.md`](Documentation~/DATA_SAFETY.md).

## License

Copyright (c) 2026 BizSim Game Studios.

Released under the [MIT License](LICENSE.md).

## Third-Party Licenses

| Library | Version | License |
|---------|---------|---------|
| `com.google.android.play:asset-delivery` | 2.3.0 | Apache 2.0 |

Full attribution text in [NOTICES.md](NOTICES.md).
