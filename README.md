# BizSim Google Play Asset Delivery Bridge

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.1.2-orange.svg)](CHANGELOG.md)

Unity bridge for the [Google Play Asset Delivery API](https://developer.android.com/guide/playcore/asset-delivery) (v2.3.0).
Supports **on-demand**, **fast-follow**, and **install-time** asset pack delivery modes, exposes a per-pack state stream, and ships with a mock provider so you can iterate in the Editor without a Play Store install.

> **⚠️ Unofficial package.** This is a community-built Unity bridge for the Google Play Asset Delivery API. It is **not** an official Google product.

## Features

- **Java-to-C# Bridge** — Play Core `AssetPackManager` wrapped in a main-thread-safe singleton
- **Three delivery modes** — on-demand, fast-follow, and install-time asset packs
- **Per-pack state stream** — `OnPackStateChanged` event plus `ReadStatesAsync(ct)` async iteration yielding full-dictionary snapshots
- **`AssetPackState` classifier helpers** — `IsCompleted`, `IsFailed`, `RequiresCellularConfirmation`
- **Mock provider** — 14 ScriptableObject presets covering all delivery modes + cellular confirmation + error paths
- **Cellular confirmation** via `showConfirmationDialog(Activity)` — no fragment shim required, any Activity works
- **Analytics adapter** — Optional `IAssetDeliveryAnalyticsAdapter` with a Firebase implementation guarded by `BIZSIM_FIREBASE`
- **UniTask support** — Optional extension assembly guarded by `BIZSIM_UNITASK`
- **Editor integration** — Auto-registered `BIZSIM_ASSETDELIVERY_INSTALLED` define via `editor.core`

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
    "com.bizsim.google.play.assetdelivery": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery.git#v1.1.2"
  }
}
```

After the package imports, EDM4U is automatically resolved by UPM — no manual `.unitypackage` import required. EDM4U then resolves the Android Maven dependency declared in `Editor/Dependencies.xml` (`com.google.android.play:asset-delivery:2.3.0`) at the next Android build, or immediately via `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

**Asset pack setup.** Before fetching packs at runtime, declare each pack in your host project's `android/app/build.gradle` and per-pack subproject `build.gradle`. See [Documentation~/BUILD_GRADLE_GUIDE.md](Documentation~/BUILD_GRADLE_GUIDE.md). Install-time pack names must also be listed in `AssetDeliverySettings` — open `BizSim → Google Play → Asset Delivery → Configuration` and populate the `InstallTimePackNames` field.

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

- Unity 6000.0 or later
- Android target platform
- **[EDM4U](https://github.com/googlesamples/unity-jar-resolver) (External Dependency Manager for Unity)** — auto-resolved via OpenUPM scoped registry (see Installation)
- Google Play Asset Delivery library 2.3.0 (resolved automatically via `Editor/Dependencies.xml`)

## Google Play Data Safety

### Data Collected

This package does **not** collect any user data. Play Core handles all communication with the Play Store directly; this bridge only relays method calls and result codes. The package has no `PlayerPrefs` persistence — every `FetchAsync` call queries the Play Store fresh.

### Data NOT Collected or Shared

- **No personal data** is collected by this package
- **No data is shared** with third parties
- **No network calls** are made by this package (Play Core handles all IPC to Google Play)

### Play Console Data Safety Form

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469) in Google Play Console:

1. **Data types**: None collected by this package
2. **Collection purpose**: N/A
3. **Shared with third parties**: No

Full input text for the Data Safety form lives in [`Documentation~/DATA_SAFETY.md`](Documentation~/DATA_SAFETY.md).

## License

This package's C# and Java source code is licensed under the [MIT License](LICENSE.md) — Copyright (c) 2026 BizSim Game Studios.

## Third-Party Licenses

This package does **not** bundle any Google SDK binaries. The native Android dependency is resolved at build time by [EDM4U](https://github.com/googlesamples/unity-jar-resolver) from the Google Maven repository (`maven.google.com`):

| Dependency | Version | License |
|-----------|---------|---------|
| `com.google.android.play:asset-delivery` | 2.3.0 | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) |

For full third-party license details, see [NOTICES.md](NOTICES.md).
