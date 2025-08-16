namespace Hexa.NET.Utilities.Threading
{
    using System;

    public class ReadWriteLock : IDisposable
    {
        private readonly ManualResetEventSlim writeLock = new(true);
        private readonly ManualResetEventSlim readLock = new(true);
        private readonly int maxReader;
        private readonly int maxWriter;
        private readonly SemaphoreSlim readSemaphore;
        private readonly SemaphoreSlim writeSemaphore;
        private bool disposedValue;

        public ReadWriteLock(int maxReader, int maxWriter)
        {
            this.maxReader = maxReader;
            this.maxWriter = maxWriter;
            readSemaphore = new(maxReader);
            writeSemaphore = new(maxWriter);
        }

        private readonly struct ReadBlock(ReadWriteLock readWriteLock) : IDisposable
        {
            private readonly ReadWriteLock readWriteLock = readWriteLock;

            public void Dispose()
            {
                readWriteLock.EndRead();
            }
        }

        private readonly struct WriteBlock(ReadWriteLock readWriteLock) : IDisposable
        {
            private readonly ReadWriteLock readWriteLock = readWriteLock;

            public void Dispose()
            {
                readWriteLock.EndWrite();
            }
        }

        public void BeginRead()
        {
            writeLock.Wait();
            readLock.Reset();
            readSemaphore.Wait();
        }

        public IDisposable BeginReadBlock()
        {
            BeginRead();
            return new ReadBlock(this);
        }

        public void EndRead()
        {
            var value = readSemaphore.Release();
            if (value == maxReader - 1)
            {
                readLock.Set();
            }
        }

        public void BeginWrite()
        {
            readLock.Wait();
            writeLock.Reset();
            writeSemaphore.Wait();
        }

        public IDisposable BeginWriteBlock()
        {
            BeginWrite();
            return new WriteBlock(this);
        }

        public void EndWrite()
        {
            var value = writeSemaphore.Release();
            if (value == maxWriter - 1)
            {
                writeLock.Set();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writeLock.Dispose();
                    readLock.Dispose();
                    writeSemaphore.Dispose();
                    readSemaphore.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}