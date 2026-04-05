#if NET7_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hexa.NET.Utilities
{
    public struct ReaderWriterLockLight
    {
        private ulong state = 0;
        private const ulong writerBit = 1ul << 63;
        private const ulong readerMask = ~writerBit;

        public ReaderWriterLockLight()
        {
        }

        public void EnterRead()
        {
            var value = Volatile.Read(ref state);
            do
            {
                if ((value & writerBit) != 0)
                {
                    do
                    {
                        WaitOnAddressHelper<ulong>.Wait(ref state, value);
                        value = Volatile.Read(ref state);
                    }
                    while ((value & writerBit) != 0);
                }
                var wanted = value + 1;
                if (wanted > readerMask)
                {
                    throw new InvalidOperationException("Too many concurrent readers");
                }
                var newValue = Interlocked.CompareExchange(ref state, wanted, value);
                if (newValue == value)
                {
                    break;
                }
                value = newValue;
            } while (true);
        }

        public void ExitRead()
        {
            Interlocked.Decrement(ref state);
            WaitOnAddressHelper<ulong>.SignalAll(ref state); // We sadly cannot use SignalSingle here because if a writer is waiting and a reader too then the reader could steal the signal from the writer causing a deadlock.
        }

        public void EnterWrite()
        {
            ulong oldValue;
            while (true)
            {
                oldValue = Interlocked.Or(ref state, writerBit);
                if ((oldValue & writerBit) == 0)
                {
                    break;
                }
                WaitOnAddressHelper<ulong>.Wait(ref state, oldValue);
            }

            while (((oldValue = Volatile.Read(ref state)) & readerMask) != 0)
            {
                WaitOnAddressHelper<ulong>.Wait(ref state, oldValue);
            }
        }

        public void ExitWrite()
        {
            Interlocked.And(ref state, readerMask);
            WaitOnAddressHelper<ulong>.SignalAll(ref state);
        }

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
    }
}

#endif