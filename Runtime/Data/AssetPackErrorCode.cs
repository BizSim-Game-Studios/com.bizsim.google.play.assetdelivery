namespace BizSim.Google.Play.AssetDelivery
{
    public enum AssetPackErrorCode
    {
        NoError                  =    0,
        AppUnavailable           =   -1,
        PackUnavailable          =   -2,
        InvalidRequest           =   -3,
        DownloadNotFound         =   -4,
        ApiNotAvailable          =   -5,
        NetworkError             =   -6,
        AccessDenied             =   -7,
        // Note: -8 and -9 are reserved (not used in public constants)
        InsufficientStorage      =  -10,
        PlayStoreNotFound        =  -11,
        NetworkUnrestricted      =  -12,
        AppNotOwned              =  -13,
        ConfirmationNotRequired  =  -14,
        UnrecognizedInstallation =  -15,
        InternalError            = -100,

        // BizSim extensions — stay below -200 to never collide with Google's int space
        BridgeNotInitialized     = -200,
        Timeout                  = -201,
        CancelledByCaller        = -202,
        StuckAtZero              = -203,  // r2: ADR-016 stall detector — download made no progress for StallTimeoutSeconds
        EditorMockError          = -204,
        InvalidPackName          = -205,  // Q14 — validator rejected the pack name
    }
}
