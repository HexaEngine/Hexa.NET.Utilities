namespace Hexa.NET.Utilities
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.InteropServices;

    public static unsafe partial class Utils
    {
#if NET7_0_OR_GREATER

        [RequiresDynamicCode("MemoryDump requires Dynamic Code.")]
#endif
        public static void MemoryDump<
#if NET5_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
#endif
        T>(T* ptr) where T : unmanaged
        {
            byte* p = (byte*)ptr;
            int sizeInBytes = sizeof(T);
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            (FieldInfo info, int offset, int size)[] offsetData = new (FieldInfo info, int offset, int size)[fields.Length];

            int currentOffset = 0;
            for (int j = 0; j < fields.Length; j++)
            {
                FieldInfo field = fields[j];
                int size = Marshal.SizeOf(field.FieldType);
                offsetData[j] = (field, currentOffset, size);
                Console.WriteLine($"Field: {field.Name}, Offset: {currentOffset:X8}, Size: {size}, Type: {field.FieldType}");
                currentOffset += size;
            }

            int startInfo = 0;
            for (int i = 0; i < sizeInBytes; i++)
            {
                (FieldInfo info, int offset, int size)? info = default;

                if (startInfo < offsetData.Length && offsetData[startInfo].offset == i)
                {
                    info = offsetData[startInfo];
                    startInfo++;
                }

                if (info.HasValue)
                {
                    Console.WriteLine($"{((nint)p + i):X8}: {p[i]:X} : {info.Value.info.Name} {info.Value.size}");
                }
                else
                {
                    Console.WriteLine($"{((nint)p + i):X8}: {p[i]:X}");
                }
            }
        }
    }
}