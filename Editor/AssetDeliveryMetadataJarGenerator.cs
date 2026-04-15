using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Generates a metadata JAR at build time (ADR-031, §E2). The JAR embeds package version
    /// information in <c>META-INF/bizsim-assetdelivery-version.txt</c> so the version can be
    /// read post-upload from the AAB via Play Console or <c>aapt2 dump</c>.
    ///
    /// Runs at <c>callbackOrder = 100</c> (after the main <see cref="AssetDeliveryBuildValidator"/>
    /// at order 0). Opt-out: delete
    /// <c>Runtime/Plugins/Android/com.bizsim.google.play.assetdelivery.metadata.jar</c>.
    /// </summary>
    internal sealed class AssetDeliveryMetadataJarGenerator : IPreprocessBuildWithReport
    {
        // Must be > AssetDeliveryBuildValidator.callbackOrder (0) so the validator runs first.
        public int callbackOrder => 100;

        private const string JarAssetPath =
            "Packages/com.bizsim.google.play.assetdelivery/Runtime/Plugins/Android/" +
            "com.bizsim.google.play.assetdelivery.metadata.jar";

        // Also try next to the current file for local-symlinked packages.
        private static readonly string[] JarFallbackPaths =
        {
            "Assets/../Packages/com.bizsim.google.play.assetdelivery/Runtime/Plugins/Android/" +
                "com.bizsim.google.play.assetdelivery.metadata.jar",
            "Runtime/Plugins/Android/com.bizsim.google.play.assetdelivery.metadata.jar",
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            try
            {
                GenerateJar();
            }
            catch (Exception ex)
            {
                // Never hard-fail the build for a metadata artifact.
                Debug.LogWarning(BizSimLogger.Prefix +
                    "MetadataJarGenerator: failed to generate metadata JAR — " + ex.Message);
            }
        }

        private static void GenerateJar()
        {
            // Resolve the output path.
            string outputPath = ResolveJarPath();
            if (outputPath == null)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "MetadataJarGenerator: cannot resolve Plugins/Android path — skipping.");
                return;
            }

            // Build the version text.
            string versionText =
                $"version={PackageVersion.Current}\n" +
                $"released={PackageVersion.ReleaseDate}\n" +
                $"playCoreVersion={PackageVersion.PlayCoreVersion}\n";

            byte[] versionBytes = Encoding.UTF8.GetBytes(versionText);

            // Write JAR (ZIP) in memory.
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry("META-INF/bizsim-assetdelivery-version.txt",
                    CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(versionBytes, 0, versionBytes.Length);
            }

            // Ensure directory exists.
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(outputPath, ms.ToArray());

            BizSimLogger.Info($"MetadataJarGenerator: wrote metadata JAR to '{outputPath}' " +
                $"(v{PackageVersion.Current}, released {PackageVersion.ReleaseDate}).");

            AssetDatabase.ImportAsset(
                outputPath.StartsWith("Assets") ? outputPath : JarAssetPath,
                ImportAssetOptions.ForceUpdate);
        }

        private static string ResolveJarPath()
        {
            if (File.Exists(JarAssetPath) || Directory.Exists(Path.GetDirectoryName(JarAssetPath)))
                return JarAssetPath;
            foreach (var path in JarFallbackPaths)
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return path;
            }
            return null;
        }
    }
}
