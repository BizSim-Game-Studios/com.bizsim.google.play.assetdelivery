using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Pre-build checks for <c>com.bizsim.google.play.assetdelivery</c>.
    ///
    /// r1 checks (advisory warnings — never throw):
    ///   1. EDM4U present
    ///   2. Settings asset exists
    ///   3. minSdkVersion &gt;= 21
    ///   4. InstallTimePackNames info hint
    ///
    /// r2 checks (HARD FAIL on release builds; warning on dev builds — see Step 14R.6):
    ///   5. targetSdkVersion &gt;= 34 (Android 14 — ADR-030)
    ///   6. Universal APK + install-time pack conflict (R-PAD-5)
    ///   7. 16 KB page alignment (R-PAD-8)
    ///   8. Implicit manifest audit (R-PAD-6)
    ///   9. Pack rename check (ADR-015)
    ///  10. Install-time pack size advisory
    /// </summary>
    internal sealed class AssetDeliveryBuildValidator : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // ─── r1 advisory path ─────────────────────────────────────────────────────

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            bool isRelease = report.summary.options.HasFlag(BuildOptions.Development) == false;

            // r1.1 — EDM4U
            if (!HasEdm4U())
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "EDM4U not detected at build time. The package declares " +
                    "'com.google.android.play:asset-delivery:2.3.0' in Editor/Dependencies.xml but " +
                    "EDM4U must be installed and force-resolved first. " +
                    "Add the OpenUPM scoped registry to Packages/manifest.json (see README.md) " +
                    "then run Android Resolver → Force Resolve.");
            }

            // r1.2 — Settings asset
            if (AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                    AssetDeliverySettings.AssetDatabasePath) == null)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "AssetDeliverySettings.asset not found at " + AssetDeliverySettings.AssetDatabasePath +
                    ". AssetDeliveryController will fall back to compile-time defaults at runtime. " +
                    "Open BizSim → Google Play → Asset Delivery → Configuration to create the asset.");
            }

            // r1.3 — minSdkVersion
            int minSdk = (int)PlayerSettings.Android.minSdkVersion;
            if (minSdk < 21)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    $"Player Settings minSdk is {minSdk}; Play Core Asset Delivery requires API 21+. " +
                    "Raise Player Settings → Android → Minimum API Level to Android 5.0 (API 21) or higher.");
            }

            // r1.4 — InstallTimePackNames info
            var settings = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);
            if (settings != null &&
                (settings.InstallTimePackNames == null || settings.InstallTimePackNames.Length == 0))
            {
                BizSimLogger.Verbose(
                    "InstallTimePackNames is empty. If your host project declares install-time asset packs " +
                    "in build.gradle, list their names in AssetDeliverySettings to enable the FetchAsync short-circuit.");
            }

            // ─── r2 checks ────────────────────────────────────────────────────────

            CheckTargetSdk(isRelease);
            CheckUniversalApkConflict(settings, isRelease);
            Check16KbPageAlignment(isRelease);
            CheckImplicitManifest(isRelease);
            CheckPackRename(isRelease);
            CheckInstallTimePackSizes(settings);
        }

        // ─── IPostprocessBuildWithReport ─────────────────────────────────────────

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            // Write the pack manifest cache so the rename guard has a baseline for the NEXT build.
            AssetDeliveryPackManifestCache.WriteCurrent();
        }

        // ─── r2 check methods ─────────────────────────────────────────────────────

        private static void CheckTargetSdk(bool isRelease)
        {
            int targetSdk = (int)PlayerSettings.Android.targetSdkVersion;
            if (targetSdk != 0 && targetSdk < 34)
            {
                string msg = BizSimLogger.Prefix +
                    $"targetSdkVersion is {targetSdk}. Android 14+ (API 34) is required for " +
                    "Play Core Asset Delivery 2.3.0 (ADR-030 / R-PAD-7). " +
                    "Raise Player Settings → Android → Target API Level to 34 or higher.";
                if (isRelease) throw new BuildFailedException(msg);
                else Debug.LogWarning(msg);
            }
        }

        private static void CheckUniversalApkConflict(AssetDeliverySettings settings, bool isRelease)
        {
            if (settings == null) return;
            bool hasInstallTimePacks = false;
            if (settings.PackDescriptors != null)
            {
                foreach (var d in settings.PackDescriptors)
                    if (d.DeliveryMode == DeliveryMode.InstallTime) { hasInstallTimePacks = true; break; }
            }
            if (!hasInstallTimePacks) return;

            // Check if build system is set to produce a universal APK (basic heuristic).
            bool universalApk = EditorUserBuildSettings.androidBuildSystem == AndroidBuildSystem.Gradle
                && EditorUserBuildSettings.buildAppBundle == false;

            if (universalApk)
            {
                string msg = BizSimLogger.Prefix +
                    "Building a universal APK with install-time asset packs declared. " +
                    "Universal APKs do not include asset packs — use Build App Bundle (AAB) instead " +
                    "(R-PAD-5). Enable Build App Bundle in Player Settings → Android → Publishing Settings.";
                if (isRelease) throw new BuildFailedException(msg);
                else Debug.LogWarning(msg);
            }
        }

        private static void Check16KbPageAlignment(bool isRelease)
        {
            string buildGradlePath =
                "Assets/../Packages/com.bizsim.google.play.assetdelivery/Runtime/Plugins/Android/" +
                "BizSimAssetDelivery.androidlib/build.gradle";

            // Also check relative to the package folder directly.
            string[] candidates = new[]
            {
                "Packages/com.bizsim.google.play.assetdelivery/Runtime/Plugins/Android/BizSimAssetDelivery.androidlib/build.gradle",
                "Runtime/Plugins/Android/BizSimAssetDelivery.androidlib/build.gradle",
            };

            string gradleText = null;
            foreach (var path in candidates)
            {
                if (File.Exists(path)) { gradleText = File.ReadAllText(path); break; }
            }

            if (gradleText == null) return; // Can't check — skip silently.

            bool hasUncompressedLibs = Regex.IsMatch(gradleText,
                @"useLegacyPackaging\s*=?\s*false", RegexOptions.IgnoreCase);
            bool hasUncompressedAssets = Regex.IsMatch(gradleText,
                @"enableUncompressedNativeLibs\s*=?\s*true", RegexOptions.IgnoreCase);

            if (!hasUncompressedLibs || !hasUncompressedAssets)
            {
                string msg = BizSimLogger.Prefix +
                    "16 KB page alignment (R-PAD-8 / ADR-030): the .androidlib build.gradle is missing " +
                    "'jniLibs { useLegacyPackaging false }' or " +
                    "'android.bundle.asset.enableUncompressedNativeLibs true'. " +
                    "Required for Android 15+ 16 KB page-aligned devices.";
                if (isRelease) throw new BuildFailedException(msg);
                else Debug.LogWarning(msg);
            }
        }

        private static void CheckImplicitManifest(bool isRelease)
        {
            string[] candidates = new[]
            {
                "Packages/com.bizsim.google.play.assetdelivery/Runtime/Plugins/Android/BizSimAssetDelivery.androidlib/AndroidManifest.xml",
                "Runtime/Plugins/Android/BizSimAssetDelivery.androidlib/AndroidManifest.xml",
            };

            string manifestText = null;
            foreach (var path in candidates)
            {
                if (File.Exists(path)) { manifestText = File.ReadAllText(path); break; }
            }

            if (manifestText == null) return;

            string[] forbidden = { "<uses-permission", "<activity", "<receiver", "<service", "<meta-data" };
            foreach (var tag in forbidden)
            {
                if (manifestText.Contains(tag))
                {
                    string msg = BizSimLogger.Prefix +
                        $"Manifest audit (R-PAD-6): the .androidlib AndroidManifest.xml contains '{tag}'. " +
                        "The manifest should be a stub with package declaration only. " +
                        "Remove the element to prevent permission injection (github.com/google/play-unity-plugins#194).";
                    if (isRelease) throw new BuildFailedException(msg);
                    else Debug.LogWarning(msg);
                }
            }
        }

        private static void CheckPackRename(bool isRelease)
        {
            try
            {
                AssetDeliveryPackManifestCache.CheckForUnsafeRename(isRelease);
            }
            catch (BuildFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "Pack rename check threw unexpectedly: " + ex.Message);
            }
        }

        private static void CheckInstallTimePackSizes(AssetDeliverySettings settings)
        {
            if (settings?.PackDescriptors == null) return;
            long warnThresholdBytes = 1L * 1024L * 1024L * 1024L; // 1 GB
            foreach (var desc in settings.PackDescriptors)
            {
                if (desc.DeliveryMode != DeliveryMode.InstallTime) continue;
                // Estimate on-disk size by summing file sizes in Assets/StreamingAssets/... (heuristic).
                // We don't have access to the actual pack at build time so this is advisory.
                Debug.Log(BizSimLogger.Prefix +
                    $"Install-time pack '{desc.StablePackId}': verify on-disk size < 1 GB " +
                    "(Google's soft limit is 1.5 GB; CI should catch 1 GB early).");
            }
        }

        // ─── Helper ───────────────────────────────────────────────────────────────

        private static bool HasEdm4U()
        {
            var type = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
            return type != null;
        }
    }
}
