# Addressables Integration Guide

## What this feature does

`com.bizsim.google.play.assetdelivery` integrates with Unity Addressables so you can use
the Addressables workflow (groups, labels, async loading) while delivering content via
Google Play Asset Delivery packs. Your on-demand and fast-follow packs are fetched through
`AssetDeliveryController`, giving you:

- **Stall detector** — cancels a stuck fetch after a configurable timeout
- **Retry policy** — automatic back-off retries on transient errors
- **Cellular consent event** — `OnCellularConsentRequired` fires before accepting mobile data
- **Pack Inspector** — monitor per-pack download progress in the Editor
- **Analytics catalog** — 14 pre-wired analytics events (with Firebase or custom adapter)
- **14 mock presets** — reproduce every delivery scenario without a real device

## When to use this vs Unity's built-in Play Asset Delivery

| Feature | Unity's `PlayAssetDeliveryAssetBundleProvider` | BizSim Asset Delivery Provider |
|---|---|---|
| Addressables integration | Yes | Yes |
| Install-time packs | Yes | Yes |
| On-demand / fast-follow | Yes | Yes |
| Stall detector | No | Yes |
| Retry policy | No | Yes |
| Cellular consent event | Basic | Custom event with accept/deny |
| Pack Inspector | No | Yes |
| Analytics events | No | Yes (Firebase + custom) |
| Mock presets in Editor | Basic | 14 ScriptableObject presets |
| Dependency | `com.unity.addressables.android` | `com.unity.addressables` only |

## Prerequisites

1. `com.unity.addressables` **1.19+** installed in your host project via Package Manager.
2. External Dependency Manager (EDM4U) resolved — run **Assets → External Dependency Manager
   → Android Resolver → Force Resolve** after adding this package.
3. A custom Gradle template enabled in **Player Settings → Publishing Settings**.

## Setup (step-by-step)

### Step 1 — Create the Build Script asset

In the Project window, right-click an empty area and select:

```
Assets → Create → Addressables → Custom Build → BizSim Asset Delivery
```

This creates `BuildScriptBizSimAssetDelivery.asset`. The `BizSimAddressablesEditorInit`
[InitializeOnLoad] class creates this asset automatically the first time the editor loads
with both packages installed, so you may already have it.

### Step 2 — Set it as the active builder

1. Open **Window → Asset Management → Addressables → Groups**.
2. In the Groups window toolbar, select **Build → New Build**.
3. Confirm **BizSim Asset Delivery** appears in the dropdown (it should after Step 1).
4. Select it as the active builder.

### Step 3 — Add BizSimAssetDeliverySchema to groups

For each Addressables group whose content should be delivered as an asset pack:

1. In the Groups window, select the group.
2. Click **Add Schema → BizSim Asset Delivery**.
3. Configure:
   - **Delivery Mode** — `OnDemand`, `FastFollow`, or `InstallTime`
   - **Pack Name** — leave blank to auto-derive from the group name, or enter a custom
     Gradle pack identifier (letters, digits, underscores; must start with a letter)
   - **Route via BizSim Controller** — keep `true` (recommended) unless you want to
     manage the pack fetch yourself

### Step 4 — Run the build

In the Addressables Groups window, click **Build → New Build → BizSim Asset Delivery**.

The build script:
1. Temporarily swaps each schema'd group's provider to `BizSimAssetDeliveryBundleProvider`.
2. Runs the standard Addressables packed build.
3. Emits `Assets/StreamingAssets/BizSim/AddressablesPackMap.json` — the bundle-to-pack
   routing map read at runtime by `BizSimAddressablesInitialization`.
4. Creates `Assets/BizSimAssetDelivery/Build/<packName>/pack_manifest.json` for each pack.
5. Restores all group settings to their original values.

### Step 5 — Configure Gradle (required)

After the Player build completes, the `BizSimAssetDeliveryBuildProcessor` prints the
required Gradle declarations to the Unity Console. Copy them into your custom Gradle templates.

**launcherTemplate.gradle** — add pack names to `assetPacks`:

```groovy
android {
    assetPacks = [":myOnDemandPack", ":myFastFollowPack"]
}
```

**For each pack**, create a Gradle subproject directory at `<gradleProject>/<packName>/`
with a `build.gradle`:

```groovy
apply plugin: 'com.android.asset-pack'

assetPack {
    packName = "myOnDemandPack"
    dynamicDelivery {
        deliveryType = "on-demand"   // or "fast-follow" or "install-time"
    }
}
```

Copy your bundle files from `Assets/BizSimAssetDelivery/Build/<packName>/` into the pack's
`src/main/assets/` directory.

> **Note:** Full automated Gradle subproject emission is planned for v1.1.
> See `TODO(v1.1)` in `BizSimAssetDeliveryBuildProcessor.cs`.

## Runtime usage

No changes needed in game code. Standard Addressables calls work transparently:

```csharp
using UnityEngine.AddressableAssets;

// The BizSimAssetDeliveryBundleProvider intercepts this load if the bundle
// is in an on-demand pack. It calls AssetDeliveryController.FetchAsync()
// internally before delegating to the base AssetBundleProvider.
var handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/BigEnvironment");
await handle.Task;
var prefab = handle.Result;
```

To monitor pack download progress from your own UI, subscribe to
`AssetDeliveryController.Instance.OnPackStateChanged` as usual.

## Known limitations

- **No synchronous loading.** `WaitForCompletion()` is not supported for packs that require
  a network fetch. Calling it will log an error and fail gracefully.
- **Texture compression targeting** is not automated. If you use multiple TCF variants,
  run the build once per variant and manage pack folder suffixes manually (same limitation
  as Unity's built-in script in single-variant scenarios).
- **Gradle automation** requires manual step in v1.0 (see Step 5). Full automation
  via `Unity.Android.Gradle` API is planned for v1.1.
- The Addressables **catalog** and **settings.json** are NOT automatically moved to an
  install-time pack. If you want them in a pack, create an `InstallTime` delivery-mode
  group and add the catalog group to it.

## Comparison: BizSim vs Unity

This package's build script replicates the core routing logic of
`BuildScriptPlayAssetDelivery` (Apache 2.0, Unity Technologies) and adds:

1. Routing through `AssetDeliveryController` for enterprise features.
2. A simpler schema (`BizSimAssetDeliverySchema`) focused on the three delivery modes
   plus a per-group override for the pack name.
3. No dependency on `com.unity.addressables.android` — this package works with
   `com.unity.addressables` alone.
