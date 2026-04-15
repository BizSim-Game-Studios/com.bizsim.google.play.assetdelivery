using System.IO;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Editor-only helper that manages the <see cref="AssetDeliverySettings"/> ScriptableObject
    /// asset. Follows the LoadOrCreate / Save / ResetToDefaults pattern mandated by
    /// CROSS-INVARIANTS §12 and used by every other <c>com.bizsim.google.play.*</c> package.
    /// </summary>
    internal static class AssetDeliverySettingsAsset
    {
        // Single source of truth for the asset path — see CROSS-INVARIANTS §12.5.
        private static readonly string AssetPath   = AssetDeliverySettings.AssetDatabasePath;
        private static readonly string AssetFolder = Path.GetDirectoryName(AssetPath).Replace('\\', '/');

        /// <summary>
        /// Returns the existing Settings asset or creates it (including all parent folders)
        /// if it does not exist yet.
        /// </summary>
        public static AssetDeliverySettings LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(AssetPath);
            if (existing != null) return existing;

            EnsureFolder(AssetFolder);
            var inst = ScriptableObject.CreateInstance<AssetDeliverySettings>();
            AssetDatabase.CreateAsset(inst, AssetPath);
            AssetDatabase.SaveAssets();
            return inst;
        }

        /// <summary>Marks the asset dirty and saves it to disk.</summary>
        public static void Save()
        {
            var asset = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(AssetPath);
            if (asset == null) return;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        /// <summary>Copies fresh default values onto the existing asset without deleting it.</summary>
        public static void ResetToDefaults()
        {
            var asset = LoadOrCreate();
            var fresh = ScriptableObject.CreateInstance<AssetDeliverySettings>();
            EditorUtility.CopySerialized(fresh, asset);
            Object.DestroyImmediate(fresh);
            Save();
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
