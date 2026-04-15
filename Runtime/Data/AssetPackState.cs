namespace BizSim.Google.Play.AssetDelivery
{
    [System.Serializable]
    public readonly struct AssetPackState
    {
        public readonly string Name;
        public readonly AssetPackStatus Status;
        public readonly AssetPackErrorCode ErrorCode;
        public readonly long BytesDownloaded;
        public readonly long TotalBytesToDownload;
        public readonly int TransferProgressPercentage;
        public readonly System.DateTime ObservedAtUtc;

        public AssetPackState(
            string name,
            AssetPackStatus status,
            AssetPackErrorCode errorCode,
            long bytesDownloaded,
            long totalBytesToDownload,
            int transferProgressPercentage,
            System.DateTime observedAtUtc)
        {
            Name                       = name;
            Status                     = status;
            ErrorCode                  = errorCode;
            BytesDownloaded            = bytesDownloaded;
            TotalBytesToDownload       = totalBytesToDownload;
            TransferProgressPercentage = transferProgressPercentage;
            ObservedAtUtc              = observedAtUtc;
        }

        public float DownloadProgress => TotalBytesToDownload == 0
            ? 0f
            : (float)BytesDownloaded / TotalBytesToDownload;

        public bool IsTerminal =>
            Status == AssetPackStatus.Completed ||
            Status == AssetPackStatus.Failed    ||
            Status == AssetPackStatus.Canceled  ||
            Status == AssetPackStatus.NotInstalled;  // Play Core fires this after RemovePackAsync — consumers
                                                      // using IsTerminal to stop observing a pack must handle it.

        public bool RequiresConfirmation =>
            Status == AssetPackStatus.WaitingForWifi ||
            Status == AssetPackStatus.RequiresUserConfirmation;
    }
}
