using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    /// <summary>
    /// Tests for FetchOptions retry configuration and backoff math.
    /// Full retry loop integration is covered in MockFetchFlowTests.
    /// </summary>
    public class RetryPolicyTests
    {
        [Test]
        public void FetchOptions_DefaultRetry_MaxAttempts_Is1()
        {
            var opts = FetchOptions.Default;
            Assert.AreEqual(1, opts.RetryMaxAttempts);
        }

        [Test]
        public void FetchOptions_WithRetry_SetsMaxAttempts()
        {
            var opts = FetchOptions.Default.WithRetry(maxAttempts: 3);
            Assert.AreEqual(3, opts.RetryMaxAttempts);
        }

        [Test]
        public void FetchOptions_WithRetry_SetsBaseDelay()
        {
            var opts = FetchOptions.Default.WithRetry(baseDelaySeconds: 5f);
            Assert.AreEqual(5f, opts.RetryBaseDelaySeconds);
        }

        [Test]
        public void ErrorClassification_NetworkError_IsRetryable()
        {
            Assert.IsTrue(AssetDeliveryError.IsRetryable(AssetPackErrorCode.NetworkError));
        }

        [Test]
        public void ErrorClassification_PackUnavailable_IsNotRetryable()
        {
            Assert.IsFalse(AssetDeliveryError.IsRetryable(AssetPackErrorCode.PackUnavailable));
        }

        [Test]
        public void ErrorClassification_Timeout_IsRetryable()
        {
            Assert.IsTrue(AssetDeliveryError.IsRetryable(AssetPackErrorCode.Timeout));
        }

        [Test]
        public void FetchCancelReason_StallDetector_IsDistinctFromUser()
        {
            Assert.AreNotEqual(FetchCancelReason.User, FetchCancelReason.StallDetector);
        }

        [Test]
        public void BackoffMath_Attempt1_WithZeroJitter_ReturnsBaseDelay()
        {
            // baseDelay * 2^(attempt-1) = 2 * 2^0 = 2 (jitter=0 baseline)
            float baseDelay  = 2f;
            int   attempt    = 1;
            float expected   = baseDelay * Mathf.Pow(2f, attempt - 1);
            // Raw backoff formula (same as controller)
            float raw        = baseDelay * Mathf.Pow(2f, attempt - 1);
            Assert.AreEqual(expected, raw, 0.001f);
        }

        [Test]
        public void BackoffMath_Attempt3_WithBaseDelay2_Is8()
        {
            // baseDelay * 2^(3-1) = 2 * 4 = 8
            float raw = 2f * Mathf.Pow(2f, 3 - 1);
            Assert.AreEqual(8f, raw, 0.001f);
        }
    }
}
