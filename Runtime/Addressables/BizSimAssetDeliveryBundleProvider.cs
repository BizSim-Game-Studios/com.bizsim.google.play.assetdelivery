// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// BizSimAssetDeliveryBundleProvider — Addressables AssetBundleProvider that routes
// bundle loads through AssetDeliveryController for enterprise features.
//
// REFERENCE IMPLEMENTATION:
//   Unity Technologies, PlayAssetDeliveryAssetBundleProvider.cs (Apache 2.0)
//   https://github.com/needle-mirror/com.unity.addressables.android
//   Adapted with permission. BizSim additions: AssetDeliveryController routing,
//   stall detector awareness, cellular consent passthrough.

#if BIZSIM_ADDRESSABLES
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets;

namespace BizSim.Google.Play.AssetDelivery
{
    // ─── Sync-load error resource (Android device only) ───────────────────────────
    // Mirrors Unity's PlayAssetDeliveryResource pattern — surfaces a clear error
    // when WaitForCompletion is called on a provider that requires async fetch.
#if UNITY_ANDROID && !UNITY_EDITOR
    internal sealed class BizSimAssetDeliveryResource
    {
        private const string SyncMessage =
            "BizSim Asset Delivery provider does not support synchronous Addressable loading. " +
            "Do not use WaitForCompletion with BizSimAssetDeliveryBundleProvider.";

        private readonly BizSimAssetDeliveryBundleProvider _provider;
        private readonly ProvideHandle _handle;

        internal BizSimAssetDeliveryResource(BizSimAssetDeliveryBundleProvider provider, ProvideHandle handle)
        {
            _provider = provider;
            _handle = handle;
            _handle.SetWaitForCompletionCallback(OnWaitForCompletion);
        }

        private bool OnWaitForCompletion()
        {
            Debug.LogError(BizSimLogger.Prefix + SyncMessage);
            _provider.CompleteInterface(_handle);
            _handle.Complete<AssetBundleResource>(null, false, new RemoteProviderException(SyncMessage));
            return true;
        }
    }
#endif

    /// <summary>
    /// Addressables <see cref="AssetBundleProvider"/> that ensures the Google Play asset pack
    /// containing the requested bundle is fetched (via <see cref="AssetDeliveryController"/>)
    /// before delegating the actual load to the base provider.
    /// <para>
    /// Assign this provider to Addressables groups via <c>BizSimAssetDeliverySchema</c>.
    /// The <c>BuildScriptBizSimAssetDelivery</c> build script sets it automatically during build.
    /// </para>
    /// </summary>
    [DisplayName("BizSim Asset Delivery Provider")]
    public class BizSimAssetDeliveryBundleProvider : AssetBundleProvider, IUpdateReceiver
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Pending ProvideHandle sets per pack name, waiting for fetch to complete.
        private readonly Dictionary<string, HashSet<ProvideHandle>> _pendingHandles
            = new Dictionary<string, HashSet<ProvideHandle>>();

        // Queue of pack names being fetched (drives the Update polling loop).
        private readonly List<string> _fetchQueue = new List<string>();

        // ─── AssetBundleProvider overrides ────────────────────────────────────────

        /// <inheritdoc/>
        public override void Provide(ProvideHandle providerInterface)
            => LoadFromPack(providerInterface);

        /// <inheritdoc/>
        public override void Release(IResourceLocation location, object asset)
        {
            base.Release(location, asset);
            _pendingHandles.Clear();
        }

        // ─── Core fetch-or-load logic ─────────────────────────────────────────────

        private void LoadFromPack(ProvideHandle handle)
        {
            if (!BizSimAddressablesRuntimeData.Instance.Initialized)
            {
                // Data not loaded yet — surface sync-error so caller sees a useful message.
                new BizSimAssetDeliveryResource(this, handle);
                return;
            }

            var bundleName = BundleNameFromHandle(handle);
            var data = BizSimAddressablesRuntimeData.Instance;

            if (!data.AssetBundleNameToStablePackId.TryGetValue(bundleName, out var packId))
            {
                // Bundle not in any managed pack — fall through to the default local loader.
                base.Provide(handle);
                return;
            }

            // Install-time pack: path known immediately from streamingAssetsPath.
            var packLocalPath = GetInstallTimeOrCachedPath(packId, data);
            if (packLocalPath != null)
            {
                base.Provide(handle);
                return;
            }

            // On-demand / fast-follow: enqueue a fetch via AssetDeliveryController.
            new BizSimAssetDeliveryResource(this, handle);
            EnqueuePackFetch(handle, packId);
        }

        private static string GetInstallTimeOrCachedPath(
            string packId,
            BizSimAddressablesRuntimeData data)
        {
            if (data.StablePackIdToLocalPath.TryGetValue(packId, out var cached))
                return cached;

            // Check if the pack is already on-device (install-time or previously fetched).
            var onDevicePath = AndroidAssetPacks.GetAssetPackPath(packId);
            if (!string.IsNullOrEmpty(onDevicePath))
            {
                data.StablePackIdToLocalPath[packId] = onDevicePath;
                return onDevicePath;
            }
            return null;
        }

        private void EnqueuePackFetch(ProvideHandle handle, string packId)
        {
            if (!_pendingHandles.ContainsKey(packId))
            {
                if (_fetchQueue.Count == 0)
                    Addressables.ResourceManager.AddUpdateReceiver(this);

                _pendingHandles[packId] = new HashSet<ProvideHandle>();
                _fetchQueue.Add(packId);

                // Kick off the fetch via AssetDeliveryController (our enterprise layer).
                KickFetchAsync(packId);
            }
            _pendingHandles[packId].Add(handle);
        }

        private async void KickFetchAsync(string packId)
        {
            var controller = AssetDeliveryController.Instance;
            if (controller == null)
            {
                FailPack(packId, $"[Addressables] AssetDeliveryController.Instance is null. " +
                    "Ensure a controller is present in the scene before loading addressables.");
                return;
            }

            try
            {
                await controller.FetchAsync(packId);
                var localPath = AndroidAssetPacks.GetAssetPackPath(packId);
                if (string.IsNullOrEmpty(localPath))
                {
                    FailPack(packId, $"[Addressables] Pack '{packId}' fetch completed but could not be located on device.");
                    return;
                }
                BizSimAddressablesRuntimeData.Instance.StablePackIdToLocalPath[packId] = localPath;
                // Completion is processed on the next Update() tick.
            }
            catch (Exception ex)
            {
                FailPack(packId, $"[Addressables] Pack '{packId}' fetch failed: {ex.Message}");
            }
        }

        // ─── IUpdateReceiver ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void Update(float unscaledDeltaTime)
        {
            if (_fetchQueue.Count == 0) return;

            var data = BizSimAddressablesRuntimeData.Instance;
            var completed = new List<string>();

            foreach (var packId in _fetchQueue)
            {
                if (!data.StablePackIdToLocalPath.ContainsKey(packId)) continue;

                completed.Add(packId);
                if (!_pendingHandles.TryGetValue(packId, out var handles)) continue;

                foreach (var h in handles)
                    base.Provide(h);
                _pendingHandles.Remove(packId);
            }

            foreach (var packId in completed)
                _fetchQueue.Remove(packId);

            if (_fetchQueue.Count == 0)
                Addressables.ResourceManager.RemoveUpdateReceiver(this);
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private void FailPack(string packId, string message)
        {
            BizSimLogger.Error(message);
            if (!_pendingHandles.TryGetValue(packId, out var handles)) return;
            foreach (var h in handles)
                h.Complete<AssetBundleResource>(null, false, new RemoteProviderException(message));
            _pendingHandles.Remove(packId);
            _fetchQueue.Remove(packId);
        }

        private static string BundleNameFromHandle(ProvideHandle handle)
            => Path.GetFileNameWithoutExtension(handle.Location.InternalId.Replace("\\", "/"));

#else
        // In-Editor and non-Android builds: delegate straight to the base provider.
        // This allows Addressables to work in-editor with the mock provider path.

        /// <inheritdoc/>
        public override void Provide(ProvideHandle providerInterface)
            => base.Provide(providerInterface);

        /// <inheritdoc/>
        public void Update(float unscaledDeltaTime) { }

#endif // UNITY_ANDROID && !UNITY_EDITOR
    }
}
#endif // BIZSIM_ADDRESSABLES
