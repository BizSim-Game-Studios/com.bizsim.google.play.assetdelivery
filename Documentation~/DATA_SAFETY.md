# Data Safety Disclosure — BizSim Google Play Asset Delivery Bridge

This document is the source-of-truth for the package's Google Play Data Safety
form answers. Consumers must fill out their own Play Console Data Safety form
based on their full app's data practices; the entries below cover ONLY what this
package adds.

## Data collected

**None.** This package does not collect user-identifying data. No user identity,
device identifier, advertising ID, or account information is read, stored, or
transmitted by this package's code or JNI bridge.

## Data transmitted to Google

The Google Play Core library (`com.google.android.play:asset-delivery:2.3.0`)
handles all communication with the Play Store from its own process. This package's
JNI bridge marshals only:

- **Pack names** — developer-chosen identifiers (e.g., `"level_boss_1"`),
  not user data. These are sent to Play Core as part of the `AssetPackManager`
  fetch request, which Play Core forwards to the Play Store.
- **AssetPackState callbacks** — status codes (`int`) and byte-count progress
  (`long`) returned by Play Core, relayed from Java to C# over the JNI boundary.

No user-identifying data crosses the JNI boundary.

## Data transmitted to Firebase (if enabled)

The optional `IAssetDeliveryAnalyticsAdapter` + its default
`FirebaseAssetDeliveryAnalyticsAdapter` implementation (guarded by the
`BIZSIM_FIREBASE` scripting define) log **technical events only**:

- `bizsim_assetdelivery_fetch_requested` — fires when `FetchAsync()` is called;
  parameters: `delivery_mode_bucket` (install_time / fast_follow / on_demand / mixed),
  `pack_count`
- `bizsim_assetdelivery_state_changed` — parameters: `status` (int),
  `error_code` (int), `pack_name_hash` (first 8 hex chars of SHA1 of pack name),
  `size_bucket` (0 / 0-10MB / 10-100MB / 100-512MB / 512MB+)
- `bizsim_assetdelivery_fetch_completed` — parameters: `delivery_mode_bucket`,
  `elapsed_ms`, `pack_count`, `all_completed` (0/1)
- `bizsim_assetdelivery_error` — parameters: `error_code` (int), `retryable` (0/1)
- `bizsim_assetdelivery_confirmation_shown` — fires when `ShowConfirmationDialogAsync()`
  is called; parameters: `trigger_status` (WaitingForWifi / RequiresUserConfirmation)
- `bizsim_assetdelivery_confirmation_result` — parameters: `result_code` (Ok/Canceled)

**Raw pack names are NEVER transmitted.** The `pack_name_hash` parameter is the
first 8 hex characters of the SHA1 digest of the pack name — a prefix used only
for aggregate analytics. The full pack name cannot be reconstructed from this prefix.

No user identity, device identifier, ad ID, or any user-attributable data appears
in any event parameter.

Consumers who enable Firebase must complete their own Play Console Data Safety
form covering Firebase's data collection — this disclosure covers only what THIS
package adds on top.

## Data persisted locally

**None by this package.** Asset pack files are written to `/data/data/<package>/
files/assetpacks/` by the Play Core library itself. This storage belongs to Play
Core's own process sandbox — this package does not read from, write to, or manage
that directory.

This package does NOT maintain any `PlayerPrefs` keys, files under
`Application.persistentDataPath`, or `EncryptedPlayerPrefs` entries. `AssetPackState`
is fetched fresh on every `GetPackStatesAsync()` call; no state is cached between
sessions by this package.

Contrast with the sibling `com.bizsim.google.play.agesignals` package, which does
store derived behavior flags (not raw age data) in encrypted PlayerPrefs. Asset
Delivery has no equivalent persistence requirement.

## User controls

- **Remove a pack:** Call `AssetDeliveryController.Instance.RemovePackAsync(packName)`.
  This calls `AssetPackManager.removePack()` on the Java side, which instructs Play
  Core to delete the downloaded pack from its storage.
- **Cancel an in-flight download:** Call `AssetDeliveryController.Instance.CancelAsync(packNames)`.
- **Analytics opt-out:** Consumers who want to disable analytics call
  `AssetDeliveryController.Instance.SetAnalyticsAdapter(null)` (or never call
  `SetAnalyticsAdapter` — the default is no adapter).
- **Full package opt-out:** Removing the package from `Packages/manifest.json`
  leaves no residual state behind (no PlayerPrefs, no files).
- **No cooldown to clear:** Unlike `com.bizsim.google.play.review` (which has a
  `ClearLocalCooldownForTesting()` QA knob), Asset Delivery has no local state
  to clear — there is no equivalent method.

## Play Console Data Safety form answers

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469):

- **Does your app collect or share any of the required user data types?**
  Not from this package alone. Answer based on your full app including
  Firebase/other SDKs.
- **Data types collected by this package:** None.
- **Data shared with third parties by this package:**
  Pack names (developer-chosen identifiers, not user data) are sent to Google Play
  via the Play Core library. This is a first-party Google service and does not
  constitute third-party data sharing.
- **Is the data encrypted in transit?** N/A (this package does not transmit user
  data). Play Core's communication with the Play Store uses HTTPS.
- **Can users request their data be deleted?** N/A (this package does not persist
  user data). Downloaded pack files can be removed with `RemovePackAsync`.

## References

- Package source: <https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery>
- Google Play Asset Delivery: <https://developer.android.com/guide/playcore/asset-delivery>
- Play Console Data Safety: <https://support.google.com/googleplay/android-developer/answer/10787469>
- CROSS-PACKAGE-INVARIANTS.md §10 (shared template source)
