# BasicIntegration Sample — BizSim Google Play Asset Delivery

Demonstrates on-demand fetch, AssetBundle loading, and cellular-consent handling
in eight steps. Works in Unity Editor via the mock provider and on Android via the
real Play Core library.

## Prerequisites

- Unity 6000.0 or later
- Android Build Support installed
- A host Unity project (packages cannot be opened directly)

---

## Integration steps

### Step 1 — Add the OpenUPM scoped registry

In your host project's `Packages/manifest.json`, add the scoped registry so
UPM can resolve EDM4U automatically:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.google.external-dependency-manager"]
    }
  ]
}
```

### Step 2 — Install the package

Add via Git URL in **Window > Package Manager > Add package from git URL**:

```
https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery.git#v1.0.0
```

Or add directly to `manifest.json`:

```json
"com.bizsim.google.play.assetdelivery": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery.git#v1.0.0"
```

### Step 3 — Force Resolve EDM4U

After install, run **Assets > External Dependency Manager > Android Resolver > Force Resolve**.
This downloads the Google Play Asset Delivery Maven artifact (`com.google.android.play:asset-delivery:2.3.0`).

### Step 4 — Declare asset packs in build.gradle

In your host project's `Assets/Plugins/Android/mainTemplate.gradle`, add an
`android.assetPacks` block. For example:

```groovy
android {
    assetPacks = [":level_boss_1", ":hd_textures", ":tutorial_pack"]
}
```

Each entry must be a Gradle subproject folder at the repo root (or under `build/`).
See `Documentation~/BUILD_GRADLE_GUIDE.md` for the full guide including install-time,
fast-follow, and on-demand pack configurations, size limits, and `bundletool` local testing.

### Step 5 — Register install-time pack names

Open **BizSim > Google Play > Asset Delivery > Configuration** and enter each
install-time pack name in the **Install-Time Pack Names** list. The controller
short-circuits `FetchAsync` for these packs (they are always available) without
making a Play Core API call.

### Step 6 — Place AssetDeliveryController in the scene

Add an empty GameObject named `[AssetDeliveryController]`, attach the
`AssetDeliveryController` component, and check **Don't Destroy On Load**.
Assign a `MockConfig` asset for editor testing (e.g., `Mock_OnDemand_Success_Small`
from the MockPresets sample).

### Step 7 — Wire the sample scripts

Add `BasicFetchSample`, `OnDemandLevelLoaderSample`, and `CellularConfirmationSample`
to GameObjects in the sample scene. Wire the serialized fields:

| Script | Required fields |
|--------|----------------|
| `BasicFetchSample` | `_progressBar`, `_statusLabel`, `_fetchButton`; set `_packName` |
| `OnDemandLevelLoaderSample` | `_loadButton`, `_statusLabel`; set `_packName`, `_bundleFileName` |
| `CellularConfirmationSample` | `_fetchButton`, `_statusLabel`; set `_largePackName` |

### Step 8 — Open BasicIntegration.unity and press Play

Open `Samples~/BasicIntegration/BasicIntegration.unity` via the Project window.

> **Note:** This scene file is a minimal YAML stub. After opening it in the Unity Editor,
> add the GameObjects described in Step 7, wire the components, and save the scene.
> The mock provider drives the full fetch lifecycle in Play mode — no device required.

Press Play. Click **Fetch** in the sample UI. The progress bar advances, the status
label updates, and the fetch completes. Switch mock configs to exercise error and
consent flows.

---

## Known limitations

- **Sideloaded APKs:** Asset Delivery requires a Play Store distribution. Sideloaded
  builds return `ApiNotAvailable (-5)`. Use `Mock_ApiNotAvailable_Emulator` to
  test graceful degradation.
- **512 MB per-pack limit:** Google Play enforces a 512 MB per-pack ceiling.
  Split large packs across multiple on-demand entries.
- **Offline local testing:** Use `bundletool` to test without uploading to Play:
  ```
  java -jar bundletool.jar build-apks --bundle=my.aab --output=my.apks --local-testing
  java -jar bundletool.jar install-apks --apks=my.apks
  ```
  See `Documentation~/BUILD_GRADLE_GUIDE.md` for full `bundletool` instructions.
- **Editor mock limitations:** `GetPackLocationAsync` returns a fake
  `temporaryCachePath`-based path. `AssetBundle.LoadFromFile` against this path
  will fail unless you copy actual `.bundle` files there. For editor UI testing,
  mock pack state transitions without loading real bundles.
