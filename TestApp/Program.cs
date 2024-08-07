namespace TestApp
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;
    using Hexa.NET.Utilities;

    public class Program
    {
        private static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<UnsafeDictionaryBenchmark>();
        }

        public class ModuloBenchmark
        {
            private const uint divisor = 123456789;
            private readonly ulong multiplier = HashHelpers.GetFastModMultiplier(divisor);
            private readonly uint value = 987654321;

            [Benchmark]
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

        public class UnsafeDictionaryBenchmark
        {
            private UnsafeDictionary<uint, int> customDict;
            private List<(uint, int)> keyValues;
            private Random random;

            [Params(100000)]
            public int Iterations;

            [GlobalSetup]
            public void Setup()
            {
                customDict = new UnsafeDictionary<uint, int>((int)(Iterations * 1.5f));
                keyValues = new List<(uint, int)>(Iterations);
                random = new Random();
            }

            [IterationSetup]
            public void IterationSetup()
            {
                customDict.Clear();
                keyValues.Clear();
                StressInsert(Iterations);
            }

            [Benchmark]
            public void InsertStressTest()
            {
                customDict.Clear();
                keyValues.Clear();
                StressInsert(Iterations);
            }

            [Benchmark]
            public void LookupStressTest()
            {
                StressLookup(keyValues);
            }

            [Benchmark]
            public void DeleteStressTest()
            {
                var clone = customDict.Clone();
                StressRemove(keyValues, ref clone);
                clone.Clear();
            }

            private void StressInsert(int iterations)
            {
                int range = int.MaxValue / iterations;
                for (int i = 0; i < iterations; i++)
                {
                    uint key = (uint)random.Next(range * i, range * (i + 1));
                    int value = random.Next();
                    customDict.Add(key, value);
                    keyValues.Add((key, value));
                }
            }

            private void StressLookup(List<(uint, int)> keyValues)
            {
                foreach (var (key, value) in keyValues)
                {
                    if (customDict[key] != value)
                    {
                        throw new InvalidOperationException("Lookup failed");
                    }
                }
            }

            private void StressRemove(List<(uint, int)> keyValues, ref UnsafeDictionary<uint, int> dict)
            {
                foreach (var (key, _) in keyValues)
                {
                    dict.Remove(key);
                }
            }
        }
    }
}