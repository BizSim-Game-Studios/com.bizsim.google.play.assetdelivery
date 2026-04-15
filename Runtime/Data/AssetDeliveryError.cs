namespace BizSim.Google.Play.AssetDelivery
{
    public readonly struct AssetDeliveryError
    {
        public readonly AssetPackErrorCode Code;
        public readonly string Message;
        public readonly string PackName;         // empty for non-pack-scoped errors
        public readonly bool Retryable;
        public readonly System.DateTime OccurredAtUtc;

        public AssetDeliveryError(
            AssetPackErrorCode code,
            string message,
            string packName,
            System.DateTime occurredAtUtc)
        {
            Code          = code;
            Message       = message;
            PackName      = packName;
            OccurredAtUtc = occurredAtUtc;
            Retryable     = IsRetryable(code);
        }

        public static bool IsRetryable(AssetPackErrorCode c) =>
            c == AssetPackErrorCode.NetworkError          ||
            c == AssetPackErrorCode.InternalError         ||
            c == AssetPackErrorCode.Timeout               ||
            c == AssetPackErrorCode.BridgeNotInitialized;
    }
}
