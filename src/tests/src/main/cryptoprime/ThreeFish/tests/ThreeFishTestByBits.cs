﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BytesBuilder = cryptoprime.BytesBuilder;
using System.Runtime.CompilerServices;
using alien_SkeinFish;
using cryptoprime;
using DriverForTestsLib;

// ::test:O0s1QcshQ7zCGVMMKZtf:

namespace main_tests
{
    // [TestTagAttribute("inWork")]
    [TestTagAttribute("ThreeFish", duration: 70e3)]
    class ThreeFishTestByBits: TestTask
    {
        public ThreeFishTestByBits(TestConstructor constructor):
                                        base(nameof(ThreeFishTestByBits), constructor: constructor)
        {
            sources   = SourceTask.GetIterator();
            TaskFunc = () =>
            {
                StartTests();
            };
        }

        class SourceTask
        {
            public string?   Key;
            public byte[][]? Value;

            public static IEnumerable<SourceTask> GetIterator()
            {
                // 128 - это размер одного блока
                long size = 128;
                for (nint valk = 0; valk < (nint) (size << 3); valk++)
                {
                    var bk1 = new byte[size];
                    BytesBuilder.ToNull(bk1, 0xFFFF_FFFF__FFFF_FFFF);
                    BitToBytes.ResetBit(bk1, valk);

                    var bk2 = new byte[size];
                    BytesBuilder.ToNull(bk2);
                    BitToBytes.SetBit(bk2, valk);

                    for (nint valt = 0; valt < (nint) (size << 3); valt++)
                    {
                        var b1 = new byte[size];
                        BytesBuilder.ToNull(b1, 0xFFFF_FFFF__FFFF_FFFF);
                        BitToBytes.ResetBit(b1, valt);

                        var b2 = new byte[size];
                        BytesBuilder.ToNull(b2);
                        BitToBytes.SetBit(b2, valt);

                        yield return new SourceTask() {Key = "Threfish with valk = " + valk, Value = new byte[][] {bk1, b1}};
                        yield return new SourceTask() {Key = "Threfish with valk = " + valk, Value = new byte[][] {bk2, b2}};
                    }
                }

                yield break;
            }
        }

        readonly IEnumerable<SourceTask>? sources = null;

        public unsafe void StartTests()
        {
            if (sources == null) throw new NullReferenceException();

            Parallel.ForEach<SourceTask>
            (
                sources,
                (task, state, _) =>
                {
                    try
                    {
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

        private unsafe void TestForKeyAndText(SourceTask ts)
        {
            if (ts.Value == null) throw new NullReferenceException();

            var s0 = BytesBuilder.CloneBytes(ts.Value[0]);      // Ключ
            // var s1 = BytesBuilder.CloneBytes(ts.Value[1]);      // Текст

            //byte[] h1 = new byte[128], h2;
            byte[] h1, h2;
            var tft = new ThreefishTransform(ts.Value[0], ThreefishTransformMode.Encrypt);
            var tw = new ulong[2];
            tft.SetTweak(tw);
            h1 = (byte[])ts.Value[1].Clone();
            tft.TransformBlock(h1, 0, 128, h1, 0);

            tw = new ulong[2];
            var b0 = Threefish_slowly.BytesToUlong(ts.Value[0]);      // Ключ
            var b1 = Threefish_slowly.BytesToUlong(ts.Value[1]);      // Текст
            h2 = Threefish_slowly.UlongToBytes(Threefish_slowly.Encrypt(b0, tw, b1), null);

            // h2 = new SHA3(1024).getHash512(s);

            if (!BytesBuilder.UnsecureCompare(s0, ts.Value[0]))
            {
                error.Add(new TestError() { Message = "Sources arrays has been changed for test array (1a): " + ts.Key });
                throw new Exception("Sources arrays has been changed for test array (1a): " + ts.Key);
            }

            if (!BytesBuilder.UnsecureCompare(h1, h2))
            {
                error.Add(new TestError() { Message = "Hashes are not equal for test array (1b): " + ts.Key });
                throw new Exception("Hashes are not equal for test array (1b): " + ts.Key);
            }
        }

        private unsafe void TestForKeyAndTweak(SourceTask ts)
        {
            if (ts.Value == null) throw new NullReferenceException();

            var s0 = BytesBuilder.CloneBytes(ts.Value[0]);      // Ключ
            // var s1 = BytesBuilder.CloneBytes(ts.Value[1]);      // Твик

            byte[] h1 = new byte[128], h2;          // Шифротекст оставляем нулём
            // BytesBuilder.CopyTo(s0, bn);
            // BytesBuilder.CopyTo(s1, bn, s0.Length);
            var tft = new ThreefishTransform(ts.Value[0], ThreefishTransformMode.Encrypt);
            var tw = new ulong[2];

            var b = Threefish_slowly.BytesToUlong(ts.Value[1]);
            tw[0] = b[0];
            tw[1] = b[2];
            tft.SetTweak(tw);

            tft.TransformBlock(h1, 0, 128, h1, 0);

            var input = new ulong[16];
            tw = new ulong[2];
            var b0 = Threefish_slowly.BytesToUlong(ts.Value[0]);
            // var b1 = Threefish_slowly.BytesToUlong(ts.Value[1]);
            tw[0] = b[0];
            tw[1] = b[2];
            h2 = Threefish_slowly.UlongToBytes(Threefish_slowly.Encrypt(b0, tw, input), null);

            // h2 = new SHA3(1024).getHash512(s);

            if (!BytesBuilder.UnsecureCompare(s0, ts.Value[0]))
            {
                error.Add(new TestError() { Message = "Sources arrays has been changed for test array (2a): " + ts.Key });
                throw new Exception("Sources arrays has been changed for test array (2a): " + ts.Key);
            }

            if (!BytesBuilder.UnsecureCompare(h1, h2))
            {
                error.Add(new TestError() { Message = "Hashes are not equal for test array (2b): " + ts.Key });
                throw new Exception("Hashes are not equal for test array (2b): " + ts.Key);
            }
        }
    }
}
