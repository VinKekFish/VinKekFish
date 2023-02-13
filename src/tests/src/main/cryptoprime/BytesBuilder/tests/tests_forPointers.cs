// Флаг изменения тестовых файлов идёт в tests.cs

namespace cryptoprime_tests;

using System.Text;
using cryptoprime;
using DriverForTestsLib;

using static cryptoprime.BytesBuilderForPointers;

[TestTagAttribute("BytesBuilder_ForPointers", duration: 4*10e3, singleThread: true)]
public unsafe class BytesBuilder_ForPointers_test1: BytesBuilder_test_parent
{
    public BytesBuilder_ForPointers_test1(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var bb = new BytesBuilder();

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


[TestTagAttribute("BytesBuilder_ForPointers", duration: 8500d)]
public unsafe class BytesBuilder_ForPointers_test2: BytesBuilder_test_parent
{
    public BytesBuilder_ForPointers_test2(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var bb = new BytesBuilder();

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

[TestTagAttribute("inWork")]
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

        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var bb = new BytesBuilder();

            // Тестируем пустой массив
            lst.Add(bb.getBytes());

            // Пытаемся взять больше, чем есть
            try
            {
                lst.Add(bb.getBytes(1));
            }
            catch (BytesBuilder.ResultCountIsTooLarge)
            {
                lst.Add(new byte[0]);
            }

            // Тестируем конкатенацию массивов
            bb.add(Encoding.UTF8.GetBytes(Str1));
            bb.add(Encoding.UTF8.GetBytes(Str2));
            bb.add(Encoding.UTF8.GetBytes(Str3));

            if (bb.countOfBlocks != 3)
                throw new Exception("bb.countOfBlocks != 3");
            if (  !BytesBuilder.UnsecureCompare(bb.getBlock(0), Encoding.UTF8.GetBytes(Str1))  )
                throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(0), Encoding.UTF8.GetBytes(Str1))");
            if (  !BytesBuilder.UnsecureCompare(bb.getBlock(1), Encoding.UTF8.GetBytes(Str2))  )
                throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(1), Encoding.UTF8.GetBytes(Str2))");
            if (  !BytesBuilder.UnsecureCompare(bb.getBlock(2), Encoding.UTF8.GetBytes(Str3))  )
                throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(2), Encoding.UTF8.GetBytes(Str3))");


            var bb2 = new BytesBuilder();
            var strBytes = Encoding.UTF8.GetBytes(Str1 + Str2 + Str3);
            bb2.add(strBytes);

            lst.Add(bb.getBytes());

            // Проверяем, что эти две строки равны
            for (int i = 0; i < 2; i++)
            if (  !BytesBuilder.UnsecureCompare( bb.getBytes(), bb2.getBytes() )  )
                throw new Exception("!BytesBuilder.UnsecureCompare( bb.getBytes(), bb2.getBytes() ): kePOaxC8Iz");

            // Проверяем, что getBytes правильно возвращает частичные вхождения
            for (int i = 0; i < strBytes.Length; i++)
            if (  !BytesBuilder.UnsecureCompare( bb.getBytes(i), bb2.getBytes(i) )  )
                throw new Exception("!BytesBuilder.UnsecureCompare( bb.getBytes(), bb2.getBytes() ): qo3ZIsRLpQ");


            // Проверяем, что при маленьком ra будет выдано исключение
            var ra = new byte[1];
            try
            {
                bb.getBytes(2, ra);
            }
            catch (BytesBuilder.ResultAIsTooSmall)
            {
                lst.Add(new byte[0]);
            }


            var bbt  = new BytesBuilder();
            strBytes = Encoding.UTF8.GetBytes(Str1 + Str2 + Str3);
            try
            {
                var strBytesT = strBytes;
                int counter_a = 1;
                while (true)
                {
                    if (counter_a >= bb.Count)
                        counter_a = (int) bb.Count - 3;
                    if (counter_a < 1)
                        counter_a = 1;

                    var cnt = bb.Count;

                    var a = bb .getBytesAndRemoveIt(new byte[counter_a]);
                    var b = bb2.getBytesAndRemoveIt(new byte[counter_a]);

                    bbt.add(a);

                    if (cnt - bb.Count != counter_a)
                        throw new Exception("cnt - bb.Count != counter_a");

                    var c     = strBytesT[0 .. counter_a];
                    strBytesT = strBytesT[counter_a .. ^0];
                    counter_a++;

                    if (  !BytesBuilder.UnsecureCompare(a, b)  )
                        throw new Exception("!BytesBuilder.UnsecureCompare(a, b): KMLk440ywd");
                    if (  !BytesBuilder.UnsecureCompare(a, c)  )
                        throw new Exception("!BytesBuilder.UnsecureCompare(a, c): KMLk441ywd");

                    lst.Add(a);
                }                
            }
            catch (BytesBuilder.ResultCountIsTooLarge)
            {}

            var bbt_bytes = BytesBuilder.CloneBytes(  bbt.getBytes()  );
            if (  !BytesBuilder.UnsecureCompare(strBytes, bbt_bytes)  )
                throw new Exception("!BytesBuilder.UnsecureCompare(strBytes, bbt_bytes): KMLk442ywd");

            return lst;
        }
    }
}
