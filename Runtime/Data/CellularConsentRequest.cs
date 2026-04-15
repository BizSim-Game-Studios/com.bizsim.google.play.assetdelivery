namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Passed to the OnCellularConsentRequired event (ADR-023, §B5).
    /// Consumer shows their own UI, then calls AcceptConsent or DenyConsent.
    /// Both delegates are guarded by an Interlocked one-shot so a second call is a no-op.
    /// This is a HARD LOCK surface per SemVer §4.
    /// </summary>
    public sealed class CellularConsentRequest
    {
        public string[]      PackNames          { get; }
        public long          TotalBytes         { get; }
        public bool          IsMeteredConnection{ get; }

        private int _invoked; // Interlocked one-shot — 0 = not invoked, 1 = invoked.

        private readonly System.Action _acceptAction;
        private readonly System.Action _denyAction;

        public CellularConsentRequest(
            string[] packNames,
            long totalBytes,
            bool isMeteredConnection,
            System.Action acceptAction,
            System.Action denyAction)
        {
            PackNames           = packNames;
            TotalBytes          = totalBytes;
            IsMeteredConnection = isMeteredConnection;
            _acceptAction       = acceptAction;
            _denyAction         = denyAction;
        }

        /// <summary>
        /// Invoke Google's native showConfirmationDialog. Idempotent — second call is a no-op.
        /// Known v2.0 design flaw: the one-shot guard means the delegate cannot be reused across
        /// retries; the controller creates a fresh CellularConsentRequest per retry attempt.
        /// </summary>
        public void AcceptConsent()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _invoked, 1, 0) == 0)
                _acceptAction?.Invoke();
            else
                BizSimLogger.Warning("AcceptConsent called more than once — ignoring duplicate call.");
        }

        /// <summary>Cancel the fetch. Idempotent — second call is a no-op.</summary>
        public void DenyConsent()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _invoked, 1, 0) == 0)
                _denyAction?.Invoke();
            else
                BizSimLogger.Warning("DenyConsent called more than once — ignoring duplicate call.");
        }
    }
}
