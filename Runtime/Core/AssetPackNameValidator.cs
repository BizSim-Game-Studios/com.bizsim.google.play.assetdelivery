namespace BizSim.Google.Play.AssetDelivery
{
    internal static class AssetPackNameValidator
    {
        // Play documents pack names as: start with a lowercase letter, contain only
        // lowercase letters / digits / underscores, length 1–50. This regex enforces the
        // documented constraints client-side so a malformed pack name never reaches the JNI.
        private static readonly System.Text.RegularExpressions.Regex Pattern =
            new System.Text.RegularExpressions.Regex("^[a-z][a-z0-9_]{0,49}$",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        public static bool IsValid(string packName) =>
            !string.IsNullOrEmpty(packName) && Pattern.IsMatch(packName);

        public static void ThrowIfInvalid(string packName)
        {
            if (!IsValid(packName))
                throw new System.ArgumentException(
                    $"Invalid asset pack name '{packName}'. Must match ^[a-z][a-z0-9_]{{0,49}}$.",
                    nameof(packName));
        }
    }
}
