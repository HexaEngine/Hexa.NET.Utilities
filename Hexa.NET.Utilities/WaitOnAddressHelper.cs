#if NET7_0_OR_GREATER

using System.Numerics;
using static Hexa.NET.Utilities.WaitOnAddressHelper;

namespace Hexa.NET.Utilities
{
    internal unsafe static class WaitOnAddressHelper
    {
        [DllImport("API-MS-Win-Core-Synch-l1-2-0.dll")]
        public static extern bool WaitOnAddress(void* address, void* compareAddress, nuint size, uint milliseconds);

        [DllImport("API-MS-Win-Core-Synch-l1-2-0.dll")]
        public static extern void WakeByAddressSingle(void* address);

        [DllImport("API-MS-Win-Core-Synch-l1-2-0.dll")]
        public static extern void WakeByAddressAll(void* address);

        [DllImport("libc", SetLastError = true)]
        public static extern nint Syscall(nint number, void* arg1, void* arg2, void* arg3, void* arg4, void* arg5, void* arg6);

        public static readonly nint SYS_futex =
            RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => 202,
                Architecture.X86 => 240,
                Architecture.Arm => 240,
                Architecture.Armv6 => 240,
                Architecture.Arm64 => 98,
#if NET9_0_OR_GREATER
                Architecture.RiscV64 => 98,
#endif
                Architecture.LoongArch64 => 98,
                Architecture.S390x => 238,
                Architecture.Ppc64le => 221,
                _ => throw new PlatformNotSupportedException()
            };

        public const int FUTEX_WAIT = 0;
        public const int FUTEX_WAKE = 1;

        [DllImport("/usr/lib/system/libsystem_kernel.dylib", EntryPoint = "__ulock_wait")]
        public static extern int ULockWait(uint operation, void* addr, ulong value, uint timeout);

        [DllImport("/usr/lib/system/libsystem_kernel.dylib", EntryPoint = "__ulock_wake")]
        public static extern int ULockWake(uint operation, void* addr, ulong wakeValue);

        public const uint UL_COMPARE_AND_WAIT = 1;
        public const uint ULF_WAKE_ALL = 0x00000100;
        public const uint ULF_NO_ERRNO = 0x01000000;

        public const uint WaitTableSize = 256;
        public const uint WaitTableMask = WaitTableSize - 1;
        public static readonly uint* WaitTable = (uint*)AlignedAlloc(sizeof(uint) * WaitTableSize, (nuint)sizeof(nuint));

        public static uint* GetWaitTableSlot(void* address)
        {
            ulong x = (nuint)address;
            // splitmix64 finalizer
            x ^= x >> 30;
            x *= 0xbf58476d1ce4e5b9UL;
            x ^= x >> 27;
            x *= 0x94d049bb133111ebUL;
            x ^= x >> 31;
            return WaitTable + (nuint)(x & WaitTableMask);
        }

        static WaitOnAddressHelper()
        {
            ZeroMemoryT(WaitTable, WaitTableSize);
        }
    }

    public unsafe static class WaitOnAddressHelper<TValue> where TValue : unmanaged, IEquatable<TValue>, INumber<TValue>, IBinaryNumber<TValue>, IComparisonOperators<TValue, TValue, bool>
    {
        private static readonly bool IsWindows = OperatingSystem.IsWindows();
        private static readonly bool IsLinux = OperatingSystem.IsLinux();
        private static readonly bool IsMacOS = OperatingSystem.IsMacOS();

        static WaitOnAddressHelper()
        {
            var type = typeof(TValue);
            if (type != typeof(ulong) && type != typeof(uint) && type != typeof(ushort) && type != typeof(byte) && type != typeof(long) && type != typeof(int) && type != typeof(short) && type != typeof(sbyte))
            {
                throw new NotSupportedException("Unsupported type for WaitOnAddressHelper");
            }
        }

        private static T AtomicRead<T>(T* address) where T : unmanaged
        {
            if (typeof(T) == typeof(ulong))
            {
                return BitCast<ulong, T>(Volatile.Read(ref Unsafe.AsRef<ulong>(address)));
            }
            else if (typeof(T) == typeof(uint))
            {
                return BitCast<uint, T>(Volatile.Read(ref Unsafe.AsRef<uint>(address)));
            }
            else if (typeof(T) == typeof(ushort))
            {
                return BitCast<ushort, T>(Volatile.Read(ref Unsafe.AsRef<ushort>(address)));
            }
            else if (typeof(T) == typeof(byte))
            {
                return BitCast<byte, T>(Volatile.Read(ref Unsafe.AsRef<byte>(address)));
            }
            else if (typeof(T) == typeof(long))
            {
                return BitCast<long, T>(Volatile.Read(ref Unsafe.AsRef<long>(address)));
            }
            else if (typeof(T) == typeof(int))
            {
                return BitCast<int, T>(Volatile.Read(ref Unsafe.AsRef<int>(address)));
            }
            else if (typeof(T) == typeof(short))
            {
                return BitCast<short, T>(Volatile.Read(ref Unsafe.AsRef<short>(address)));
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return BitCast<sbyte, T>(Volatile.Read(ref Unsafe.AsRef<sbyte>(address)));
            }
            else
            {
                throw new NotSupportedException("Unsupported type for AtomicRead");
            }
        }

        private static void AtomicStore<T>(T* address, T value) where T : unmanaged
        {
            if (typeof(T) == typeof(ulong))
            {
                Volatile.Write(ref Unsafe.AsRef<ulong>(address), BitCast<T, ulong>(value));
            }
            else if (typeof(T) == typeof(uint))
            {
                Volatile.Write(ref Unsafe.AsRef<uint>(address), BitCast<T, uint>(value));
            }
            else if (typeof(T) == typeof(ushort))
            {
                Volatile.Write(ref Unsafe.AsRef<ushort>(address), BitCast<T, ushort>(value));
            }
            else if (typeof(T) == typeof(byte))
            {
                Volatile.Write(ref Unsafe.AsRef<byte>(address), BitCast<T, byte>(value));
            }
            else if (typeof(T) == typeof(long))
            {
                Volatile.Write(ref Unsafe.AsRef<long>(address), BitCast<T, long>(value));
            }
            else if (typeof(T) == typeof(int))
            {
                Volatile.Write(ref Unsafe.AsRef<int>(address), BitCast<T, int>(value));
            }
            else if (typeof(T) == typeof(short))
            {
                Volatile.Write(ref Unsafe.AsRef<short>(address), BitCast<T, short>(value));
            }
            else if (typeof(T) == typeof(sbyte))
            {
                Volatile.Write(ref Unsafe.AsRef<sbyte>(address), BitCast<T, sbyte>(value));
            }
            else
            {
                throw new NotSupportedException("Unsupported type for AtomicRead");
            }
        }

        private static void AtomicAdd<T>(T* address, T value) where T : unmanaged
        {
            if (typeof(T) == typeof(ulong))
            {
                Interlocked.Add(ref Unsafe.AsRef<ulong>(address), BitCast<T, ulong>(value));
            }
            else if (typeof(T) == typeof(uint))
            {
                Interlocked.Add(ref Unsafe.AsRef<uint>(address), BitCast<T, uint>(value));
            }
            else if (typeof(T) == typeof(long))
            {
                Interlocked.Add(ref Unsafe.AsRef<long>(address), BitCast<T, long>(value));
            }
            else if (typeof(T) == typeof(int))
            {
                Interlocked.Add(ref Unsafe.AsRef<int>(address), BitCast<T, int>(value));
            }
            else
            {
                throw new NotSupportedException("Unsupported type for AtomicRead");
            }
        }

        public static void Wait(ref TValue address, in TValue compare)
        {
            TValue* pAddress = (TValue*)Unsafe.AsPointer(ref address);
            if (IsWindows)
            {
                fixed (TValue* pCmp = &compare)
                {
                    WaitOnAddress(pAddress, pCmp, (nuint)sizeof(TValue), uint.MaxValue);
                }
            }
            else if (IsLinux)
            {
                do
                {
                    var slot = GetWaitTableSlot(pAddress);
                    var val = AtomicRead(slot);
                    Syscall(SYS_futex, slot, (void*)FUTEX_WAIT, (void*)(nuint)val, null, null, null);
                } while (AtomicRead(pAddress) == compare);
            }
            else if (IsMacOS)
            {
                do
                {
                    var slot = GetWaitTableSlot(pAddress);
                    uint val = AtomicRead(slot);
                    _ = ULockWait(UL_COMPARE_AND_WAIT | ULF_NO_ERRNO, slot, val, 0);
                } while (AtomicRead(pAddress) == compare);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS for Wait");
            }
        }

        public static void SignalSingle(ref TValue address)
        {
            TValue* pAddress = (TValue*)Unsafe.AsPointer(ref address);
            if (IsWindows)
            {
                WakeByAddressSingle(pAddress);
            }
            else if (IsLinux)
            {
                var slot = GetWaitTableSlot(pAddress);
                AtomicAdd(slot, 1u);
                Syscall(SYS_futex, slot, (void*)FUTEX_WAKE, (void*)1, null, null, null);
            }
            else if (IsMacOS)
            {
                var slot = GetWaitTableSlot(pAddress);
                AtomicAdd(slot, 1u);
                _ = ULockWake(UL_COMPARE_AND_WAIT | ULF_NO_ERRNO, slot, 0);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS for SignalSingle");
            }
        }

        public static void SignalAll(ref TValue address)
        {
            TValue* pAddress = (TValue*)Unsafe.AsPointer(ref address);
            if (IsWindows)
            {
                WakeByAddressAll(pAddress);
            }
            else if (IsLinux)
            {
                var slot = GetWaitTableSlot(pAddress);
                AtomicAdd(slot, 1u);
                Syscall(SYS_futex, slot, (void*)FUTEX_WAKE, (void*)int.MaxValue, null, null, null);
            }
            else if (IsMacOS)
            {
                var slot = GetWaitTableSlot(pAddress);
                AtomicAdd(slot, 1u);
                _ = ULockWake(UL_COMPARE_AND_WAIT | ULF_NO_ERRNO | ULF_WAKE_ALL, slot, 0);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS for SignalAll");
            }
        }
    }
}

#endif