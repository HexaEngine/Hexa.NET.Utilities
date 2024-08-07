namespace Hexa.NET.Utilities
{
    public unsafe struct BitHelper
    {
        private byte* data;
        private int length;
        private bool owns;

        public BitHelper(byte* data, int length)
        {
            this.data = data;
            this.length = length;
            owns = false;
        }

        public BitHelper(int length)
        {
            data = AllocT<byte>(length);
            ZeroMemoryT(data, length);
            this.length = length;
            owns = true;
        }

        public readonly byte* Data => data;

        public readonly int Length => length;

        public static int ToByteArrayLength(int length)
        {
            int totalBits = length + 1;
            int totalBytes = (totalBits + 7) >> 3;
            return totalBytes;
        }

        public void MarkBit(int index)
        {
            int byteIndex = index >> 3;
            int bitIndex = index & 0x07;
            byte mask = (byte)(1 << bitIndex);
            data[byteIndex] |= mask;
        }

        public void UnmarkBit(int index)
        {
            int byteIndex = index >> 3;
            int bitIndex = index & 0x07;
            byte mask = (byte)~(1 << bitIndex);
            data[byteIndex] &= mask;
        }

        public bool IsMarked(int index)
        {
            int byteIndex = index >> 3;
            int bitIndex = index & 0x07;
            byte mask = (byte)(1 << bitIndex);
            return (data[byteIndex] & mask) != 0;
        }

        public bool AllMarked(int length)
        {
            int bytes = length >> 3; // for aligned probing.
            int bits = length & 0x07;  // for the remaining bits.

            for (int i = 0; i < bytes; i++)
            {
                if (data[i] != 0xFF)
                {
                    return false;
                }
            }

            if (bits > 0)
            {
                byte mask = (byte)((1 << bits) - 1);
                return (data[bytes] & mask) == mask;
            }

            return true;
        }

        public void Release()
        {
            if (data != null && owns)
            {
                Free(data);
                this = default;
            }
        }
    }
}