using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Stable Pack ID rename guard (ADR-015, §A1). Caches the previous build's pack descriptor
    /// list at <c>Library/BizSim/AssetDelivery/PackManifest.last.json</c> so the build validator
    /// can detect renames between builds.
    ///
    /// A rename (StablePackId present in cache but absent in current, AND a new ID in current but
    /// absent in cache) bricks existing installs. The build validator hard-fails release builds on
    /// rename unless the incoming <see cref="AssetPackDescriptor"/> has
    /// <see cref="AssetPackDescriptor.AllowUnsafeRename"/> = true.
    /// </summary>
    internal static class AssetDeliveryPackManifestCache
    {
        private const string CacheDir  = "Library/BizSim/AssetDelivery";
        private const string CachePath = CacheDir + "/PackManifest.last.json";

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Reads the previously cached manifest. Returns empty if the file is missing.</summary>
        public static PackManifest ReadLast()
        {
            if (!File.Exists(CachePath))
                return new PackManifest { StablePackIds = new string[0] };

            try
            {
                var text = File.ReadAllText(CachePath);
                return JsonUtility.FromJson<PackManifest>(text) ?? new PackManifest { StablePackIds = new string[0] };
            }
            catch (Exception ex)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "PackManifestCache: failed to read cache — " + ex.Message);
                return new PackManifest { StablePackIds = new string[0] };
            }
        }

        /// <summary>
        /// Writes the current settings' PackDescriptors to the cache.
        /// Called by <see cref="AssetDeliveryBuildValidator.OnPostprocessBuild"/>.
        /// </summary>
        public static void WriteCurrent()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);

            var ids = new List<string>();
            if (settings?.PackDescriptors != null)
                foreach (var d in settings.PackDescriptors)
                    if (!string.IsNullOrEmpty(d.StablePackId)) ids.Add(d.StablePackId);

            var manifest = new PackManifest { StablePackIds = ids.ToArray() };
            Write(manifest);
        }

        /// <summary>Writes an explicit manifest (for tests).</summary>
        public static void Write(PackManifest manifest)
        {
            try
            {
                Directory.CreateDirectory(CacheDir);
                File.WriteAllText(CachePath, JsonUtility.ToJson(manifest, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "PackManifestCache: failed to write cache — " + ex.Message);
            }
        }

        /// <summary>Deletes the cache file (for tests / fresh-start scenarios).</summary>
        public static void DeleteCache()
        {
            if (File.Exists(CachePath))
                File.Delete(CachePath);
        }

        /// <summary>
        /// Compares current settings against the cached manifest and throws
        /// <see cref="BuildFailedException"/> (release) or logs a warning (dev) on unsafe renames.
        /// </summary>
        public static void CheckForUnsafeRename(bool isRelease)
        {
            var last = ReadLast();
            if (last.StablePackIds.Length == 0) return; // First build — nothing to compare.

            var settings = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);
            if (settings?.PackDescriptors == null) return;

            var currentIds  = new HashSet<string>();
            var allowRename = new HashSet<string>();
            foreach (var d in settings.PackDescriptors)
            {
                if (string.IsNullOrEmpty(d.StablePackId)) continue;
                currentIds.Add(d.StablePackId);
                if (d.AllowUnsafeRename) allowRename.Add(d.StablePackId);
            }

            var cachedSet = new HashSet<string>(last.StablePackIds);

            // Removed = in cache but not in current.
            var removed = new HashSet<string>(cachedSet);
            removed.ExceptWith(currentIds);

            // Added = in current but not in cache.
            var added = new HashSet<string>(currentIds);
            added.ExceptWith(cachedSet);

            // A rename pair = one removed + one added simultaneously.
            if (removed.Count > 0 && added.Count > 0)
            {
                foreach (var oldId in removed)
                {
                    // Check if any new descriptor explicitly allows the rename.
                    bool allowed = false;
                    foreach (var newId in added)
                        if (allowRename.Contains(newId)) { allowed = true; break; }

                    if (!allowed)
                    {
                        string msg = BizSimLogger.Prefix +
                            $"Pack rename detected: a pack that was previously named '{oldId}' " +
                            "appears to have been removed while a new pack ID was added. " +
                            "Renaming a StablePackId bricks existing installs (ADR-015). " +
                            "Set AllowUnsafeRename = true on the new AssetPackDescriptor in " +
                            "AssetDeliverySettings.asset to acknowledge the risk and override this check.";
                        if (isRelease) throw new BuildFailedException(msg);
                        else Debug.LogWarning(msg);
                    }
                }
            }
        }

        // ─── Serializable data model ──────────────────────────────────────────────

        [Serializable]
        public sealed class PackManifest
        {
            public string[] StablePackIds = new string[0];
        }
    }
}
