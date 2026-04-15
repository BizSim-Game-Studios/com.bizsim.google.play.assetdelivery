// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// AddressablesSample — minimal scene script demonstrating Addressables loading
// of a prefab stored in an on-demand Google Play asset pack via BizSim routing.

#if BIZSIM_ADDRESSABLES
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BizSim.Google.Play.AssetDelivery.Samples
{
    /// <summary>
    /// Attach to a GameObject in the sample scene. On Start it subscribes to
    /// pack state changes, then loads a prefab whose bundle lives in an on-demand pack.
    /// The <see cref="BizSimAssetDeliveryBundleProvider"/> transparently fetches the
    /// pack (via <see cref="AssetDeliveryController"/>) before completing the load.
    /// </summary>
    public sealed class AddressablesSample : MonoBehaviour
    {
        [Tooltip("Addressables address of the prefab to load. Must be in a group with BizSimAssetDeliverySchema.")]
        public string PrefabAddress = "Prefabs/BigEnvironment";

        private void Start()
        {
            // Subscribe to pack state changes so we can log download progress.
            var controller = AssetDeliveryController.Instance;
            if (controller != null)
                controller.OnPackStateChanged += OnPackStateChanged;

            StartCoroutine(LoadPrefabCoroutine());
        }

        private void OnDestroy()
        {
            if (AssetDeliveryController.Instance != null)
                AssetDeliveryController.Instance.OnPackStateChanged -= OnPackStateChanged;
        }

        private IEnumerator LoadPrefabCoroutine()
        {
            Debug.Log($"[AddressablesSample] Starting load of '{PrefabAddress}'...");

            var handle = Addressables.LoadAssetAsync<GameObject>(PrefabAddress);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var prefab = handle.Result;
                Instantiate(prefab, Vector3.zero, Quaternion.identity);
                Debug.Log($"[AddressablesSample] '{PrefabAddress}' loaded and instantiated.");
            }
            else
            {
                Debug.LogError(
                    $"[AddressablesSample] Failed to load '{PrefabAddress}': {handle.OperationException}");
            }
        }

        private void OnPackStateChanged(string packId, AssetPackState state)
        {
            Debug.Log(
                $"[AddressablesSample] Pack '{packId}' status={state.Status} " +
                $"bytes={state.BytesDownloaded}/{state.TotalBytesToDownload}");
        }
    }
}
#else
// Addressables not installed — stub so the sample compiles without BIZSIM_ADDRESSABLES.
namespace BizSim.Google.Play.AssetDelivery.Samples
{
    using UnityEngine;

    public sealed class AddressablesSample : MonoBehaviour
    {
        private void Start()
        {
            Debug.LogWarning(
                "[AddressablesSample] com.unity.addressables is not installed. " +
                "Install it via Package Manager to use this sample.");
        }
    }
}
#endif
