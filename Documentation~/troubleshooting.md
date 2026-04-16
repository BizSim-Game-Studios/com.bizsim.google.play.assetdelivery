# Troubleshooting

Last reviewed: 2026-04-16

## 1. FetchAsync returns PackUnavailable

**Problem:** `FetchAsync` returns `AssetPackErrorCode.PackUnavailable` on a real device.

**Cause:** The asset pack name passed to `FetchAsync` does not match any pack declared in the app's `build.gradle`. Pack names are case-sensitive.

**Fix:** Verify the pack name in your `build.gradle` asset pack subproject matches exactly. Rebuild the AAB and re-upload to the Play Store. Use `bundletool dump manifest` to verify pack declarations.

## 2. Download stalls at WaitingForWifi

**Problem:** The pack state stops at `WaitingForWifi` and never progresses.

**Cause:** The device is on a metered (cellular) connection and the user has not granted permission to download over cellular data.

**Fix:** Call `AssetDeliveryController.Instance.ShowConfirmationDialogAsync()` to present the Play Store cellular consent dialog. If the user confirms, the download resumes automatically.

## 3. GetPackLocationAsync returns null AssetsPath

**Problem:** After a successful fetch, the location's `AssetsPath` is null or empty.

**Cause:** The pack may not be fully extracted yet, or the pack storage method is `ApkAssets` (install-time packs embedded in the APK have a different access pattern).

**Fix:** For `ApkAssets` storage, use `AssetManager` access instead of file paths. For `StorageFiles`, ensure the pack status is `Completed` before calling `GetPackLocationAsync`. Re-check with `GetPackStatesAsync` first.

## 4. EDM4U fails to resolve com.google.android.play:asset-delivery

**Problem:** Android build fails with missing dependency errors.

**Cause:** EDM4U has not resolved the Maven dependency, or the OpenUPM scoped registry is missing.

**Fix:** Run **Assets > External Dependency Manager > Android Resolver > Force Resolve**. Verify that `Packages/manifest.json` contains the OpenUPM scoped registry with `com.google.external-dependency-manager` in its scopes.

## 5. Mock provider does not simulate cellular confirmation

**Problem:** In Editor play mode, `ShowConfirmationDialogAsync` returns immediately without simulating the dialog.

**Cause:** The mock config's `SimulatedCellularResult` may not match the expected flow, or the mock states do not include `WaitingForWifi`.

**Fix:** Use one of the cellular confirmation presets from `Samples~/MockPresets`, or create a custom mock config with states that include `WaitingForWifi` and set `SimulatedCellularResult` to the desired outcome.

## 6. Addressables build fails with missing BuildScriptBizSimAssetDelivery

**Problem:** The Addressables build script is not found in the Build Script dropdown.

**Cause:** The Addressables integration is optional and lives in a separate assembly. It requires the `com.unity.addressables` package to be installed.

**Fix:** Ensure `com.unity.addressables` is in your project's dependencies. The build script should appear automatically. See [ADDRESSABLES_INTEGRATION.md](ADDRESSABLES_INTEGRATION.md) for setup.

## 7. InsufficientStorage error on device

**Problem:** `FetchAsync` returns `AssetPackErrorCode.InsufficientStorage`.

**Cause:** The device does not have enough free storage space to download and extract the asset pack.

**Fix:** This is a device-level constraint. Consider reducing pack sizes, splitting large packs into smaller ones, or prompting the user to free storage space. Use `TotalBytesToDownload` from the pack state to show the required space in your UI.
