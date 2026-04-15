namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Localized error message catalog (ADR-028, §D3).
    /// Returns com.unity.localization-compatible keys for each AssetPackErrorCode.
    /// Consumers using Unity Localization package resolve these via LocalizationSettings.StringDatabase.
    /// </summary>
    public static class AssetDeliveryErrorMessages
    {
        /// <summary>
        /// Returns a localization key for the given error code.
        /// Format: "pad.error.{errorCodeName.ToLower()}"
        /// Consumers resolve against the shipped StringTable in Samples~/ErrorMessages/.
        /// </summary>
        public static LocalizationKey Get(AssetPackErrorCode code)
        {
            string codeName = code.ToString();
            return new LocalizationKey("pad.error." + codeName.ToLower());
        }
    }

    /// <summary>
    /// Lightweight key type compatible with com.unity.localization's LocalizedString contract.
    /// Does NOT require the Unity Localization package — consumers without it use .Key directly.
    /// </summary>
    public readonly struct LocalizationKey
    {
        public readonly string Key;
        public LocalizationKey(string key) { Key = key; }
        public override string ToString() => Key;
    }
}
