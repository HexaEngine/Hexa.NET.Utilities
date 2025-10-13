namespace Hexa.NET.Utilities
{
    public static class AlignmentHelper
    {
        public static ulong AlignUp(ulong size, ulong alignment)
        {
            return size + alignment - 1 & ~(alignment - 1);
        }

        public static uint AlignUp(uint size, uint alignment)
        {
            return size + alignment - 1 & ~(alignment - 1);
        }

        public static long AlignUp(long size, long alignment)
        {
            return size + alignment - 1 & ~(alignment - 1);
        }

        public static int AlignUp(int size, int alignment)
        {
            return size + alignment - 1 & ~(alignment - 1);
        }

        public static ulong AlignDown(ulong size, ulong alignment)
        {
            return size & ~(alignment - 1);
        }

        public static long AlignDown(long size, long alignment)
        {
            return size & ~(alignment - 1);
        }

        public static int AlignDown(int size, int alignment)
        {
            return size & ~(alignment - 1);
        }
    }
}