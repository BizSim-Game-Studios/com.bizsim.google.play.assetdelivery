using System.Collections.Generic;

namespace BizSim.Google.Play.AssetDelivery
{
    [System.Serializable]
    public readonly struct AssetPackStates
    {
        public readonly IReadOnlyDictionary<string, AssetPackState> Packs;
        public readonly System.DateTime FetchedAtUtc;

        public AssetPackStates(IReadOnlyDictionary<string, AssetPackState> packs, System.DateTime fetchedAtUtc)
        {
            Packs        = packs;
            FetchedAtUtc = fetchedAtUtc;
        }

        public long TotalBytesToDownload
        {
            get
            {
                long total = 0;
                foreach (var p in Packs.Values)
                    total += p.TotalBytesToDownload;
                return total;
            }
        }

        public long TotalBytesDownloaded
        {
            get
            {
                long total = 0;
                foreach (var p in Packs.Values)
                    total += p.BytesDownloaded;
                return total;
            }
        }

        public bool TryGet(string packName, out AssetPackState state) =>
            Packs.TryGetValue(packName, out state);

        public bool AllCompleted
        {
            get
            {
                foreach (var p in Packs.Values)
                    if (p.Status != AssetPackStatus.Completed) return false;
                return true;
            }
        }
    }
}
