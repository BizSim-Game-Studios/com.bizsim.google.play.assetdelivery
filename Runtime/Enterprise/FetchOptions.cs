namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Options for FetchGroupAsync / FetchAsync. Fluent builder pattern.
    /// ADDITIVE surface per SemVer lock — new fields always optional.
    /// </summary>
    public sealed class FetchOptions
    {
        public int   RetryMaxAttempts              { get; private set; } = 1;
        public float RetryBaseDelaySeconds         { get; private set; } = 2f;
        public float RetryJitterFraction           { get; private set; } = 0.25f;
        public bool  SkipPreflight                 { get; private set; }
        public bool  AutoEvictLruOnInsufficientStorage { get; private set; }
        public float TimeoutSeconds                { get; private set; } = -1f;

        private FetchOptions() { }

        public static FetchOptions Default => new FetchOptions();

        /// <summary>Configure exponential-backoff retry on retryable errors (ADR-026).</summary>
        public FetchOptions WithRetry(
            int maxAttempts = 3,
            float baseDelaySeconds = 2f,
            float jitterFraction = 0.25f)
        {
            RetryMaxAttempts      = maxAttempts;
            RetryBaseDelaySeconds = baseDelaySeconds;
            RetryJitterFraction   = jitterFraction;
            return this;
        }

        public FetchOptions WithSkipPreflight(bool skip = true) { SkipPreflight = skip; return this; }

        public FetchOptions WithAutoEvictLru(bool autoEvict = true)
        { AutoEvictLruOnInsufficientStorage = autoEvict; return this; }

        public FetchOptions WithTimeout(float seconds) { TimeoutSeconds = seconds; return this; }
    }
}
