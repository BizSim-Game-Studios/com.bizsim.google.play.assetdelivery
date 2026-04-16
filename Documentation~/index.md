# BizSim Google Play Asset Delivery

Last reviewed: 2026-04-16

## Overview

This package provides a production-ready Unity bridge for the Google Play Asset Delivery API
(v2.3.0). It wraps the native `AssetPackManager` via a JNI bridge, supports all three
delivery modes (on-demand, fast-follow, and install-time), and ships with per-pack state
streaming, cellular confirmation flow, batch fetch, retry policy, 14 mock presets, and an
optional Addressables integration via `BuildScriptBizSimAssetDelivery`.

The package compiles only for Android and Editor platforms. On non-Android builds and in the
Unity Editor, the mock provider is used automatically so you can iterate without a device.

## Contents

| File | Description |
|------|-------------|
| [getting-started.md](getting-started.md) | Step-by-step installation and first API call |
| [api-reference.md](api-reference.md) | Full public API surface with types, methods, and parameters |
| [configuration.md](configuration.md) | AssetDeliverySettings asset fields and Editor window walkthrough |
| [architecture.md](architecture.md) | JNI bridge diagram, thread model, provider selection |
| [troubleshooting.md](troubleshooting.md) | Common errors with root causes and fixes |
| [DATA_SAFETY.md](DATA_SAFETY.md) | Play Store Data Safety form input |

## Additional documentation

| File | Description |
|------|-------------|
| [ADDRESSABLES_INTEGRATION.md](ADDRESSABLES_INTEGRATION.md) | Addressables custom build script setup and usage |
| [BUILD_GRADLE_GUIDE.md](BUILD_GRADLE_GUIDE.md) | Asset pack declaration in build.gradle |
| [LOCAL_DEV_WORKFLOW.md](LOCAL_DEV_WORKFLOW.md) | Local development and testing workflow |

## Links

- [README](../README.md) — Quick-start experience and feature overview
- [CHANGELOG](../CHANGELOG.md) — Release history
- [GitHub Repository](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery)
