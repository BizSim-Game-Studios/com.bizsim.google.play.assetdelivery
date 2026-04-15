namespace BizSim.Google.Play.AssetDelivery
{
    public sealed class AssetDeliveryException : System.Exception
    {
        public AssetDeliveryError Error { get; }

        public AssetDeliveryException(AssetDeliveryError error)
            : base($"{error.Code}: {error.Message}") => Error = error;
    }
}
