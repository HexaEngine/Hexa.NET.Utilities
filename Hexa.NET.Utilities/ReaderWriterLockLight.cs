using Hexa.NET.Utilities.Native;
using System.Diagnostics.CodeAnalysis;
using ReaderWriterLock = Hexa.NET.Utilities.Native.ReaderWriterLock;

namespace Hexa.NET.Utilities
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ReaderWriterLockLight
    {
        private ReaderWriterLock cLock;

        public ReaderWriterLockLight()
        {
            HexaUtils.ReaderWriterLockInit((ReaderWriterLock*)Unsafe.AsPointer(ref cLock));
        }

        public void EnterRead()
        {
            int result = HexaUtils.ReaderWriterLockLockRead((ReaderWriterLock*)Unsafe.AsPointer(ref cLock));
            if (result == -1)
            {
                throw new OverflowException("Too many readers.");
            }
        }

        public bool TryEnterRead()
        {
            int result = HexaUtils.ReaderWriterLockTryLockRead((ReaderWriterLock*)Unsafe.AsPointer(ref this));
            if (result == -1)
            {
                throw new OverflowException("Too many readers.");
            }
            return result > 0;
        }

        public void ExitRead()
        {
            HexaUtils.ReaderWriterLockUnlockRead((ReaderWriterLock*)Unsafe.AsPointer(ref this));
        }

        public void EnterWrite()
        {
            HexaUtils.ReaderWriterLockLockWrite((ReaderWriterLock*)Unsafe.AsPointer(ref this));
        }

        public bool TryEnterWrite(bool preserveWriterFairness = true)
        {
            return HexaUtils.ReaderWriterLockTryLockWrite((ReaderWriterLock*)Unsafe.AsPointer(ref this), preserveWriterFairness) > 0;
        }

        public void ExitWrite()
        {
            HexaUtils.ReaderWriterLockUnlockWrite((ReaderWriterLock*)Unsafe.AsPointer(ref this));
        }

#if NET7_0_OR_GREATER

        [UnscopedRef]
        public LockGuard AcquireReadLock() => new(ref this, false);

        [UnscopedRef]
        public LockGuard AcquireWriteLock() => new(ref this, true);

        public ref struct LockGuard : IDisposable
        {
            private ref ReaderWriterLockLight rwLock;
            private LockState state;

            private enum LockState
            {
                None,
                Read,
                Write
            }

            public LockGuard(ref ReaderWriterLockLight rwLock, bool isWrite)
            {
                this.rwLock = ref rwLock;
                if (isWrite)
                {
                    state = LockState.Write;
                    rwLock.EnterWrite();
                }
                else
                {
                    state = LockState.Read;
                    rwLock.EnterRead();
                }
            }

            public void Unlock()
            {
                if (state == LockState.Write)
                {
                    rwLock.ExitWrite();
                }
                else if (state == LockState.Read)
                {
                    rwLock.ExitRead();
                }
                state = LockState.None;
            }

            public void LockWrite()
            {
                if (state != LockState.None) throw new InvalidOperationException("Lock already held");
                state = LockState.Write;
                rwLock.EnterWrite();
            }

            public void LockRead()
            {
                if (state != LockState.None) throw new InvalidOperationException("Lock already held");
                state = LockState.Read;
                rwLock.EnterRead();
            }

            public void Dispose()
            {
                Unlock();
            }
        }

#endif
    }
}