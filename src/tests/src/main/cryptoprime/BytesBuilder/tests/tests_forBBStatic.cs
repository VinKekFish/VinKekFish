// Флаг изменения тестовых файлов идёт в tests.cs
/*
Это тесты для класса BytesBuilderForPointers и BytesBuilderForPointers.Record
*/

// ::test:VOWNOWU4qu1Al9x07uh0:
// ::test:5oshFi0o683L3KZGalTM:

namespace cryptoprime_tests;

using System.Text;
using cryptoprime;
using DriverForTestsLib;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.BytesBuilderForPointers.Record;

// [TestTagAttribute("inWork")]
[TestTagAttribute("BytesBuilder_ForPointers", duration: 50, singleThread: false)]
/// <summary>Тест для BytesBuilderStatic
/// BytesBuilder_Static_test2 является почти точной копией этого теста</summary>
public unsafe class BytesBuilder_Static_test1: BytesBuilder_test_parent
{
    public BytesBuilder_Static_test1(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<string> lst = new();

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

            var bbs = new BytesBuilderStatic(1024);
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

            const int L1 = 512, S1 = 1024, L2 = L1+L1;
            var et0  = etalon >> 0;
            var etS1 = etalon >> S1;
            if (et0.array != etalon.array)
                throw new Exception("et0");
            if (etS1.array != etalon.array + S1)
                throw new Exception("etS1");

            bbs.WriteBytes(etalon >> 0,  L1);
            bbs.WriteBytes(etalon >> S1, L1);

            result[L2+0] = 251;
            result[L2+1] = 253;
            result[L2+2] = 254;

            if (bbs.Len1 != L1 + L1 || bbs.Count != bbs.Len1 || bbs.Len2 != 0)
                throw new Exception("1.0");

            bbs.GetBytesAndRemoveIt(result); // , L1 + L1
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

            if (bbs.Len1 != 0 || bbs.Len2 != 0 || bbs.Count != 0)
                throw new Exception("1.1.3");
            if (result[L2+0] != 251)
                throw new Exception("1.1.4.0");
            if (result[L2+1] != 253)
                throw new Exception("1.1.4.1");
            if (result[L2+2] != 254)
                throw new Exception("1.1.4.2");

            var resultInList = result.NoCopyClone(L2+3);
            lst.Add(resultInList.ToString());

            bbs.Add(etalon >> 768, 1);
            bbs.GetBytes(-1, result >> L2);
            if (result[L2+0] != 128)
                throw new Exception("1.1.5.1a");
            if (result[L2+1] != 253)
                throw new Exception("1.1.5.1b");

            bbs.GetBytesAndRemoveIt(result >> L2 + 1);
            if (result[L2+0] != 128)
                throw new Exception("1.1.5.2a");
            if (result[L2+1] != 128)
                throw new Exception("1.1.5.2b");
            if (result[L2+2] != 254)
                throw new Exception("1.1.5.2c");

            if (bbs.Len1 != 0 || bbs.Len2 != 0 || bbs.Count != 0)
                throw new Exception("1.1.6");
            // При забирании данных буфер автоматически очищается
            if (!bbs.IsEntireNull())
                throw new Exception("1.1.7a");
            bbs.Add(etalon >> 768, 1);
            if (bbs.IsEntireNull())
                throw new Exception("1.1.7b");
            bbs.Clear();
            if (!bbs.IsEntireNull())
                throw new Exception("1.1.7c");

            result.Dispose();
            etalon.Dispose();
            bbs   .Dispose();

            GC.Collect();

            return lst;
        }
    }
}

// [TestTagAttribute("inWork")]
[TestTagAttribute("BytesBuilder_ForPointers", duration: 50, singleThread: false)]
/// <summary>Тест для BytesBuilderStatic. Тест - копия BytesBuilder_Static_test1, но тестирует перегруженный метод getBytesAndRemoveIt</summary>
public unsafe class BytesBuilder_Static_test2: BytesBuilder_test_parent
{
    public BytesBuilder_Static_test2(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<string> lst = new();

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

            var bbs = new BytesBuilderStatic(1024);
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

            const int L1 = 512, S1 = 1024, L2 = L1+L1;
            var et0  = etalon >> 0;
            var etS1 = etalon >> S1;
            if (et0.array != etalon.array)
                throw new Exception("et0");
            if (etS1.array != etalon.array + S1)
                throw new Exception("etS1");

            bbs.WriteBytes(etalon >> 0,  L1);
            bbs.WriteBytes(etalon >> S1, L1);

            result[L2+0] = 251;
            result[L2+1] = 253;
            result[L2+2] = 254;

            if (bbs.Len1 != L1 + L1 || bbs.Count != bbs.Len1 || bbs.Len2 != 0)
                throw new Exception("1.0");

            bbs.GetBytesAndRemoveIt(result.array, L1 + L1);
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

            if (bbs.Len1 != 0 || bbs.Len2 != 0 || bbs.Count != 0)
                throw new Exception("1.1.3");
            if (result[L2+0] != 251)
                throw new Exception("1.1.4.0");
            if (result[L2+1] != 253)
                throw new Exception("1.1.4.1");
            if (result[L2+2] != 254)
                throw new Exception("1.1.4.2");

            var resultInList = result.NoCopyClone(L2+3);
            lst.Add(resultInList.ToString());

            bbs.Add(etalon >> 768, 1);
            bbs.GetBytes(-1, result >> L2);
            if (result[L2+0] != 128)
                throw new Exception("1.1.5.1a");
            if (result[L2+1] != 253)
                throw new Exception("1.1.5.1b");

            var rL2 = result >> L2 + 1;
            bbs.GetBytesAndRemoveIt(rL2.array, 1);
            if (result[L2+0] != 128)
                throw new Exception("1.1.5.2a");
            if (result[L2+1] != 128)
                throw new Exception("1.1.5.2b");
            if (result[L2+2] != 254)
                throw new Exception("1.1.5.2c");

            if (bbs.Len1 != 0 || bbs.Len2 != 0 || bbs.Count != 0)
                throw new Exception("1.1.6");
            // При забирании данных буфер автоматически очищается
            if (!bbs.IsEntireNull())
                throw new Exception("1.1.7a");
            bbs.Add(etalon >> 768, 1);
            if (bbs.IsEntireNull())
                throw new Exception("1.1.7b");
            bbs.Clear();
            if (!bbs.IsEntireNull())
                throw new Exception("1.1.7c");

            result.Dispose();
            etalon.Dispose();
            bbs   .Dispose();

            GC.Collect();

            return lst;
        }
    }
}
