using System.Diagnostics.CodeAnalysis;

namespace Hexa.NET.Utilities
{
    public unsafe struct GCObject<T> : IEquatable<GCObject<T>>, IDisposable, IFreeable where T : class
    {
        public void* Ptr;

        public GCObject(T? obj)
        {
            Ptr = obj == null ? null : GCUtils.GCAlloc(obj);
        }

        public GCObject(void* ptr)
        {
            Ptr = ptr;
        }

        public readonly T? Value => GCUtils.GetObjectAs<T>(Ptr);

        public readonly bool IsNull => Ptr == null;

        public readonly T ValueNotNull => GCUtils.GetObject<T>(Ptr);

        public U? As<U>() where U : class => GCUtils.GetObjectAs<U>(Ptr);

        public bool Is<U>([NotNullWhen(true)] out U? value) where U : class
        {
            value = As<U>();
            return value != null;
        }

        public void Dispose()
        {
            if (Ptr != null)
            {
                GCUtils.GCFree(Ptr);
                Ptr = null;
            }
        }

        public void Release()
        {
            Dispose();
        }

        public static implicit operator T?(in GCObject<T> obj) => obj.Value;

        public static implicit operator void*(in GCObject<T> obj) => obj.Ptr;

        public static explicit operator GCObject<T>(T? obj) => new(obj);

        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is GCObject<T> other && Equals(other);
        }

        public readonly bool Equals(GCObject<T> other)
        {
            return Ptr == other.Ptr;
        }

        public readonly bool Equals<U>(GCObject<U> other) where U : class
        {
            return Ptr == other.Ptr;
        }

        public readonly override int GetHashCode()
        {
            return ((nuint)Ptr).GetHashCode();
        }

        public static bool operator ==(in GCObject<T> left, in GCObject<T> right) => left.Equals(right);

        public static bool operator !=(in GCObject<T> left, in GCObject<T> right) => !left.Equals(right);

        public override string ToString()
        {
            return Value?.ToString() ?? "null";
        }
    }
}