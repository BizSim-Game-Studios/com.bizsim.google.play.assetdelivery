namespace BizSim.Google.Play.AssetDelivery
{
    public sealed class AssetDeliveryException : System.Exception
    {
        public AssetDeliveryError Error { get; }

        public AssetDeliveryException(AssetDeliveryError error)
            : base($"{error.Code}: {error.Message}") => Error = error;

        public AssetDeliveryException(string message, AssetPackErrorCode code)
            : base(message)
        {
            Error = new AssetDeliveryError(code, message, string.Empty, System.DateTime.UtcNow);
        }
    }
}
