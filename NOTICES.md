# Third-Party Notices

This package depends on third-party libraries that are **not bundled** with the package.
They are resolved at build time via EDM4U (External Dependency Manager for Unity) from
the Google Maven repository (`maven.google.com`).

---

## Google Play Asset Delivery Library

- **Library:** `com.google.android.play:asset-delivery:2.3.0`
- **Copyright:** Copyright The Android Open Source Project
- **License:** [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0)

```
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

The Play Core Asset Delivery library coordinates with the Google Play Store app on the
device to download and install asset packs. The package itself makes no direct network calls
— all communication happens through the Play Store client.

---

## Google Play Services Tasks (transitive)

- **Library:** `com.google.android.gms:play-services-tasks` (transitive — pulled in by `com.google.android.play:asset-delivery`, not declared directly)
- **Copyright:** Copyright Google LLC
- **License:** [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0)

The JNI bridge marshals the GMS `Task` type returned by the asset-delivery manager, and the
package keeps `com.google.android.gms.tasks.**` in its ProGuard rules. The resolved version is
whatever `com.google.android.play:asset-delivery` pulls in transitively.

---

## Unity Editor APIs

This package uses Unity Editor APIs (`UnityEditor` namespace) for the configuration
window, custom inspectors, and build validators. These APIs are subject to the
[Unity Software Additional Terms](https://unity.com/legal/terms-of-service/software).

---

## BizSim Editor Core

- **Library:** `com.bizsim.google.play.editor.core`
- **Copyright:** Copyright BizSim Game Studios
- **License:** [MIT License](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.editor.core/blob/main/LICENSE.md)

Used for shared editor utilities (package detection, scripting define management).
Required by the Editor assembly (`BizSim.Google.Play.AssetDelivery.Editor` references it): the
runtime bridge compiles without it, but the Editor assembly will not compile if it is absent.
