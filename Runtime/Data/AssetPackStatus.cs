namespace BizSim.Google.Play.AssetDelivery
{
    public enum AssetPackStatus
    {
        Unknown                  = 0,
        Pending                  = 1,
        Downloading              = 2,
        Transferring             = 3,
        Completed                = 4,
        Failed                   = 5,
        Canceled                 = 6,
        WaitingForWifi           = 7,
        NotInstalled             = 8,
        RequiresUserConfirmation = 9,
    }
}
