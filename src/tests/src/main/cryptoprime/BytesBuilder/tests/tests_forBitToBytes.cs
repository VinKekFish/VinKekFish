// Флаг изменения тестовых файлов идёт в tests.cs
/*
Это тесты для класса BytesBuilderForPointers и BytesBuilderForPointers.Record
*/

namespace cryptoprime_tests;

using System.Text;
using cryptoprime;
using DriverForTestsLib;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.BytesBuilderForPointers.Record;

[TestTagAttribute("inWork")]
[TestTagAttribute("BytesBuilder_ForPointers", duration: 4*10e3, singleThread: true)]
/// <summary>Тест для BytesBuilderStatic</summary>
public unsafe class BitToBytes_test1: BytesBuilder_test_parent
{
    public BitToBytes_test1(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var btb = new byte[1024];
            for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                CheckBit(btb, i, j);
            }

            for (int i = 256; i < 256+8; i++)
            for (int j = 256; j < 256+8; j++)
            {
                CheckBit(btb, i, j);
            }

            var steps = new byte[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 };
            foreach (var step in steps)
            {
                for (int i = step << 1; i < btb.Length << 3; i += step)
                    BitToBytes.setBit(btb, i);
            }

            lst.Add(btb);
            return lst;
        }

        private static void CheckBit(byte[] btb, int i, int j)
        {
            var i7 = i & 0x07;
            var j7 = j & 0x07;
            var a  = (1 << i7) | (1 << j7);
            BitToBytes.setBit(btb, i);
            BitToBytes.setBit(btb, j);
            if (btb[i >> 3] != a)
                throw new Exception($"btb[i >> 3] != a: {btb[i >> 3]} != {a}; {i}; {j}");


            for (int k = 0; k > btb.Length << 3; k++)
            {
                var bk = BitToBytes.getBit(btb, k);
                if (k == i || k == j)
                {
                    if (!bk)
                        throw new Exception("bk != 1");
                }
                else
                {
                    if (bk)
                        throw new Exception("bk != 0");
                }
            }

            BitToBytes.resetBit(btb, i);
            BitToBytes.resetBit(btb, j);
            if (btb[0] != 0)
                throw new Exception("btb[0] != 0");
        }
    }
}
