# Getting Started

Last reviewed: 2026-04-16

## Prerequisites

- Unity 6000.0 or later
- Android build target selected in Build Settings
- EDM4U (External Dependency Manager for Unity) installed via OpenUPM scoped registry
- Asset packs declared in your project's `build.gradle` (see [BUILD_GRADLE_GUIDE.md](BUILD_GRADLE_GUIDE.md))

## Step 1 — Install the package

Add the OpenUPM scoped registry to your project's `Packages/manifest.json` if not already present:

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

Then add the package dependency:

```json
{
  "dependencies": {
    "com.bizsim.google.play.assetdelivery": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery.git#v1.1.2"
  }
}
```

## Step 2 — Resolve Android dependencies

Run **Assets > External Dependency Manager > Android Resolver > Force Resolve**. This pulls `com.google.android.play:asset-delivery:2.3.0` from Google Maven.

## Step 3 — Configure asset packs

Open **BizSim > Google Play > Asset Delivery > Configuration**. Populate the `InstallTimePackNames` field with the names of any install-time packs. On-demand and fast-follow packs do not need to be registered here.

For `build.gradle` configuration of asset pack subprojects, see [BUILD_GRADLE_GUIDE.md](BUILD_GRADLE_GUIDE.md).

## Step 4 — Fetch an on-demand pack

Add the following to any MonoBehaviour:

```csharp
using BizSim.Google.Play.AssetDelivery;
using UnityEngine;

public class AssetPackExample : MonoBehaviour
{
    async void Start()
    {
        var result = await AssetDeliveryController.Instance.FetchAsync("myOnDemandPack");
        if (result.ErrorCode != AssetPackErrorCode.NoError)
        {
            Debug.LogError($"Fetch failed: {result.ErrorCode}");
            return;
        }

        var location = await AssetDeliveryController.Instance.GetPackLocationAsync("myOnDemandPack");
        Debug.Log($"Assets available at: {location.AssetsPath}");
    }
}
```

## Step 5 — Verify in Editor

Enter Play Mode. The mock provider simulates a successful fetch by default. Check the Console for `[BizSim.AssetDelivery]` log entries.

## Step 6 — Test on a device

Deploy via internal test track with an Android App Bundle (AAB). Asset packs are only delivered through the Play Store. Use `bundletool` for local testing with `--local-testing` flag.

## What to expect

- Install-time packs are bundled with the APK/AAB and are available immediately. `FetchAsync` on install-time packs synthesizes a `COMPLETED` status without a JNI round-trip.
- Fast-follow packs download automatically after install. Check their status before use.
- On-demand packs download at runtime on request and may prompt for cellular confirmation.
- Subscribe to `OnPackStateChanged` to track download progress in real time.
