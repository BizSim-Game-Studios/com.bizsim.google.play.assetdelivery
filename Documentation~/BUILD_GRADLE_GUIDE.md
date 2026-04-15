# Declaring asset packs in your Unity host project's build.gradle

This package is a runtime wrapper. Asset packs themselves are declared at build time
in your host project's `android/app/build.gradle` + per-pack subproject `build.gradle`.

## Top-level declaration

In `android/app/build.gradle`:

```gradle
android {
    assetPacks = [":install_time_pack", ":fast_follow_pack", ":on_demand_pack"]
}
```

Each name is a Gradle subproject path — the pack name itself lives inside the per-pack
subproject's `build.gradle`.

## Per-pack `build.gradle`

Create `install_time_pack/build.gradle`:

```gradle
plugins { id 'com.android.asset-pack' }

assetPack {
    packName = "install_time_pack"
    dynamicDelivery {
        deliveryType = "install-time"   // or "fast-follow" / "on-demand"
    }
}
```

Place your pack content under `install_time_pack/src/main/assets/`. These files
will be exposed via `AssetPackLocation.AssetsPath` at runtime.

## Sync install-time pack names to AssetDeliverySettings

After declaring install-time packs in `build.gradle`, also register their names in
the BizSim `AssetDeliverySettings` asset:

1. Open `BizSim → Google Play → Asset Delivery → Configuration`
2. Under the Settings panel, add each install-time pack name to `InstallTimePackNames`
3. Click Apply

This lets `FetchAsync(installTimePackName)` short-circuit without a JNI round-trip —
install-time packs are already present on first launch.

## Pack size limits

| Delivery mode | Per-pack limit | Total across all packs of this mode |
|---|---|---|
| install-time | — | 1 GB |
| fast-follow | 512 MB | — |
| on-demand | 512 MB | — |
| **Total in AAB** | — | **2 GB** |

Higher limits require [Google Play Partner Program for Games](https://play.google.com/console/about/programs/partnerprogram/) enrollment.

## Local testing with bundletool

```bash
./gradlew bundleRelease
java -jar bundletool.jar build-apks --bundle=app.aab --output=output.apks --local-testing
java -jar bundletool.jar install-apks --apks=output.apks
```

The `--local-testing` flag serves asset packs from local storage instead of Play Store,
so runtime `FetchAsync` calls resolve without a live Play connection.
