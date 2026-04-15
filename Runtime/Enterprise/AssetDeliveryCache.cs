using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// LRU cache manager for downloaded asset packs (ADR-029, §D4).
    /// Eviction exclusion rules (I4): skip in-flight, in-load, install-time, or live-bundle packs.
    /// TODO(v1.1): expand eviction algo to handle multi-volume drives — ADR-029.
    /// </summary>
    public interface IAssetDeliveryCache
    {
        long TotalBytesUsed { get; }
        long BudgetBytes    { get; }
        CachedPack[] ListPacks();
        Task EvictLruAsync(long targetBytes, CancellationToken ct = default);
        Task EvictPackAsync(string packId, bool forceUnload = false, CancellationToken ct = default);
    }

    public readonly struct CachedPack
    {
        public readonly string   PackId;
        public readonly long     SizeBytes;
        public readonly DateTime LastAccessedUtc;
        public CachedPack(string id, long size, DateTime accessed)
        { PackId = id; SizeBytes = size; LastAccessedUtc = accessed; }
    }

    internal sealed class AssetDeliveryCache : IAssetDeliveryCache
    {
        private const string PrefKey = "BizSim.AssetDelivery.LastAccess.";

        private readonly IAssetDeliveryProvider  _provider;
        private readonly AssetDeliverySettings   _settings;
        private readonly Func<HashSet<string>>   _getInFlightFetches;
        private readonly Func<HashSet<string>>   _getInFlightLoads;
        private readonly Func<Dictionary<string, List<WeakReference<UnityEngine.AssetBundle>>>>
                                                 _getLoadedBundles;

        public long BudgetBytes => _settings?.LruBudgetBytes ?? 0L;

        public AssetDeliveryCache(
            IAssetDeliveryProvider provider,
            AssetDeliverySettings settings,
            Func<HashSet<string>> getInFlightFetches,
            Func<HashSet<string>> getInFlightLoads,
            Func<Dictionary<string, List<WeakReference<UnityEngine.AssetBundle>>>> getLoadedBundles)
        {
            _provider         = provider;
            _settings         = settings;
            _getInFlightFetches = getInFlightFetches;
            _getInFlightLoads   = getInFlightLoads;
            _getLoadedBundles   = getLoadedBundles;
        }

        public long TotalBytesUsed
        {
            get
            {
                // TODO(v1.1): query AssetPackLocation.SizeBytes via GetPackLocationsAsync — ADR-029.
                return 0L;
            }
        }

        public CachedPack[] ListPacks()
        {
            // Read last-accessed from PlayerPrefs for all declared packs.
            var result = new List<CachedPack>();
            var all    = _settings?.AllDeclaredPackNames ?? Array.Empty<string>();
            foreach (var id in all)
            {
                var raw = UnityEngine.PlayerPrefs.GetString(PrefKey + id, "");
                DateTime accessed = raw.Length > 0
                    && long.TryParse(raw, out var bin)
                    ? DateTime.FromBinary(bin)
                    : DateTime.MinValue;
                result.Add(new CachedPack(id, 0L, accessed));
            }
            return result.ToArray();
        }

        public async Task EvictLruAsync(long targetBytes, CancellationToken ct = default)
        {
            await EvictLruAsync(targetBytes, _getInFlightFetches(), _getLoadedBundles(),
                new HashSet<string>(_settings?.InstallTimePackNames ?? Array.Empty<string>()),
                _provider);
        }

        internal static async Task EvictLruAsync(
            long targetBytes,
            HashSet<string> inFlightFetches,
            Dictionary<string, List<WeakReference<UnityEngine.AssetBundle>>> loadedBundles,
            HashSet<string> installTimePacks,
            IAssetDeliveryProvider provider)
        {
            // Gather all packs with PlayerPrefs last-access timestamps.
            var candidates = new List<(string packId, DateTime accessed)>();
            var prefKeys   = UnityEngine.PlayerPrefs.GetString("BizSim.AssetDelivery.PackList", "");
            // Simplified: iterate known packs — full disk inventory is Phase 5 editor task.
            // TODO(v1.1): enumerate all packs from AssetPackManager.getPackLocations — ADR-029.

            long freed = 0L;
            candidates.Sort((a, b) => a.accessed.CompareTo(b.accessed)); // oldest first

            foreach (var (packId, _) in candidates)
            {
                if (freed >= targetBytes) break;

                // Exclusion rules (I4)
                if (inFlightFetches.Contains(packId))
                { BizSimLogger.Verbose($"LRU skip: '{packId}' is in-flight fetch"); continue; }
                if (installTimePacks.Contains(packId))
                { BizSimLogger.Verbose($"LRU skip: '{packId}' is install-time — cannot remove"); continue; }
                if (loadedBundles.TryGetValue(packId, out var refs)
                    && refs.Exists(wr => wr.TryGetTarget(out _)))
                { BizSimLogger.Verbose($"LRU skip: '{packId}' has live AssetBundle references"); continue; }

                try
                {
                    await provider.RemovePackAsync(packId, 60f, CancellationToken.None);
                    UnityEngine.PlayerPrefs.DeleteKey("BizSim.AssetDelivery.LastAccess." + packId);
                    // freed += sizeBytes; // TODO(v1.1): track size — ADR-029
                }
                catch (Exception ex)
                {
                    BizSimLogger.Warning($"LRU evict '{packId}' threw: {ex.Message}");
                }
            }
        }

        public async Task EvictPackAsync(string packId, bool forceUnload = false, CancellationToken ct = default)
        {
            await _provider.RemovePackAsync(packId, 60f, ct);
            UnityEngine.PlayerPrefs.DeleteKey(PrefKey + packId);
        }
    }

    // Expose static helper so AssetDeliveryController.FetchGroupAsync can call EvictLruAsync
    internal static class Cache
    {
        internal static Task EvictLruAsync(
            long targetBytes,
            HashSet<string> inFlightFetches,
            Dictionary<string, List<WeakReference<UnityEngine.AssetBundle>>> loadedBundles,
            HashSet<string> installTimePacks,
            IAssetDeliveryProvider provider)
            => AssetDeliveryCache.EvictLruAsync(targetBytes, inFlightFetches, loadedBundles,
                installTimePacks, provider);
    }
}
