# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.0.x   | Yes       |

## Reporting a Vulnerability

If you discover a security vulnerability in this package, please report it responsibly:

1. **Do not** open a public GitHub issue
2. Email: **security@bizsim.com**
3. Include: package name, version, description of the vulnerability, and steps to reproduce

We will acknowledge your report within 48 hours and provide a fix timeline within 7 days.

## Scope

This package wraps the Google Play Asset Delivery API with the following security profile:

- **No user data collected.** The package does not read, transmit, or persist any
  personally identifiable information.
- **No local cache.** AssetDelivery has no PlayerPrefs persistence — every
  `FetchAsync` call queries Play Store fresh.
- **No network calls.** All communication with Google Play happens via the Play Store
  client app on the device. The package itself opens no sockets.
- **Cellular confirmation flow** is performed via `showConfirmationDialog(Activity)`,
  which accepts any Activity. It does not observe, mutate, or forward results from
  any other intent.
- **ProGuard rules** are embedded (`consumer-rules.pro`) to prevent reverse engineering
  of the Java bridge and to keep PlayCore's reflective entry points alive.
