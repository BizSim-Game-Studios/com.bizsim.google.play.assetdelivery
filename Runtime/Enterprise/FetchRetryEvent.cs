namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>Payload for OnFetchRetrying event (ADR-026).</summary>
    public readonly struct FetchRetryEvent
    {
        public readonly int                Attempt;
        public readonly int                MaxAttempts;
        public readonly float              NextDelaySeconds;
        public readonly AssetPackErrorCode LastError;
        public readonly FetchCancelReason  LastCancelReason;

        public FetchRetryEvent(int attempt, int max, float delay,
            AssetPackErrorCode err, FetchCancelReason reason)
        {
            Attempt          = attempt;
            MaxAttempts      = max;
            NextDelaySeconds = delay;
            LastError        = err;
            LastCancelReason = reason;
        }
    }
}
