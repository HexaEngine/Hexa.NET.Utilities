namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;

    public struct SemaphoreLight
    {
        private int currentCount;
        private readonly int maxCount;
        private int spinLock;

        public SemaphoreLight(int initialCount, int maxCount)
        {
            if (initialCount > maxCount || initialCount < 0 || maxCount <= 0)
            {
                throw new ArgumentException("Invalid semaphore initial or max count");
            }

            currentCount = initialCount;
            this.maxCount = maxCount;
            spinLock = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AcquireLock()
        {
            while (Interlocked.CompareExchange(ref spinLock, 1, 0) != 0)
            {
                Thread.Yield(); // Minimiert CPU-Belastung, aber ist noch kein Spin-Waiting
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseLock()
        {
            Volatile.Write(ref spinLock, 0);
        }

        public void Wait()
        {
            while (true)
            {
                AcquireLock();
                if (currentCount > 0)
                {
                    currentCount--;
                    ReleaseLock();
                    break;
                }
                ReleaseLock();
                Thread.Sleep(1); // Minimale Blockierung, um CPU zu entlasten, kein Spin-Waiting
            }
        }

        public void Release()
        {
            AcquireLock();
            if (currentCount < maxCount)
            {
                currentCount++;
            }
            else
            {
                throw new InvalidOperationException("Semaphore released too many times");
            }
            ReleaseLock();
        }
    }
}