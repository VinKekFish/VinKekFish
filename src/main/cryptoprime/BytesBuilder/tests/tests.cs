#define CAN_CREATEFILE_FOR_BytesBuilder

namespace cryptoprime_tests;

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

    public override DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath("src/main/cryptoprime/BytesBuilder/tests/");
    }

    protected SaverParent parentSaver;
    protected abstract class SaverParent: TaskResultSaver
    {}
}


[TestTagAttribute("fast")]
[TestTagAttribute("fast_level2")]

[TestTagAttribute("mandatory")]
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

[TestTagAttribute("fast")]
[TestTagAttribute("fast_level2")]
public class BytesBuilder_test2: BytesBuilder_test_parent
{
    public BytesBuilder_test2(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    protected class Saver: SaverParent
    {
        public byte[] createByteArray(int len, int f, int c = 0, int f2 = 0, int c2 = 1)
        {
            var r = new byte[len];
            for (int i = 0; i < r.Length; i++)
            {
                r[i] = (byte) (f + c*i + c2*(i * f2));
            }

            return r;
        }

        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            var bb1 = new BytesBuilder();
            var bb2 = new BytesBuilder();
            var bb3 = new BytesBuilder();

            // Тестируем addCopy и CloneBytes
            var a1 = createByteArray(64, 0);
            var a2 = BytesBuilder.CloneBytes(a1);
            // Меняем исходный массив - это ни на что уже не должно повлиять
            BytesBuilder.ToNull(a1);
            a1[0] = unchecked( (byte) -2 );

            bb1.addCopy(a2);
            bb1.add(a2);
            bb2.addCopy(a2);
            bb2.addCopy(a2);

            a2[0] = unchecked( (byte) -1 );

            // В массиве r1 теперь 64-ый байт должен быть равен -1, т.к. a2 в начале bb1 - это оригинальный массив и все изменения в нём отражаются на изменениях в bb1
            var r1 = bb1.getBytes();
            var r2 = bb2.getBytes();
            r2[64] = unchecked( (byte) -1 );

            if (r2[0] != 0)
                throw new Exception("BytesBuilder_test2: 000");

            if (!BytesBuilder.UnsecureCompare(r1, r2))
                throw new Exception("BytesBuilder_test2: 001");


            a2[1] = 0xFE;
            r1 = bb1.getBytes();
            r2[65] = 0xFE;

            if (!BytesBuilder.UnsecureCompare(r1, r2))
                throw new Exception("BytesBuilder_test2: 002");

            lst.Add(r1);

            bb2.Clear();
            lst.Add(bb2.getBytes());
            if (a2[0] != 0xFF)
                throw new Exception("BytesBuilder_test2: 002a");

            bb1.Clear();
            // После Clear a2 должно быть очищено, т.к. добавлялось по ссылке
            BytesBuilder.ToNull(a1);
            if (a2[0] != 0 || !BytesBuilder.UnsecureCompare(a1, a2))
                throw new Exception("BytesBuilder_test2: 002b");


            // Тестируем CloneBytes
            a1 = createByteArray(64, 1, 1);
            try
            {
                // При верной работе должен выдать ArgumentOutOfRangeException
                bb1.add(new byte[0]);
                throw new Exception("BytesBuilder_test2: ex01");
            }
            catch (ArgumentOutOfRangeException)
            {}

            try
            {
                // При верной работе должен выдать ArgumentOutOfRangeException
                bb1.add(BytesBuilder.CloneBytes(a1, 0, 0));
                throw new Exception("BytesBuilder_test2: ex02");
            }
            catch (ArgumentOutOfRangeException)
            {}

            bb1.add(BytesBuilder.CloneBytes(a1, 0, 32));
            if (!BytesBuilder.UnsecureCompare(a1, bb1.getBytes(), 32))
                throw new Exception("BytesBuilder_test2: 003");

            bb1.add(BytesBuilder.CloneBytes(a1, 32, 63));
            if (!BytesBuilder.UnsecureCompare(a1, bb1.getBytes(), 63))
                throw new Exception("BytesBuilder_test2: 004");

            bb1.add(BytesBuilder.CloneBytes(a1, 63, 64));
            if (!BytesBuilder.UnsecureCompare(a1, bb1.getBytes()))
                throw new Exception("BytesBuilder_test2: 005");

            lst.Add(bb1.getBytes());


            return lst;
        }
    }
}
