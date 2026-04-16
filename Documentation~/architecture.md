# Architecture

Last reviewed: 2026-04-16

## Overview

The Asset Delivery package follows the canonical BizSim Google Play bridge pattern: a Java
bridge on the Android side, a C# provider abstraction on the Unity side, and a MonoBehaviour
singleton controller that selects the provider at compile time. Unlike the AppUpdate package,
no fragment shim is needed -- the cellular confirmation dialog accepts any Activity.

## Component diagram

```
AssetDeliveryController (MonoBehaviour singleton)
    |
    +-- IAssetDeliveryProvider (compile-time selection)
    |       |
    |       +-- AndroidAssetDeliveryProvider (#if UNITY_ANDROID && !UNITY_EDITOR)
    |       |       |
    |       |       +-- AssetDeliveryCallbackProxy (AndroidJavaProxy)
    |       |       +-- PackStateCallbackProxy (AndroidJavaProxy)
    |       |       |       |
    |       |       |       +-- UnityMainThreadDispatcher.Enqueue()
    |       |       |
    |       |       +-- AssetDeliveryBridge.java (JNI entry point)
    |       |               |
    |       |               +-- AssetPackManager (Play Core SDK)
    |       |               +-- showConfirmationDialog(Activity)
    |       |
    |       +-- MockAssetDeliveryProvider (Editor + non-Android)
    |               |
    |               +-- AssetDeliveryMockConfig (ScriptableObject)
    |               +-- 14 preset scenarios
    |
    +-- IAssetDeliveryAnalyticsAdapter (optional telemetry)
    |
    +-- Addressables/ (optional integration)
            |
            +-- BuildScriptBizSimAssetDelivery
```

## Thread model

All public methods on `AssetDeliveryController` enforce main-thread execution via
`EnsureMainThread()`. Calling from a background thread throws `InvalidOperationException`.

On the Android side, `AssetDeliveryBridge.java` posts all `AssetPackManager` calls to the
main `Handler` (UI thread). Pack state callbacks from Play Core are forwarded to C# via
`PackStateCallbackProxy`, which uses `UnityMainThreadDispatcher.Enqueue()` to marshal back
to Unity's main thread.

## Provider selection

Provider selection happens at compile time:

- `#if UNITY_ANDROID && !UNITY_EDITOR` selects `AndroidAssetDeliveryProvider`
- All other configurations select `MockAssetDeliveryProvider`
- In Development Builds, `AssetDeliverySettings.UseMockInDevelopmentBuild` can override

## Per-pack state streaming

The `OnPackStateChanged` event fires with full-dictionary snapshots (`AssetPackStates`)
containing the current state of every pack being tracked. This is a push-based model;
consumers can also pull state via `GetPackStatesAsync()`.

The state stream covers all 29 `AssetPackStatus` enum values, providing parity with the
native Java API. The mock provider can simulate any sequence of state transitions via
the 14 preset ScriptableObject configs.

## Delivery modes

- **Install-time** packs are bundled with the APK/AAB. The controller detects these via
  the `InstallTimePackNames` setting and synthesizes a `COMPLETED` state without a JNI
  round-trip, avoiding an unnecessary native call.
- **Fast-follow** packs are downloaded automatically after install. They may still be
  in-progress when the user first opens the app.
- **On-demand** packs are downloaded at runtime on explicit request via `FetchAsync()`.

## Cellular confirmation

When a download is stalled due to a metered connection (`WaitingForWifi`), the consumer
calls `ShowConfirmationDialogAsync()`. On the Java side, this calls
`assetPackManager.showConfirmationDialog(activity)` which shows a system dialog. No
fragment shim is required; any Activity works. The result (Ok or Canceled) is returned
as a `ConfirmationResult`.

## Data flow

1. Consumer calls `AssetDeliveryController.Instance.FetchAsync(packNames)`
2. For install-time packs, a synthetic `COMPLETED` state is returned immediately
3. For on-demand/fast-follow, the Java bridge calls `AssetPackManager.fetch()`
4. Pack state transitions stream via `OnPackStateChanged`
5. If `WaitingForWifi`, consumer calls `ShowConfirmationDialogAsync()`
6. Once completed, `GetPackLocationAsync()` returns the on-disk path
7. Analytics adapter is notified at each stage
