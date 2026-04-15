using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AssetDelivery
{
    /// <summary>
    /// Bounded queue that backs AssetDeliveryController.ReadStatesAsync.
    /// Capacity is sourced from AssetDeliverySettings.StateQueueCapacity at Awake time.
    /// On overflow, the OLDEST snapshot is dropped (latest state is the most actionable).
    /// </summary>
    public sealed class AssetPackStatesStream
    {
        private readonly int _maxCapacity;
        private readonly Queue<IReadOnlyDictionary<string, AssetPackState>> _queue;
        private readonly object _lock = new object();

        // Signal: when a snapshot is enqueued, complete this TCS so the consumer's iterator
        // wakes up. The consumer then creates a new TCS for the next wait.
        private TaskCompletionSource<bool> _signal =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _disposed;

        public AssetPackStatesStream(int maxCapacity)
        {
            _maxCapacity = maxCapacity > 0 ? maxCapacity : 32;
            _queue       = new Queue<IReadOnlyDictionary<string, AssetPackState>>(_maxCapacity);
        }

        /// <summary>
        /// Enqueues a snapshot. If the queue is full, the oldest snapshot is dropped first.
        /// Thread-safe — may be called from HandleState on any thread (but typically main thread).
        /// </summary>
        public void Enqueue(IReadOnlyDictionary<string, AssetPackState> snapshot)
        {
            TaskCompletionSource<bool> toComplete = null;
            lock (_lock)
            {
                if (_disposed) return;
                if (_queue.Count >= _maxCapacity)
                    _queue.Dequeue(); // drop oldest — latest is more actionable
                _queue.Enqueue(new Dictionary<string, AssetPackState>(
                    (Dictionary<string, AssetPackState>)snapshot)); // defensive copy
                toComplete = _signal;
                _signal    = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            toComplete?.TrySetResult(true);
        }

        /// <summary>
        /// Async iterator yielding snapshots as they arrive.
        /// Each yielded item is a defensive copy — mutating it does not affect the queue.
        /// </summary>
        public async IAsyncEnumerable<IReadOnlyDictionary<string, AssetPackState>> ReadAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested && !_disposed)
            {
                TaskCompletionSource<bool> currentSignal;
                lock (_lock)
                {
                    if (_queue.Count > 0)
                    {
                        yield return _queue.Dequeue();
                        continue;
                    }
                    currentSignal = _signal;
                }

                // Wait for the next item or cancellation.
                var waitTask   = currentSignal.Task;
                var cancelTask = Task.Delay(Timeout.Infinite, ct);
                var winner     = await Task.WhenAny(waitTask, cancelTask).ConfigureAwait(false);
                if (winner == cancelTask) yield break;
            }
        }

        /// <summary>Drain and reset — used by ClearForTesting.</summary>
        public void Clear()
        {
            lock (_lock) { _queue.Clear(); }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _signal.TrySetCanceled();
            }
        }
    }
}
