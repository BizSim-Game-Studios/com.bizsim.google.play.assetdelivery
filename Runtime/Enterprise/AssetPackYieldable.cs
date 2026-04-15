using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery
{
    // ─── CustomYieldInstruction companions (ADR-032, §E3) ───────────────────────
    // Public surface is HARD LOCK per SemVer §SOFT LOCK.
    // Accessing Result on a faulted or canceled yieldable returns default — does NOT throw.
    // Consumers check IsCompletedSuccessfully / Error / IsCanceled first.

    /// <summary>Wraps Task&lt;AssetPackHandle&gt; as a coroutine-friendly CustomYieldInstruction.</summary>
    public sealed class AssetPackYieldable : CustomYieldInstruction
    {
        private readonly Task<AssetPackHandle> _task;

        public AssetPackYieldable(Task<AssetPackHandle> task) { _task = task ?? throw new ArgumentNullException(nameof(task)); }

        public override bool keepWaiting => !_task.IsCompleted;

        public AssetPackHandle Result           => _task.IsCompletedSuccessfully ? _task.Result : default;
        public Exception       Error            => _task.IsFaulted   ? (_task.Exception?.InnerException ?? _task.Exception)
                                                 : _task.IsCanceled  ? new OperationCanceledException()
                                                                      : null;
        public bool IsCanceled               => _task.IsCanceled;
        public bool IsFaulted                => _task.IsFaulted;
        public bool IsCompletedSuccessfully  => _task.IsCompletedSuccessfully;
    }

    /// <summary>Wraps Task&lt;AssetPackGroupResult&gt; as a coroutine-friendly CustomYieldInstruction.</summary>
    public sealed class AssetPackGroupYieldable : CustomYieldInstruction
    {
        private readonly Task<AssetPackGroupResult> _task;

        public AssetPackGroupYieldable(Task<AssetPackGroupResult> task) { _task = task ?? throw new ArgumentNullException(nameof(task)); }

        public override bool keepWaiting => !_task.IsCompleted;

        public AssetPackGroupResult Result          => _task.IsCompletedSuccessfully ? _task.Result : default;
        public Exception            Error           => _task.IsFaulted   ? (_task.Exception?.InnerException ?? _task.Exception)
                                                    : _task.IsCanceled  ? new OperationCanceledException()
                                                                        : null;
        public bool IsCanceled              => _task.IsCanceled;
        public bool IsFaulted               => _task.IsFaulted;
        public bool IsCompletedSuccessfully => _task.IsCompletedSuccessfully;
    }

    /// <summary>Wraps Task&lt;AssetBundle&gt; as a coroutine-friendly CustomYieldInstruction.</summary>
    public sealed class AssetBundleYieldable : CustomYieldInstruction
    {
        private readonly Task<AssetBundle> _task;

        public AssetBundleYieldable(Task<AssetBundle> task) { _task = task ?? throw new ArgumentNullException(nameof(task)); }

        public override bool keepWaiting => !_task.IsCompleted;

        public AssetBundle Result           => _task.IsCompletedSuccessfully ? _task.Result : default;
        public Exception   Error            => _task.IsFaulted   ? (_task.Exception?.InnerException ?? _task.Exception)
                                            : _task.IsCanceled  ? new OperationCanceledException()
                                                                : null;
        public bool IsCanceled              => _task.IsCanceled;
        public bool IsFaulted               => _task.IsFaulted;
        public bool IsCompletedSuccessfully => _task.IsCompletedSuccessfully;
    }

    /// <summary>Wraps Task&lt;FetchPreflightResult&gt; as a coroutine-friendly CustomYieldInstruction.</summary>
    public sealed class AssetPackPreflightYieldable : CustomYieldInstruction
    {
        private readonly Task<FetchPreflightResult> _task;

        public AssetPackPreflightYieldable(Task<FetchPreflightResult> task) { _task = task ?? throw new ArgumentNullException(nameof(task)); }

        public override bool keepWaiting => !_task.IsCompleted;

        public FetchPreflightResult Result          => _task.IsCompletedSuccessfully ? _task.Result : default;
        public Exception            Error           => _task.IsFaulted   ? (_task.Exception?.InnerException ?? _task.Exception)
                                                    : _task.IsCanceled  ? new OperationCanceledException()
                                                                        : null;
        public bool IsCanceled              => _task.IsCanceled;
        public bool IsFaulted               => _task.IsFaulted;
        public bool IsCompletedSuccessfully => _task.IsCompletedSuccessfully;
    }
}
