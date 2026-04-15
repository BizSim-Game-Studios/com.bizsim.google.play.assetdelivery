// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// BizSimAddressablesEditorInit — registers the BizSim Asset Delivery build script
// with Addressables DataBuilders on editor load, creating the asset if missing.

#if BIZSIM_ADDRESSABLES
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Editor-load initializer that ensures a <see cref="BuildScriptBizSimAssetDelivery"/>
    /// asset exists under <c>Assets/AddressableAssetsData/DataBuilders/</c> and is registered
    /// with the default <see cref="AddressableAssetSettings"/> data builders list.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    internal static class BizSimAddressablesEditorInit
    {
        private const string BuilderAssetDir  = "Assets/AddressableAssetsData/DataBuilders";
        private const string BuilderAssetName = "BuildScriptBizSimAssetDelivery.asset";

        static BizSimAddressablesEditorInit()
        {
            UnityEditor.EditorApplication.delayCall += RegisterBuilder;
        }

        private static void RegisterBuilder()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return; // Addressables not yet initialized in this project.

            var assetPath = Path.Combine(BuilderAssetDir, BuilderAssetName)
                .Replace("\\", "/");

            // Create the asset if it doesn't exist.
            var builder = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildScriptBizSimAssetDelivery>(assetPath);
            if (builder == null)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder(BuilderAssetDir))
                    Directory.CreateDirectory(BuilderAssetDir);

                builder = ScriptableObject.CreateInstance<BuildScriptBizSimAssetDelivery>();
                UnityEditor.AssetDatabase.CreateAsset(builder, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
                BizSimLogger.Info(
                    $"[Addressables] Created BizSim Asset Delivery builder asset at {assetPath}.");
            }

            // Register with DataBuilders if not already present.
            bool alreadyRegistered = false;
            foreach (var existing in settings.DataBuilders)
            {
                if (existing is BuildScriptBizSimAssetDelivery)
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (!alreadyRegistered)
            {
                settings.AddDataBuilder(builder);
                UnityEditor.EditorUtility.SetDirty(settings);
                BizSimLogger.Info("[Addressables] BizSim Asset Delivery builder registered with Addressables.");
            }
        }
    }
}
#endif // BIZSIM_ADDRESSABLES
