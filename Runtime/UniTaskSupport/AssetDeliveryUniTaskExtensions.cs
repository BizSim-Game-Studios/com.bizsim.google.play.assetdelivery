#if BIZSIM_UNITASK
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

namespace BizSim.Google.Play.AssetDelivery.UniTask
{
    /// <summary>
    /// UniTask adapter for <see cref="AssetDeliveryController"/>. Consumers who pull UniTask into
    /// their project automatically get the <c>*UniTask</c> variants on the controller.
    /// </summary>
    /// <remarks>
    /// All extensions use the <c>-1f</c> sentinel for timeouts, delegating sentinel resolution to
    /// <see cref="AssetDeliveryController"/> which reads <c>AssetDeliverySettings.DefaultTimeoutSeconds</c>.
    /// Hardcoded timeout defaults are intentionally avoided here.
    /// </remarks>
    public static class AssetDeliveryUniTaskExtensions
    {
        /// <summary>
        /// UniTask variant of <see cref="AssetDeliveryController.FetchAsync"/>.
        /// </summary>
        public static async UniTask<AssetPackHandle> FetchAsyncUniTask(
            this AssetDeliveryController c,
            string packName,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            return await c.FetchAsync(packName, ct, timeoutSeconds);
        }

        /// <summary>
        /// UniTask variant of <see cref="AssetDeliveryController.FetchGroupAsync"/>.
        /// </summary>
        public static async UniTask<AssetPackGroupResult> FetchGroupAsyncUniTask(
            this AssetDeliveryController c,
            IEnumerable<string> packNames,
            FetchOptions options = null,
            CancellationToken ct = default)
        {
            return await c.FetchGroupAsync(packNames, options, ct);
        }

        /// <summary>
        /// UniTask variant of <see cref="AssetDeliveryController.GetPackStatesAsync"/>.
        /// </summary>
        public static async UniTask<AssetPackStates> GetPackStatesAsyncUniTask(
            this AssetDeliveryController c,
            System.Collections.Generic.IReadOnlyList<string> packNames,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            return await c.GetPackStatesAsync(packNames, ct, timeoutSeconds);
        }

        /// <summary>
        /// UniTask variant of <see cref="AssetDeliveryController.RemovePackAsync"/>.
        /// </summary>
        public static async UniTask RemovePackAsyncUniTask(
            this AssetDeliveryController c,
            string packName,
            RemovePackOptions options = default,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            await c.RemovePackAsync(packName, options, ct, timeoutSeconds);
        }

        /// <summary>
        /// UniTask variant of <see cref="AssetDeliveryController.CanFetchAsync"/>.
        /// </summary>
        public static async UniTask<FetchPreflightResult> CanFetchAsyncUniTask(
            this AssetDeliveryController c,
            IEnumerable<string> packNames,
            CancellationToken ct = default)
        {
            return await c.CanFetchAsync(packNames, ct);
        }

        /// <summary>
        /// UniTask variant of <see cref="AssetDeliveryController.LoadAssetBundleAsync"/>.
        /// </summary>
        public static async UniTask<UnityEngine.AssetBundle> LoadAssetBundleAsyncUniTask(
            this AssetDeliveryController c,
            AssetPackHandle handle,
            string bundleFileName,
            CancellationToken ct = default)
        {
            return await c.LoadAssetBundleAsync(handle, bundleFileName, ct);
        }

        /// <summary>
        /// Wraps the controller's <c>IAsyncEnumerable&lt;IReadOnlyDictionary&lt;string, AssetPackState&gt;&gt;</c>
        /// stream as a <see cref="IUniTaskAsyncEnumerable{T}"/> so consumers can use UniTask LINQ operators.
        /// </summary>
        public static IUniTaskAsyncEnumerable<System.Collections.Generic.IReadOnlyDictionary<string, AssetPackState>>
            ReadStatesAsUniTaskAsyncEnumerable(
                this AssetDeliveryController c,
                CancellationToken ct = default)
        {
            return UniTaskAsyncEnumerable.Create<System.Collections.Generic.IReadOnlyDictionary<string, AssetPackState>>(
                async (writer, token) =>
                {
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, token);
                    await foreach (var snapshot in c.ReadStatesAsync(linkedCts.Token))
                    {
                        await writer.YieldAsync(snapshot);
                    }
                });
        }
    }
}
#endif
