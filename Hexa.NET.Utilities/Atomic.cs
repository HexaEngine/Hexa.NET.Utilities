#if NET8_0_OR_GREATER

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Hexa.NET.Utilities
{
    public unsafe struct Atomic<T> where T : unmanaged, INumber<T>
    {
        private T value;

        public Atomic(T initialValue)
        {
            value = initialValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Cast<U>(U v) where U : unmanaged => Unsafe.BitCast<U, T>(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static U Cast<U>(T v) where U : unmanaged => Unsafe.BitCast<T, U>(v);

        public T Load()
        {
            if (sizeof(T) == 1)
            {
                return Cast(Volatile.Read(ref Unsafe.As<T, byte>(ref value)));
            }
            if (sizeof(T) == 2)
            {
                return Cast(Volatile.Read(ref Unsafe.As<T, short>(ref value)));
            }
            if (sizeof(T) == 4)
            {
                return Cast(Volatile.Read(ref Unsafe.As<T, int>(ref value)));
            }
            if (sizeof(T) == 8)
            {
                return Cast(Volatile.Read(ref Unsafe.As<T, long>(ref value)));
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }

        public void Store(T newValue)
        {
            if (sizeof(T) == 1)
            {
                Volatile.Write(ref Unsafe.As<T, byte>(ref value), Cast<byte>(newValue));
                return;
            }
            if (sizeof(T) == 2)
            {
                Volatile.Write(ref Unsafe.As<T, short>(ref value), Cast<short>(newValue));
                return;
            }
            if (sizeof(T) == 4)
            {
                Volatile.Write(ref Unsafe.As<T, int>(ref value), Cast<int>(newValue));
                return;
            }
            if (sizeof(T) == 8)
            {
                Volatile.Write(ref Unsafe.As<T, long>(ref value), Cast<long>(newValue));
                return;
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }

        public T Exchange(T newValue)
        {
#if NET9_0_OR_GREATER
            if (sizeof(T) == 1)
            {
                return Cast(Interlocked.Exchange(ref Unsafe.As<T, byte>(ref value), Cast<byte>(newValue)));
            }

            if (sizeof(T) == 2)
            {
                return Cast(Interlocked.Exchange(ref Unsafe.As<T, short>(ref value), Cast<short>(newValue)));
            }
#endif

            if (sizeof(T) == 4)
            {
                return Cast(Interlocked.Exchange(ref Unsafe.As<T, int>(ref value), Cast<int>(newValue)));
            }

            if (sizeof(T) == 8)
            {
                return Cast(Interlocked.Exchange(ref Unsafe.As<T, long>(ref value), Cast<long>(newValue)));
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }

        public bool CompareExchange(T newValue, T comparand)
        {
#if NET9_0_OR_GREATER
            if (sizeof(T) == 1)
            {
                var c = Cast<byte>(comparand);
                return Interlocked.CompareExchange(ref Unsafe.As<T, byte>(ref value), Cast<byte>(newValue), c) == c;
            }

            if (sizeof(T) == 2)
            {
                var c = Cast<short>(comparand);
                return Interlocked.CompareExchange(ref Unsafe.As<T, short>(ref value), Cast<short>(newValue), c) == c;
            }
#endif

            if (sizeof(T) == 4)
            {
                var c = Cast<int>(comparand);
                return Interlocked.CompareExchange(ref Unsafe.As<T, int>(ref value), Cast<int>(newValue), c) == c;
            }

            if (sizeof(T) == 8)
            {
                var c = Cast<long>(comparand);
                return Interlocked.CompareExchange(ref Unsafe.As<T, long>(ref value), Cast<long>(newValue), c) == c;
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }

        public T FetchAdd(T operand)
        {
            if (typeof(T) == typeof(int))
            {
                return Cast(Interlocked.Add(ref Unsafe.As<T, int>(ref value), Cast<int>(operand))) - operand;
            }
            if (typeof(T) == typeof(uint))
            {
                return Cast(Interlocked.Add(ref Unsafe.As<T, uint>(ref value), Cast<uint>(operand))) - operand;
            }
            if (typeof(T) == typeof(long))
            {
                return Cast(Interlocked.Add(ref Unsafe.As<T, long>(ref value), Cast<long>(operand))) - operand;
            }
            if (typeof(T) == typeof(ulong))
            {
                return Cast(Interlocked.Add(ref Unsafe.As<T, ulong>(ref value), Cast<ulong>(operand))) - operand;
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }

        public T FetchSub(T operand)
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
            {
                return Cast(Interlocked.Add(ref Unsafe.As<T, int>(ref value), -Cast<int>(operand))) - operand;
            }
            if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
            {
                return Cast(Interlocked.Add(ref Unsafe.As<T, long>(ref value), -Cast<long>(operand))) - operand;
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }

        public T FetchOr(T operand)
        {
            if (typeof(T) == typeof(int))
            {
                return Cast(Interlocked.Or(ref Unsafe.As<T, int>(ref value), Cast<int>(operand)));
            }
            if (typeof(T) == typeof(uint))
            {
                return Cast(Interlocked.Or(ref Unsafe.As<T, uint>(ref value), Cast<uint>(operand)));
            }
            if (typeof(T) == typeof(long))
            {
                return Cast(Interlocked.Or(ref Unsafe.As<T, long>(ref value), Cast<long>(operand)));
            }
            if (typeof(T) == typeof(ulong))
            {
                return Cast(Interlocked.Or(ref Unsafe.As<T, ulong>(ref value), Cast<ulong>(operand)));
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }

        public T FetchAnd(T operand)
        {
            if (typeof(T) == typeof(int))
            {
                return Cast(Interlocked.And(ref Unsafe.As<T, int>(ref value), Cast<int>(operand)));
            }
            if (typeof(T) == typeof(uint))
            {
                return Cast(Interlocked.And(ref Unsafe.As<T, uint>(ref value), Cast<uint>(operand)));
            }
            if (typeof(T) == typeof(long))
            {
                return Cast(Interlocked.And(ref Unsafe.As<T, long>(ref value), Cast<long>(operand)));
            }
            if (typeof(T) == typeof(ulong))
            {
                return Cast(Interlocked.And(ref Unsafe.As<T, ulong>(ref value), Cast<ulong>(operand)));
            }

            throw new NotSupportedException($"Unsupported type: {typeof(T)}");
        }
    }
}

#endif