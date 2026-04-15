# Local Dev Workflow — Asset Delivery

This guide explains how to use the **Local HTTP provider** to iterate on asset pack
integration without waiting for a full Play Store build cycle.

Typical Play Store iteration: ~45 minutes (upload AAB → review → device install).
Typical local HTTP iteration: ~5 seconds (copy pack ZIP → reconnect → fetch).

---

## Prerequisites

- Python 3.8+ on the dev machine
- Android emulator or device with USB debugging enabled
- `adb` on your PATH

---

## 5-Step Setup

### Step 1 — Run the local pack server

```bash
cd com.bizsim.google.play.assetdelivery/Tools~
python3 local-pack-server.py --port 8080 --dir ./packs
```

Place your pack ZIPs in `Tools~/packs/` named `<StablePackId>.zip`. Example:

```
Tools~/packs/
    my_on_demand_pack.zip
    fast_follow_hd.zip
```

### Step 2 — Set up ADB reverse forwarding

```bash
adb reverse tcp:8080 tcp:8080
```

This makes `http://10.0.2.2:8080` on the device resolve to port 8080 on your machine.

### Step 3 — Enable the setting in Unity

In `AssetDeliverySettings.asset` (or via
`BizSim → Google Play → Asset Delivery → Configuration`):

```
UseLocalHttpInDevelopmentBuild = true
LocalHttpBaseUrl = "http://10.0.2.2:8080/packs/"
```

Make sure your build is a **Development Build** (Player Settings → check "Development Build").

### Step 4 — Build to device

Use the normal Unity Build & Run (Ctrl+B). The controller will automatically
use `LocalHttpAssetDeliveryProvider` instead of Play Core when
`UseLocalHttpInDevelopmentBuild = true` AND the build is a development build.

### Step 5 — Verify

Call `AssetDeliveryController.Instance.FetchAsync("my_on_demand_pack")`.
The Python server log should show `GET /packs/my_on_demand_pack.zip 200`.

---

## Analytics note

**Analytics events DO fire during local HTTP fetches.** They are tagged with
`source=local_http` in the event payload (via `BizSimLogger.Prefix` context).
Filter them out of your production dashboards using that tag to avoid polluting
production metrics with dev-time data.

---

## Stall detector behaviour

When `Debugger.IsAttached == true` AND the provider is `LocalHttp`, the stall
detector is disabled. This prevents spurious stall cancellations during step-through
debugging sessions. When no debugger is attached, the stall detector runs normally
(localhost is fast enough that 30-second stalls are real bugs).

---

## Pack format

The server serves plain ZIP archives. The `LocalHttpAssetDeliveryProvider` writes
the downloaded ZIP to `Application.persistentDataPath/LocalHttpPacks/<packName>/`.
`GetPackLocationAsync` returns that directory as the `AssetsPath`.

---

## Troubleshooting

| Symptom | Check |
|---------|-------|
| `404` in server log | Pack ZIP name must match StablePackId exactly (case-sensitive) |
| Connection refused | Run `adb reverse tcp:8080 tcp:8080` after connecting the device |
| Play Core fetch instead of local | Verify Development Build checkbox and `UseLocalHttpInDevelopmentBuild = true` |
| Stall detector fires at 30s | Attach a debugger or increase `StallTimeoutSeconds` in settings |
