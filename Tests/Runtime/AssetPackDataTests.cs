using System;
using System.Collections.Generic;
using NUnit.Framework;
using BizSim.Google.Play.AssetDelivery;

public class AssetPackDataTests
{
    [Test] public void DownloadProgress_ZeroTotal_ReturnsZero() {
        var s = new AssetPackState("p", AssetPackStatus.Pending, AssetPackErrorCode.NoError, 0, 0, 0, DateTime.UtcNow);
        Assert.AreEqual(0f, s.DownloadProgress);
    }
    [Test] public void DownloadProgress_Half_Returns050() {
        var s = new AssetPackState("p", AssetPackStatus.Downloading, AssetPackErrorCode.NoError, 500, 1000, 50, DateTime.UtcNow);
        Assert.AreEqual(0.5f, s.DownloadProgress, 0.001f);
    }
    [Test] public void IsTerminal_Completed_True() {
        var s = new AssetPackState("p", AssetPackStatus.Completed, AssetPackErrorCode.NoError, 1000, 1000, 100, DateTime.UtcNow);
        Assert.IsTrue(s.IsTerminal);
    }
    [Test] public void RequiresConfirmation_WaitingForWifi_True() {
        var s = new AssetPackState("p", AssetPackStatus.WaitingForWifi, AssetPackErrorCode.NoError, 500, 1000, 0, DateTime.UtcNow);
        Assert.IsTrue(s.RequiresConfirmation);
    }
    [Test] public void AssetPackStates_TotalBytesDownloaded_SumsAcrossPacks() {
        var dict = new Dictionary<string, AssetPackState>
        {
            { "a", new AssetPackState("a", AssetPackStatus.Completed, AssetPackErrorCode.NoError, 100, 100, 100, DateTime.UtcNow) },
            { "b", new AssetPackState("b", AssetPackStatus.Downloading, AssetPackErrorCode.NoError, 50, 200, 25, DateTime.UtcNow) },
        };
        var states = new AssetPackStates(dict, DateTime.UtcNow);
        Assert.AreEqual(150L, states.TotalBytesDownloaded);
        Assert.AreEqual(300L, states.TotalBytesToDownload);
    }
    [Test] public void AssetPackStates_AllCompleted_MixedStatus_False() {
        var dict = new Dictionary<string, AssetPackState>
        {
            { "a", new AssetPackState("a", AssetPackStatus.Completed, AssetPackErrorCode.NoError, 100, 100, 100, DateTime.UtcNow) },
            { "b", new AssetPackState("b", AssetPackStatus.Downloading, AssetPackErrorCode.NoError, 50, 200, 25, DateTime.UtcNow) },
        };
        Assert.IsFalse(new AssetPackStates(dict, DateTime.UtcNow).AllCompleted);
    }
    [Test] public void AssetPackLocation_IsValid_EmptyPath_False() {
        var loc = new AssetPackLocation("pack", "", "", AssetPackStorageMethod.StorageFiles);
        Assert.IsFalse(loc.IsValid);
    }
    [Test] public void AssetDeliveryError_IsRetryable_NetworkError_True() =>
        Assert.IsTrue(AssetDeliveryError.IsRetryable(AssetPackErrorCode.NetworkError));
    [Test] public void AssetDeliveryError_IsRetryable_PackUnavailable_False() =>
        Assert.IsFalse(AssetDeliveryError.IsRetryable(AssetPackErrorCode.PackUnavailable));
}
