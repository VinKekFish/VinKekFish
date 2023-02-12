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

namespace main_tests
{
    [TestTagAttribute("inWork")]
    [TestTagAttribute("ThreeFish", duration: 192e3d)]
    class ThreeFishGenTestByBits: TestTask
    {
        public unsafe ThreeFishGenTestByBits(TestConstructor constructor):
                                        base(nameof(ThreeFishGenTestByBits), constructor: constructor)
        {
            this.sources = SourceTask.getIterator();
            taskFunc = () =>
            {
                StartTests();
            };
        }

        class SourceTask
        {
            public string?   Key;
            public byte[][]? Value;

            public static IEnumerable<SourceTask> getIterator()
            {
                // 128 - это размер одного блока
                const long size = 128;
                const long sb   = size << 3;    // Размер одного блока в битах
                for (ulong valk = 0; valk < (ulong) sb; valk++)
                {
                    // Здесь всё получаем именно внутри цикла, т.к. всё это идёт параллельно и нужно передавать каждой задаче разные массивы
                    var bk1 = new byte[size];
                    BytesBuilder.ToNull(bk1, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.resetBit(bk1, valk);

                    var bk2 = new byte[size];
                    BytesBuilder.ToNull(bk2);
                    BitToBytes.setBit(bk2, valk);

                    for (ulong valt = 0; valt < (ulong) sb; valt++)
                    {
                        var b1 = new byte[size];
                        BytesBuilder.ToNull(b1, 0xFFFF_FFFF__FFFF_FFFF);
                        BitToBytes.resetBit(b1, valt);

                        var b2 = new byte[size];
                        BytesBuilder.ToNull(b2);
                        BitToBytes.setBit(b2, valt);
Console.WriteLine("AA");
                        yield return new SourceTask() {Key = $"Threfish with (0x00) valk = {valk}; valt = {valt}", Value = new byte[][] {bk2, b2}};
                        yield return new SourceTask() {Key = $"Threfish with (0xFF) valk = {valk}; valt = {valt}", Value = new byte[][] {bk1, b1}};
                    }
                }
Console.WriteLine("BB");
                yield break;
            }
        }

        readonly IEnumerable<SourceTask>? sources = null;

        public unsafe void StartTests()
        {
            if (sources == null) throw new NullReferenceException();

            // foreach (var ts in sources)
            Parallel.ForEach<SourceTask>
            (
                sources,
                (task, state, _) =>
                {
                    try
                    {
                        Console.WriteLine(_);
                        TestForKeyAndText (task);
                        TestForKeyAndTweak(task);
                    }
                    catch
                    {
                        state.Break();
                        throw;
                    }
                }
            );
        }

        ulong[] tw_null  = new ulong[2];
        byte[]  twb_null = new byte[16];
        private unsafe void TestForKeyAndText(SourceTask ts)
        {
            if (ts.Value == null) throw new NullReferenceException();

            var s0 = BytesBuilder.CloneBytes(ts.Value[0]);
            var s1 = BytesBuilder.CloneBytes(ts.Value[1]);
            var bn = new byte[128];

            byte[] h1 = new byte[128], h2;
            var tft = new ThreefishTransform(ts.Value[0], ThreefishTransformMode.Encrypt);
            tft.SetTweak(tw_null);
            h1 = (byte[])ts.Value[1].Clone();
            tft.TransformBlock(h1, 0, 128, h1, 0);

            var tfg = new cryptoprime.Threefish1024(ts.Value[0], twb_null);
            h2 = (byte[])ts.Value[1].Clone();
            fixed (ulong * key = tfg.key, tweak = tfg.tweak)
            fixed (byte * h1b = h2)
            {
                ulong * h1u = (ulong *) h1b;
                Threefish_Static_Generated.Threefish1024_step(key, tweak, h1u);
            }

            if (!BytesBuilder.UnsecureCompare(s0, ts.Value[0]))
            {
                this.error.Add(new TestError() { Message = "Sources arrays has been changed for test array (1a-gen): " + ts.Key });
                throw new Exception("Sources arrays has been changed for test array (1a-gen): " + ts.Key);
            }

            if (!BytesBuilder.UnsecureCompare(h1, h2))
            {
                this.error.Add(new TestError() { Message = "Hashes are not equal for test array (1b-gen): " + ts.Key });
                throw new Exception("Hashes are not equal for test array (1b-gen): " + ts.Key);
            }
        }

        private unsafe void TestForKeyAndTweak(SourceTask ts)
        {
            if (ts == null || ts.Value == null) throw new NullReferenceException();

            var s0 = BytesBuilder.CloneBytes(ts.Value[0]);
            var s1 = BytesBuilder.CloneBytes(ts.Value[1]);

            byte[] h1 = new byte[128], h2;
            // Ключ ts.Value[0]
            // Твик ts.Value[1]
            // Для шифрования используется массив из 128-ми нулевых байтов
            var tft = new ThreefishTransform(ts.Value[0], ThreefishTransformMode.Encrypt);
            var tw = new ulong[2];

            var b = threefish_slowly.BytesToUlong(ts.Value[1]);
            tw[0] = b[0];
            tw[1] = b[1];
            tft.SetTweak(tw);

            tft.TransformBlock(h1, 0, 128, h1, 0);

                 h2 = new byte[128];
            var tfg = new cryptoprime.Threefish1024(ts.Value[0], ts.Value[1]);

            fixed (ulong * b0p = tfg.key)
            fixed (ulong * twp = tfg.tweak)
            fixed (byte  * h2p = h2)
            {
                Threefish_Static_Generated.Threefish1024_step(b0p, twp, (ulong *) h2p);
            }


            // h2 = new SHA3(1024).getHash512(s);

            if (!BytesBuilder.UnsecureCompare(s0, ts.Value[0]) || !BytesBuilder.UnsecureCompare(s1, ts.Value[1]))
            {
                this.error.Add(new TestError() { Message = "Sources arrays has been changed for test array (2a-gen): " + ts.Key });
                throw new Exception("Sources arrays has been changed for test array (2a-gen): " + ts.Key);
            }

            if (!BytesBuilder.UnsecureCompare(h1, h2))
            {
                this.error.Add(new TestError() { Message = "Hashes are not equal for test array (2b-gen): " + ts.Key });
                throw new Exception("Hashes are not equal for test array (2b-gen): " + ts.Key);
            }
        }
    }
}
