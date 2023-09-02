// TODO: tests
// © Виноградов С.В. (1984, Мытищи)
// ::test:bGx3blJD6yexv1d8VgC7:

using cryptoprime;
using static cryptoprime.KeccakPrime;
using static cryptoprime.BytesBuilderForPointers;

// В эту версию внесены правки в 2023-ем году, 25 мая и далее

namespace maincrypto.keccak;

// Ссылка на документацию по состояниям .\Documentation\Keccak_PRNG_20201128.md
// Там же см. рекомендуемый порядок использования функций ("Рекомендуемый порядок вызовов
// Пример использования в \VinKekFish\vinkekfish\VinKekFish\VinKekFish-20210419\VinKekFish_k1_base_20210419.cs
// в функции GenStandardPermutationTables (вызовы doRandomPermutationForUShorts)
/// <summary>Криптостойкий ГПСЧ</summary>
public unsafe class Keccak_PRNG_20201128 : Keccak_base_20200918
{                                                                                                       /// <summary>Главный аллокатор: используется для однократного выделения памяти под вспомогательные буферы inputTo и outputBuffer</summary>
    public readonly AllocatorForUnsafeMemoryInterface allocator             = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();      /// <summary>Аллокатор для использования в многократных операциях по выделению памяти при сохранении данных или их преобразовании</summary>
    public          AllocatorForUnsafeMemoryInterface allocatorForSaveBytes = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory(); // new BytesBuilderForPointers.Fixed_AllocatorForUnsafeMemory();
    // Fixed работает раза в 3 медленнее почему-то

    /// <summary>Создаёт пустой объект</summary>
    /// <param name="allocator">Способ выделения памяти внутри объекта (см. поле allocator), кроме выделения памяти для вывода. Может быть null.</param>
    /// <param name="outputSize">Размер буффера output для приёма выхода. Если outputSize недостаточен, получить данные за один раз будет невозможно</param>
    /// <exception cref="OutOfMemoryException"></exception>
    /// <exception cref="ArgumentOutOfRangeException">Если outputSize &lt; InputSize</exception>
    /// <remarks>init() вызывается автоматически всегда</remarks>
    public Keccak_PRNG_20201128(AllocatorForUnsafeMemoryInterface? allocator = null, nint outputSize = 4096): base(noInit: true)
    {
        if (outputSize < InputSize)
        {
            this.Dispose();
            throw new ArgumentOutOfRangeException("outputSize");
        }

        if (allocator != null)
            this.allocator = allocator;

        inputTo      = AllocMemory(InputSize);
        outputBuffer = AllocMemory(InputSize);
        output       = new BytesBuilderStatic(outputSize);

        init();
    }

    /// <summary>Инициализация объекта нулями</summary>
    public override void init()
    {
        base.init();
        inputTo!.Clear();
        outputBuffer!.Clear();
    }
                                                                    /// <summary>Выделение памяти с помощью allocator</summary><param name="len">Размер выделяемого участка памяти</param><returns>Record, инкапсулирующий выделенный участок памяти</returns>
    public Record AllocMemory(nint len)
    {
        return allocator.AllocMemory(len);
    }
                                                                    /// <summary>Выделение памяти с помощью AllocMemoryForSaveBytes</summary><param name="len">Размер выделяемого участка памяти</param><returns>Record, инкапсулирующий выделенный участок памяти</returns>
    public Record AllocMemoryForSaveBytes(nint len)
    {
        return allocatorForSaveBytes.AllocMemory(len);
    }

    /// <summary>Клонирует внутреннее состояние объекта и аллокаторы. Вход и выход не копируются</summary><returns></returns>
    public override Keccak_abstract Clone()
    {
        var result = new Keccak_PRNG_20201128(allocator: allocator);

        result.allocatorForSaveBytes = this.allocatorForSaveBytes;

        // Очищаем C и B, чтобы не копировать какие-то значения, которые не стоит копировать, да и хранить тоже
        clearOnly_C_and_B();

        // Копировать всё состояние не обязательно. Но здесь, для надёжности, копируется всё (в т.ч. ранее очищенные нули)
        BytesBuilder.CopyTo(StateLen, StateLen, State, result.State);

        return result;
    }


    /// <summary>Сюда можно добавлять байты для ввода</summary>
    protected          BytesBuilderForPointers? INPUT = new BytesBuilderForPointers(); // Не забыт ли вызов InputBytesImmediately при добавлении сюда?
    /// <summary>Размер блока вводимой (и выводимой) информации</summary>
    public    const    int InputSize = 64;

    /// <summary>Это массив для немедленного введения в Sponge на следующем шаге</summary>
    protected          Record? inputTo      = null;
    /// <summary>В массиве inputTo ожидают данные в количестве inputReady. Можно вызывать calStep</summary>
    protected          byte    inputReady   = 0;
                                                                    /// <summary>Если <see langword="true"/>, то в массиве inputTo ожидают данные. Можно вызывать calStep</summary>
    public             bool    isInputReady => inputReady > 0;      /// <summary>Количество данных, которые доступны в массиве inputTo для непосредственного ввода в губку. Используются calStep автоматически для ввода данных перед криптографическим преобразованием</summary>
    public             byte    InputReady   => inputReady;

    /// <summary>Массив, представляющий результаты вывода</summary>
    public    readonly BytesBuilderStatic? output       = null;
    /// <summary>Буффер используется для вывода данных и в других целях. Осторожно, его могут использовать совершенно разные функции</summary>
    protected          Record?             outputBuffer = null;

    /// <summary>Количество элементов, которые доступны для вывода без применения криптографических операций</summary>
    public long outputCount => output!.Count;

    /// <summary>Ввести рандомизирующие байты (в том числе, открытый вектор инициализации). Не выполняет криптографических операций</summary>
    /// <param name="bytesToInput">Рандомизирующие байты. Копируются. bytesToInput должны быть очищены вручную, если больше не нужны</param>
    /// <remarks>После ввода данных необходимо вручную вызвать InputBytesImmediately (один раз) перед calcStep</remarks>
    public void InputBytes(byte[] bytesToInput)
    {
        if (bytesToInput == null)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputBytes: bytesToInput == null");

        INPUT!.add(BytesBuilderForPointers.CloneBytes(bytesToInput, allocator));
        // InputBytesImmediately(notException: true);
    }

    /// <summary>Ввести рандомизирующие байты (в том числе, открытый вектор инициализации). Не выполняет криптографических операций</summary>
    /// <param name="bytesToInput">Рандомизирующие байты. Копируются. bytesToInput должны быть очищены вручную, если больше не нужны</param>
    /// <param name="len">Длина рандомизирующей последовательности</param>
    /// <remarks>После ввода данных необходимо вручную вызвать InputBytesImmediately (один раз) перед calcStep</remarks>
    public void InputBytes(byte * bytesToInput, nint len)
    {
        if (bytesToInput == null)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputBytes: bytesToInput == null");

        INPUT!.add(BytesBuilderForPointers.CloneBytes(bytesToInput, 0, len, allocator));
        // InputBytesImmediately(notException: true);
    }

    /// <summary>Ввести рандомищирующие байты. Не выполняет криптографических операций.</summary>
    /// <param name="data">Вводимые байты. Будут очищены автоматически. Не должны использоваться ещё где-либо</param>
    /// <remarks>После ввода данных необходимо вручную вызвать InputBytesImmediately (один раз) перед calcStep</remarks>
    public void InputBytesWithoutClone(Record data)
    {
        if (data.array == null)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputBytes: data.array == null");

        INPUT!.add(data);
        // InputBytesImmediately(notException: true);
    }

    /// <summary>Ввести секретный ключ и ОВИ (вместе с криптографическим преобразованием)</summary>
    /// <param name="key">Ключ. Должен быть очищен вручную (можно сразу после вызова функции)</param>
    /// <param name="key_length">Длина ключа</param>
    /// <param name="OIV">Открытый вектор инициализации, не более InputSize (не более 64 байтов). Может быть null. Должен быть очищен вручную (можно сразу после вызова функции)</param>
    /// <param name="OIV_length">Длина ОВИ</param>
    public void InputKeyAndStep(byte * key, nint key_length, byte * OIV, nint OIV_length)
    {
        if (INPUT!.countOfBlocks > 0)
            throw new ArgumentException("Keccak_PRNG_20201128.InputKeyAndStep:key must be input before the generation or input an initialization vector (or see InputKeyAndStep code)");

        if (OIV_length > InputSize)
            throw new ArgumentException("Keccak_PRNG_20201128.InputKeyAndStep: OIV_length > InputSize", nameof(OIV));

        if (key == null || key_length <= 0)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputKeyAndStep: key == null || key_length <= 0");

        if (inputReady > 0)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputKeyAndStep: inputReady > 0");

        if (OIV != null)
        {
            if (OIV_length <= 0)
                throw new ArgumentOutOfRangeException("Keccak_PRNG_20201128.InputKeyAndStep: OIV_length <= 0");

            INPUT.addWithCopy(OIV, OIV_length, allocator);
            InputBytesImmediately();
            if (inputReady <= 0)
                throw new ArgumentNullException("Keccak_PRNG_20201128.InputKeyAndStep: inputReady != true with OIV != null after first input");

            // Вводим столько байтов, сколько есть
            while (inputReady > 0)
                calcStep(true, Overwrite: false, regime: 1);
        }

        if (INPUT.Count > 0)
            throw new Exception("Keccak_PRNG_20201128.InputKeyAndStep: fatal algorithmic error. INPUT.Count > 0 || inputReady > 0 after OIV input");

        // Завершаем ввод открытого вектора инициализации
        calcStep(false, Overwrite: false, inputAlways: true, regime: 2);

        // Вводим ключ
        INPUT.addWithCopy(key, key_length, allocator);
        InputBytesImmediately();
        while (inputReady > 0)
            calcStep(Overwrite: false, regime: 3);

        // Завершаем ввод ключа конструкцией Overwrite, которая даёт некую необратимость состояния в отношении ключа
        calcStep(false, Overwrite: true, inputAlways: true, regime: 4);

        if (INPUT.countOfBlocks > 0 || inputReady > 0)
        {
            INPUT.Clear();
            Clear(true);
            throw new ArgumentException("Keccak_PRNG_20201128.InputKeyAndStep: fatal algorithmic error. INPUT.countOfBlocks > 0", nameof(key));
        }
    }

    /// <summary>Очистка объекта (перезабивает данные нулями)</summary>
    /// <param name="GcCollect"></param>
    public override void Clear(bool GcCollect = false)
    {
        inputTo     ?.Clear();
        INPUT       ?.Clear();
        output      ?.Clear();
        outputBuffer?.Clear();

        inputReady = 0;

        base.Clear(GcCollect);
    }

    /// <summary>Уничтожение объекта: очищает объект и освобождает все связанные с ним ресурсы</summary>
    /// <param name="disposing">True из любого места программы, кроме деструктора</param>
    public override void Dispose(bool disposing)
    {
        var throwException = !disposing && inputTo != null;

        base.Dispose(disposing);        // Clear вызывается здесь

        try
        {
            inputTo     ?.Dispose();
            output      ?.Dispose();
            outputBuffer?.Dispose();
        }
        finally
        {
            inputTo      = null;
            INPUT        = null;
            outputBuffer = null;
        }

        if (throwException)
        {
            throw new Exception("Keccak_PRNG_20201128: Object must be manually disposed");
        }
    }

    /// <summary>Переносит байты из очереди ожидания в массив байтов для непосредственного ввода в криптографическое состояние. Не выполняет криптографических операций</summary>
    /// <param name="notException">Если false, то при установленном флаге inputReady будет выдано исключение. Если true, то inputReady != 0 - функция ничего не делает</param>
    /// <remarks>Если inputReady установлен, то функция выдаст исключение. Установить notException, если исключение не нужно</remarks>
    /// <remarks>В случае, если ввод успешен, inputReady устанавливается в true</remarks>
    /// <remarks>Если INPUT.Count == 0 (очередь входа пуста), то функция ничего не делает (при inputReady !=0 - выдаёт исключение)</remarks>
    // При INPUT.Count == 0 не должен изменять inputReady
    public void InputBytesImmediately(bool notException = false)
    {
        if (inputTo == null)
            throw new Exception("Keccak_PRNG_20201128.InputBytesImmediately: object is destroyed and can not work");

        if (inputReady == 0)
        {
            if (INPUT!.Count >= 0)
            {
                var a = INPUT!.Count;
                if (INPUT!.Count < inputTo.len)
                {
                    inputTo.Clear();
                }
                else
                    a = inputTo.len;

                INPUT.getBytesAndRemoveIt(inputTo);
                inputReady = (byte) a;

                if (a > InputSize)
                    throw new Exception("Keccak_PRNG_20201128.InputBytesImmediately: fatal algorithmic error. inputReady > InputSize");
            }
        }
        else
        if (!notException)
            throw new Exception("Keccak_PRNG_20201128.InputBytesImmediately: inputReady != 0");
    }

    /// <summary>Выполняет шаг keccak и сохраняет полученный результат в output</summary>
    public void calcStepAndSaveBytes(bool inputReadyCheck = true, byte SaveBytes = InputSize)
    {
        calcStep(inputReadyCheck: inputReadyCheck, SaveBytes: SaveBytes);
    }

    /// <summary>Расчитывает один шаг губки keccak. Если есть InputSize (64) байта для ввода (точнее, inputReady == true), то вводит первые 64-ре байта</summary>
    /// <param name="inputReadyCheck">Параметр должен совпадать с флагом isInputReady. Этот параметр введён для дополнительной проверки, что функция вызывается в правильном контексте</param>
    /// <param name="SaveBytes">Если 0, выход не сохраняется, в противном случае сохраняется SaveBytes байтов</param>
    /// <param name="Overwrite">Если <see langword="true"/>, то вместо xor применяет перезапись внешней части состояния на вводе данных (конструкция Overwrite)</param>
    /// <param name="inputAlways">Если <see langword="true"/>, то ввод данных будет производится даже при inputReady == 0</param>
    /// <remarks>Перед calcStep должен быть установлен inputReady, если нужна обработка введённой информации. Функции Input* устанавливают этот флаг автоматически</remarks>
    /// <remarks>Если inputReady, то после поглощения inputTo, вызывается InputBytesImmediately, чтобы подготовить новый inputTo, если очередь ожидания на входе заполнена</remarks>
    /// <remarks>while (inputReady > 0) calcStep; позволяет рассчитывать дуплекс с заранее введёнными данными в массив INPUT</remarks>
    public void calcStep(bool inputReadyCheck = true, byte SaveBytes = 0, bool Overwrite = false, byte regime = 0, bool inputAlways = false)
    {
        if (isInputReady != inputReadyCheck)
            throw new ArgumentException("Keccak_PRNG_20201128.calcStep: isInputReady != inputReadyCheck");

        if (State == null)
            throw new Exception("Keccak_PRNG_20201128.calcStep: State == null");

        // Осуществляем ввод данных, если они есть или ввод идёт принудительно вне зависимости от того, есть данные или нет (например, если вводится иной режим)
        if (inputReady > 0 || inputAlways)
        {
            byte * input = inputTo!.array;

            if (Overwrite)
                Keccak_InputOverwrite64_512(message: input, len: inputReady, S: S, regime: regime);
            else
                Keccak_Input64_512(message: input, len: inputReady, S: S, regime: regime);

            inputTo.Clear();
            inputReady = 0;
            InputBytesImmediately();
        }

        //Keccackf(a: Slong, c: Clong, b: Blong);
        base.CalcStep();


        if (SaveBytes != 0)
        {
            if (SaveBytes > InputSize || SaveBytes < 0)
                throw new ArgumentOutOfRangeException("Keccak_PRNG_20201128.calcStep: SaveBytes > InputSize || SaveBytes < 0");

            Keccak_Output_512(output: outputBuffer!.array, len: SaveBytes, S: S);

            output     !.add(outputBuffer.array, SaveBytes);
            outputBuffer.Clear();
        }
    }

    /// <summary>Выдаёт случайные криптостойкие значения байтов. Выгодно использовать при большом количестве байтов (64 и более). Выполняет криптографические операции, если байтов не хватает. Автоматически берёт данные из INPUT, если они уже введены</summary>
    /// <param name="outputRecord">Массив, в который записывается результат</param>
    /// <param name="len">Количество байтов, которые необходимо записать. Используйте outputCount, чтобы узнать, сколько байтов уже готово к выводу (без выполнения криптографических операций)</param>
    public void getBytes(Record outputRecord, nint len)
    {
        var output = outputRecord.array;
        if (outputRecord.len < len)
            throw new ArgumentException("Keccak_PRNG_20201128.getBytes: outputRecord.len < len");

        // Проверяем уже готовые байты
        if (this.output!.Count > 0)
        {
            var readyLen = this.output.Count;
            if (readyLen > len)
            {
                readyLen = len;
            }

            using var b = this.output.getBytesAndRemoveIt(  AllocMemoryForSaveBytes(readyLen)  );

            BytesBuilder.CopyTo(b.len, readyLen, b.array, output);

            output += readyLen;
            len    -= readyLen;

            if (len == 0)
                return;

            if (len < 0)
                throw new Exception("Keccak_PRNG_20201128.getBytes: len < 0 - fatal algorithmic error");
        }

        // Если готовых байтов нет, то начинаем вычислять те, что ещё не готовы
        // И сразу же их записываем
        while (len > 0)
        {
            InputBytesImmediately(notException: true);
            calcStep(inputReadyCheck: isInputReady);
            Keccak_Output_512(output: output, len: (byte) (len >= 64 ? 64 : len), S: S);
            len    -= 64;
            output += 64;
        }
    }
                                                    /// <summary>Получает случайный байт</summary><returns>Случайный криптостойкий байт</returns>
    public byte getByte()
    {
        if (this.output!.Count <= 0)
        {
            InputBytesImmediately(notException: true);
            calcStepAndSaveBytes(inputReadyCheck: isInputReady);
        }

        var ba = stackalloc byte[1];
        var b  = new Record() { array = ba, len = 1 };
        // using var b = output.getBytesAndRemoveIt(  AllocMemoryForSaveBytes(1)  );
        output.getBytesAndRemoveIt(b);

        var result = ba[0];
        ba[0]      = 0;

        return result;
    }

    /// <summary>Выдаёт случайное криптостойкое число от 0 до cutoff включительно. Это вспомогательная функция для основной функции генерации случайных чисел</summary>
    /// <param name="cutoff">Максимальное число (включительно) для генерации. cutoff должен быть близок к ulong.MaxValue или к 0x8000_0000__0000_0000U, иначе неопределённая отсрочка будет очень долгой</param>
    /// <returns>Случайное число в диапазоне [0; cutoff]</returns>
    public ulong getUnsignedInteger(Cutoff cutoff)
    {
        var b = stackalloc byte[cutoff.cbytes];
        try
        {
            while (true)
            {
                if (this.output!.Count < cutoff.cbytes)
                {
                    InputBytesImmediately(notException: true);
                    calcStepAndSaveBytes(inputReadyCheck: isInputReady);
                }

                output.getBytesAndRemoveIt(result: b, cutoff.cbytes);

                ulong result = 0;
                byte  bn     = 0;
                while (cutoff.cbytes > 0)
                {
                    result <<= 8;
                    result |= b[bn];
                    bn++;
                }
                result &= cutoff.mask;

                if (result <= cutoff.cutoff)
                    return result;
            }
        }
        finally
        {
            BytesBuilder.ToNull(cutoff.cbytes, b);
            b = null;
        }
    }

    /// <summary>Получает случайное значение в диапазоне, указанном в функции getCutoffForUnsignedInteger</summary>
    /// <param name="min">Минимальное значение</param>
    /// <param name="cutoff">Результат функции getCutoffForUnsignedInteger</param>
    /// <param name="range">Результат функции getCutoffForUnsignedInteger</param>
    /// <returns>Случайное число в указанном диапазоне</returns>
    public ulong getUnsignedInteger(ulong min, Cutoff cutoff)
    {
        var random = getUnsignedInteger(cutoff);

        return random + min;
    }

    /// <summary>Представляет информацию о том, какое именно случайное число надо сгенерировать</summary>
    public class Cutoff
    {                                       /// <summary>Генерация чисел от 0 до cutoff включительно</summary>
        public ulong cutoff;                /// <summary>Маска, накладываемая на сгенерированное число, чтобы оно не выходило за рамки cutoff</summary>
        public ulong mask;                  /// <summary>Количество битов в числе для генерации</summary>
        public byte  cbits;                 /// <summary>Количество байтов в числе для генерации</summary>
        public byte  cbytes;

        public Cutoff()
        {
            cutoff = ulong.MaxValue;
            mask   = ulong.MaxValue;

            cbits  = 64;
            cbytes = 8;
        }

        /// <summary>Создаёт описатель случайного числа для генерации</summary>
        /// <param name="range">Число будет генерироваться в диапазоне [0; range] (обе границы включены в диапазон)</param>
        public Cutoff(ulong range): this()
        {
            cutoff = range;
            if (range >= 0x8000_0000__0000_0000U)
            {
                return;
            }
            if (range == 0)
                throw new ArgumentOutOfRangeException("range", "Keccak_PRNG_20201128.Cutoff.Cutoff: range == 0");

            // cutoff <- [1; 0x7FFF_FFFF__FFFF_FFFFU]

            // Переменные будут выкидываться, если они были сгенерированны выше, чем граница обрезания cutoff
            // Оптимизируем обрезание переменной: вычисляем старший бит, который всегда должен быть обрезан
            cbits  = (byte)(    64 - UInt64.LeadingZeroCount(cutoff)    ); // cbits <- [1; 63]
            cbytes = (byte)(    cbits >> 3    );
            if ((cbits & 7) > 0)
                cbytes++;

            ulong cf = 1U << cbits;     // cf всегда больше нуля (и даже больше 1)

            if (cf <= range)
                throw new Exception("Keccak_PRNG_20201128.Cutoff.Cutoff: fatal algorithmic error: cf <= range");

            mask = cf - 1;
        }
    }

    /// <summary>Вычисляет параметры для применения в getUnsignedInteger</summary>
    /// <param name="min">Минимальное значение для генерации</param>
    /// <param name="max">Максимальное значнеие для генерации (включительно)</param>
    /// <param name="cutoff">Параметр cutoff для передачи getUnsignedInteger</param>
    /// <param name="range">Диапазон для ввода в функцию getUnsignedInteger</param>
    // TODO: хорошо протестировать
    public static Cutoff getCutoffForUnsignedInteger(ulong min, ulong max)
    {
        var range = max - min;

        return new Cutoff(range);
    }

    /// <summary>Осуществляет перестановки таблицы 2-хбайтовых целых чисел</summary>
    /// <param name="table">Исходная таблица для перестановок длиной не более int.MaxValue и не менее чем 4 числа</param>
    public void doRandomPermutationForUShorts(ushort[] table)
    {
        // Иначе всё равно будет слишком долго
        if (table.LongLength > int.MaxValue)
            throw new ArgumentException("doRandomCubicPermutationForUShorts: table is very long");
        if (table.Length <= 3)
            throw new ArgumentException("doRandomCubicPermutationForUShorts: table is very short");

        fixed (ushort * T = table)
        {
            doRandomPermutationForUShorts((ulong) table.LongLength, T);
        }
    }

    public void doRandomPermutationForUShorts(ulong len, ushort* T)
    {
        ushort a;
        ulong  index;

        // Алгоритм тасования Дурштенфельда
        // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
        for (ulong i = 0; i < len - 1; i++)
        {
            var cutoff = getCutoffForUnsignedInteger(0, (ulong)len - i - 1);
            index = getUnsignedInteger(0, cutoff) + i;

            a        = T[i];
            T[i]     = T[index];
            T[index] = a;
        }

        a = 0;
    }
}
