# Addressables Sample

This sample demonstrates loading a `GameObject` prefab from an on-demand
Google Play asset pack via the BizSim Asset Delivery Addressables integration.

## Prerequisites

- `com.unity.addressables` installed in your project
- A BizSim Asset Delivery build completed (see `Documentation~/ADDRESSABLES_INTEGRATION.md`)
- An Addressables group named **Environments** with `BizSimAssetDeliverySchema` added,
  `DeliveryMode = OnDemand`, and a prefab with address `"Prefabs/BigEnvironment"` inside it

## Setup

1. Import this sample via the Package Manager.
2. Open `Assets/Samples/BizSim Asset Delivery/.../Addressables/AddressablesSample.unity`.
3. In the Addressables Groups window, create an **Environments** group:
   - Add the `BizSimAssetDeliverySchema` schema.
   - Set **Delivery Mode** to `OnDemand`.
   - Add a prefab with address `"Prefabs/BigEnvironment"`.
4. Run **Build → New Build → BizSim Asset Delivery**.
5. Complete the Gradle setup described in `Documentation~/ADDRESSABLES_INTEGRATION.md`.
6. Build to an Android device and run the scene.

## What the sample shows

- `AddressablesSample.cs` calls `Addressables.LoadAssetAsync<GameObject>` with a key
  that lives in an on-demand pack.
- The `BizSimAssetDeliveryBundleProvider` detects the pack, calls
  `AssetDeliveryController.Instance.FetchAsync()`, and completes the load once the
  pack is on-device.
- Pack download progress is logged to the console via `OnPackStateChanged`.
