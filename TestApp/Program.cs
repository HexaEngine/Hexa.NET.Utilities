namespace TestApp
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using Hexa.NET.Utilities;

    public class Program
    {
        private static unsafe void Main(string[] args)
        {
            HashMap<int, int> map = default;
            for (int j = 0; j < 100; ++j)
            {
                for (int i = 0; i < 1000000; ++i)
                {
                    map.Add(i, 2);
                }
                map.Clear();
            }
            map.Release();
            //var summary = BenchmarkRunner.Run<UnsafeDictionaryBenchmark>();
            //var summary = BenchmarkRunner.Run<UnsafeListBenchmark>();
            /*
             UnsafeDictionaryBenchmark benchmark = new();
             benchmark.Setup();

             while (true)
             {
                 for (int i = 0; i < 2147421474; i++)
                 {
                     benchmark.AddUnsafeList();
                 }

                 Console.WriteLine("Continue? (Y/N): ");
                 if (Console.ReadLine().ToUpper() == "N")
                 {
                     break;
                 }
             }*/
        }

        public unsafe class UnsafeListBenchmark
        {
            private UnsafeList<int> _unsafeList = new();
            private List<int> _managedList = new();

            [Benchmark]
            public void AddUnsafeList()
            {
                _unsafeList.Add(1); // Add elements to the managed list
                if (_unsafeList.Count > 100000)
                {
                    _unsafeList.Clear();
                }
            }

            [Benchmark(Baseline = true)]
            public void AddManagedList()
            {
                _managedList.Add(1); // Add elements to the managed list
                if (_managedList.Count > 100000)
                {
                    _managedList.Clear();
                }
            }
        }

        public class ModuloBenchmark
        {
            private const uint divisor = 123456789;
            private readonly ulong multiplier = HashHelpers.GetFastModMultiplier(divisor);

            [Benchmark(Baseline = true)]
            public uint ModOperator()
            {
                var value = (uint)Random.Shared.Next();
                return value % divisor;
            }

            [Benchmark]
            public uint FastModMethod()
            {
                var value = (uint)Random.Shared.Next();
                ulong lowbits = multiplier * value;
                return (uint)(((lowbits >> 32) + 1) * divisor >> 32);
            }
        }

        public unsafe class UnsafeDictionaryBenchmark
        {
            // worst case scenario benchmark using linear keys where the hash code of int is always the value of itself.

            private UnsafeDictionary<int, int> _unsafeDict;   // about 29% faster and saves 7 bytes per entry and has a way lower Std-dev.
            private HashMap<int, int> _hashMap;
            private Dictionary<int, int> _managedDict = new();
            private int iterationIndex = 0;

            [GlobalSetup]
            public void Setup()
            {
                iterationIndex = 0;
                _unsafeDict.Clear();
                _hashMap.Clear();
                _managedDict.Clear();
            }

            [Benchmark]
            public void AddUnsafeDictionary()
            {
                _unsafeDict.Add(iterationIndex++, 2); // Add elements to the managed list
                if (_unsafeDict.Count > 100000)
                {
                    _unsafeDict.Clear();
                }
            }

            [Benchmark]
            public void AddHashMap()
            {
                _hashMap.Add(iterationIndex++, 2); // Add elements to the managed list
                if (_hashMap.Count > 100000)
                {
                    _hashMap.Clear();
                }
            }

            [Benchmark(Baseline = true)]
            public void AddManagedDictionary()
            {
                _managedDict.Add(iterationIndex++, 2); // Add elements to the managed list
                if (_managedDict.Count > 100000)
                {
                    _managedDict.Clear();
                }
            }
        }
    }
}