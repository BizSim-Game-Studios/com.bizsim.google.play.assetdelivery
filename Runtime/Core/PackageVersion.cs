namespace BizSim.Google.Play.AssetDelivery
{
    internal static class PackageVersion
    {
        public const string Current         = "1.1.1";
        public const string ReleaseDate     = "2026-04-17";

        // === Canonical K8 fields (Plan G) ===
        public const string NativeSdkVersion       = "2.3.0";
        public const string NativeSdkLabel         = "Play Core (asset-delivery)";
        public const string NativeSdkArtifactCoord = "com.google.android.play:asset-delivery:2.3.0";

        // === Legacy alias (deprecated; removed in 2.0.0 per ADR-009) ===
        [System.Obsolete("Use NativeSdkVersion. Removed in 2.0.0 per ADR-009.", error: false)]
        public const string PlayCoreVersion = NativeSdkVersion;
    }
}
