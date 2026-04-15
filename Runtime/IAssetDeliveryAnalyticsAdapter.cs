namespace BizSim.Google.Play.AssetDelivery
{
    // ---------------------------------------------------------------------------
    // Analytics event payloads — privacy-by-design:
    //   - NO user ID, device ID, or advertising ID in any payload.
    //   - Pack names are truncated to an 8-char prefix (non-reversible) in pad_fetch_failed
    //     and pad_fetch_succeeded to reduce fingerprinting risk.
    //   - AnalyticsEventCatalogTests.cs asserts via reflection that no payload struct
    //     contains a field named UserId, DeviceId, or AdId.
    // ---------------------------------------------------------------------------

    public readonly struct PadFetchStartedEvent
    {
        public readonly int PackCount;
        public readonly bool IsRetry;
        public readonly int AttemptNumber;
        public PadFetchStartedEvent(int packCount, bool isRetry, int attemptNumber)
        { PackCount = packCount; IsRetry = isRetry; AttemptNumber = attemptNumber; }
    }

    public readonly struct PadFetchSucceededEvent
    {
        public readonly int PackCount;
        public readonly long TotalBytesDownloaded;
        public readonly float DurationSeconds;
        public PadFetchSucceededEvent(int packCount, long totalBytes, float durationSeconds)
        { PackCount = packCount; TotalBytesDownloaded = totalBytes; DurationSeconds = durationSeconds; }
    }

    public readonly struct PadFetchFailedEvent
    {
        public readonly AssetPackErrorCode ErrorCode;
        public readonly int AttemptNumber;
        public readonly bool WillRetry;
        public PadFetchFailedEvent(AssetPackErrorCode code, int attempt, bool willRetry)
        { ErrorCode = code; AttemptNumber = attempt; WillRetry = willRetry; }
    }

    public readonly struct PadFetchCanceledEvent
    {
        public readonly FetchCancelReason Reason;
        public readonly int AttemptNumber;
        public PadFetchCanceledEvent(FetchCancelReason reason, int attempt)
        { Reason = reason; AttemptNumber = attempt; }
    }

    public readonly struct PadRetryEvent
    {
        public readonly int Attempt;
        public readonly int MaxAttempts;
        public readonly float NextDelaySeconds;
        public readonly AssetPackErrorCode LastError;
        public readonly FetchCancelReason LastCancelReason;
        public PadRetryEvent(int attempt, int max, float delay, AssetPackErrorCode err, FetchCancelReason reason)
        { Attempt = attempt; MaxAttempts = max; NextDelaySeconds = delay; LastError = err; LastCancelReason = reason; }
    }

    public readonly struct PadStallDetectedEvent
    {
        public readonly float StallDurationSeconds;
        public readonly long BytesDownloaded;
        public PadStallDetectedEvent(float stallDuration, long bytes)
        { StallDurationSeconds = stallDuration; BytesDownloaded = bytes; }
    }

    public readonly struct PadPreflightBlockedEvent
    {
        public readonly FetchPreflightReason Reason;
        public readonly long EstimatedBytesRequired;
        public readonly long AvailableBytesOnStorage;
        public PadPreflightBlockedEvent(FetchPreflightReason reason, long estimated, long available)
        { Reason = reason; EstimatedBytesRequired = estimated; AvailableBytesOnStorage = available; }
    }

    public readonly struct PadCellularConsentShownEvent
    {
        public readonly int PackCount;
        public readonly long TotalBytes;
        public readonly int AttemptNumber;
        public PadCellularConsentShownEvent(int packCount, long totalBytes, int attempt)
        { PackCount = packCount; TotalBytes = totalBytes; AttemptNumber = attempt; }
    }

    public readonly struct PadCellularConsentAcceptedEvent
    {
        public readonly int PackCount;
        public readonly int AttemptNumber;
        public PadCellularConsentAcceptedEvent(int packCount, int attempt)
        { PackCount = packCount; AttemptNumber = attempt; }
    }

    public readonly struct PadCellularConsentDeniedEvent
    {
        public readonly int PackCount;
        public readonly int AttemptNumber;
        public PadCellularConsentDeniedEvent(int packCount, int attempt)
        { PackCount = packCount; AttemptNumber = attempt; }
    }

    public readonly struct PadPackRemovedEvent
    {
        public readonly bool WasForcedUnload;
        public PadPackRemovedEvent(bool forcedUnload) { WasForcedUnload = forcedUnload; }
    }

    public readonly struct PadBundleLoadedEvent
    {
        public readonly float DurationSeconds;
        public PadBundleLoadedEvent(float duration) { DurationSeconds = duration; }
    }

    public readonly struct PadEvictionEvent
    {
        public readonly int PacksEvicted;
        public readonly long BytesFreed;
        public PadEvictionEvent(int packs, long bytes) { PacksEvicted = packs; BytesFreed = bytes; }
    }

    /// <summary>
    /// Pluggable telemetry contract. Inject via
    /// <c>AssetDeliveryController.Instance.SetAnalyticsAdapter(myAdapter)</c>.
    /// All implementations must be main-thread-safe and must NEVER throw — the controller
    /// wraps every call in try/catch + BizSimLogger.Warning.
    ///
    /// EXACTLY 12 methods — asserted by AnalyticsEventCatalogTests via reflection.
    /// Method names are the canonical pad_* event names (underscore convention preserved).
    /// </summary>
    public interface IAssetDeliveryAnalyticsAdapter
    {
        // Event 1
        void pad_fetch_started(PadFetchStartedEvent e);
        // Event 2
        void pad_fetch_succeeded(PadFetchSucceededEvent e);
        // Event 3
        void pad_fetch_failed(PadFetchFailedEvent e);
        // Event 4
        void pad_fetch_canceled(PadFetchCanceledEvent e);
        // Event 5
        void pad_fetch_retrying(PadRetryEvent e);
        // Event 6
        void pad_stall_detected(PadStallDetectedEvent e);
        // Event 7
        void pad_preflight_blocked(PadPreflightBlockedEvent e);
        // Event 8
        void pad_cellular_consent_shown(PadCellularConsentShownEvent e);
        // Event 9
        void pad_cellular_consent_accepted(PadCellularConsentAcceptedEvent e);
        // Event 10
        void pad_cellular_consent_denied(PadCellularConsentDeniedEvent e);
        // Event 11
        void pad_pack_removed(PadPackRemovedEvent e);
        // Event 12
        void pad_bundle_loaded(PadBundleLoadedEvent e);
    }
}
