namespace BizSim.Google.Play.AssetDelivery
{
    [System.Serializable]
    public readonly struct AssetPackLocation
    {
        public readonly string PackName;
        public readonly string Path;             // e.g. /data/data/<app>/files/assetpacks/<pack>
        public readonly string AssetsPath;       // Path + "/assets"
        public readonly AssetPackStorageMethod StorageMethod;

        public AssetPackLocation(string packName, string path, string assetsPath, AssetPackStorageMethod storageMethod)
        {
            PackName      = packName;
            Path          = path;
            AssetsPath    = assetsPath;
            StorageMethod = storageMethod;
        }

        public bool IsValid => !string.IsNullOrEmpty(Path);
    }
}
