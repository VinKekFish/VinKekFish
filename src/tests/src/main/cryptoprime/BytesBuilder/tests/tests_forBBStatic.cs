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
public unsafe class BytesBuilder_Static_test1: BytesBuilder_test_parent
{
    public BytesBuilder_Static_test1(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<string> lst = new List<string>();

            // Проверяем на то, что аллокатор умеет удалять память (работает IDisposable)
            var allocator  = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
            // nint maxMemory = getMaxMemory() << 2;
            const int slen = 1<<16;
            const int blen = slen << 1;

            var etalon = allocator.AllocMemory(blen);
            var result = allocator.AllocMemory(blen);
            var rshort = (ushort *) result;
            var eshort = (ushort *) etalon;
            for (int i = 0; i < slen; i++)
            {
                eshort[i] = (ushort) i;
            }

            using var bbs = new BytesBuilderStatic(1024);
            try
            {
                bbs.WriteBytes(etalon, 1025);
                var te = new TestError() {Message = "bbs.WriteBytes(etalon, 1025)"};
                task.error.Add(te);
            }
            catch (ArgumentOutOfRangeException)
            {}
            try
            {
                bbs.WriteBytes(etalon, 0);
                var te = new TestError() {Message = "bbs.WriteBytes(etalon, 0)"};
                task.error.Add(te);
            }
            catch (ArgumentOutOfRangeException)
            {}
            try
            {
                bbs.WriteBytes(etalon, -1);
                var te = new TestError() {Message = "bbs.WriteBytes(etalon, -1)"};
                task.error.Add(te);
            }
            catch (ArgumentOutOfRangeException)
            {}

            const int L1 = 512, S1 = 1024, L2 = L1+S1;
            bbs.WriteBytes(etalon >> 0,  L1);
            bbs.WriteBytes(etalon >> S1, L1);

            result[L2+0] = 255;
            result[L2+1] = 253;
            result[L2+2] = 254;

            if (bbs.len1 != L1 + L1 || bbs.Count != bbs.len1 || bbs.len2 != 0)
                throw new Exception("1.0");

            bbs.getBytesAndRemoveIt(result); // , L1 + L1
            for (int i = 0; i < 256; i++)
            {
                if (rshort[i] != i)
                    throw new Exception("1.1.1");
                if (
                    rshort[i + (L1 >> 1)] !=
                           i + (S1 >> 1)
                   )
                {
                    using var er = result.NoCopyClone(1024);
                    throw new Exception($"1.1.2 {i}; {rshort[i + (L1 >> 1)]} == {i + (S1 >> 1)}; {er.ToString()}");
                }
            }

            if (bbs.len1 != 0 || bbs.len2 != 0 || bbs.Count != 0)
                throw new Exception("1.1.3");
            if (result[L2+0] != 255)
                throw new Exception("1.1.4.0");
            if (result[L2+1] != 253)
                throw new Exception("1.1.4.1");
            if (result[L2+2] != 254)
                throw new Exception("1.1.4.2");

            var resultInList = result.NoCopyClone(L2+2);
            lst.Add(resultInList.ToString());

            bbs.add(etalon >> 768, 1);
            bbs.getBytes(1, result >> L2);
            if (result[L2+0] != 128)
                throw new Exception("1.1.5.1a");
            if (result[L2+1] != 253)
                throw new Exception("1.1.5.1b");

            bbs.getBytesAndRemoveIt(result >> L2 + 1);
            if (result[L2+0] != 128)
                throw new Exception("1.1.5.2a");
            if (result[L2+1] != 128)
                throw new Exception("1.1.5.2b");
            if (result[L2+2] != 254)
                throw new Exception("1.1.5.2c");


            result.Dispose();
            etalon.Dispose();

            GC.Collect();

            return lst;
        }
    }
}
