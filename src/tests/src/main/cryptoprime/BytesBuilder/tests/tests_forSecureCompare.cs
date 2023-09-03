// Флаг изменения тестовых файлов идёт в tests.cs
/*
Это тесты для класса BytesBuilderForPointers и BytesBuilderForPointers.Record
*/

// ::test:VOWNOWU4qu1Al9x07uh0:
// ::test:U2ZqhKq1NoPFwcP9yyLB:
// ::test:FOibRZrv5zW1lwU2AT7y:


namespace cryptoprime_tests;

using System.Text;
using cryptoprime;
using DriverForTestsLib;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.BytesBuilderForPointers.Record;

using static VinKekFish_Utils.Utils;


// [TestTagAttribute("inWork")]
[TestTagAttribute("SecureCompare", duration: 4300, singleThread: false)]
/// <summary>Тест для VinKekFish_Utils.Utils.SecureCompare</summary>
public unsafe class SecureCompare_test: BytesBuilder_test_parent
{
    public SecureCompare_test(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var b = new byte[256];
            for (int i = 0; i < b.Length; i++)
                b[i] = (byte) i;

            using var re = Record.getRecordFromBytesArray(b);
            using var rb = Record.getRecordFromBytesArray(b);

            if (!SecureCompare(re, rb))
                throw new Exception("1.0");

            for (int i = 0; i < b.Length - 1; i++)
            {
                for (int s = 1; s < b.Length - i; s++)
                {
                    var a = (rb >> s) << i;
                    var c = (rb << i) >> s;
                    if (SecureCompareSpeed(re, a))
                        throw new Exception("1.1a");
                    if (SecureCompare(re, a))
                        throw new Exception("1.1b");
                    if (!SecureCompareSpeed(re, a, s, 0, a.len, a.len))
                        throw new Exception("1.2a");
                    if (!SecureCompareSpeed(a, re, 0, s, a.len, a.len))
                        throw new Exception("1.2b");

                    if (!SecureCompareSpeed(a, a))
                        throw new Exception("1.3a");
                    if (!SecureCompare(a, a))
                        throw new Exception("1.3b");
                    if (!SecureCompare(a, c))
                        throw new Exception("1.3c");
                    if (!SecureCompare(c, a))
                        throw new Exception("1.3d");
                    if (!SecureCompareSpeed(a, c))
                        throw new Exception("1.3e");
                    if (!SecureCompareSpeed(c, a))
                        throw new Exception("1.3f");
                }
            }


            return lst;
        }
    }
}
