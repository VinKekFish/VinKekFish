// #define CAN_CREATEFILE_FOR_BytesBuilder

/*
Это тесты для базового класса BytesBuilder
*/

// ::test:VOWNOWU4qu1Al9x07uh0:
// ::test:EFme5Fl1JymrEF4JVjL4:

namespace cryptoprime_tests;

using System.Reflection.PortableExecutable;
using System.Text;
using cryptoprime;
using DriverForTestsLib;

public class BytesBuilder_test_parent: ParentAutoSaveTask
{
    #if CAN_CREATEFILE_FOR_BytesBuilder
    public static readonly bool canCreateFile = true;
    #warning CAN_CREATEFILE_FOR_BytesBuilder
    #else
    public static readonly bool canCreateFile = false;
    #endif

    protected BytesBuilder_test_parent(TestConstructor constructor, SaverParent parentSaver): base
    (
        executer_and_saver: parentSaver,
        constructor:        constructor,
        canCreateFile:      canCreateFile
    )
    {
        this.parentSaver = parentSaver;
    }

    public override DirectoryInfo SetDirForFiles()
    {
        return GetDirectoryPath("src/tests/src/main/cryptoprime/BytesBuilder/tests/");
    }

    protected SaverParent parentSaver;
    protected abstract class SaverParent: TaskResultSaver
    {
        /// <summary>
        /// Создаёт инициализированный массив байтов. Инициализация по формуле (byte) (f + c*i + c2*(i + f2))
        /// </summary>
        /// <param name="len">Длина массива</param>
        /// <param name="f">Константа, которой инициализируется каждый элемент массива</param>
        /// <param name="c">Константа-множитель, определяющий значение, на которое домножается индекс (для f = 0 и c = 1 элемент массива равен его индексу)</param>
        /// <param name="f2">Доп. константа</param>
        /// <param name="c2">Доп. константа</param>
        /// <returns></returns>
        public static byte[] CreateByteArray(int len, int f, int c = 0, int f2 = 0, int c2 = 1)
        {
            var r = new byte[len];
            for (int i = 0; i < r.Length; i++)
            {
                r[i] = (byte) (f + c*i + c2*(i + f2));
            }

            return r;
        }
    }
/*
    public static nint getMaxMemory_ByOutOfMemory(nint maxPortionForAllocate = (1 << 32), bool gcTotalMemory = true)
    {
        nint result = 0;
        nint pfa    = maxPortionForAllocate;        // Порция для выделения памяти
        var  list   = new List<byte[]>(65536);

        do
        {
            try
            {
                list.Add(new byte[pfa]);
                result += pfa;
            }
            catch (OutOfMemoryException)
            {
                pfa >>= 1;
            }
        }
        while (pfa >= 65536);

        if (gcTotalMemory)
        {
            var a = (nint) GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            if (result < a)
                result = a;
        }

        return result;
    }
    */

    /// <summary>Работает только под Linux (требуется "/proc/meminfo")</summary>
    /// <returns></returns>
    public static nint GetMaxMemory()
    {
        // entry::warn:onlylinux:sOq1JvFKRxQyw7FQ:
        try
        {
            var mi = File.ReadAllLines("/proc/meminfo");
            var a1 = GetMaxMemory_getValueOfRecord(mi, "MemTotal:");
            var a2 = GetMaxMemory_getValueOfRecord(mi, "SwapTotal:");

            return a1 + a2;
        }
        catch
        {
            try
            {
                var mi = File.ReadAllLines(".debug.getMaxMemory");
                return nint.Parse(  mi[0].Trim()  );
            }
            catch (Exception e)
            {
                throw new Exception("getMaxMemory is not work properly. See the getMaxMemory code and let's create a '.debug.getMaxMemory' file", innerException: e);
            }
        }

        throw new Exception();
    }

    public static nint GetMaxMemory_getValueOfRecord(string[] records, string recordName)
    {
        foreach (var m in records)
        {
            if (m.StartsWith(recordName))
                continue;

            var ent   = m  .Split(':', StringSplitOptions.TrimEntries)[1];
            var rec   = ent.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ratio = 1;

            if (rec.Length != 2)
                throw new FormatException("getMaxMemory_getValueOfRecord: Unknown (1) format in " + recordName);

            if (rec[1] == "kB")
                ratio = 1024;
            else
            if (rec[1] == "MB")
                ratio = 1024 * 1024;
            else
                throw new FormatException("getMaxMemory_getValueOfRecord: Unknown (2) format in " + recordName);

            return nint.Parse(rec[0]) * ratio;
        }

        throw new Exception();
    }
}



[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("mandatory")]
[TestTagAttribute("BytesBuilder", duration: 40)]
public class BytesBuilder_test1: BytesBuilder_test_parent
{
    public BytesBuilder_test1(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public readonly static string Str1 = "Некоторая строка";
        public readonly static string Str2 = "Другой массив строк";
        public readonly static string Str3 = "Третья неизвестно что";

        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new();

            var bb = new BytesBuilder();

            // Тестируем пустой массив
            lst.Add(bb.GetBytes());

            // Пытаемся взять больше, чем есть
            try
            {
                lst.Add(bb.GetBytes(1));
            }
            catch (BytesBuilder.ResultCountIsTooLarge)
            {
                lst.Add(Array.Empty<byte>());
            }

            // Тестируем конкатенацию массивов
            bb.Add(Encoding.UTF8.GetBytes(Str1));
            bb.Add(Encoding.UTF8.GetBytes(Str2));
            bb.Add(Encoding.UTF8.GetBytes(Str3));

            if (bb.CountOfBlocks != 3)
                throw new Exception("bb.countOfBlocks != 3");
            if (  !BytesBuilder.UnsecureCompare(bb.GetBlock(0), Encoding.UTF8.GetBytes(Str1))  )
                throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(0), Encoding.UTF8.GetBytes(Str1))");
            if (  !BytesBuilder.UnsecureCompare(bb.GetBlock(1), Encoding.UTF8.GetBytes(Str2))  )
                throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(1), Encoding.UTF8.GetBytes(Str2))");
            if (  !BytesBuilder.UnsecureCompare(bb.GetBlock(2), Encoding.UTF8.GetBytes(Str3))  )
                throw new Exception("!BytesBuilder.UnsecureCompare(bb.getBlock(2), Encoding.UTF8.GetBytes(Str3))");


            var bb2 = new BytesBuilder();
            var strBytes = Encoding.UTF8.GetBytes(Str1 + Str2 + Str3);
            bb2.Add(strBytes);

            lst.Add(bb.GetBytes());

            // Проверяем, что эти две строки равны
            for (int i = 0; i < 2; i++)
            if (  !BytesBuilder.UnsecureCompare( bb.GetBytes(), bb2.GetBytes() )  )
                throw new Exception("!BytesBuilder.UnsecureCompare( bb.getBytes(), bb2.getBytes() ): kePOaxC8Iz");

            // Проверяем, что getBytes правильно возвращает частичные вхождения
            for (int i = 0; i < strBytes.Length; i++)
            if (  !BytesBuilder.UnsecureCompare( bb.GetBytes(i), bb2.GetBytes(i) )  )
                throw new Exception("!BytesBuilder.UnsecureCompare( bb.getBytes(), bb2.getBytes() ): qo3ZIsRLpQ");


            // Проверяем, что при маленьком ra будет выдано исключение
            var ra = new byte[1];
            try
            {
                bb.GetBytes(2, ra);
            }
            catch (BytesBuilder.ResultAIsTooSmall)
            {
                lst.Add(Array.Empty<byte>());
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

                    var a = bb .GetBytesAndRemoveIt(new byte[counter_a]);
                    var b = bb2.GetBytesAndRemoveIt(new byte[counter_a]);

                    bbt.Add(a);

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

            var bbt_bytes = BytesBuilder.CloneBytes(  bbt.GetBytes()  );
            if (  !BytesBuilder.UnsecureCompare(strBytes, bbt_bytes)  )
                throw new Exception("!BytesBuilder.UnsecureCompare(strBytes, bbt_bytes): KMLk442ywd");

            return lst;
        }
    }
}

[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder", duration: 9e3)]
public class BytesBuilder_test2: BytesBuilder_test_parent
{
    public BytesBuilder_test2(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new();

            var bb1 = new BytesBuilder();
            var bb2 = new BytesBuilder();
            // var bb3 = new BytesBuilder();

            // Тестируем addCopy и CloneBytes
            var a1 = CreateByteArray(64, 0);
            var a2 = BytesBuilder.CloneBytes(a1);
            // Меняем исходный массив - это ни на что уже не должно повлиять
            BytesBuilder.ToNull(a1);
            a1[0] = unchecked( (byte) -2 );

            bb1.AddCopy(a2);
            bb1.Add(a2);
            bb2.AddCopy(a2);
            bb2.AddCopy(a2);

            a2[0] = unchecked( (byte) -1 );

            // В массиве r1 теперь 64-ый байт должен быть равен -1, т.к. a2 в начале bb1 - это оригинальный массив и все изменения в нём отражаются на изменениях в bb1
            var r1 = bb1.GetBytes();
            var r2 = bb2.GetBytes();
            r2[64] = unchecked( (byte) -1 );

            if (r2[0] != 0)
                throw new Exception("BytesBuilder_test2: 000");

            if (!BytesBuilder.UnsecureCompare(r1, r2))
                throw new Exception("BytesBuilder_test2: 001");


            a2[1] = 0xFE;
            r1 = bb1.GetBytes();
            r2[65] = 0xFE;

            if (!BytesBuilder.UnsecureCompare(r1, r2))
                throw new Exception("BytesBuilder_test2: 002");

            lst.Add(r1);

            bb2.Clear();
            lst.Add(bb2.GetBytes());
            if (a2[0] != 0xFF)
                throw new Exception("BytesBuilder_test2: 002a");

            bb1.Clear();
            // После Clear a2 должно быть очищено, т.к. добавлялось по ссылке
            BytesBuilder.ToNull(a1);
            if (a2[0] != 0 || !BytesBuilder.UnsecureCompare(a1, a2))
                throw new Exception("BytesBuilder_test2: 002b");


            // Тестируем CloneBytes
            a1 = CreateByteArray(64, 1, 1);
            try
            {
                // При верной работе должен выдать ArgumentOutOfRangeException
                bb1.Add(Array.Empty<byte>());
                throw new Exception("BytesBuilder_test2: ex01");
            }
            catch (ArgumentOutOfRangeException)
            {}

            try
            {
                // При верной работе должен выдать ArgumentOutOfRangeException
                bb1.Add(BytesBuilder.CloneBytes(a1, 0, 0));
                throw new Exception("BytesBuilder_test2: ex02");
            }
            catch (ArgumentOutOfRangeException)
            {}

            bb1.Add(BytesBuilder.CloneBytes(a1, 0, 32));
            if (!BytesBuilder.UnsecureCompare(a1, bb1.GetBytes(), 32))
                throw new Exception("BytesBuilder_test2: 003");

            bb1.Add(BytesBuilder.CloneBytes(a1, 32, 63));
            if (!BytesBuilder.UnsecureCompare(a1, bb1.GetBytes(), 63))
                throw new Exception("BytesBuilder_test2: 004");

            bb1.Add(BytesBuilder.CloneBytes(a1, 63, 64));
            if (!BytesBuilder.UnsecureCompare(a1, bb1.GetBytes()))
                throw new Exception("BytesBuilder_test2: 005");

            lst.Add(bb1.GetBytes());


            // Тест getBytes с диапазонами и addByte
            a1 = CreateByteArray(1 << 17, 1, 1, 3, 5);
            bb1.Clear();
            bb2.Clear();
            bb1.Add(a1);
            foreach (var a in a1)
                bb2.AddByte(a);

            if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(), bb2.GetBytes()))
                throw new Exception("BytesBuilder_test2: 006-0");
            if (!BytesBuilder.UnsecureCompare(a1, bb2.GetBytes()))
                throw new Exception("BytesBuilder_test2: 006-1");
            if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(16), bb2.GetBytes(16)))
                throw new Exception("BytesBuilder_test2: 006-2");
            if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(2, 0), bb2.GetBytes(2, 0)))
                throw new Exception("BytesBuilder_test2: 006-3a");
            if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(2, 16), bb2.GetBytes(2, 16)))
                throw new Exception("BytesBuilder_test2: 006-3");
            if (!BytesBuilder.UnsecureCompare(a1, bb2.GetBytes(2, 16), 2, 16))
                throw new Exception("BytesBuilder_test2: 006-4");
            if (!BytesBuilder.UnsecureCompare(a1, bb2.GetBytes(1029, 65537), 1029, 65537))
                throw new Exception("BytesBuilder_test2: 006-5");
            
            lst.Add(bb2.GetBytes());


            // Продолжаем тестировать getBytes и тестируем addUshort
            a1 = CreateByteArray(1 << 11, 1, 1, 3, 5);
            bb1.Clear();
            bb2.Clear();

            bb1.Add(a1);
            for (int i = 0; i < a1.Length; i += 2)
            {
                int a = a1[i] + (a1[i+1] << 8);
                bb2.AddUshort((ushort) a);
            }

            if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(), bb2.GetBytes()))
                throw new Exception("BytesBuilder_test2: 007-0");

            for (int i = 0; i < a1.Length; i += 5)
            for (int j = a1.Length - i; j > 0; j -= 3)
            {
                if (!BytesBuilder.UnsecureCompare(a1, bb2.GetBytes(j, i), j, i))
                    throw new Exception($"BytesBuilder_test2: 007-1 ({i}, {j})");
                if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(j, i), bb2.GetBytes(j, i)))
                    throw new Exception($"BytesBuilder_test2: 007-2 ({i}, {j})");
            }

            lst.Add(bb2.GetBytes());


            a1 = CreateByteArray(1 << 11, 0, 1, 0, 0);
            a2 = new byte[a1.LongLength];
            BytesBuilder.CopyTo(a1, a2);
            if (!BytesBuilder.UnsecureCompare(a1, a2))
                throw new Exception("BytesBuilder_test2: 010-1");

            BytesBuilder.ToNull(a2);
            foreach (var a in a2)
                if (a != 0)
                    throw new Exception("BytesBuilder_test2: 010-2");

            for (int i = 1; i < a2.Length - 1; i++)
            {
                BytesBuilder.CopyTo(a1, a2, i);
                if (!BytesBuilder.UnsecureCompare(a2, a1, a2.Length - i, i))
                    throw new Exception($"BytesBuilder_test2: 010-3 (i={i})");
            }

            for (int k = 1; k < a2.Length - 2; k += 11)
            for (int i = 1; i < a2.Length - 1; i++)
            {
                BytesBuilder.ToNull(a2);                // Чтобы a2 был заполнен нулями: уменьшает вероятность случайного совпадения
                BytesBuilder.CopyTo(a1, a2, k, -1, i);
                for (int j = 0; j+k < a2.Length && j+i < a2.Length; j++)
                    if (a1[j+i] != a2[j+k])
                        throw new Exception($"BytesBuilder_test2: 010-4 (i={i})");
            }

            a1 = null;
            a2 = new byte[4];
            var @int = 0xEE7755AA;
            BytesBuilder.UIntToBytes(@int, ref a1, 0);

            a2[3] = (byte) (@int >> 24);
            a2[2] = (byte) (@int >> 16);
            a2[1] = (byte) (@int >> 8);
            a2[0] = (byte) (@int     );
            if (!BytesBuilder.UnsecureCompare(a2, a1 ?? throw new Exception()))
                throw new Exception($"BytesBuilder_test2: 010-5");

            BytesBuilder.BytesToUInt(out uint data, a2, 0);
            if (data != @int)
                throw new Exception($"BytesBuilder_test2: 010-5b");

            a1 = null;
            a2 = new byte[8];
            var @long = 0xFF8833CCEE7755AA;
            BytesBuilder.ULongToBytes(@long, ref a1);

            a2[7] = (byte) (@long >> 56);
            a2[6] = (byte) (@long >> 48);
            a2[5] = (byte) (@long >> 40);
            a2[4] = (byte) (@long >> 32);
            a2[3] = (byte) (@long >> 24);
            a2[2] = (byte) (@long >> 16);
            a2[1] = (byte) (@long >> 8);
            a2[0] = (byte) (@long     );
            if (!BytesBuilder.UnsecureCompare(a2, a1 ?? throw new Exception()))
                throw new Exception($"BytesBuilder_test2: 010-6");

            BytesBuilder.BytesToULong(out ulong datal, a2, 0);
            if (datal != @long)
                throw new Exception($"BytesBuilder_test2: 010-6b");

            ulong step = 1;
            a2 = new byte[12];
            // i > 0 - здесь переполнение
            for (ulong i = 0; i < long.MaxValue; i += step)
            {
                BytesBuilder.VariableULongToBytes(i, ref a2, 0);
                if (a2 == null) throw new Exception();
                BytesBuilder.BytesToVariableULong(out ulong datall, a2, 0);

                if (datall != (ulong) i)
                    throw new Exception($"BytesBuilder_test2: 010-7; i = {i}; {datall} == {(ulong)i}");

                for (int thr = 56; thr >= 8; thr -= 8)
                    if (i > (1UL << (thr+1)))
                    {
                        step = (1UL << thr) + 1UL;
                        break;
                    }
            }



            // UnsecureCompare - тестируем другую версию
            a1 = CreateByteArray(1 << 15, 1, 1, 3, 5);
            if (!BytesBuilder.UnsecureCompare(a1, a1, out _))
                throw new Exception("BytesBuilder_test2: 011-0");

            a2 = BytesBuilder.CloneBytes(a1);
            if (!BytesBuilder.UnsecureCompare(a1, a2, out _))
                throw new Exception("BytesBuilder_test2: 011-1");

            a2[^1] = 0xFF;
            if (BytesBuilder.UnsecureCompare(a1, a2, out nint index10) || index10 != a2.Length - 1)
                throw new Exception("BytesBuilder_test2: 011-2");

            for (int i = a2.Length - 2; i >= 0; i--)
            {
                if (a2[i] != 0)
                    a2[i] = 0;
                else
                    a2[i] = 0xFF;

                if (BytesBuilder.UnsecureCompare(a1, a2, out index10) || index10 != i)
                    throw new Exception($"BytesBuilder_test2: 011-2 i = {i}");
            }

            var estr = "01234567890";
            BytesBuilder.ClearString(estr);
            if (estr != "           ")
                throw new Exception("BytesBuilder_test2: 012");

            return lst;
        }
    }
}


[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder", duration: 12e3)]
public class BytesBuilder_test3: BytesBuilder_test_parent
{
    public BytesBuilder_test3(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new();

            var bb1 = new BytesBuilder();
            var bb2 = new BytesBuilder();

            // Продолжаем тестировать getBytes и тестируем addInt
            var a1 = CreateByteArray(1 << 12, 2, 1, 3, 5);
            bb1.Clear();
            bb2.Clear();

            bb1.Add(a1);
            for (int i = 0; i < a1.Length; i += 4)
            {
                int a = a1[i] + (a1[i+1] << 8) + (a1[i+2] << 16) + (a1[i+3] << 24);
                bb2.AddInt(a);
            }

            if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(), bb2.GetBytes()))
                throw new Exception("BytesBuilder_test3: 008-0");

            for (int i = 0; i < a1.Length; i += 3)
            for (int j = a1.Length - i; j > 0; j -= 7)
            {
                if (!BytesBuilder.UnsecureCompare(a1, bb2.GetBytes(j, i), j, i))
                    throw new Exception($"BytesBuilder_test3: 008-1 ({i}, {j})");
                if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(j, i), bb2.GetBytes(j, i)))
                    throw new Exception($"BytesBuilder_test3: 008-2 ({i}, {j})");
            }

            lst.Add(bb2.GetBytes());

            return lst;
        }
    }
}


[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder", duration: 20e3)]
public class BytesBuilder_test4: BytesBuilder_test_parent
{
    public BytesBuilder_test4(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new();

            var bb1 = new BytesBuilder();
            var bb2 = new BytesBuilder();

            // Продолжаем тестировать getBytes и тестируем addULong
            var a1 = CreateByteArray(1 << 13, 3, 1, 3, 5);
            bb1.Clear();
            bb2.Clear();

            bb1.Add(a1);
            for (int i = 0; i < a1.Length; i += 8)
            {
                ulong a = 0;
                for (int j = 7; j >= 0; j--)
                    a = a1[i + j] + (a << 8);

                bb2.AddULong(a);
            }

            if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(), bb2.GetBytes()))
                throw new Exception("BytesBuilder_test4: 009-0");

            for (int i = 0; i < a1.Length; i += 5)
            for (int j = a1.Length - i; j > 0; j -= 11)
            {
                if (!BytesBuilder.UnsecureCompare(a1, bb2.GetBytes(j, i), j, i))
                    throw new Exception($"BytesBuilder_test4: 009-1 ({i}, {j})");
                if (!BytesBuilder.UnsecureCompare(bb1.GetBytes(j, i), bb2.GetBytes(j, i)))
                    throw new Exception($"BytesBuilder_test4: 009-2 ({i}, {j})");
            }

            lst.Add(bb2.GetBytes());

            return lst;
        }
    }
}

[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder", duration: 4)]
public unsafe class BytesBuilder_test5_add: BytesBuilder_test_parent
{
    public BytesBuilder_test5_add(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new();

            const int len = 17;

            var bb1 = CreateByteArray(len, 127, 1, 0, 0);
            var bb2 = CreateByteArray(len, 127, 3, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            bb1 = CreateByteArray(len, 127, 1, 0, 0);
            bb2 = CreateByteArray(len, 127, 1, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            bb1 = CreateByteArray(len, 127, 1,   0, 0);
            bb2 = CreateByteArray(len, 204, 255, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            bb1 = CreateByteArray(len, 129, 0, 0, 0);
            bb2 = CreateByteArray(len, 127, 0, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            return lst;

            static void TestMethod(List<byte[]> lst, int len, ref byte[] bb1, ref byte[] bb2)
            {
                byte[] bba1 = new byte[len];
                byte[] bba2 = new byte[len];
                BytesBuilder.CopyTo(bb1, bba1);
                BytesBuilder.CopyTo(bb2, bba2);

                fixed (byte* bp1 = bb1)
                fixed (byte* bp2 = bb2)
                {
                    BytesBuilder.ArithmeticAddBytesTail(len, bp1, bp2);
                }

                fixed (byte* bpa1 = bba1)
                fixed (byte* bpa2 = bba2)
                {
                    uint* upa1 = (uint*)bpa1;
                    uint* upa2 = (uint*)bpa2;


                    unchecked
                    {
                        ulong cf = 0, a, b;
                        a = upa1[0];
                        b = upa2[0];

                        a += b;

                        upa1[0] = (uint)(a);
                        if (a > uint.MaxValue)
                            cf++;


                        a = upa1[1];
                        b = upa2[1];

                        a += b + cf; cf = 0;

                        upa1[1] = (uint)(a);
                        if (a > uint.MaxValue)
                            cf++;


                        a = upa1[2];
                        b = upa2[2];

                        a += b + cf; cf = 0;

                        upa1[2] = (uint)(a);
                        if (a > uint.MaxValue)
                            cf++;


                        a = upa1[3];
                        b = upa2[3];

                        a += b + cf; cf = 0;

                        upa1[3] = (uint)(a);
                        if (a > uint.MaxValue)
                            cf++;

                        bpa1[16] = (byte)((ulong)bpa1[16] + (ulong)bpa2[16] + cf);
                    }
                }

                if (!BytesBuilder.UnsecureCompare(bb1, bba1))
                    throw new Exception("BytesBuilder_test5_add: bb1 != bba1");

                lst.Add(bb1);
                lst.Add(bba1);
            }
        }
    }
}

[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder", duration: 800)]
public unsafe class BytesBuilder_test5_add4: BytesBuilder_test_parent
{
    public BytesBuilder_test5_add4(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new();

            const int len = 17*11;

            var bb1 = CreateByteArray(len, 127, 1, 0, 0);
            var bb2 = CreateByteArray(len, 127, 3, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            bb1 = CreateByteArray(len, 127, 1, 0, 0);
            bb2 = CreateByteArray(len, 127, 1, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            bb1 = CreateByteArray(len, 127, 1,   0, 0);
            bb2 = CreateByteArray(len, 204, 255, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            bb1 = CreateByteArray(len, 129, 0, 0, 0);
            bb2 = CreateByteArray(len, 127, 0, 0, 0);
            TestMethod(lst, len, ref bb1, ref bb2);

            for (int i = 1; i <= byte.MaxValue; i++)
            {
                bb1 = CreateByteArray(len, i, 1, 0, 0);
                bb2 = CreateByteArray(len, i, 3, 0, 0);
                TestMethod(lst, len, ref bb1, ref bb2);

                for (int j = 1; j <= byte.MaxValue; j++)
                {
                    bb1 = CreateByteArray(len, i, 0, 0, 0);
                    bb2 = CreateByteArray(len, j, 0, 0, 0);
                    TestMethod(lst, len, ref bb1, ref bb2, false);
                }
            }

            return lst;

            static void TestMethod(List<byte[]> lst, int len, ref byte[] bb1, ref byte[] bb2, bool addToList = true)
            {
                byte[] bba1 = new byte[len];
                byte[] bba2 = new byte[len];
                BytesBuilder.CopyTo(bb1, bba1);
                BytesBuilder.CopyTo(bb2, bba2);

                fixed (byte* bp1 = bb1)
                fixed (byte* bp2 = bb2)
                {
                    BytesBuilder.ArithmeticAddBytesTail(len, bp1, bp2);
                }

                fixed (byte* bpa1 = bba1)
                fixed (byte* bpa2 = bba2)
                {
                    BytesBuilder.ArithmeticAddBytes(len, bpa1, bpa2);
                }

                if (!BytesBuilder.UnsecureCompare(bb1, bba1))
                    throw new Exception("BytesBuilder_test5_add4: bb1 != bba1");

                if (addToList)
                {
                    lst.Add(bb1);
                    lst.Add(bba1);
                }
            }
        }
    }
}

[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder")]
[TestTagAttribute("performance", duration: 580d, singleThread: true)]
public unsafe class BytesBuilder_test5_add_performance: TestTask
{
    public BytesBuilder_test5_add_performance(TestConstructor constructor):
                                        base(nameof(BytesBuilder_test5_add_performance), constructor: constructor)
    {
        TaskFunc = () =>
        {
            static byte[] createByteArray(int len, int f, int c = 0, int f2 = 0, int c2 = 1)
            {
                var r = new byte[len];
                for (int i = 0; i < r.Length; i++)
                {
                    r[i] = (byte) (f + c*i + c2*(i + f2));
                }

                return r;
            }

            const int len = 100000;
            var bb1 = createByteArray(len, 0, 1, 0, 0);
            var bb2 = createByteArray(len, 129, 3, 0, 0);
            var bb3 = createByteArray(len, 0, 1, 0, 0);
            var bb4 = createByteArray(len, 129, 3, 0, 0);

            var st_etalon = new DriverForTestsLib.SimpleTimeMeter();
            var st        = new DriverForTestsLib.SimpleTimeMeter();


            const int cnt = 1000;
            Parallel.Invoke
            (
                () =>
                {
                    fixed (byte* bp1 = bb1)
                    fixed (byte* bp2 = bb2)
                    using (st_etalon)
                    {
                        for (int i = 0; i < cnt; i++)
                            BytesBuilder.ArithmeticAddBytesTail(len, bp1, bp2);
                    }
                },
                () =>
                {
                    fixed (byte* bp3 = bb3)
                    fixed (byte* bp4 = bb4)
                    using (st)
                    {
                        for (int i = 0; i < cnt; i++)
                            BytesBuilder.ArithmeticAddBytes(len, bp3, bp4);
                    }
                }
            );

            var k = st_etalon.TotalMilliseconds / (double) st.TotalMilliseconds;
            this.Name += $" ({k:F3})";

            double maxK = 2;
            if (k < maxK)
                throw new Exception($"BytesBuilder_test5_add_performance: k < {maxK}; k = {k};");
        };
    }
}

[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder")]
[TestTagAttribute("performance", duration: 20d, singleThread: false)]
public unsafe class BytesBuilder_test5_reverse: TestTask
{
    public BytesBuilder_test5_reverse(TestConstructor constructor):
                                        base(nameof(BytesBuilder_test5_reverse), constructor: constructor)
    {
        TaskFunc = () =>
        {
            static byte[] createByteArray(int len, int f, int c)
            {
                var r = new byte[len];
                for (int i = 0; i < r.Length; i++)
                {
                    r[i] = (byte) (f + c*i);
                }

                return r;
            }

            const int len = 255;
            var bb1 = createByteArray(len+0, 0, 1);
            var bb2 = createByteArray(len+1, 0, 1);

            fixed (byte* bp1 = bb1)
            BytesBuilder.ReverseBytes(len+0, bp1);

            fixed (byte* bp2 = bb2)
            BytesBuilder.ReverseBytes(len+1, bp2);

            for (int i = 0; i < len; i++)
            {
                if (bb1[i] != len - i - 1)
                    throw new Exception($"BytesBuilder_test5_reverse: bb1[i] != len - i - 1 (i={i}; len-i-1={len-i-1}; bb1[i]={bb1[i]})");
            }

            for (int i = 0; i < len+1; i++)
            {
                if (bb2[i] != len - i)
                    throw new Exception($"BytesBuilder_test5_reverse: bb2[i] != len - i (i={i}; len-i={len-i}; bb2[i]={bb2[i]})");
            }
        };
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("UP_BytesBuilder")]
[TestTagAttribute("BytesBuilder")]
[TestTagAttribute("performance", duration: 20d, singleThread: false)]
public unsafe class BytesBuilder_test5_xor: TestTask
{
    public BytesBuilder_test5_xor(TestConstructor constructor):
                                        base(nameof(BytesBuilder_test5_xor), constructor: constructor)
    {
        TaskFunc = () =>
        {
            static byte[] createByteArray(int len, int f, int c)
            {
                var r = new byte[len];
                for (int i = 0; i < r.Length; i++)
                {
                    r[i] = (byte) (f + c*i);
                }

                return r;
            }

            const int len = 137;
            const int A   = 173;
            var bb1 = createByteArray(len,  0, 1);
            var bb2 = createByteArray(len, 11, 0);
            var bb3 = createByteArray(len,  0, A);
            var bb0 = createByteArray(len,  0, 0);

            fixed (byte* bp1 = bb1)
            fixed (byte* bp0 = bb0)
            BytesBuilder.Xor(len, bp1, bp0);

            for (int i = 0; i < len; i++)
            {
                if (bb1[i] != i)
                    throw new Exception($"BytesBuilder_test5_xor: bb1[i] != i (i={i}; bb1[i]={bb1[i]})");
            }

            fixed (byte* bp1 = bb1)
            fixed (byte* bp2 = bb2)
            BytesBuilder.Xor(len, bp1, bp2);

            for (int i = 0; i < len; i++)
            {
                if (bb1[i] != (byte)(i ^ 11))
                    throw new Exception($"BytesBuilder_test5_xor: bb1[i] != i (i={i}; i^11={i^11}; bb1[i]={bb1[i]})");
            }

            fixed (byte* bp1 = bb1)
            fixed (byte* bp2 = bb2)
            BytesBuilder.Xor(len, bp1, bp2);

            for (int i = 0; i < len; i++)
            {
                if (bb1[i] != i)
                    throw new Exception($"BytesBuilder_test5_xor: bb1[i] != i (2) (i={i}; bb1[i]={bb1[i]})");
            }

            fixed (byte* bp1 = bb1)
            fixed (byte* bp3 = bb3)
            BytesBuilder.Xor(len, bp1, bp3);

            for (int i = 0; i < len; i++)
            {/*
                if (bb3[i] != (byte) (i*A))
                    throw new Exception($"BytesBuilder_test5_xor: bb3[i] != i*A (i={i}; i*A={(byte)(i*A)}; bb1[i]={bb3[i]})");
*/
                if (bb1[i] != (byte) (i ^ (i*A)))
                    throw new Exception($"BytesBuilder_test5_xor: bb1[i] != (i ^ (i*A)) (i={i}; i*i^a={i ^ (i*A)}; bb1[i]={bb1[i]})");
            }
        };
    }
}
