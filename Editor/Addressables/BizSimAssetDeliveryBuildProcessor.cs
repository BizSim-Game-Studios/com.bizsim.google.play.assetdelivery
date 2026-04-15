// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// BizSimAssetDeliveryBuildProcessor — IPostprocessBuildWithReport hook that
// prints the required Gradle asset pack declarations to the console so consumers
// can paste them into their custom Gradle templates.
//
// TODO(v1.1): Automate full Gradle subproject emission using Unity.Android.Gradle
// (requires Unity 6 Dedicated Android Build module). Until then, we emit guidance
// to the console and document the manual steps in ADDRESSABLES_INTEGRATION.md.

#if BIZSIM_ADDRESSABLES && UNITY_ANDROID
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Post-build processor that reads pack manifests written by
    /// <see cref="BuildScriptBizSimAssetDelivery"/> and prints the Gradle
    /// asset pack declarations required for each pack.
    /// <para>
    /// The printed declarations must be added to the consumer's custom Gradle
    /// templates. See <c>Documentation~/ADDRESSABLES_INTEGRATION.md</c> for
    /// the complete step-by-step guide.
    /// </para>
    /// <remarks>
    /// Full automated Gradle project modification is deferred to v1.1.
    /// See TODO comment at the top of this file.
    /// </remarks>
    /// </summary>
    public sealed class BizSimAssetDeliveryBuildProcessor : IPostprocessBuildWithReport
    {
        /// <inheritdoc/>
        public int callbackOrder => 10;

        /// <inheritdoc/>
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != UnityEditor.BuildTarget.Android) return;

            var buildDir = BuildScriptBizSimAssetDelivery.BuildOutputDirectory;
            if (!Directory.Exists(buildDir)) return;

            var packDirs = Directory.GetDirectories(buildDir);
            if (packDirs.Length == 0) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(BizSimLogger.Prefix +
                "[Addressables] ── BizSim Asset Delivery: Gradle Integration Required ──");
            sb.AppendLine();
            sb.AppendLine("  Add the following to your launcherTemplate.gradle / mainTemplate.gradle:");
            sb.AppendLine();
            sb.AppendLine("  android {");
            sb.Append("      assetPacks = [");

            bool first = true;
            foreach (var packDir in packDirs)
            {
                var manifestPath = Path.Combine(packDir, "pack_manifest.json");
                if (!File.Exists(manifestPath)) continue;

                var manifestJson = File.ReadAllText(manifestPath);
                // Simple field extraction (avoid JsonUtility dependency on build-time type)
                var packId = ExtractJsonStringField(manifestJson, "packId");
                var deliveryMode = ExtractJsonStringField(manifestJson, "deliveryMode");
                if (string.IsNullOrEmpty(packId)) continue;

                if (!first) sb.Append(", ");
                sb.Append($"\":{packId}\"");
                first = false;

                sb.AppendLine();
                sb.AppendLine($"  // ── {packId} asset pack subproject ──");
                sb.AppendLine($"  // Create: <projectRoot>/{packId}/build.gradle");
                sb.AppendLine( "  //");
                sb.AppendLine( "  // apply plugin: 'com.android.asset-pack'");
                sb.AppendLine( "  // assetPack {");
                sb.AppendLine($"  //     packName = \"{packId}\"");
                sb.AppendLine($"  //     dynamicDelivery {{ deliveryType = \"{MapDeliveryMode(deliveryMode)}\" }}");
                sb.AppendLine( "  // }");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  See Documentation~/ADDRESSABLES_INTEGRATION.md for full setup instructions.");
            sb.AppendLine("  (Full automation planned for v1.1 — TODO(v1.1))");

            Debug.Log(sb.ToString());
        }

        private static string MapDeliveryMode(string mode)
            => mode switch
            {
                "InstallTime" => "install-time",
                "FastFollow"  => "fast-follow",
                "OnDemand"    => "on-demand",
                _             => "on-demand",
            };

        private static string ExtractJsonStringField(string json, string fieldName)
        {
            var marker = $"\"{fieldName}\"";
            var idx = json.IndexOf(marker, System.StringComparison.Ordinal);
            if (idx < 0) return string.Empty;
            idx = json.IndexOf('"', idx + marker.Length);
            if (idx < 0) return string.Empty;
            idx++;
            var end = json.IndexOf('"', idx);
            return end < 0 ? string.Empty : json.Substring(idx, end - idx);
        }
    }
}
#endif // BIZSIM_ADDRESSABLES && UNITY_ANDROID
