using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.Core {
    public sealed class DownloadQueueService : IDisposable {
        private readonly SemaphoreSlim semaphore;
        private readonly Queue<int> slots = new Queue<int>();
        private readonly HashSet<int> leased = new HashSet<int>();
        private readonly object sync = new object();
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private bool disposed;
        private int waiters;

        public DownloadQueueService(int maxConcurrency) {
            if (maxConcurrency < 1 || maxConcurrency > 16) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            for (int i = 0; i < maxConcurrency; i++) slots.Enqueue(i);
        }

        public async Task<int> AcquireSlotAsync(CancellationToken token) {
            CancellationTokenSource linked;
            lock (sync) {
                if (disposed) throw new ObjectDisposedException(nameof(DownloadQueueService));
                linked = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
                waiters++;
            }
            try {
                await semaphore.WaitAsync(linked.Token).ConfigureAwait(false);
                lock (sync) {
                    if (disposed) { semaphore.Release(); throw new OperationCanceledException(linked.Token); }
                    int slot = slots.Dequeue();
                    leased.Add(slot);
                    return slot;
                }
            } finally {
                linked.Dispose();
                lock (sync) { waiters--; DisposeWhenIdle(); }
            }
        }

        public void ReleaseSlot(int slot) {
            lock (sync) {
                if (!leased.Remove(slot)) return;
                slots.Enqueue(slot);
                semaphore.Release();
                DisposeWhenIdle();
            }
        }

        public void Dispose() {
            lock (sync) {
                if (disposed) return;
                disposed = true;
                shutdown.Cancel();
                DisposeWhenIdle();
            }
        }

        private void DisposeWhenIdle() {
            if (disposed && waiters == 0 && leased.Count == 0) { semaphore.Dispose(); shutdown.Dispose(); }
        }
    }
}
