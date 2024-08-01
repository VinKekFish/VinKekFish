using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BytesBuilder = cryptoprime.BytesBuilder;
using System.Runtime.CompilerServices;
using alien_SkeinFish;
using cryptoprime;
using CodeGenerated.Cryptoprimes;
using DriverForTestsLib;

// ::test:O0s1QcshQ7zCGVMMKZtf:

namespace main_tests
{
    // [TestTagAttribute("inWork")]
    [TestTagAttribute("ThreeFish", duration: 120)]
    [TestTagAttribute("mandatory")]
    /// <summary>Быстрый текст для оптимизированного сгенерированного ThreeFish.
    /// Тест тестирует оптимизированный сгенерированный ThreeFish, просто устанавливая биты по одному</summary>
    class ThreeFishGenTestByBits_AllBits: TestTask
    {
        public unsafe ThreeFishGenTestByBits_AllBits(TestConstructor constructor):
                                        base(nameof(ThreeFishGenTestByBits_AllBits), constructor: constructor)
        {
            this.sources   = SourceTask.getIterator();
            this.TaskFunc  = StartTests;
            /* () =>
            {
                StartTests();
            };*/
        }

        class SourceTask
        {
            public string? Name;
            public byte[]? key;
            public byte[]? tweak;
            public byte[]? text;

            public List<byte[]>? vals = null;


            public static IEnumerable<SourceTask> getIterator()
            {
                // 128 - это размер одного блока
                const long size  = 128;
                const long tsize = 16;  // Размер tweak

                var key   = new byte[size];
                var text  = new byte[size];
                var tweak = new byte[tsize];
                yield return new SourceTask() {Name = $"Threfish key = 0; tweak = 0; text = 0", key = key, text = text, tweak = tweak };

                const long sb   = size << 3;    // Размер одного блока в битах
                for (nint valk = 0; valk < (nint) sb; valk++)
                {
                    // Здесь всё получаем именно внутри цикла, т.к. всё это идёт параллельно и нужно передавать каждой задаче разные массивы
                    key = new byte[size];
                    BytesBuilder.ToNull(key, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.ResetBit(key, valk);

                    yield return new SourceTask() {Name = $"Threfish (0xFF) key set at {valk}; tweak = 0; text = 0", key = key, text = text, tweak = tweak };

                    key = new byte[size];
                    BytesBuilder.ToNull(key);
                    BitToBytes.SetBit(key, valk);

                    yield return new SourceTask() {Name = $"Threfish (0x00) key set at {valk}; tweak = 0; text = 0", key = key, text = text, tweak = tweak };
                }

                key = new byte[size];
                for (nint valk = 0; valk < (nint) sb; valk++)
                {
                    text = new byte[size];
                    BytesBuilder.ToNull(text, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.ResetBit(text, valk);

                    yield return new SourceTask() {Name = $"Threfish (0xFF) key=0; tweak = 0; text set at {valk}", key = key, text = text, tweak = tweak };

                    text = new byte[size];
                    BytesBuilder.ToNull(text);
                    BitToBytes.SetBit(text, valk);

                    yield return new SourceTask() {Name = $"Threfish (0x00) key=0; tweak = 0; text set at {valk}", key = key, text = text, tweak = tweak };
                }

                text = new byte[size];
                const long st   = tsize << 3;    // Размер одного блока tweak в битах
                for (nint valk = 0; valk < (nint) st; valk++)
                {
                    tweak = new byte[tsize];
                    BytesBuilder.ToNull(tweak, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.ResetBit(tweak, valk);

                    yield return new SourceTask() {Name = $"Threfish (0xFF) key=0; tweak set at {valk}; text = 0", key = key, text = text, tweak = tweak };

                    tweak = new byte[tsize];
                    BytesBuilder.ToNull(tweak);
                    BitToBytes.SetBit(tweak, valk);

                    yield return new SourceTask() {Name = $"Threfish (0x00) key=0; tweak set at {valk}; text = 0", key = key, text = text, tweak = tweak };
                }

                yield break;
            }
        }

        readonly IEnumerable<SourceTask>? sources   = null;

        public unsafe void StartTests()
        {
            if (sources   == null) throw new NullReferenceException();

            var vals = new List<byte[]>(1024);

            // foreach (var ts in sources)
            Parallel.ForEach<SourceTask>
            (
                sources,
                (task, state, _) =>
                {
                    try
                    {
                        task.vals = vals;
                        TestForKeyAndText (task);
                    }
                    catch
                    {
                        state.Break();
                        throw;
                    }
                }
            );

            vals.Sort
            (
                (byte[] x, byte[] y) =>
                {
                    if (x.Length != y.Length)
                        return x.Length - y.Length;

                    for (int i = 0; i < x.Length; i++)
                        if (x[i] != y[i])
                            return x[i] - y[i];

                    return 0;
                }
            );

            // Все блоки ThreeFish здесь должны быть разными, т.к. данные для них тоже были разные
            for (int i = 1; i < vals.Count; i++)
            {
                if (BytesBuilder.UnsecureCompare(vals[i-1], vals[i]))
                    throw new Exception("ERROR: Two threefish block are equal");
            }
        }

        private unsafe void TestForKeyAndText(SourceTask ts)
        {
            if (ts.key   == null) throw new NullReferenceException();
            if (ts.text  == null) throw new NullReferenceException();
            if (ts.tweak == null) throw new NullReferenceException();
            if (ts.vals  == null) throw new NullReferenceException();

            var key   = BytesBuilder.CloneBytes(ts.key);
            var text  = BytesBuilder.CloneBytes(ts.text);
            var tweak = BytesBuilder.CloneBytes(ts.tweak);

            var bn = new byte[128];

            byte[] h1, h2;
            var tft = new ThreefishTransform(key, ThreefishTransformMode.Encrypt);

            var tw = Threefish_slowly.BytesToUlong(ts.tweak);
            tft.SetTweak(tw);

            h1 = BytesBuilder.CloneBytes(ts.text);
            tft.TransformBlock(h1, 0, 128, h1, 0);

            fixed (byte * tskey = ts.key, tstweak = ts.tweak)
            {
                using var tfg = new cryptoprime.Threefish1024(tskey, 128, tstweak, 16);
                h2 = BytesBuilder.CloneBytes(ts.text);

                // fixed (ulong * pkey = tfg.key, ptweak = tfg.tweak)
                fixed (byte * h2b = h2)
                {
                    ulong * h2u = (ulong *) h2b;
                    Threefish_Static_Generated.Threefish1024_step(tfg.key, tfg.tweak, h2u);
                }
            }

            if (!BytesBuilder.UnsecureCompare(h1, h2))
            {
                this.error.Add(new TestError() { Message = "Hashes are not equal for test array (1b-gen): " + ts.Name });
                throw new Exception("Hashes are not equal for test array (1b-gen): " + ts.Name);
            }

            lock (ts.vals)
            {
                ts.vals.Add(h2);
            }
        }
    }
}
