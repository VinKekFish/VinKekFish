// TODO: tests
namespace vinkekfish;

using cryptoprime;
using static cryptoprime.BytesBuilderForPointers;

using static CascadeSponge_1t_20230905.InputRegime;
using System.Data;

// code::docs:rQN6ZzeeepyOpOnTPKAT:
// ::cp:alg:a7L6XjXsuwWGVxwJSN1x.step:20230930

// Описание шага шифрования, инициализации ключей ThreeFish через губку, а также инициализации губки ключом шифрования и синхропосылкой (открытым вектором инициализации)

/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_1t_20230905: IDisposable
{
    public delegate void Keccak_Input_Delegate(byte * message, byte len, byte * S, byte regime, byte logicalLen);

    protected volatile Keccak_Input_Delegate input = KeccakPrime.Keccak_Input64_512;

    public class StepProgress
    {                                                     /// <summary>Сколько шагов нужно выполнить всего</summary>
        public volatile nint allSteps       = 0;          /// <summary>Сколько шагов уже закончено</summary>
        public volatile nint processedSteps = 0;
    }

    /// <summary>Осуществить шаг алгоритма (полный шаг каскадной губки - все губки делают по одному шагу)</summary>
    /// <param name="countOfSteps">Количество шагов алгоритма. 0 - значение будет рассчитано исходя из dataLen (не менее одного шага)</param>
    /// <param name="ArmoringSteps">Количество усиливающих шагов алгоритма, которые будут проведены вхолостую после каждого шага поглощения. Не ноль для усиленных режимов, например, инициализации или генерации ключа. См. countStepsForKeyGeneration и countStepsForHardening.</param>
    /// <param name="data">Данные для ввода, не более maxDataLen на один шаг</param>
    /// <param name="dataLen">Количество данных для ввода</param>
    /// <param name="regime">Режим ввода (логический параметр, декларируемый схемой шифрования; может быть любым однобайтовым значением)</param>
    /// <param name="inputRegime">Режим ввода данных в губку: либо обычный xor, либо режим overwrite для обеспечения необратимости шифрования и защиты ключа перед его использованием</param>
    /// <param name="progress">Структура, получающая прогресс расчёта</param>
    /// <param name="StepsForAbsorption">Количество шагов, которые губка делает при выпитывании данных. 0 - количество будет рассчитано исходя из параметра StepTypeForAbsorption: если StepTypeForAbsorption == true, то 1, иначе tall. Если значение StepsForAbsorption установлено, то StepTypeForAbsorption игнорируется.</param>
    /// <returns>Количество данных, введённых в губку</returns>
    public virtual nint Step(nint countOfSteps = 0, nint ArmoringSteps = 0, byte * data = null, nint dataLen = -1, byte regime = 0, InputRegime inputRegime = xor, StepProgress? progress = null, nint StepsForAbsorption = 0)
    {
        ObjectDisposedCheck("CascadeSponge_1t_20230905.step");

        haveOutput = false;         // На всякий случай устанавливаем значение в false. Если где что упадёт, это не даст дальше работать в некоторых функциях
        if (!isThreeFishInitialized)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: !isThreeFishInitialized. See ThreeFish key initialization for reverse connection");

        if (data != null && dataLen < 0)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: data != null && dataLen < 0");

        if (countOfSteps < 1)
        {
            if (dataLen <= 0 || data == null)
            {
                countOfSteps = 1;
            }
            else
            {
                countOfSteps = dataLen / maxDataLen;
                if (dataLen % maxDataLen > 0)
                    countOfSteps++;
            }
        }

        if (StepsForAbsorption <= 0)
        {
            StepsForAbsorption = GetCountOfStepsForAbsorption();
        }

        if (countOfSteps <= 0)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: fatal algorithmic error (send message to VinKekFish developer): countOfSteps <= 0");
        if (dataLen > maxDataLen*countOfSteps)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: dataLen > maxDataLen*countOfSteps");

        if (data == null)
        {
            if (dataLen > 0)
                throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: dataLen != 0 && data == null");

            dataLen = 0;
        }

        if (progress is not null)
        {
            progress.allSteps   = countOfSteps;
            progress.processedSteps = 0;
        }

        nint curDataLen, dataUsedLen = 0;
        for (int stepNum = 0; stepNum < countOfSteps; stepNum++)
        {
            curDataLen = dataLen;
            if (curDataLen > maxDataLen)
                curDataLen = maxDataLen;

            // Мы вводим одни и те же данные tall раз. Это связано с тем, что в верхних слоях губки могут быть коллизии и их нужно избежать. Кроме этого, это позволяет лучше перемешать данные.
            for (int i = 0; i < StepsForAbsorption; i++)
                Step_once(data, curDataLen, regime, inputRegime);

            data        += curDataLen;
            dataLen     -= curDataLen;
            dataUsedLen += curDataLen;

            // Дополнительный шаг для восполнения упущенной обратной связи
            // Этот дополнительный шаг реализует требования к двойному шагу после ввода данных
            if (curDataLen > 0)
                Step_once(null, 0, regime, inputRegime);

            for (int M = 0; M < ArmoringSteps; M++)
                Step_once(null, 0, regime, inputRegime);

            if (progress is not null)
            {
                progress.processedSteps++;
            }
        }

        // Выполняем заключительное преобразование для отбивки обратной связи от выхода
        DoThreeFish    (fullOutput, this.threefishCrypto!.array + countOfThreeFish_RC*256);     // Заключительное преобразование для выхода
        TransposeOutput(fullOutput, 128);

        // Копируем начало вывода нижних губок в массив пользовательского вывода
        BytesBuilder.CopyTo(ReverseConnectionLen, maxDataLen, fullOutput, lastOutput);

        haveOutput = true;

        return dataUsedLen;
    }

    // ::docs:blukp2nlAfFcIXUzG6Pd:
    /// <summary>Возвращает количество шагов на впитывание одних и те же данных, которое вычисленно исходя из значения StepTypeForAbsorption. Эта функция вычисляет количество шагов по умолчанию для функции step.</summary>
    public nint GetCountOfStepsForAbsorption()
    {
        return StepTypeForAbsorption switch
        {
            TypeForShortStepForAbsorption.weak      => 1,
            TypeForShortStepForAbsorption.effective => countStepsForEffectiveAbsorption, // => 1
            TypeForShortStepForAbsorption.elevated  => 2,
            TypeForShortStepForAbsorption.full      => tall,
            TypeForShortStepForAbsorption.log       => tall2log2 > 0 ? tall2log2 : throw new CascadeSpongeException("tall2log2 is not initialized"),
            _ => throw new InvalidDataException("CascadeSponge_1t_20230905.step: switch (StepTypeForAbsorption).default"),
        };
    }

    /// <summary>Выполняет одиночный шаг. Двойной шаг при вводе данных этот алгоритм не выполняет!</summary>
    /// <param name="data">Дата для ввода</param>
    /// <param name="dataLen">Данные для ввода. не более чем maxDataLen</param>
    /// <param name="regime">Логический режим ввода (определяемой схемой шифрования)</param>
    /// <param name="inputRegime">Режим ввода данных в губку: xor или overwrite (перезапись)</param>
    /// <param name="calcOut">Если false, то выход не рассчитывается</param>
    protected virtual void Step_once(byte * data = null, nint dataLen = 0, byte regime = 0, InputRegime inputRegime = xor)
    {
        // Вводим данные, включая обратную связь, в верхний слой губки
        InputData(data, dataLen, regime, rcOutput, inputRegime);

        var buffer = stackalloc byte[(int) ReverseConnectionLen];
        if (inputRegime == overwrite)
            input = KeccakPrime.Keccak_InputOverwrite64_512;
        else
            input = KeccakPrime.Keccak_Input64_512;

        byte * S, B, C;
        for (nint layer = 0; layer < tall; layer++)
        {
            // Рассчитываем для данного уровня все данные
            for (nint i = 0; i < wide; i++)
            {
                GetKeccakS(layer, i, S: out S, B: out B, C: out C);
                KeccakPrime.Keccackf(a: (ulong *) S, c: (ulong *) C, b: (ulong *) B);
            }

            // Если это не последний уровень губки
            if (layer == tall - 1)
                break;

            var buff = buffer;
            // Выводим во временный буфер выход со всех губок этого уровня
            for (nint i = 0; i < wide; i++)
            {
                GetKeccakS(layer, i, S: out S, B: out B, C: out C);
                KeccakPrime.Keccak_Output_512(buff, MaxInputForKeccak, S: S);
                buff += MaxInputForKeccak;
            }

            TransposeOutput(buffer);

            // Вводим данные на уровень ниже
            buff = buffer;
            for (nint i = 0; i < wide; i++)
            {
                GetKeccakS(layer+1, i, S: out S, B: out B, C: out C);

                input(buff, MaxInputForKeccak, S, regime, 255);
                buff += MaxInputForKeccak;
            }
        }

        // Последний уровень губки, включая преобразование обратной связи
        OutputAllData();

        BytesBuilder.ToNull(ReverseConnectionLen, buffer);
        _countOfProcessedSteps++;
        LastRegime = regime;
    }

    /// <summary>Режим последнего шага (следующий шаг схемы не должен совпадать с последним режимом, если это не предусмотрено схемой)</summary>
    public byte LastRegime { get; protected set;}

    // code::docs:Wt74dfPfEIcGzPN5Jrxe:
    /// <summary>Инициализирует ThreeFish и таблицы подстановок с помощью этого же каскада. Каскад должен быть до этого проинициализирован ключами, вводимыми внутрь каскада (см. функцию setupKeyAndOIV).<remarks>Заключительный шаг проводится в режиме 5 (следующий шаг схемы не должен быть в этом режиме), начальный шаг - режим 1.</remarks></summary>
    /// <param name="stepToKeyConst">Рекомендуется не менее чем 2 раза. Количество раз, которое губка выполняет инициализацию ключей и твиков ThreeFish (каждый раз с вновь вычисленными значениями). В случае, если не особо нужна стойкость, для рандомизации ключей можно вызвать только один раз, либо если в дальнейшем будет проведён повторный вызов данного метода.</param>
    /// <param name="doCheckSafty">Если false, то данный метод можно вызвать с параметром stepToKeyConst = 1 или на непроинициализированной губке</param>
    /// <param name="dataLenFromStep">Параметр определяет, сколько будет взято байтов для ключей ThreeFish с каждого шага губки. Не более maxDataLen</param>
    /// <param name="noInitSubstitutionTables">Если true, то не делает инициализацию таблиц подстановок.</param>
    /// <param name="countOfSteps">Количество шагов, которые будет делать губка для генерации одного вывода губки. 0 - это количество шагов по умолчанию (1 шаг). Этот параметр передаётся в функцию step. Релевантные значения: 0 (==countStepsForKeyGeneration), 1,  countStepsForHardening, countStepsForKeyGeneration</param>
    /// <param name="countOfStepsForSubstitutionTable">Количество шагов, которые будет делать губка для генерации одного вывода губки при формировании таблицы подстановок. Формирование таблицы подстановок может занимать длительное время, поэтому не рекомендуется увеличивать количество шагов. Параметр по умолчанию 0 означает 1 шаг</param>
    public void InitThreeFishByCascade(nint stepToKeyConst = 2, bool doCheckSafty = true, nint dataLenFromStep = 0, bool noInitSubstitutionTables = false, nint countOfSteps = 0, nint countOfStepsForSubstitutionTable = 0)
    {
        // Защита от вызова на непроинициализированной губке
        if (doCheckSafty && CountOfProcessedSteps < countStepsForKeyGeneration)
            throw new CascadeSpongeException("InitThreeFishByCascade: countOfProcessedSteps < countStepsForKeyGeneration || !haveOutput");
        if (doCheckSafty && stepToKeyConst < 2)
            throw new CascadeSpongeException("InitThreeFishByCascade: stepToKeyConst < 2");
        if (stepToKeyConst < 1)
            throw new CascadeSpongeException("InitThreeFishByCascade: stepToKeyConst < 1");

        if (countOfSteps <= 0)
            countOfSteps = countStepsForKeyGeneration;

        haveOutput  = false;     // Сбрасываем, чтобы если случилась ошибка, флаг был бы сброшен и не дал бы дальше работать
        var needLen = countOfThreeFish*Threefish_slowly.keyLen + countOfThreeFish*Threefish_slowly.twLen;
        var buffer  = new BytesBuilderStatic(needLen + maxDataLen);                     // Берём длину с запасом на один шаг, чтобы не считать лишние данные

        if (LastRegime == 1)
            throw new CascadeSpongeException("InitThreeFishByCascade: lastRegime == 1");
        if (dataLenFromStep > maxDataLen)
            throw new CascadeSpongeException("InitThreeFishByCascade: dataLenFromStep > maxDataLen");

        if (dataLenFromStep <= 0)
            dataLenFromStep = maxDataLen;

        try
        {
            // Делаем инициализацию ключей два раза (точнее, stepToKeyConst раз)
            for (int stepToKey = 0; stepToKey < stepToKeyConst; stepToKey++)
            {
                // Берём данные из губки для инициализации ключей
                do
                {
                    Step(countOfSteps, regime: 1); haveOutput = false;        // Хотя губка уже проинициализированна, на всякий случай делаем лишние шаги. Для !doCheckSafty губка может быть и непроинициализированна
                    buffer.Add(lastOutput, dataLenFromStep);
                }
                while (buffer.Count < needLen);

                if (!noInitSubstitutionTables)
                    InitSubstitutionTable(countOfStepsForSubstitutionTable);

                // Копируем значения ключей
                var rc = threefishCrypto!.array + 0;    // Сразу выполняем переход на ключи
                for (int i = 0; i < countOfThreeFish; i++, rc += 256)
                {
                    buffer.GetBytesAndRemoveIt(rc, Threefish_slowly.keyLen);
                }

                // Копируем значения твиков
                rc = threefishCrypto!.array + 192;    // Сразу выполняем переход на твики
                for (int i = 0; i < countOfThreeFish; i++, rc += 256)
                {
                    buffer.GetBytesAndRemoveIt(rc, Threefish_slowly.twLen);
                }

                buffer.Clear();

                // Расширяем ключи и твики как надо
                ExpandThreeFish();
                Step(countOfSteps, regime: 2);
                Step(1           , regime: 3, inputRegime: overwrite);
                Step(countOfSteps, regime: 5);
            }

            haveOutput = true;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    /// <summary>Эту функцию не нужно вызывать напрямую (если только для отдельной инициализации таблиц подстановок после инициализации ключей ThreeFish из внешних ключей). Она вызывается из InitThreeFishByCascade. Функция инициализирует таблицу подстановок обратной связи с помощью самой губки. Шаги производятся в режиме 7.</summary>
    /// <param name="countOfSteps">Количество шагов, которые будет делать губка для генерации одного вывода губки. 0 - это количество шагов по умолчанию (1 шаг). Этот параметр передаётся в функцию step и не является аналогом параметра stepToKeyConst в функции InitThreeFishByCascade. Релевантные значения: 0 (==1), countStepsForHardening, countStepsForKeyGeneration (в порядке возрастания трудоёмкости)</param>
    public void InitSubstitutionTable(nint countOfSteps = 0)
    {
        var tmp = stackalloc byte[SubstitutionTableLen_inBytes];

        BytesBuilder.CopyTo(SubstitutionTableLen_inBytes, SubstitutionTableLen_inBytes, SubstitutionTable, tmp);
        this.DoRandomPermutationForUShorts(SubstitutionTableLen_inUShort, (ushort *) tmp, countOfSteps, 7);
        BytesBuilder.CopyTo(SubstitutionTableLen_inBytes, SubstitutionTableLen_inBytes, tmp, SubstitutionTable);

        if (!SubstitutionTable_IsCorrect())
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.InitSubstitutionTable: !SubstitutionTable_IsCorrect");
    }

    // code::docs:CJXTMlFBHbtxFNSpqeC8:
    /// <summary>Проводит инициализацию губки ключом и синхропосылкой. Можно вызывать не один раз. Заключительный шаг проводится в режиме 5 (следующий шаг схемы не должен быть в этом режиме). Начальный шаг - режим 254, если есть синхропосылка, 255 - если нет сихропосылки.</summary>
    /// <param name="key">Ключ шифрования либо иной объект инициализации. Не освобождается и не изменяется при работе функции.</param>
    /// <param name="OIV">Синхропосылка (открытый вектор инициализации). Открытый вектор инициализации может быть любой, в том числе предсказуемый противником, но не повторяющийся. Может быть null. Не освобождается и не изменяется при работе функции.</param>
    /// <param name="InitThreeFishByCascade_stepToKeyConst">0 - ничего не делать. 2 или более: вызвать InitThreeFishByCascade со значением stepToKeyConst равным InitThreeFishByCascade_stepToKeyConst. Это количество генераций ключей ThreeFish, если они отдельно не вводились пользователем. По-умолчанию - 2. 0 - если перед этой функцией была сделана инициализация ключей ThreeFish функцией setThreeFishKeysAndTweak</param>
    /// <param name="doCheckSafty">Если false, то данный метод можно вызвать с параметром stepToKeyConst = 1 или на непроинициализированной губке</param>
    public void InitKeyAndOIV(Record key, Record? OIV = null, nint InitThreeFishByCascade_stepToKeyConst = 2, bool doCheckSafty = true)
    {
        InitKeyAndOIV(key, key.len, OIV, OIV?.len ?? 0, InitThreeFishByCascade_stepToKeyConst, doCheckSafty);
    }

    /// <summary>Проводит инициализацию губки ключом и синхропосылкой. Можно вызывать не один раз. Заключительный шаг проводится в режиме 5 (следующий шаг схемы не должен быть в этом режиме). Начальный шаг - режим 254, если есть синхропосылка, 255 - если нет сихропосылки. Функция не портит предыдущие инициализации таблиц перестановок и ключей ThreeFish, но если вызывается InitThreeFishByCascade (InitThreeFishByCascade_stepToKeyConst > 0), то функция использует старые значения для вычисления новых значений ThreeFish и таблицы перестановок и заменит эти ключи и таблицы.</summary>
    /// <param name="key">Ключ шифрования либо иной объект инициализации. Не освобождается и не изменяется при работе функции.</param>
    /// <param name="OIV">Синхропосылка (открытый вектор инициализации). Открытый вектор инициализации может быть любой, в том числе предсказуемый противником, но не повторяющийся. Может быть null. Не освобождается и не изменяется при работе функции.</param>
    /// <param name="InitThreeFishByCascade_stepToKeyConst">0 - ничего не делать. 2 или более: вызвать InitThreeFishByCascade со значением stepToKeyConst равным InitThreeFishByCascade_stepToKeyConst. Это количество генераций ключей ThreeFish, если они отдельно не вводились пользователем. По-умолчанию - 2. 0 - если перед этой функцией была сделана инициализация ключей ThreeFish функцией setThreeFishKeysAndTweak</param>
    /// <param name="doCheckSafty">Если false, то данный метод можно вызвать с параметром stepToKeyConst = 1 или на непроинициализированной губке</param>
    public void InitKeyAndOIV(byte * key, nint key_length, byte * OIV = null, nint OIV_length = 0, nint InitThreeFishByCascade_stepToKeyConst = 2, bool doCheckSafty = true)
    {
        if (OIV is not null)
        {
            if (key == OIV)
                throw new CascadeSpongeException("InitThreeFishByCascade: key.array == OIV.array");

            if (LastRegime == 254)
                throw new CascadeSpongeException("InitThreeFishByCascade: lastRegime == 254 (OIV is not null)");

            Step(0, 0, OIV, OIV_length,  regime: 254);
            Step(countStepsForHardening, regime: 0);
        }
        else
        {
            if (LastRegime == 255)
                throw new CascadeSpongeException("InitThreeFishByCascade: lastRegime == 255 (OIV==null)");
        }

        Step(0, countStepsForKeyGeneration, key, key_length, regime: 255);
        Step(1, inputRegime: overwrite);
        Step(countStepsForKeyGeneration, regime: 5);

        if (InitThreeFishByCascade_stepToKeyConst != 0)
            InitThreeFishByCascade(InitThreeFishByCascade_stepToKeyConst, doCheckSafty);
    }
}
