namespace Hexa.NET.Utilities
{
    public unsafe partial class Utils
    {
        public static int StrCmp(char* a, char* b)
        {
            if (a == null)
            {
                if (b != null)
                {
                    return -1;
                }

                return 0;
            }

            if (b == null)
            {
                return 1;
            }

            while (*a != 0 && *b != 0)
            {
                if (*a != *b)
                {
                    return *a - *b;
                }

                a++;
                b++;
            }

            if (*a == 0 && *b == 0)
            {
                return 0;
            }

            if (*a != 0)
            {
                return 1;
            }

            return -1;
        }
    }
}