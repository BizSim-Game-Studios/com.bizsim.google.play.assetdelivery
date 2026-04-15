namespace BizSim.Google.Play.AssetDelivery
{
    internal static class AssetPackErrorCodes
    {
        public const int BridgeNotInitialized = -200;
        public const int Timeout              = -201;
        public const int CancelledByCaller    = -202;
        public const int StuckAtZero          = -203;  // r2: ADR-016 stall detector
        public const int EditorMockError      = -204;
        public const int InvalidPackName      = -205;  // Q14
    }
}
