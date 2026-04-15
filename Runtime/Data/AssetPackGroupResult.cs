using System.Collections.Generic;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Result returned by FetchGroupAsync — one handle per pack in the group.
    /// Install-time packs appear in PackStates with Status=Completed but have no entry in PackHandles
    /// (they are accessed via GetPackLocationAsync directly).
    /// </summary>
    public sealed class AssetPackGroupResult
    {
        public IReadOnlyDictionary<string, AssetPackState>  PackStates  { get; }
        public IReadOnlyDictionary<string, AssetPackHandle> PackHandles { get; }
        public float OverallProgress            { get; }
        public long  OverallBytesPerSecond      { get; }
        public float EstimatedSecondsRemaining  { get; }

        public event System.Action<AssetPackGroupResult> OnGroupProgress;

        public AssetPackGroupResult(
            IReadOnlyDictionary<string, AssetPackState>  packStates,
            IReadOnlyDictionary<string, AssetPackHandle> packHandles,
            float overallProgress,
            long  overallBytesPerSecond,
            float estimatedSecondsRemaining)
        {
            PackStates                = packStates;
            PackHandles               = packHandles;
            OverallProgress           = overallProgress;
            OverallBytesPerSecond     = overallBytesPerSecond;
            EstimatedSecondsRemaining = estimatedSecondsRemaining;
        }

        internal void FireGroupProgress() => OnGroupProgress?.Invoke(this);
    }
}
