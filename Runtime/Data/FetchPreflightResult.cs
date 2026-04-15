namespace BizSim.Google.Play.AssetDelivery
{
    public enum FetchPreflightReason
    {
        OK,
        InsufficientStorage,
        NoNetwork,
        MeteredNetworkRefused,
        PackNotDeclared,
        DataSaverOn,
    }

    /// <summary>
    /// Result of CanFetchAsync pre-flight check (ADR-022, §B4).
    /// Does NOT invoke any Java fetch() — purely read-only queries.
    /// </summary>
    public readonly struct FetchPreflightResult
    {
        public readonly bool                CanFetch;
        public readonly FetchPreflightReason Reason;
        public readonly long                EstimatedBytesRequired;
        public readonly long                AvailableBytesOnStorage;
        public readonly string              HumanReadableMessage;

        public FetchPreflightResult(
            bool canFetch,
            FetchPreflightReason reason,
            long estimatedBytesRequired,
            long availableBytesOnStorage,
            string humanReadableMessage)
        {
            CanFetch                = canFetch;
            Reason                  = reason;
            EstimatedBytesRequired  = estimatedBytesRequired;
            AvailableBytesOnStorage = availableBytesOnStorage;
            HumanReadableMessage    = humanReadableMessage;
        }

        public static FetchPreflightResult Ok(long estimatedBytes, long available)
            => new FetchPreflightResult(true, FetchPreflightReason.OK, estimatedBytes, available, "OK");
    }
}
