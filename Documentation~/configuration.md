# Configuration

Last reviewed: 2026-04-16

## AssetDeliverySettings asset

The project-wide defaults are stored in a ScriptableObject at:

```
Assets/Resources/BizSim/GooglePlay/AssetDeliverySettings.asset
```

This asset is auto-created by `AssetDeliverySettingsAsset.LoadOrCreate()` the first time you
open the Configuration window. The controller reads it at `Awake()` via `Resources.Load`.

### Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogsEnabled` | `bool` | `true` | Master switch for all `[BizSim.AssetDelivery]` log output |
| `LogLevel` | `LogLevel` | `Info` | Minimum severity: Verbose, Info, Warning, Error, Silent |
| `UseMockInDevelopmentBuild` | `bool` | `false` | When true, Development Builds use the mock provider |
| `EnableAnalyticsByDefault` | `bool` | `false` | Auto-registers the default analytics adapter at startup |
| `InstallTimePackNames` | `string[]` | `[]` | Names of install-time asset packs; used to synthesize COMPLETED status without a JNI round-trip |

### Per-instance overrides

`AssetDeliveryController` has matching `[SerializeField]` fields. When a MonoBehaviour field
has a non-default value, it overrides the asset value for that instance.

## Editor Configuration window

Open via **BizSim > Google Play > Asset Delivery > Configuration**.

### Sections

1. **Package Info** — displays current package version, Play Core version, and EDM4U status.
2. **Settings** — draws the `AssetDeliverySettings` asset with full `SerializedObject` editing.
   - **Apply** — saves changes to disk and calls `BizSimLogger.InvalidateCache()`.
   - **Revert** — discards unsaved changes.
   - **Reset to defaults** — restores all fields to their default values.
3. **Install-Time Packs** — dedicated list editor for `InstallTimePackNames` with add/remove buttons.
4. **Quick Actions** — buttons for Force Resolve and Open Samples.

### Log level changes

After clicking Apply, log level changes take effect immediately without a domain reload.
The Configuration window calls `BizSimLogger.InvalidateCache()` which clears the cached
settings reference inside `BizSimLogger`.

## Cellular confirmation

When a pack's state reports `AssetPackStatus.WaitingForWifi`, the user is on a metered
connection. Call `ShowConfirmationDialogAsync()` to show the Play Store cellular consent
dialog. Unlike the AppUpdate package's fragment shim, this dialog works with ANY Activity
host -- no `FragmentActivity` requirement.

## Addressables integration

The optional `BuildScriptBizSimAssetDelivery` custom build script integrates with Unity's
Addressables system. It maps Addressable groups to asset packs and handles the build-time
configuration. See [ADDRESSABLES_INTEGRATION.md](ADDRESSABLES_INTEGRATION.md) for setup.
