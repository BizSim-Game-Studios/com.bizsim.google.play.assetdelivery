namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Options for RemovePackAsync. ADDITIVE surface per SemVer lock — new fields are always optional.
    /// </summary>
    public readonly struct RemovePackOptions
    {
        /// <summary>
        /// When true, any live AssetBundles from this pack are forcibly unloaded before removal.
        /// When false (default), RemovePackAsync throws if live bundles exist.
        /// </summary>
        public readonly bool ForceUnload;

        public RemovePackOptions(bool forceUnload = false) { ForceUnload = forceUnload; }
        public static readonly RemovePackOptions Default = new RemovePackOptions(false);
    }
}
