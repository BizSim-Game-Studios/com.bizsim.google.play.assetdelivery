namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Discriminates why a fetch was cancelled.
    /// Used by the retry policy (B1) to distinguish retryable stall-detector cancels
    /// from terminal user-cancels and silent reconciliation cleanups.
    /// </summary>
    public enum FetchCancelReason
    {
        /// <summary>Consumer called CancellationToken.Cancel() or CancelAsync(). Terminal — do NOT retry.</summary>
        User,

        /// <summary>
        /// Stall detector fired (StuckAtZero, -203). Retryable up to classification max.
        /// Retry loop must call StallDetector.ReArmForPack before next attempt.
        /// </summary>
        StallDetector,

        /// <summary>
        /// Awake reconciliation removed a stale session from a previous process death.
        /// Propagate silently — no pad_fetch_failed, no OnError, no OnPackStateChanged.
        /// </summary>
        ReconciliationCleanup,
    }
}
