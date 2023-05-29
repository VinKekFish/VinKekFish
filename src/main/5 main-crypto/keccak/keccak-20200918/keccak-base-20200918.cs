using System.Runtime.InteropServices;
using static cryptoprime.KeccakPrime;
using cryptoprime;

namespace maincrypto.keccak;

/// <summary>Базовый класс для реализаций keccak. Умеет производить заполнение всей памяти (на всякий случай)</summary>
public abstract class Keccak_base_20200918: Keccak_abstract
{
    public Keccak_base_20200918(bool noInit = false): base(noInit)
    {}

    /// <summary>Производит очистку состояния объекта</summary>
    /// <param name="GCCollect">Если <see langword="true"/>, то пытается произвести полную очистку памяти приложения,
    /// выделяя кучу памяти и перезаписывая её. Без гарантий на перезапись.
    /// Осторожно!!! Функция может использовать слишком много оперативной памяти, что может повлечь за собой отказ других программ в ОС</param>
    public override void Clear(bool GCCollect = false)
    {
        base.Clear(GCCollect);

        if (GCCollect)
        {
            // Не делаем здесь Collect, потому что нам нужно, чтобы новый проверочный массив, желательно, был не в начале массива памяти
            // Он должен быть перезатёрт не сразу
            // GC.Collect();
            try
            {
                for (int i = 0; i < 1; i++)
                {
                    // Выносим в отдельную функцию, чтобы всё, что там выделено, выходило из контекста и успешно удалялось
                    AllocFullMemoryInLongBlocksWithSecondAlloc();
                    GC.Collect();
                }
            }
            catch (OutOfMemoryException)
            {
                
            }

            GC.Collect();
        }
    }

    // Смысл функции состоит в выделении большого количества памяти
    // Однако, она не ждёт, пока память совсем закончится (это негативно влияет даже на другие приложения)
    // Вместо этого она выделяет проверочный массив и запоминает указатель на него в неперемещаемом виде
    // и создаёт проверочный объект "co"
    // Когда объект "co" переходит в иное поколение сборщика, мы позволяем сборщику мусора очистить всю выделенную нами память
    // А дальше, снова здорова, перезаписываем её
    // После этого, когда объект "co" достигнет максимального поколения, мы освобождаем проверочный массив
    // Далее мы снова создаём объекты и пытаемся читать по указателю на проверочный массив, перезатёрся он или нет
    // И когда он уже перезатрся, выходим из функции
    // Весь этот маразм нужен для того, чтобы попытаться, с одной стороны, не выделять 100% всей памяти, доступной в ОС, а выделить 95%
    // С другой стороны, всё-таки, перезаписать всё, что мы хотим перезаписать
    /// <summary>Попытка очистить часть памяти</summary>
    public unsafe static void AllocPartOfMemory()
    {
        bool hIsFreed = false;
        AllocCheckArray(out long number, out GCHandle h, out long* p2);

        var co = new object();
        int MaxGenerationReached = 0;
        List<byte[]> bytes = new List<byte[]>(1024);
        try
        {
            // Выделяем память небольшими блоками, большими перезатирается хуже
            // Причём, вроде бы, и для больших блоков тоже маленькими перезатирается лучше
            long  memSize = Environment.SystemPageSize;
            while (memSize > 1)
            {
                try
                {
                    var obj = new byte[memSize];
                    BytesBuilder.ToNull(obj, 0x3737_3737__3737_3737);

                    // Создаём ещё один паразитный объект
                    // Чтобы сборщику мусора было что собирать: это поможет быстро нарастить номер поколения для проверочного объекта co
                    // Это может быть плохо, но, в целом, как-то работает
                    var a = new byte[memSize];
                    BytesBuilder.ToNull(a, 0x3737_3737__3737_3737);
                    a = null;

                    bool p2IsLive = false;
                    if (p2 != null)
                    try
                    {
                        p2IsLive = p2[0] == number;
                    }
                    catch
                    { }

                    if (p2IsLive)
                    {
                        bytes.Add(obj);
                    }

                    // Обращаю внимание на то, что co может снизить своё поколение, а не только повысить
                    if (MaxGenerationReached != GC.MaxGeneration && GC.GetGeneration(co) > MaxGenerationReached)
                    {
                        MaxGenerationReached = GC.GetGeneration(co);
                        bytes.Clear();

                        if (MaxGenerationReached == GC.MaxGeneration && !hIsFreed)
                        {
                            // Собираем мусор перед удалением проверочного массива, чтобы он не собрал сразу и наш объект
                            FreeHandle();
                        }
                    }

                    if (!p2IsLive && MaxGenerationReached == GC.MaxGeneration)
                    {
                        break;
                    }
                }
                catch (OutOfMemoryException)
                {
                    bytes.Clear();

                    FreeHandle();
                }
            }
        }
        catch (OutOfMemoryException)
        { }
        finally
        {
            if (!hIsFreed)
                h.Free();
        }

        unsafe void FreeHandle()
        {
            GC.Collect();
            h.Free();
            hIsFreed = true;
        }
    }

    /// <summary>Эта функция старается выделить для себя всю доступную память - эта функция пытается жёстко очистить память, может вызвать нехватку памяти у других приложений влоть до падения системных приложений</summary>
    public unsafe static void AllocFullMemory(long minSize = 0)
    {
        if (minSize <= 0)
            minSize = Environment.SystemPageSize;

        // Выделяем сначала большими блоками по 4096*256*4 = 4 Мб
        long  memSize = (Environment.SystemPageSize << 8) * 4;
        if (memSize < minSize)
            memSize = minSize;

        List<byte[]> bytes = new List<byte[]>(1024);
        try
        {
            while (memSize >= minSize)
            {
                try
                {
                    var obj = new byte[memSize];
                    BytesBuilder.ToNull(obj, 0x3737_3737__3737_3737);
                    bytes.Add(obj);

                    // Создаём ещё один паразитный объект
                    // Чтобы сборщику мусора было что собирать: это поможет быстро нарастить номер поколения для проверочного объекта co
                    // Это может быть плохо, но, в целом, как-то работает
                    var a = new byte[memSize];
                    BytesBuilder.ToNull(a, 0x3737_3737__3737_3737);
                    a = null;
                }
                catch (OutOfMemoryException)
                {
                    memSize >>= 1;
                }
            }
        }
        catch (OutOfMemoryException)
        {
        }
        finally
        {
        }
    }

    /// <summary>Эта функция старается выделить для себя всю доступную память, но только по размеру блока (чтобы не выделить вообще всю память на себя) - это функция пытается очистить память</summary>
    public unsafe static void AllocFullMemoryInLongBlocks(long blockSize = 0)
    {
        // Выделяем большими блоками по 4096*256*512 = 512 Мб
        if (blockSize < 1)
            blockSize = (Environment.SystemPageSize << 8) * 512;   // Блоки по 512 Мб

        List<byte[]> bytes = new List<byte[]>(1024);
        try
        {
            long  memSize = blockSize;
            while (memSize >= Environment.SystemPageSize)
            {
                try
                {
                    var obj = new byte[memSize];
                    BytesBuilder.ToNull(obj, 0x3737_3737__3737_3737);
                    bytes.Add(obj);

                    // Создаём ещё один паразитный объект
                    // Чтобы сборщику мусора было что собирать: это поможет быстро нарастить номер поколения для проверочного объекта co
                    // Это может быть плохо, но, в целом, как-то работает
                    var a = new byte[memSize];
                    BytesBuilder.ToNull(a, 0x3737_3737__3737_3737);
                    a = null;
                }
                catch (OutOfMemoryException)
                {
                    return;
                }
            }
        }
        catch (OutOfMemoryException)
        {
        }
        finally
        {
        }
    }

    /// <summary>Несколько раз выделяет память в системе большими блоками (чтобы всё совсем не перевыделить) - это функция пытается очистить память. Эта функция рекомендуется перед остальными</summary>
    /// <param name="blockSize">Размер блока</param>
    public unsafe static void AllocFullMemoryInLongBlocksWithSecondAlloc(long blockSize = 0)
    {
        AllocPartOfMemory();
        AllocFullMemory(blockSize);
        AllocPartOfMemory();
        AllocFullMemoryInLongBlocks(blockSize);
    }

    // Выделяем два массива. Один тут же удаляем. Указатели на оба, включая удалённый, передаём вверх
    private static unsafe void AllocCheckArray(out long number, out GCHandle h, out long* p2)
    {
        number = 0x01020304_09080706;

        h = GCHandle.Alloc(new long[1] { number }, GCHandleType.Pinned);
        p2 = (long*)h.AddrOfPinnedObject().ToPointer();
    }

    /// <summary>DoubleHash.one - обычный хеш 64-ре байта, DoubleHash.two - два раза по 64-ре байта, DoubleHash.full72 - один раз 72 байта</summary>
    public enum DoubleHash
    {                                                   /// <summary>Ошибка</summary>
        error  = 0,                                     /// <summary>Одинарный хеш (64 байта)</summary>
        one    = 1,                                     /// <summary>Двойной хеш (2x64 байта)</summary>
        two    = 2,                                     /// <summary>Одинарный хеш, длиной 72 байта</summary>
        full72 = 72
    };

    /// <summary>Получает 512-тибитный хеш keccak (не SHA-3, есть отличия)</summary>
    /// <param name="message">Массив для хеширования</param>
    /// <param name="messageFullLen">Полная длина массива для хеширования message (начиная с позиции message[0])</param>
    /// <param name="doClear">Если true, то после вычисления хеша выполняется очистка.
    /// Очистка производится с помощью вызова ClearState: это очистка состояния, включая вспомогательные массивы, но без вызова Clear()</param>
    /// <param name="startIndex">Начальный индекс того, что хешируем в message (по-умолчанию - 0)</param>
    /// <param name="countToHash">Количество элементов для хеширования (по-умолчанию - -1 - до конца)</param>
    /// <param name="isInitialized">Если false (по-умолчанию), то функция выполняет инициализацию матрицы S. <see langword="true"/> может быть использовано, если перед хешем была отдельная инициализация, например, модификатором</param>
    /// <param name="forResult">Если null, то массив для хеша будет создан. В противном случае, запись будет произведена в массив forResult</param>
    /// <param name="index">Если forResult != <see langword="null"/>, то запись будет произведена по индексу index</param>
    /// <param name="doubleHash">DoubleHash.one - обычный хеш 64-ре байта, DoubleHash.two - два раза по 64-ре байта, DoubleHash.full72 - один раз 72 байта</param>
    /// <returns>Массив с запрошенным хешем</returns>
    public unsafe byte[] getHash512(byte * message, long messageFullLen, bool doClear = true, long startIndex = 0, long countToHash = -1, bool isInitialized = false, byte[]? forResult = null, ulong index = 0, DoubleHash doubleHash = DoubleHash.one)
    {
        byte[]? result = forResult;
        if (result == null)
        {
            result = doubleHash switch
            {
                DoubleHash.one    => new byte[64],
                DoubleHash.two    => new byte[128],
                DoubleHash.full72 => new byte[r_512b],      // 72
                _ => throw new ArgumentOutOfRangeException("unknown doubleHash value"),
            };
        }

        long mLen = 0;
        if (countToHash < 0)
            mLen = messageFullLen - startIndex;
        else
            mLen = countToHash;

        if (startIndex + mLen > messageFullLen)
            throw new ArgumentOutOfRangeException("startIndex + mLen > messageFullLen");

        if (!isInitialized)
            init();

        var Msg = message;

        try
        {
            fixed (byte * R = result)
            {
                ulong * sl = Slong;
                ulong * bl = Blong;
                ulong * cl = Clong;

                byte * r   = R;
                byte * msg = Msg + startIndex;

                if (forResult != null)
                {
                    r += index;
                }


                long len = mLen;
                while (len >= 0)    // len == 0, это если последний блок был 72 байта и теперь идут padding или хешируется пустой массив
                {
                    if (len >= r_512b)
                    {
                        Keccak_Input_512(msg, r_512b, S, false);
                        msg += r_512b;
                        len -= r_512b;

                        Keccackf(a: sl, c: cl, b: bl);
                    }
                    else
                    {
                        Keccak_Input_512(msg, (byte) len, S, true);
                        msg += len;
                        len = 0;

                        Keccackf(a: sl, c: cl, b: bl);
                        break;
                    }
                }

                switch (doubleHash)
                {
                    case DoubleHash.one:
                        Keccak_Output_512(r, 64, S);
                        break;

                    case DoubleHash.two:
                        Keccak_Output_512(r, 64, S);
                        r += 64;
                        Keccackf(sl, cl, bl);
                        Keccak_Output_512(r, 64, S);
                        break;

                    case DoubleHash.full72:
                        Keccak_Output_512(r, 72, S);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("unknown doubleHash value");
                }
            }
        }
        finally
        {
            if (doClear)
            {
                Clear();
            }
        }

        return result;
    }
}
