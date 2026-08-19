using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.services {
    /// <summary>
    /// 다운로드 작업의 동시 실행 수와 진행 표시 슬롯을 관리합니다.
    /// UI는 슬롯 번호를 이용해 표시만 하고, 큐 내부 상태에는 직접 접근하지 않습니다.
    /// </summary>
    internal sealed class DownloadQueueService : IDisposable {
        private readonly SemaphoreSlim semaphore;
        private readonly Queue<int> availableSlots;
        private readonly object syncRoot = new object();
        private bool disposed;

        public DownloadQueueService(int maxConcurrency) {
            if (maxConcurrency < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

            semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            availableSlots = new Queue<int>(maxConcurrency);
            for (int index = 0; index < maxConcurrency; index++)
                availableSlots.Enqueue(index);
        }

        public async Task<int> AcquireSlotAsync(CancellationToken cancellationToken) {
            ThrowIfDisposed();
            await semaphore.WaitAsync(cancellationToken);
            lock (syncRoot) {
                return availableSlots.Dequeue();
            }
        }

        public void ReleaseSlot(int slot) {
            if (slot < 0 || disposed)
                return;

            lock (syncRoot) {
                availableSlots.Enqueue(slot);
            }
            semaphore.Release();
        }

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            semaphore.Dispose();
        }

        private void ThrowIfDisposed() {
            if (disposed)
                throw new ObjectDisposedException(nameof(DownloadQueueService));
        }
    }
}
