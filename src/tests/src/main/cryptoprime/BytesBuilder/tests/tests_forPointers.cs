// Флаг изменения тестовых файлов идёт в tests.cs

namespace cryptoprime_tests;

using System.Text;
using cryptoprime;
using DriverForTestsLib;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.BytesBuilderForPointers.Record;

[TestTagAttribute("BytesBuilder_ForPointers", duration: 4*10e3, singleThread: true)]
/// <summary>Тест для BytesBuilderForPointers.Record
/// Проверяет, работает ли IDisposable - удаляется ли выделенная память</summary>
public unsafe class BytesBuilder_ForPointers_Record_test1: BytesBuilder_test_parent
{
    public BytesBuilder_ForPointers_Record_test1(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            // Проверяем на то, что аллокатор умеет удалять память (работает IDisposable)
            var allocator  = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
            nint maxMemory = getMaxMemory() << 2;

            nint maxMemoryToAllocateBySingleAllocate = maxMemory >> 12;     // Иначе процесс будет длиться слишком долго, т.к. большие блоки долго выделяются
            nint memoryToAllocate = 1;
            for (nint i = 4096; i < maxMemory;)
            {
                for (int thr = 56; thr >= 16; thr -= 8)
                {
                    nint one = 1;
                    if (i > (one << thr))
                    {
                        memoryToAllocate = one << thr;
                        // Это не даёт запросить слишком большие блоки и даже ускоряет выделение памяти, т.к. большие непрерывные блоки выделять тяжелее
                        if (memoryToAllocate > maxMemoryToAllocateBySingleAllocate)
                            memoryToAllocate = maxMemoryToAllocateBySingleAllocate;

                        // Console.WriteLine(memoryToAllocate + "\t\t" + i.ToString("N"));
                        break;
                    }
                }

                byte * a = null;
                using (var r = allocator.AllocMemory(memoryToAllocate))
                {
                    i += memoryToAllocate;
                    a = r.array;
                }
            }

            return lst;
        }
    }
}


// [TestTagAttribute("inWork")]
[TestTagAttribute("BytesBuilder_ForPointers", duration: 8500d)]
/// <summary>Тест для BytesBuilderForPointers.Record
/// Проверяет некоторые функции (создание, клонирование)</summary>
public unsafe class BytesBuilder_ForPointers_Record_test2: BytesBuilder_test_parent
{
    public BytesBuilder_ForPointers_Record_test2(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var b = new byte[256];
            for (int i = 0; i < 256; i++)
                b[i] = (byte)i;

            using (var R = getRecordFromBytesArray(b))
            {
                if (!BytesBuilder.UnsecureCompare(b, R.CloneToSafeBytes()))
                    throw new Exception("Error 1.1a");

                lst.Add(R.CloneToSafeBytes());
                for (int i = 0; i < 256; i++)
                    if (b[i] != i)
                        throw new Exception("Error 1.1an");

                b[0] = 255;
                if (BytesBuilder.UnsecureCompare(b, R.CloneToSafeBytes()))
                    throw new Exception("Error 1.1b");

                R.array[0] = 255;
                if (!BytesBuilder.UnsecureCompare(b, R.CloneToSafeBytes()))
                    throw new Exception("Error 1.1c");
            }

            Check32Array(256*1024);           lst.Add(b);
            Check32Array(1024 * 1024 * 1024);

            return lst;

            static void Check32Array(int size)
            {
                var b = new byte[size];
                fixed (byte* bp = b)
                {
                    var up = (Int32*)bp;
                    var ul = b.Length / sizeof(Int32);

                    for (int i = 0; i < ul; i++)
                        up[i] = i;
                }

                using var R = getRecordFromBytesArray(b);
                // Чтобы сборщик мусора работал эффективнее, проверки вынесены в отдельные функции
                // Без явного вызова сборщика мусора - он не собирает нормально память
                Check1(b, R); GC.Collect();
                Check2(b, R); GC.Collect();
                Check3(b, R); GC.Collect();
                Check4(b, R); GC.Collect();

                static void Check1(byte[] b, Record R)
                {
                    if (!BytesBuilder.UnsecureCompare(b, R.CloneToSafeBytes()))
                        throw new Exception("Error 1.2a");
                }

                static void Check2(byte[] b, Record R)
                {
                    fixed (byte* bp = R.CloneToSafeBytes())
                    {
                        var up = (Int32*)bp;
                        var ul = b.Length / sizeof(Int32);

                        for (int i = 0; i < ul; i++)
                            if (up[i] != i)
                                throw new Exception("Error 1.2an");
                    }
                }

                static void Check3(byte[] b, Record R)
                {
                    b[0] = 255;
                    if (BytesBuilder.UnsecureCompare(b, R.CloneToSafeBytes()))
                        throw new Exception("Error 1.2b");
                }

                static void Check4(byte[] b, Record R)
                {
                    R.array[0] = 255;
                    if (!BytesBuilder.UnsecureCompare(b, R.CloneToSafeBytes()))
                        throw new Exception("Error 1.2c");
                }
            }
        }
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("BytesBuilder_ForPointers", duration: 40)]
/// <summary>Тест для BytesBuilderForPointers.Record
/// Проверяет функции клонирования</summary>
public unsafe class BytesBuilder_ForPointers_Record_test3: BytesBuilder_test_parent
{
    public BytesBuilder_ForPointers_Record_test3(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var b = new byte[256];
            for (int i = 0; i < 256; i++)
                b[i] = (byte) i;

            using var R1 = getRecordFromBytesArray(b);
            using var R2 = (Record) R1.Clone();
            using var R3 =          R1.Clone(0, -1);

            if (!R1.UnsecureCompare(R2))
                throw new Exception("Error 3.1");
            if (!R1.UnsecureCompare(R3))
                throw new Exception("Error 3.2");

            lst.Add(R1.CloneToSafeBytes());
            for (int i = 0; i < 256; i++)
                if (b[i] != i)
                    throw new Exception("Error 3.3");

            R1.array[0] = 255;
            if (R2.array[0] != 0 || R3.array[0] != 0)
                throw new Exception("Error 3.4");

            R1.Clear();
            if (!R3.UnsecureCompare(R2))
                throw new Exception("Error 3.5");

            for (int i = 0; i < R1.len; i++)
            {
                if (R1.array[i] != 0)
                    throw new Exception("Error 3.5a");

                if (R2.array[i] != i)
                    throw new Exception("Error 3.5a");

                if (R3.array[i] != i)
                    throw new Exception("Error 3.5a");
            }

            R2.array[0] = 128;
            if (R3.array[0] != 0)
                throw new Exception("Error 3.6");

            return lst;
        }
    }
}


[TestTagAttribute("BytesBuilder_ForPointers", duration: 4*10e3, singleThread: true)]
/// <summary>Тест для BytesBuilderForPointers.Record
/// </summary>
public unsafe class BytesBuilder_ForPointers_Record_test4: BytesBuilder_test_parent
{
    public BytesBuilder_ForPointers_Record_test4(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            

            return lst;
        }
    }
}



// [TestTagAttribute("inWork")]
[TestTagAttribute("BytesBuilder_ForPointers", duration: 40d)]
[TestTagAttribute("mandatory", duration: 40d)]
public class BytesBuilder_ForPointers_test: BytesBuilder_test_parent
{
    public BytesBuilder_ForPointers_test(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public readonly static string Str1 = "Некоторая строка";
        public readonly static string Str2 = "Другой массив строк";
        public readonly static string Str3 = "Третья неизвестно что";

        public readonly static byte[] bStr1 = Encoding.UTF8.GetBytes(Str1);
        public readonly static byte[] bStr2 = Encoding.UTF8.GetBytes(Str2);
        public readonly static byte[] bStr3 = Encoding.UTF8.GetBytes(Str3);

        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            // Для удобства использования using
            var allocator = new AllocHGlobal_AllocatorForUnsafeMemory();
            {
                using var bb = new BytesBuilderForPointers();

                
                try
                {
                    // Тестируем пустой массив
                    lst.Add(  bb.getBytes().CloneToSafeBytes(destroyRecord: true)  );
                }
                catch (BytesBuilder.NotFoundAllocator)
                {
                    lst.Add(new byte[0]);
                }

                try
                {
                    lst.Add(  bb.getBytes(allocator: allocator).CloneToSafeBytes(destroyRecord: true)  );
                }
                catch (ArgumentOutOfRangeException)
                {
                    lst.Add(new byte[0]);
                }

                // Пытаемся взять больше, чем есть
                try
                {
                    lst.Add(bb.getBytes(1).CloneToSafeBytes(destroyRecord: true));
                }
                catch (BytesBuilder.ResultCountIsTooLarge)
                {
                    lst.Add(new byte[0]);
                }

                var str1a = BytesBuilderForPointers.Record.getRecordFromBytesArray(bStr1);
                var str2a = BytesBuilderForPointers.Record.getRecordFromBytesArray(bStr2);
                var str3a = BytesBuilderForPointers.Record.getRecordFromBytesArray(bStr3);


                // Тестируем конкатенацию массивов
                bb.add(str1a);
                bb.add(str2a);
                bb.add(str3a);

                if (bb.countOfBlocks != 3)
                    throw new Exception("bb.countOfBlocks != 3");
                if (  !BytesBuilder.UnsecureCompare(bb.getBlock(0).CloneToSafeBytes(destroyRecord: false), bStr1)  )
                    throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(0), Encoding.UTF8.GetBytes(Str1))");
                if (  !BytesBuilder.UnsecureCompare(bb.getBlock(1).CloneToSafeBytes(destroyRecord: false), bStr2)  )
                    throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(1), Encoding.UTF8.GetBytes(Str2))");
                if (  !BytesBuilder.UnsecureCompare(bb.getBlock(2).CloneToSafeBytes(destroyRecord: false), bStr3)  )
                    throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(2), Encoding.UTF8.GetBytes(Str3))");


                using var bb2 = new BytesBuilderForPointers();
                var strBytes = getRecordFromBytesArray(   Encoding.UTF8.GetBytes(Str1 + Str2 + Str3)   );
                bb2.add(strBytes);

                lst.Add(  bb.getBytes().CloneToSafeBytes(destroyRecord: true)  );

                // Проверяем, что эти две строки равны
                for (int i = 0; i < 2; i++)
                if (  !BytesBuilder.UnsecureCompare( bb.getBytes().CloneToSafeBytes(destroyRecord: true), bb2.getBytes().CloneToSafeBytes(destroyRecord: true) )  )
                    throw new Exception("!BytesBuilder.UnsecureCompare( bb.getBytes(), bb2.getBytes() ): ke5POaxC8Iz");

                // Проверяем, что getBytes правильно возвращает частичные вхождения
                for (nint i = 1; i < strBytes.len; i++)
                if (  !BytesBuilder.UnsecureCompare( bb.getBytes(i).CloneToSafeBytes(destroyRecord: true), bb2.getBytes(i).CloneToSafeBytes(destroyRecord: true) )  )
                    throw new Exception("!BytesBuilder.UnsecureCompare( bb.getBytes(), bb2.getBytes() ): qo53ZIsRLpQ");


                // Проверяем, что при маленьком ra будет выдано исключение
                using var ra = getRecordFromBytesArray(   new byte[1]   );
                try
                {
                    bb.getBytes(2, ra);
                }
                catch (BytesBuilder.ResultAIsTooSmall)
                {
                    lst.Add(new byte[0]);
                }


                using var bbt  = new BytesBuilderForPointers();
                strBytes = getRecordFromBytesArray(   Encoding.UTF8.GetBytes(Str1 + Str2 + Str3)   );
                var strBytesT = strBytes.Clone(0);
                try
                {
                    int counter_a = 1;
                    while (true)
                    {
                        if (counter_a >= bb.Count)
                            counter_a = (int) bb.Count - 3;
                        if (counter_a < 1)
                            counter_a = 1;

                        var cnt = bb.Count;

                              var tmpR1 = allocator.AllocMemory(counter_a);
                        using var tmpR2 = allocator.AllocMemory(counter_a);

                        // Этот блок нужен для того, чтобы удалить tmpR1 в том случае, если getBytesAndRemoveIt выдаст исключение (он это должен делать в конце цикла)
                        try
                        {
                            var a = bb .getBytesAndRemoveIt(tmpR1);
                            var b = bb2.getBytesAndRemoveIt(tmpR2);

                            if (a != tmpR1 || b != tmpR2)
                                throw new Exception("a != tmpR1 || b != tmpR2");

                            bbt.add(a);
                        }
                        catch
                        {
                            tmpR1.Dispose();
                            throw;
                        }

                                        // Console.WriteLine($"bbt={bbt.Count}; a.len={a.len}; bb.Count={bb.Count}");
                        if (cnt - bb.Count != counter_a)
                            throw new Exception($"cnt - bb.Count != counter_a [cnt={cnt}; bb.Count={bb.Count}; counter_a={counter_a}; bbt.Count={bbt.Count}; strBytes.len={strBytes.len}]");

                        using var c = strBytesT!.Clone(0, counter_a);
                        if (counter_a != strBytesT!.len)
                        {
                            strBytesT = strBytesT.Clone(counter_a, strBytesT.len, destroyRecord: true);
                        }
                        else
                        {
                            strBytesT.Dispose();
                            strBytesT = null;
                        }

                        counter_a++;

                        if (  !tmpR1.UnsecureCompare(tmpR2)  )
                            throw new Exception("!a.UnsecureCompare(b): KMLk540ywd");
                        if (  !tmpR1.UnsecureCompare(c)  )
                            throw new Exception("!a.UnsecureCompare(c): KMLk541ywd");

                        lst.Add(tmpR1.CloneToSafeBytes(destroyRecord: false));
                    }                
                }
                catch (BytesBuilder.ResultCountIsTooLarge)
                {
                    strBytesT?.Dispose();
                }

                using var bbt_bytes = bbt.getBytes();
                if (  !strBytes.UnsecureCompare(bbt_bytes)  )
                    throw new Exception("!BytesBuilder_ForPointers_test: strBytes.UnsecureCompare(bbt_bytes): KMLk542ywd");

                strBytes.Dispose();
            }

            if (allocator.memAllocated != 0)
            {
                #if RECORD_DEBUG
                foreach (var rec in allocator.allocatedRecords)
                {
                    var e = new TestError();
                    e.Message = "record not disposed: " + rec.DebugName;
                    task.error.Add(e);
                }
                #endif
                throw new Exception($"BytesBuilder_ForPointers_test: allocator.memAllocated != 0 {allocator.memAllocated}");
            }

            return lst;
        }
    }
}
