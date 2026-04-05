#if NET7_0_OR_GREATER

namespace Hexa.NET.Utilities.Tests
{
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class ReaderWriterLockLightTests
    {
        // Wraps the struct in an array to give it reference semantics for lambda capture.
        private static ReaderWriterLockLight[] MakeLock() => new ReaderWriterLockLight[1];

        #region Single-threaded correctness

        [Test]
        public void EnterRead_ExitRead_AllowsSubsequentWrite()
        {
            var locks = MakeLock();
            locks[0].EnterRead();
            locks[0].ExitRead();
            locks[0].EnterWrite();
            locks[0].ExitWrite();
        }

        [Test]
        public void EnterWrite_ExitWrite_AllowsSubsequentRead()
        {
            var locks = MakeLock();
            locks[0].EnterWrite();
            locks[0].ExitWrite();
            locks[0].EnterRead();
            locks[0].ExitRead();
        }

        [Test]
        public void MultipleSequentialReadLocks_Succeed()
        {
            var locks = MakeLock();
            for (int i = 0; i < 20; i++)
            {
                locks[0].EnterRead();
                locks[0].ExitRead();
            }
        }

        [Test]
        public void MultipleSequentialWriteLocks_Succeed()
        {
            var locks = MakeLock();
            for (int i = 0; i < 20; i++)
            {
                locks[0].EnterWrite();
                locks[0].ExitWrite();
            }
        }

        [Test]
        public void InterleavedReadAndWrite_Succeed()
        {
            var locks = MakeLock();
            for (int i = 0; i < 10; i++)
            {
                locks[0].EnterRead();
                locks[0].ExitRead();
                locks[0].EnterWrite();
                locks[0].ExitWrite();
            }
        }

        [Test]
        public void NestedReads_MultipleReadersOnSingleThread_Succeed()
        {
            // Multiple EnterRead calls before ExitRead (simulating nested readers)
            var locks = MakeLock();
            locks[0].EnterRead();
            locks[0].EnterRead();
            locks[0].EnterRead();
            locks[0].ExitRead();
            locks[0].ExitRead();
            locks[0].ExitRead();
        }

        #endregion Single-threaded correctness

        #region Concurrent exclusion

        [Test]
        public void MultipleReaders_CanHoldLockSimultaneously()
        {
            var locks = MakeLock();
            const int readerCount = 8;
            var allInsideEvent = new CountdownEvent(readerCount);
            var proceedEvent = new ManualResetEventSlim(false);
            int insideCount = 0;

            var tasks = new Task[readerCount];
            for (int i = 0; i < readerCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    locks[0].EnterRead();
                    Interlocked.Increment(ref insideCount);
                    allInsideEvent.Signal();
                    proceedEvent.Wait();
                    Interlocked.Decrement(ref insideCount);
                    locks[0].ExitRead();
                });
            }

            Assert.That(allInsideEvent.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Not all readers acquired the lock in time");
            Assert.That(Volatile.Read(ref insideCount), Is.EqualTo(readerCount),
                "All readers should hold the lock simultaneously");

            proceedEvent.Set();
            Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(10)), Is.True,
                "Tasks did not complete in time");
        }

        [Test]
        public void Writer_BlocksOtherWriters()
        {
            var locks = MakeLock();
            var writer1Inside = new ManualResetEventSlim(false);
            var releaseWriter1 = new ManualResetEventSlim(false);
            bool writer2Entered = false;

            var writer1 = Task.Run(() =>
            {
                locks[0].EnterWrite();
                writer1Inside.Set();
                releaseWriter1.Wait();
                locks[0].ExitWrite();
            });

            Assert.That(writer1Inside.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Writer1 did not acquire lock in time");

            var writer2 = Task.Run(() =>
            {
                locks[0].EnterWrite();
                Volatile.Write(ref writer2Entered, true);
                locks[0].ExitWrite();
            });

            Thread.Sleep(50);
            Assert.That(Volatile.Read(ref writer2Entered), Is.False,
                "Writer2 should be blocked while writer1 holds the lock");

            releaseWriter1.Set();
            Assert.That(Task.WaitAll(new[] { writer1, writer2 }, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(writer2Entered, Is.True,
                "Writer2 should have entered after writer1 released the lock");
        }

        [Test]
        public void Reader_WaitsForActiveWriter()
        {
            var locks = MakeLock();
            var writerInside = new ManualResetEventSlim(false);
            var releaseWriter = new ManualResetEventSlim(false);
            bool readerEntered = false;

            var writer = Task.Run(() =>
            {
                locks[0].EnterWrite();
                writerInside.Set();
                releaseWriter.Wait();
                locks[0].ExitWrite();
            });

            Assert.That(writerInside.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Writer did not acquire lock in time");

            var reader = Task.Run(() =>
            {
                locks[0].EnterRead();
                Volatile.Write(ref readerEntered, true);
                locks[0].ExitRead();
            });

            Thread.Sleep(50);
            Assert.That(Volatile.Read(ref readerEntered), Is.False,
                "Reader should be blocked while writer holds the lock");

            releaseWriter.Set();
            Assert.That(Task.WaitAll(new[] { writer, reader }, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(readerEntered, Is.True,
                "Reader should have entered after writer released the lock");
        }

        [Test]
        public void Writer_WaitsForSingleActiveReader()
        {
            var locks = MakeLock();
            var readerInside = new ManualResetEventSlim(false);
            var releaseReader = new ManualResetEventSlim(false);
            bool writerEntered = false;

            var reader = Task.Run(() =>
            {
                locks[0].EnterRead();
                readerInside.Set();
                releaseReader.Wait();
                locks[0].ExitRead();
            });

            Assert.That(readerInside.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Reader did not acquire lock in time");

            var writer = Task.Run(() =>
            {
                locks[0].EnterWrite();
                Volatile.Write(ref writerEntered, true);
                locks[0].ExitWrite();
            });

            Thread.Sleep(50);
            Assert.That(Volatile.Read(ref writerEntered), Is.False,
                "Writer should be blocked while reader holds the lock");

            releaseReader.Set();
            Assert.That(Task.WaitAll(new[] { reader, writer }, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(writerEntered, Is.True,
                "Writer should have entered after reader released the lock");
        }

        [Test]
        public void Writer_WaitsForMultipleActiveReaders()
        {
            var locks = MakeLock();
            const int readerCount = 4;
            var allReadersInside = new CountdownEvent(readerCount);
            var releaseReaders = new ManualResetEventSlim(false);
            bool writerEntered = false;

            var readers = new Task[readerCount];
            for (int i = 0; i < readerCount; i++)
            {
                readers[i] = Task.Run(() =>
                {
                    locks[0].EnterRead();
                    allReadersInside.Signal();
                    releaseReaders.Wait();
                    locks[0].ExitRead();
                });
            }

            Assert.That(allReadersInside.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Not all readers acquired the lock in time");

            var writer = Task.Run(() =>
            {
                locks[0].EnterWrite();
                Volatile.Write(ref writerEntered, true);
                locks[0].ExitWrite();
            });

            Thread.Sleep(50);
            Assert.That(Volatile.Read(ref writerEntered), Is.False,
                "Writer should be blocked while readers hold the lock");

            releaseReaders.Set();
            var all = new Task[readerCount + 1];
            readers.CopyTo(all, 0);
            all[readerCount] = writer;
            Assert.That(Task.WaitAll(all, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(writerEntered, Is.True,
                "Writer should have entered after all readers released the lock");
        }

        [Test]
        public void MultipleWaitingReaders_AllWakeAfterWriterExits()
        {
            var locks = MakeLock();
            var writerInside = new ManualResetEventSlim(false);
            var releaseWriter = new ManualResetEventSlim(false);
            const int readerCount = 6;
            int readersEntered = 0;
            var allReadersEntered = new CountdownEvent(readerCount);

            var writer = Task.Run(() =>
            {
                locks[0].EnterWrite();
                writerInside.Set();
                releaseWriter.Wait();
                locks[0].ExitWrite();
            });

            Assert.That(writerInside.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Writer did not acquire lock in time");

            var readers = new Task[readerCount];
            for (int i = 0; i < readerCount; i++)
            {
                readers[i] = Task.Run(() =>
                {
                    locks[0].EnterRead();
                    Interlocked.Increment(ref readersEntered);
                    allReadersEntered.Signal();
                    locks[0].ExitRead();
                });
            }

            releaseWriter.Set();
            Assert.That(allReadersEntered.Wait(TimeSpan.FromSeconds(10)), Is.True,
                "Not all waiting readers woke up after writer exited");
            Assert.That(Volatile.Read(ref readersEntered), Is.EqualTo(readerCount));
        }

        #endregion Concurrent exclusion

        #region Stress tests

        [Test]
        public void StressTest_ConcurrentReadersAndWriters_DataRemainsConsistent()
        {
            var locks = MakeLock();
            int sharedValue = 0;
            bool inconsistencyDetected = false;
            const int iterations = 500;
            const int readerCount = 8;
            const int writerCount = 4;

            var writers = new Task[writerCount];
            for (int i = 0; i < writerCount; i++)
            {
                writers[i] = Task.Run(() =>
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        locks[0].EnterWrite();
                        sharedValue++;
                        locks[0].ExitWrite();
                    }
                });
            }

            var readers = new Task[readerCount];
            for (int i = 0; i < readerCount; i++)
            {
                readers[i] = Task.Run(() =>
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        locks[0].EnterRead();
                        int v1 = Volatile.Read(ref sharedValue);
                        Thread.SpinWait(5);
                        int v2 = Volatile.Read(ref sharedValue);
                        if (v1 != v2)
                        {
                            Volatile.Write(ref inconsistencyDetected, true);
                        }
                        locks[0].ExitRead();
                    }
                });
            }

            var all = new Task[writerCount + readerCount];
            writers.CopyTo(all, 0);
            readers.CopyTo(all, writerCount);

            Assert.That(Task.WaitAll(all, TimeSpan.FromSeconds(30)), Is.True,
                "Stress test timed out");
            Assert.That(inconsistencyDetected, Is.False,
                "Data inconsistency detected: shared value changed while a read lock was held");
            Assert.That(sharedValue, Is.EqualTo(writerCount * iterations),
                "Final shared value does not match expected number of increments");
        }

        [Test]
        public void StressTest_HighContention_NoDeadlock()
        {
            var locks = MakeLock();
            const int threadCount = 16;
            const int iterations = 200;
            int counter = 0;

            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                bool isWriter = i % 4 == 0;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        if (isWriter)
                        {
                            locks[0].EnterWrite();
                            counter++;
                            locks[0].ExitWrite();
                        }
                        else
                        {
                            locks[0].EnterRead();
                            _ = Volatile.Read(ref counter);
                            locks[0].ExitRead();
                        }
                    }
                });
            }

            Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(30)), Is.True,
                "Deadlock or timeout detected under high contention");
        }

        #endregion Stress tests

        #region LockGuard – single-threaded

        [Test]
        public void LockGuard_Unlock_WhenNoneState_DoesNotThrow()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireWriteLock();
            guard.Unlock();
            guard.Unlock(); // second call while in None state should be a no-op
        }

        [Test]
        public void LockGuard_DoubleDispose_DoesNotThrow()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireWriteLock();
            guard.Dispose();
            guard.Dispose();
        }

        [Test]
        public void LockGuard_Unlock_ThenLockRead_Succeeds()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireWriteLock();
            guard.Unlock();
            guard.LockRead();
            guard.Dispose();
        }

        [Test]
        public void LockGuard_Unlock_ThenLockWrite_Succeeds()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireReadLock();
            guard.Unlock();
            guard.LockWrite();
            guard.Dispose();
        }

        [Test]
        public void LockGuard_LockWrite_WhenWriteAlreadyHeld_Throws()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireWriteLock();
            bool threw = false;
            try
            {
                guard.LockWrite();
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            guard.Dispose();
            Assert.That(threw, Is.True,
                "Expected InvalidOperationException when acquiring write lock while write lock is already held");
        }

        [Test]
        public void LockGuard_LockRead_WhenReadAlreadyHeld_Throws()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireReadLock();
            bool threw = false;
            try
            {
                guard.LockRead();
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            guard.Dispose();
            Assert.That(threw, Is.True,
                "Expected InvalidOperationException when acquiring read lock while read lock is already held");
        }

        [Test]
        public void LockGuard_LockWrite_WhenReadAlreadyHeld_Throws()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireReadLock();
            bool threw = false;
            try
            {
                guard.LockWrite();
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            guard.Dispose();
            Assert.That(threw, Is.True,
                "Expected InvalidOperationException when acquiring write lock while read lock is already held");
        }

        [Test]
        public void LockGuard_LockRead_WhenWriteAlreadyHeld_Throws()
        {
            var locks = MakeLock();
            var guard = locks[0].AcquireWriteLock();
            bool threw = false;
            try
            {
                guard.LockRead();
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            guard.Dispose();
            Assert.That(threw, Is.True,
                "Expected InvalidOperationException when acquiring read lock while write lock is already held");
        }

        #endregion LockGuard – single-threaded
    }
}

#endif