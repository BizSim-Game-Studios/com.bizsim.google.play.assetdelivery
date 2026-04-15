using NUnit.Framework;
using BizSim.Google.Play.AssetDelivery;

public class AssetPackEnumParityTests
{
    [TestCase(0, (int)AssetPackStatus.Unknown)]
    [TestCase(1, (int)AssetPackStatus.Pending)]
    [TestCase(2, (int)AssetPackStatus.Downloading)]
    [TestCase(3, (int)AssetPackStatus.Transferring)]
    [TestCase(4, (int)AssetPackStatus.Completed)]
    [TestCase(5, (int)AssetPackStatus.Failed)]
    [TestCase(6, (int)AssetPackStatus.Canceled)]
    [TestCase(7, (int)AssetPackStatus.WaitingForWifi)]
    [TestCase(8, (int)AssetPackStatus.NotInstalled)]
    [TestCase(9, (int)AssetPackStatus.RequiresUserConfirmation)]
    public void AssetPackStatus_MatchesGoogle(int expected, int actual) => Assert.AreEqual(expected, actual);

    [TestCase(0, (int)AssetPackErrorCode.NoError)]
    [TestCase(-1, (int)AssetPackErrorCode.AppUnavailable)]
    [TestCase(-2, (int)AssetPackErrorCode.PackUnavailable)]
    [TestCase(-3, (int)AssetPackErrorCode.InvalidRequest)]
    [TestCase(-4, (int)AssetPackErrorCode.DownloadNotFound)]
    [TestCase(-5, (int)AssetPackErrorCode.ApiNotAvailable)]
    [TestCase(-6, (int)AssetPackErrorCode.NetworkError)]
    [TestCase(-7, (int)AssetPackErrorCode.AccessDenied)]
    [TestCase(-10, (int)AssetPackErrorCode.InsufficientStorage)]
    [TestCase(-11, (int)AssetPackErrorCode.PlayStoreNotFound)]
    [TestCase(-12, (int)AssetPackErrorCode.NetworkUnrestricted)]
    [TestCase(-13, (int)AssetPackErrorCode.AppNotOwned)]
    [TestCase(-14, (int)AssetPackErrorCode.ConfirmationNotRequired)]
    [TestCase(-15, (int)AssetPackErrorCode.UnrecognizedInstallation)]
    [TestCase(-100, (int)AssetPackErrorCode.InternalError)]
    public void AssetPackErrorCode_MatchesGoogle(int expected, int actual) => Assert.AreEqual(expected, actual);

    [TestCase(0, (int)AssetPackStorageMethod.StorageFiles)]
    [TestCase(1, (int)AssetPackStorageMethod.ApkAssets)]
    public void AssetPackStorageMethod_MatchesGoogle(int expected, int actual) => Assert.AreEqual(expected, actual);

    [TestCase(-1, (int)ActivityResultCode.Ok)]
    [TestCase(0, (int)ActivityResultCode.Canceled)]
    public void ActivityResultCode_MatchesAndroid(int expected, int actual) => Assert.AreEqual(expected, actual);
}
