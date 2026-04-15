namespace BizSim.Google.Play.AssetDelivery
{
    public enum AssetPackStorageMethod
    {
        StorageFiles = 0,   // Pack stored as files in app's internal storage
        ApkAssets    = 1,   // Pack stored as split APK assets (install-time packs)
    }
}
