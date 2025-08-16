namespace Hexa.NET.Utilities
{
    using System.Runtime.InteropServices;

    public unsafe partial struct UnsafeMutex
    {
        private void* _handle;

#if NET7_0_OR_GREATER

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial void* CreateMutexW(void* lpMutexAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInitialOwner, char* lpName);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ReleaseMutex(void* hMutex);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial uint WaitForSingleObject(void* hHandle, uint dwMilliseconds);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(void* hObject);

        [LibraryImport("libpthread.so.0", EntryPoint = "pthread_mutex_init")]
        private static partial int PthreadMutexInit(void* mutex, void* attr);

        [LibraryImport("libpthread.so.0", EntryPoint = "pthread_mutex_lock")]
        private static partial int PthreadMutexLock(void* mutex);

        [LibraryImport("libpthread.so.0", EntryPoint = "pthread_mutex_timedlock")]
        private static partial int PthreadMutexTimedLock(void* mutex, Timespec* absTimeout);

        [LibraryImport("libpthread.so.0", EntryPoint = "pthread_mutex_unlock")]
        private static partial int PthreadMutexUnlock(void* mutex);

        [LibraryImport("libpthread.so.0", EntryPoint = "pthread_mutex_destroy")]
        private static partial int PthreadMutexDestroy(void* mutex);

#else
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void* CreateMutexW(void* lpMutexAttributes, bool bInitialOwner, char* lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReleaseMutex(void* hMutex);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(void* hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(void* hObject);

        [DllImport("libpthread.so.0", EntryPoint = "pthread_mutex_init")]
        private static extern int PthreadMutexInit(void* mutex, void* attr);

        [DllImport("libpthread.so.0", EntryPoint = "pthread_mutex_lock")]
        private static extern int PthreadMutexLock(void* mutex);

        [DllImport("libpthread.so.0", EntryPoint = "pthread_mutex_timedlock")]
        private static extern int PthreadMutexTimedLock(void* mutex, Timespec* absTimeout);

        [DllImport("libpthread.so.0", EntryPoint = "pthread_mutex_unlock")]
        private static extern int PthreadMutexUnlock(void* mutex);

        [DllImport("libpthread.so.0", EntryPoint = "pthread_mutex_destroy")]
        private static extern int PthreadMutexDestroy(void* mutex);
#endif
        private const uint WAIT_OBJECT_0 = 0x00000000;
        private const uint WAIT_TIMEOUT = 0x00000102;
        private const uint INFINITE = 0xFFFFFFFF;

        [StructLayout(LayoutKind.Sequential)]
        private struct Timespec
        {
            public long TvSec;
            public long TvNsec;

            public static Timespec GetAbsoluteTimeout(int timeoutMilliseconds)
            {
                Timespec ts;
                long currentNanoseconds, seconds;

                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long targetTime = currentTime + timeoutMilliseconds;

                seconds = targetTime / 1000;
                currentNanoseconds = targetTime % 1000 * 1_000_000;

                ts.TvSec = seconds;
                ts.TvNsec = currentNanoseconds;
                return ts;
            }
        }

        public UnsafeMutex(string? name = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fixed (char* pName = name)
                {
                    _handle = CreateMutexW(null, false, pName);
                    if (_handle == null)
                    {
                        throw new Exception("Failed to create Windows mutex: " + Marshal.GetLastWin32Error());
                    }
                }
            }
            else
            {
#if NET5_0_OR_GREATER
                _handle = NativeMemory.Alloc((nuint)sizeof(ulong) * 5); // Allocate pthread_mutex_t memory
#else
                _handle = (byte*)Marshal.AllocHGlobal((nint)sizeof(ulong) * 5); // Allocate pthread_mutex_t memory
#endif
                if (PthreadMutexInit(_handle, null) != 0)
                {
                    throw new Exception("Failed to initialize POSIX mutex.");
                }
            }
        }

        public readonly void WaitOne()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                uint result = WaitForSingleObject(_handle, INFINITE);
                if (result != WAIT_OBJECT_0)
                {
                    throw new Exception("Failed to acquire Windows mutex: " + Marshal.GetLastWin32Error());
                }
            }
            else
            {
                if (PthreadMutexLock(_handle) != 0)
                {
                    throw new Exception("Failed to acquire POSIX mutex.");
                }
            }
        }

        public readonly bool WaitOne(int timeoutMilliseconds)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                uint result = WaitForSingleObject(_handle, timeoutMilliseconds < 0 ? INFINITE : (uint)timeoutMilliseconds);
                if (result == WAIT_OBJECT_0)
                {
                    return true;
                }

                if (result == WAIT_TIMEOUT)
                {
                    return false;
                }

                throw new Exception("Failed to acquire Windows mutex: " + Marshal.GetLastWin32Error());
            }
            else
            {
                if (timeoutMilliseconds < 0)
                {
                    return PthreadMutexLock(_handle) == 0;
                }

                Timespec absTimeout = Timespec.GetAbsoluteTimeout(timeoutMilliseconds);
                return PthreadMutexTimedLock(_handle, &absTimeout) == 0;
            }
        }

        public readonly bool WaitOne(TimeSpan timeout)
        {
            return WaitOne((int)Math.Ceiling(timeout.TotalMilliseconds));
        }

        public readonly void ReleaseMutex()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!ReleaseMutex(_handle))
                {
                    throw new Exception("Failed to release Windows mutex: " + Marshal.GetLastWin32Error());
                }
            }
            else
            {
                if (PthreadMutexUnlock(_handle) != 0)
                {
                    throw new Exception("Failed to release POSIX mutex.");
                }
            }
        }

        public void Release()
        {
            if (_handle != null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    CloseHandle(_handle);
                    _handle = null;
                }
                else
                {
                    PthreadMutexDestroy(_handle);
#if NET5_0_OR_GREATER
                    NativeMemory.Free(_handle);
#else
                    Marshal.FreeHGlobal((nint)_handle);
#endif
                    _handle = null;
                }
            }
        }
    }
}