// Виноградов С.В.

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
    /// <remarks>Рекомендуется вызвать init() после вызова конструктора.</remarks>
    public Keccak_PRNG_20201128(AllocatorForUnsafeMemoryInterface? allocator = null, int outputSize = 4096)
    {
        if (allocator != null)
            this.allocator = allocator;

        inputTo      = AllocMemory(InputSize);
        outputBuffer = AllocMemory(InputSize);
        output       = new BytesBuilderStatic(outputSize);
    }

    /// <summary>Инициализация объекта нулями</summary>
    public override void init()
    {
        base.init();
        inputTo!.Clear();
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
    /// <summary>Если <see langword="true"/>, то в массиве inputTo ожидают данные. Можно вызывать calStep</summary>
    protected          bool    inputReady   = false;
    /// <summary>Если <see langword="true"/>, то в массиве inputTo ожидают данные. Можно вызывать calStep</summary>
    public             bool    isInputReady => inputReady;

    /// <summary>Массив, представляющий результаты вывода</summary>
    public    readonly BytesBuilderStatic? output       = null;
    /// <summary>Буффер используется для вывода данных и в других целях. Осторожно, его могут использовать совершенно разные функции</summary>
    protected          Record?             outputBuffer = null;

    /// <summary>Количество элементов, которые доступны для вывода без применения криптографических операций</summary>
    public long outputCount => output!.Count;

    /// <summary>Ввести рандомизирующие байты (в том числе, открытый вектор инициализации). Не выполняет криптографических операций</summary>
    /// <param name="bytesToInput">Рандомизирующие байты. Копируются. bytesToInput должны быть очищены вручную</param>
    public void InputBytes(byte[] bytesToInput)
    {
        if (bytesToInput == null)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputBytes: bytesToInput == null");

        INPUT!.add(BytesBuilderForPointers.CloneBytes(bytesToInput, allocator));
        InputBytesImmediately(notException: true);
    }

    /// <summary>Ввести рандомизирующие байты (в том числе, открытый вектор инициализации). Не выполняет криптографических операций</summary>
    /// <param name="bytesToInput">Рандомизирующие байты. Копируются. bytesToInput должны быть очищены вручную</param>
    /// <param name="len">Длина рандомизирующей последовательности</param>
    public void InputBytes(byte * bytesToInput, nint len)
    {
        if (bytesToInput == null)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputBytes: bytesToInput == null");

        INPUT!.add(BytesBuilderForPointers.CloneBytes(bytesToInput, 0, len, allocator));
        InputBytesImmediately(notException: true);
    }

    /// <summary>Ввести рандомищирующие байты. Не выполняет криптографических операций.</summary>
    /// <param name="data">Вводимые байты. Будут очищены автоматически. Не должны использоваться ещё где-либо</param>
    public void InputBytesWithoutClone(Record data)
    {
        if (data.array == null)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputBytes: data.array == null");

        INPUT!.add(data);
        InputBytesImmediately(notException: true);
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

        if (inputReady)
            throw new ArgumentNullException("Keccak_PRNG_20201128.InputKeyAndStep: inputReady == true");

        INPUT.addWithCopy(key, key_length, allocator);
        InputBytesImmediately(ForOverwrite: true); // Это нужно, чтобы даже маленький ключ точно был записан
        do
        {
            calcStep(Overwrite: false);
            InputBytesImmediately(ForOverwrite: true);
        }
        while (inputReady);
// TODO: Почему здесь ключ перед ОВИ???
        // Завершаем ввод ключа конструкцией Overwrite, которая даёт некую необратимость состояния в отношении ключа
        if (OIV != null)
        {
            if (OIV_length <= 0)
                throw new ArgumentOutOfRangeException("Keccak_PRNG_20201128.InputKeyAndStep: OIV_length <= 0");

            INPUT.addWithCopy(OIV, OIV_length, allocator);
            InputBytesImmediately(true);
            calcStep(Overwrite: true);          // xor, к тому же, даёт больше ПЭМИН, так что просто Overwrite, хотя особо смысла в этом нет, т.к. xor в других операциях тоже идёт (но не с ключевой информацией)
        }
        else
        {
            inputTo!.Clear();
            inputReady = true;
            calcStep(Overwrite: true);
        }

        if (INPUT.countOfBlocks > 0)
        {
            INPUT.Clear();
            Clear(true);
            throw new ArgumentException("key must be a multiple of 64 bytes", nameof(key));
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

        inputReady = false;

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
    /// <param name="ForOverwrite">Если <see langword="true"/>, то записывает данные, даже если их меньше, чем блок, выравнивая вход нулями до InputSize. Эта реализация нигде не имеет paddings, поэтому осторожнее с этим, это может вызвать неоднозначность при вводе (введены нули в конце или короткое значение?)</param>
    /// <param name="notException">Если false, то при установленном флаге inputReady будет выдано исключение</param>
    /// <remarks>Если inputReady установлен, то функция выдаст исключение. Установить notException, если исключение не нужно</remarks>
    // При INPUT.Count == 0 не должен изменять inputReady
    public void InputBytesImmediately(bool ForOverwrite = false, bool notException = false)
    {
        if (inputTo == null)
            throw new Exception("Keccak_PRNG_20201128.InputBytesImmediately: object is destroyed and can not work");

        if (!inputReady)
        {
            if (INPUT!.Count >= InputSize)
            {
                // TODO: сделать тесты на верность getBytesAndRemoveIt и, по возможности, на его использование
                INPUT.getBytesAndRemoveIt(inputTo);
                inputReady = true;
            }
            else
            if (ForOverwrite && INPUT.Count > 0)
            {
                inputTo.Clear();
                INPUT.getBytesAndRemoveIt(inputTo);
                inputReady = true;
            }
        }
        else
        if (!notException)
            throw new Exception("Keccak_PRNG_20201128.InputBytesImmediately: inputReady = true");
    }

    /// <summary>Выполняет шаг keccak и сохраняет полученный результат в output</summary>
    public void calcStepAndSaveBytes(bool inputReadyCheck = true)
    {
        calcStep(inputReadyCheck: inputReadyCheck, SaveBytes: true);
    }

    /// <summary>Расчитывает шаг губки keccak. Если есть InputSize (64) байта для ввода (точнее, inputReady == true), то вводит первые 64-ре байта</summary>
    /// <param name="inputReadyCheck">Параметр должен совпадать с флагом inputReady. Этот параметр введён для дополнительной проверки, что функция вызывается в правильном контексте</param>
    /// <param name="SaveBytes">Если <see langword="null"/>, выход не сохраняется</param>
    /// <param name="Overwrite">Если <see langword="true"/>, то вместо xor применяет перезапись внешней части состояния на вводе данных (конструкция Overwrite)</param>
    /// <remarks>Перед calcStep должен быть установлен inputReady, если нужна обработка введённой информации. Функции Input* устанавливают этот флаг автоматически</remarks>
    public void calcStep(bool inputReadyCheck = true, bool SaveBytes = false, bool Overwrite = false)
    {
        if (inputReady != inputReadyCheck)
            throw new ArgumentException("Keccak_PRNG_20201128.calcStep: inputReady != inputReadyCheck");

        if (State == null)
            throw new Exception("Keccak_PRNG_20201128.calcStep: State == null");
// TODO:
        while (inputReady)      // Здесь нужен while или if ?
        {
            byte * input = inputTo!.array;

            if (Overwrite)
                Keccak_InputOverwrite64_512(message: input, len: InputSize, S: S);
            else
                Keccak_Input64_512(message: input, len: InputSize, S: S);

            inputTo.Clear();
            inputReady = false;
            InputBytesImmediately();
        }

        Keccackf(a: Slong, c: Clong, b: Blong);

        if (SaveBytes)
        {
            Keccak_Output_512(output: outputBuffer!.array, len: InputSize, S: S);

            output     !.add(outputBuffer.array, InputSize);
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
            calcStep(inputReadyCheck: inputReady);
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
            calcStepAndSaveBytes(inputReadyCheck: inputReady);
        }

        var ba = stackalloc byte[1];
        var b  = new Record() { array = ba, len = 1 };
        // using var b = output.getBytesAndRemoveIt(  AllocMemoryForSaveBytes(1)  );

        var result = ba[0];
        ba[0]      = 0;

        return result;
    }

    /// <summary>Выдаёт случайное криптостойкое число от 0 до cutoff включительно. Это вспомогательная функция для основной функции генерации случайных чисел</summary>
    /// <param name="cutoff">Максимальное число (включительно) для генерации. cutoff должен быть близок к ulong.MaxValue или к 0x8000_0000__0000_0000U, иначе неопределённая отсрочка будет очень долгой</param>
    /// <param name="arrayAt8Length">Вспомогательная выделенная память в размере не менее 8-ми байтов (можно не инициализировать). Очищается после использования внутри функции, но не освобождается. Может быть null</param>
    /// <returns>Случайное число в диапазоне [0; cutoff]</returns>
    public ulong getUnsignedInteger(ulong cutoff = ulong.MaxValue, Record? arrayAt8Length = null)
    {
        var b = arrayAt8Length ?? AllocMemoryForSaveBytes(8);
        if (b.len < 8)
            throw new ArgumentOutOfRangeException("Keccak_PRNG_20201128.getUnsignedInteger: arrayAt8Length.len < 8");

        try
        {
            while (true)
            {
                if (this.output!.Count < 8)
                {
                    InputBytesImmediately(notException: true);
                    calcStepAndSaveBytes(inputReadyCheck: inputReady);
                }

                output.getBytesAndRemoveIt(result: b, 8);

                BytesBuilderForPointers.BytesToULong(out ulong result, b.array, start: 0, length: b.len);

                if (cutoff < 0x8000_0000__0000_0000U)
                    result &= 0x7FFF_FFFF__FFFF_FFFFU;  // Сбрасываем старший бит, т.к. он не нужен никогда

                if (result <= cutoff)
                    return result;
            }
        }
        finally
        {
            if (arrayAt8Length == null)
                b.Dispose();
            else
                b.Clear();
        }
    }

    /// <summary>Получает случайное значение в диапазоне, указанном в функции getCutoffForUnsignedInteger</summary>
    /// <param name="min">Минимальное значение</param>
    /// <param name="cutoff">Результат функции getCutoffForUnsignedInteger</param>
    /// <param name="range">Результат функции getCutoffForUnsignedInteger</param>
    /// <param name="arrayAt8Length">Вспомогательный массив, длиной не менее 8-ми байтов (можно не инициализировать). После использования очищается, но не освобождается. Может быть null</param>
    /// <returns>Случайное число в указанном диапазоне</returns>
    public ulong getUnsignedInteger(ulong min, ulong cutoff, ulong range, Record? arrayAt8Length = null)
    {
        var random = getUnsignedInteger(cutoff, arrayAt8Length) % range;

        return random + min;
    }

    /// <summary>Вычисляет параметры для применения в getUnsignedInteger</summary>
    /// <param name="min">Минимальное значение для генерации</param>
    /// <param name="max">Максимальное значнеие для генерации (включительно)</param>
    /// <param name="cutoff">Параметр cutoff для передачи getUnsignedInteger</param>
    /// <param name="range">Диапазон для ввода в функцию getUnsignedInteger</param>
    // TODO: хорошо протестировать
    public static void getCutoffForUnsignedInteger(ulong min, ulong max, out ulong cutoff, out ulong range)
    {
        range = max - min + 1;

        if (range >= 0x8000_0000__0000_0000U)
        {
            cutoff = range;
            return;
        }

        var mod = (0x8000_0000__0000_0000U) % range;

        if (mod == 0)
        {
            cutoff = 0x8000_0000__0000_0000U - 1;
            return;
        }

        var result = 0x8000_0000__0000_0000U - mod - 1;

        if ((result + 1) % range != 0)
            throw new Exception("Fatal error: Keccak_PRNG_20201128.getCutoffForUnsignedInteger");

        cutoff = result;
    }

    /// <summary>Осуществляет перестановки таблицы 2-хбайтовых целых чисел</summary>
    /// <param name="table">Исходная таблица для перестановок длиной не более int.MaxValue</param>
    public void doRandomPermutationForUShorts(ushort[] table)
    {
        // Иначе всё равно будет слишком долго
        if (table.LongLength > int.MaxValue)
            throw new ArgumentException("doRandomCubicPermutationForUShorts: table is very long");
        if (table.Length <= 3)
            throw new ArgumentException("doRandomCubicPermutationForUShorts: table is very short");

        ushort a;
        ulong  index;
        fixed (ushort * T = table)
        {
            var len = (ulong) table.LongLength;

            // Алгоритм тасования Дурштенфельда
            // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
            for (ulong i = 0; i < len - 1; i++)
            {
                getCutoffForUnsignedInteger(0, (ulong) len - i - 1, out ulong cutoff, out ulong range);
                index = getUnsignedInteger (0, cutoff, range, outputBuffer!) + i;

                a        = T[i];
                T[i]     = T[index];
                T[index] = a;
            }
        }
    }
}
