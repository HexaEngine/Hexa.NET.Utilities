namespace Hexa.NET.Utilities
{
    using System.Diagnostics;
    using System.Threading;

    public struct Fence
    {
        private int _state;

        public Fence()
        {
            _state = 0;
        }

        public void Signal()
        {
            Interlocked.Exchange(ref _state, 1);
        }

        public void Wait()
        {
            while (Interlocked.CompareExchange(ref _state, 0, 0) == 0)
            {
                Thread.Yield();
            }
        }

        public bool Wait(int timeout)
        {
            if (timeout == 0)
            {
                return Interlocked.CompareExchange(ref _state, 0, 0) != 0;
            }

            var end = Stopwatch.GetTimestamp() + timeout * (Stopwatch.Frequency / 1000);
            while (Interlocked.CompareExchange(ref _state, 0, 0) == 0)
            {
                var now = Stopwatch.GetTimestamp();
                if (now >= end)
                {
                    return false;
                }

                Thread.Yield();
            }
            return true;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _state, 0);
        }
    }
}