using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.Core {
    /// <summary>
    /// 다운로드 작업의 동시 실행 수와 표시 슬롯을 UI 프레임워크와 독립적으로 관리합니다.
    /// </summary>
    public sealed class DownloadQueueService : IDisposable {
        private readonly SemaphoreSlim semaphore;
        private readonly Queue<int> availableSlots;
        private readonly object syncRoot = new object();
        private bool disposed;

        public DownloadQueueService(int maxConcurrency) {
            if (maxConcurrency < 1) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            availableSlots = new Queue<int>(maxConcurrency);
            for (int index = 0; index < maxConcurrency; index++) availableSlots.Enqueue(index);
        }

        public async Task<int> AcquireSlotAsync(CancellationToken cancellationToken) {
            ThrowIfDisposed();
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (syncRoot) return availableSlots.Dequeue();
        }

        public void ReleaseSlot(int slot) {
            if (slot < 0 || disposed) return;
            lock (syncRoot) availableSlots.Enqueue(slot);
            semaphore.Release();
        }

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            semaphore.Dispose();
        }

        private void ThrowIfDisposed() {
            if (disposed) throw new ObjectDisposedException(nameof(DownloadQueueService));
        }
    }
}
