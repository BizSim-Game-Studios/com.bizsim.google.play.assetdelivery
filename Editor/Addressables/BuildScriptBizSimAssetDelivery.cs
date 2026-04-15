// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// BuildScriptBizSimAssetDelivery — custom Addressables build script.
// Surfaces as: Addressables Groups → Build → New Build → BizSim Asset Delivery
//
// REFERENCE IMPLEMENTATION:
//   Unity Technologies, BuildScriptPlayAssetDelivery.cs (Apache 2.0)
//   https://github.com/needle-mirror/com.unity.addressables.android
//   Adapted with BizSim routing and simplified Gradle integration.

#if BIZSIM_ADDRESSABLES
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.Util;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Custom Addressables build script that routes group content to Google Play asset packs
    /// and emits the <c>AddressablesPackMap.json</c> manifest consumed by
    /// <see cref="BizSimAddressablesInitialization"/> at runtime.
    /// <para>
    /// To use: create an instance of this asset
    /// (<b>Assets → Create → Addressables → Custom Build → BizSim Asset Delivery</b>),
    /// then set it as the active builder in the Addressables Groups window.
    /// Add <see cref="BizSimAssetDeliverySchema"/> to each group whose content should be
    /// delivered as an asset pack.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "BuildScriptBizSimAssetDelivery.asset",
        menuName  = "Addressables/Custom Build/BizSim Asset Delivery",
        order     = 11)]
    public class BuildScriptBizSimAssetDelivery : BuildScriptPackedMode
    {
        // ─── IDataBuilder ─────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public override string Name => "BizSim Asset Delivery";

        // ─── Build-time restore dictionaries ─────────────────────────────────────
        // Per-group overrides applied before base.BuildDataImplementation and
        // restored afterwards to leave the project settings untouched.

        private readonly Dictionary<AddressableAssetGroup, string> _buildPathRestore
            = new Dictionary<AddressableAssetGroup, string>();
        private readonly Dictionary<AddressableAssetGroup, string> _loadPathRestore
            = new Dictionary<AddressableAssetGroup, string>();
        private readonly Dictionary<AddressableAssetGroup, Type> _providerRestore
            = new Dictionary<AddressableAssetGroup, Type>();

        // bundle name → StablePackId mapping, accumulated during build.
        private readonly Dictionary<string, string> _bundleToPackId
            = new Dictionary<string, string>();

        // ─── BuildDataImplementation ──────────────────────────────────────────────

        /// <inheritdoc/>
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            _bundleToPackId.Clear();
            _buildPathRestore.Clear();
            _loadPathRestore.Clear();
            _providerRestore.Clear();

            var settings = builderInput.AddressableSettings;

            try
            {
                PrepareGroupsForBizSimDelivery(settings);
                var result = base.BuildDataImplementation<TResult>(builderInput);
                CollectBundlePackMappings(settings);
                EmitPackMapJson();
                EmitBuildFolders(settings);
                return result;
            }
            finally
            {
                RestoreGroupsAfterBuild(settings);
            }
        }

        // ─── Preparation ──────────────────────────────────────────────────────────

        private void PrepareGroupsForBizSimDelivery(AddressableAssetSettings settings)
        {
            var localBuildPath = AddressableAssetSettings.kLocalBuildPath;
            var localLoadPath  = AddressableAssetSettings.kLocalLoadPath;

            foreach (var group in settings.groups)
            {
                if (!group.HasSchema<BizSimAssetDeliverySchema>()) continue;
                if (!group.HasSchema<BundledAssetGroupSchema>()) continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                var deliverySchema = group.GetSchema<BizSimAssetDeliverySchema>();
                if (deliverySchema.DeliveryMode == BizSimAddressablesDeliveryMode.None) continue;

                // Force local build/load paths so base script places bundles in StreamingAssets.
                if (schema.BuildPath.GetName(settings) != localBuildPath)
                {
                    _buildPathRestore[group] = schema.BuildPath.Id;
                    schema.BuildPath.SetVariableByName(settings, localBuildPath);
                }
                if (schema.LoadPath.GetName(settings) != localLoadPath)
                {
                    _loadPathRestore[group] = schema.LoadPath.Id;
                    schema.LoadPath.SetVariableByName(settings, localLoadPath);
                }

                // Swap AssetBundleProviderType to our provider.
                if (schema.AssetBundleProviderType.Value != typeof(BizSimAssetDeliveryBundleProvider))
                {
                    _providerRestore[group] = schema.AssetBundleProviderType.Value;
                    schema.AssetBundleProviderType =
                        new SerializedType { Value = typeof(BizSimAssetDeliveryBundleProvider) };
                }
            }
        }

        // ─── Post-build mapping collection ───────────────────────────────────────

        private void CollectBundlePackMappings(AddressableAssetSettings settings)
        {
            foreach (var group in settings.groups)
            {
                if (!group.HasSchema<BizSimAssetDeliverySchema>()) continue;
                var deliverySchema = group.GetSchema<BizSimAssetDeliverySchema>();
                if (deliverySchema.DeliveryMode == BizSimAddressablesDeliveryMode.None) continue;

                var packId = deliverySchema.ResolvedPackName(group.Name);

                foreach (var entry in group.entries)
                {
                    if (string.IsNullOrEmpty(entry.BundleFileId)) continue;
                    var bundleName = Path.GetFileNameWithoutExtension(
                        entry.BundleFileId.Replace("\\", "/"));
                    if (!string.IsNullOrEmpty(bundleName))
                        _bundleToPackId[bundleName] = packId;
                }
            }
        }

        // ─── Pack map JSON emission ───────────────────────────────────────────────

        internal const string PackMapDirectory       = "Assets/StreamingAssets/BizSim";
        internal const string PackMapFileName        = "AddressablesPackMap.json";
        internal const string BuildOutputDirectory   = "Assets/BizSimAssetDelivery/Build";

        private void EmitPackMapJson()
        {
            EnsureDirectory(PackMapDirectory);
            var entries = new List<PackMapEntry>();
            foreach (var kv in _bundleToPackId)
                entries.Add(new PackMapEntry { bundleName = kv.Key, packId = kv.Value });

            var payload = new PackMapPayload { entries = entries.ToArray() };
            var json    = JsonUtility.ToJson(payload, prettyPrint: true);
            var path    = Path.Combine(PackMapDirectory, PackMapFileName);
            File.WriteAllText(path, json);
            UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
            BizSimLogger.Info($"[Addressables] Pack map written → {path} ({entries.Count} bundle(s)).");
        }

        // ─── Build folders for per-pack content staging ───────────────────────────
        // These folders are read by BizSimAssetDeliveryBuildProcessor at Player build time.

        private void EmitBuildFolders(AddressableAssetSettings settings)
        {
            foreach (var group in settings.groups)
            {
                if (!group.HasSchema<BizSimAssetDeliverySchema>()) continue;
                var deliverySchema = group.GetSchema<BizSimAssetDeliverySchema>();
                if (deliverySchema.DeliveryMode == BizSimAddressablesDeliveryMode.None) continue;
                var packId  = deliverySchema.ResolvedPackName(group.Name);
                var packDir = Path.Combine(BuildOutputDirectory, packId);
                EnsureDirectory(packDir);
                // Write a manifest stub so the build processor can enumerate pack folders.
                var manifest = new PackBuildManifest
                {
                    packId       = packId,
                    deliveryMode = deliverySchema.DeliveryMode.ToString(),
                };
                File.WriteAllText(
                    Path.Combine(packDir, "pack_manifest.json"),
                    JsonUtility.ToJson(manifest, prettyPrint: true));
            }
            UnityEditor.AssetDatabase.Refresh();
        }

        // ─── Restore ──────────────────────────────────────────────────────────────

        private void RestoreGroupsAfterBuild(AddressableAssetSettings settings)
        {
            foreach (var kv in _buildPathRestore)
                kv.Key.GetSchema<BundledAssetGroupSchema>()
                    ?.BuildPath.SetVariableById(settings, kv.Value);
            _buildPathRestore.Clear();

            foreach (var kv in _loadPathRestore)
                kv.Key.GetSchema<BundledAssetGroupSchema>()
                    ?.LoadPath.SetVariableById(settings, kv.Value);
            _loadPathRestore.Clear();

            foreach (var kv in _providerRestore)
                if (kv.Key.GetSchema<BundledAssetGroupSchema>() is { } s)
                    s.AssetBundleProviderType = new SerializedType { Value = kv.Value };
            _providerRestore.Clear();
        }

        // ─── Utility ──────────────────────────────────────────────────────────────

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        // ─── Serialization helpers ────────────────────────────────────────────────

        [Serializable]
        private sealed class PackMapPayload
        {
            public PackMapEntry[] entries;
        }

        [Serializable]
        private sealed class PackMapEntry
        {
            public string bundleName;
            public string packId;
        }

        [Serializable]
        private sealed class PackBuildManifest
        {
            public string packId;
            public string deliveryMode;
        }
    }
}
#endif // BIZSIM_ADDRESSABLES
