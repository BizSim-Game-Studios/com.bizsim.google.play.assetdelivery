using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Atomic replacement of a <see cref="TaskCompletionSource{T}"/> field. Cancels any pending
    /// TCS on replacement so stale await continuations don't resolve against the new instance.
    /// Used by AssetDeliveryController's flow state machine.
    /// </summary>
    internal static class TcsGuard
    {
        internal static TaskCompletionSource<T> Replace<T>(ref TaskCompletionSource<T> field)
        {
            var previous = Interlocked.Exchange(ref field, new TaskCompletionSource<T>());
            if (previous != null)
            {
                bool wasCanceled = previous.TrySetCanceled();
                if (wasCanceled)
                    // Sub-tag `[TcsGuard]` removed per CROSS-INVARIANTS §12.4 — BizSimLogger.Prefix is the only bracket.
                    BizSimLogger.Warning($"Previous TCS<{typeof(T).Name}> was still pending — canceled it in TcsGuard.Replace");
            }
            return field;
        }
    }
}
