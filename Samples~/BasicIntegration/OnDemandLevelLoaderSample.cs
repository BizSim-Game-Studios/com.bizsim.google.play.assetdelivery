using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using BizSim.Google.Play.AssetDelivery;

namespace BizSim.Google.Play.AssetDelivery.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates fetching an on-demand pack, resolving its location, and loading an AssetBundle.
    ///
    /// Setup:
    ///   1. Set _packName to an on-demand pack declared in build.gradle.
    ///   2. Set _bundleFileName to the .bundle file inside the pack's assets/ folder.
    ///   3. Wire _loadButton and _statusLabel.
    ///   4. Assign Mock_OnDemand_Success_Small to the controller for editor testing.
    /// </summary>
    public sealed class OnDemandLevelLoaderSample : MonoBehaviour
    {
        [SerializeField] private string _packName        = "level_boss_1";
        [SerializeField] private string _bundleFileName  = "boss.bundle";
        [SerializeField] private Button _loadButton;
        [SerializeField] private Text   _statusLabel;

        private void Awake()
        {
            if (_loadButton != null)
                _loadButton.onClick.AddListener(LoadLevel);
        }

        public async void LoadLevel()
        {
            SetStatus($"Fetching pack {_packName}...");
            try
            {
                await AssetDeliveryController.Instance.FetchAsync(new[] { _packName });
                SetStatus("Fetch done. Resolving location...");

                var loc = await AssetDeliveryController.Instance
                    .GetPackLocationAsync(_packName);

                if (!loc.HasValue || !loc.Value.IsValid)
                {
                    SetStatus($"Error: {_packName} not installed after fetch.");
                    return;
                }

                var bundlePath = Path.Combine(loc.Value.AssetsPath, _bundleFileName);
                SetStatus($"Loading bundle: {bundlePath}");

                // IMPORTANT: use LoadFromFileAsync for asset packs, NOT LoadFromFile.
                // Asset packs commonly contain bundles in the 100+ MB range — the
                // synchronous API hitches the main thread for multiple seconds on
                // mid-range devices. The async variant offloads I/O to a background
                // thread and yields every frame while waiting.
                var request = AssetBundle.LoadFromFileAsync(bundlePath);
                while (!request.isDone)
                    await Task.Yield();

                var bundle = request.assetBundle;
                if (bundle == null)
                {
                    SetStatus($"Error: failed to load bundle at {bundlePath}");
                    return;
                }

                var prefabs = bundle.LoadAllAssets<GameObject>();
                if (prefabs.Length == 0)
                {
                    SetStatus("Bundle loaded but contains no GameObjects.");
                    return;
                }

                Instantiate(prefabs[0]);
                SetStatus($"Level loaded: {prefabs[0].name}");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Level load canceled.");
            }
            catch (Exception ex)
            {
                SetStatus($"Level load failed: {ex.Message}");
            }
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null)
                _statusLabel.text = msg;
            Debug.Log($"[OnDemandLevelLoaderSample] {msg}");
        }
    }
}
