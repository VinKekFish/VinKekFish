// TODO: tests
namespace vinkekfish;

using cryptoprime;
using static cryptoprime.BytesBuilderForPointers;

using static CascadeSponge_1t_20230905.InputRegime;

// code::docs:rQN6ZzeeepyOpOnTPKAT:

// Описание шага шифрования, инициализации ключей ThreeFish через губку, а также инициализации губки ключом шифрования и синхропосылкой (открытым вектором инициализации)

/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_1t_20230905: IDisposable
{
    /// <summary>Осуществить шаг алгоритма (полный шаг каскадной губки - все губки делают по одному шагу)</summary>
    /// <param name="countOfSteps">Количество шагов алгоритма. 0 - значение будет рассчитано исходя из dataLen</param>
    /// <param name="ArmoringSteps">Количество усиливающих шагов алгоритма, которые будут проведены вхолостую после каждого шага поглощения. Не ноль для усиленных режимов, например, инициализации или генерации ключа. См. countStepsForKeyGeneration</param>
    /// <param name="data">Данные для ввода, не более maxDataLen на один шаг</param>
    /// <param name="dataLen">Количество данных для ввода</param>
    /// <param name="regime">Режим ввода (логический параметр, декларируемый схемой шифрования; может быть любым однобайтовым значением)</param>
    /// <param name="inputRegime">Режим ввода данных в губку: либо обычный xor, либо режим overwrite для обеспечения необратимости шифрования и защиты ключа перед его использованием</param>
    public nint step(nint countOfSteps = 1, nint ArmoringSteps = 0, byte * data = null, nint dataLen = 0, byte regime = 0, InputRegime inputRegime = xor)
    {
        if (!isThreeFishInitialized)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: !isThreeFishInitialized. See ThreeFish key initialization for reverse connection");

        if (countOfSteps < 1)
        {
            if (dataLen <= 0 || data == null)
                throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: countOfSteps < 1 && dataLen <= 0");

            countOfSteps = dataLen / maxDataLen;
            if (dataLen % maxDataLen > 0)
                countOfSteps++;
        }

        if (dataLen > maxDataLen*countOfSteps)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: dataLen > maxDataLen*countOfSteps");

        nint curDataLen, dataUsedLen = 0;
        for (int stepNum = 0; stepNum < countOfSteps; stepNum++)
        {
            curDataLen = dataLen;
            if (curDataLen > maxDataLen)
                curDataLen = maxDataLen;

            step(data, curDataLen, regime, inputRegime);
            data        += curDataLen;
            dataLen     -= curDataLen;
            dataUsedLen += curDataLen;

            for (int M = 0; M < ArmoringSteps; M++)
                step(null, 0, regime, inputRegime);
        }

        return dataUsedLen;
    }

    /// <summary>Выполняет одиночный шаг</summary>
    /// <param name="data">Дата для ввода</param>
    /// <param name="dataLen">Данные для ввода. не более чем maxDataLen</param>
    /// <param name="regime">Логический режим ввода (определяемой схемой шифрования)</param>
    /// <param name="inputRegime">Режим ввода данных в губку: xor или overwrite (перезапись)</param>
    protected void step(byte * data = null, nint dataLen = 0, byte regime = 0, InputRegime inputRegime = xor)
    {
        // Вводим данные, включая обратную связь, в верхний слой губки
        InputData(data, dataLen, regime, fullOutput, inputRegime);

        var buffer = stackalloc byte[(int) ReserveConnectionLen];
        var input = KeccakPrime.Keccak_Input64_512;
        if (inputRegime == overwrite)
            input = KeccakPrime.Keccak_InputOverwrite64_512;

        for (nint layer = 0; layer < tall; layer++)
        {
            // Рассчитываем для данного уровня все данные
            for (nint i = 0; i < wide; i++)
                CascadeKeccak[layer, i]!.CalcStep();

            // Если это не последний уровень губки
            if (layer == tall - 1)
                continue;

            var buff = buffer;
            // Выводим во временный буфер выход со всех губок этого уровня
            for (nint i = 0; i < wide; i++)
            {
                var keccak = CascadeKeccak[layer, i]!;
                KeccakPrime.Keccak_Output_512(buff, MaxInputForKeccak, keccak.S);
                buff += MaxInputForKeccak;
            }

            transposeOutput(buffer);

            // Вводим данные на уровень ниже
            buff = buffer;
            for (nint i = 0; i < wide; i++)
            {
                var keccak = CascadeKeccak[layer+1, i]!;

                input(buff, MaxInputForKeccak, keccak.S, regime);
                buff += MaxInputForKeccak;
            }
        }

        // Последний уровень губки, включая преобразование обратной связи
        outputAllData(fullOutput);

        BytesBuilder.ToNull(ReserveConnectionLen, buffer);
        _countOfProcessedSteps++;
        haveOutput = true;
        lastRegime = regime;
    }

    /// <summary>Режим последнего шага (следующий шаг схемы не должен совпадать с последним режимом, если это не предусмотрено схемой)</summary>
    public byte lastRegime { get; protected set;}

    // code::docs:Wt74dfPfEIcGzPN5Jrxe:
    /// <summary>Инициализирует ThreeFish с помощью каскада. Каскад должен быть до этого проинициализирован ключами, вводимыми внутрь каскада (см. функцию setupKeyAndOIV). Заключительный шаг проводится в режиме 5 (следующий шаг схемы не должен быть в этом режиме), начальный шаг - режим 1</summary>
    /// <param name="stepToKeyConst">Не менее, чем 2 раза. Количество раз, которое губка выполняет инициализацию ключей и твиков ThreeFish (каждый раз с вновь вычисленными значениями)</param>
    public void InitThreeFishByCascade(int stepToKeyConst = 2)
    {
        // Защита от вызова на непроинициализированной губке
        if (countOfProcessedSteps < countStepsForKeyGeneration || !haveOutput)
            throw new CascadeSpongeException("InitThreeFishByCascade: countOfProcessedSteps < countStepsForKeyGeneration || !haveOutput");
        if (stepToKeyConst < 2)
            throw new CascadeSpongeException("InitThreeFishByCascade: stepToKeyConst < 2");

        haveOutput  = false;     // Сбрасываем, чтобы если случилась ошибка, флаг был бы сброшен и не дал бы дальше работать
        var needLen = ReserveConnectionLen + countOfThreeFish*threefish_slowly.twLen;
        var buffer  = new BytesBuilderStatic(needLen + maxDataLen);                     // Берём длину с запасом на один шаг, чтобы не считать лишние данные

        if (lastRegime == 1)
            throw new CascadeSpongeException("InitThreeFishByCascade: lastRegime == 1");

        try
        {
            // Делаем инициализацию ключей два раза
            for (int stepToKey = 0; stepToKey < stepToKeyConst; stepToKey++)
            {
                // Берём данные из губки для инициализации ключей
                do
                {
                    buffer.add(lastOutput, maxDataLen);         // Губка уже проинициализированна, значит, берём значения сачала, а потом уже выполняем повторный расчёт
                    step(countStepsForKeyGeneration, regime: 1); haveOutput = false;
                }
                while (buffer.Count < needLen);

                // Копируем значения ключей
                var rc = reverseCrypto!.array + 0;    // Сразу выполняем переход на ключи
                for (int i = 0; i < countOfThreeFish; i++, rc += 256)
                {
                    buffer.getBytesAndRemoveIt(rc, threefish_slowly.keyLen);
                }

                // Копируем значения твиков
                rc = reverseCrypto!.array + 192;    // Сразу выполняем переход на твики
                for (int i = 0; i < countOfThreeFish; i++, rc += 256)
                {
                    buffer.getBytesAndRemoveIt(rc, threefish_slowly.twLen);
                }

                // Расширяем ключи и твики как надо
                ExpandThreeFish();
                step(countStepsForKeyGeneration,  regime: 2);
                step(1,                           inputRegime: overwrite, regime: 3);
                step(countStepsForKeyGeneration,  regime: 5);
            }

            haveOutput = true;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    /// <summary>Проводит инициализацию губки ключом и синхропосылкой. Заключительный шаг проводится в режиме 5 (следующий шаг схемы не должен быть в этом режиме). Начальный шаг - режим 254.</summary>
    /// <param name="key">Ключ шифрования</param>
    /// <param name="OIV">Синхропосылка (открытый вектор инициализации). Открытый вектор инициализации может быть любой, в том числе предсказуемый противником, но не повторяющийся.</param>
    /// <param name="InitThreeFishByCascade_stepToKeyConst">0 - ничего не делать. 2 или более: вызвать InitThreeFishByCascade со значением stepToKeyConst равным InitThreeFishByCascade_stepToKeyConst. Это количество генераций ключей ThreeFish, если они отдельно не вводились пользователем. По-умолчанию - 2.</param>
    public void setupKeyAndOIV(Record key, Record OIV, int InitThreeFishByCascade_stepToKeyConst = 0)
    {
        if (lastRegime == 254)
            throw new CascadeSpongeException("InitThreeFishByCascade: lastRegime == 254");

        step(0, 0, OIV, OIV.len, regime: 254);
        step(1, regime: 0);
        step(0, countStepsForKeyGeneration, key, key.len, regime: 255);
        step(1, inputRegime: overwrite);
        step(countStepsForKeyGeneration, regime: 5);

        if (InitThreeFishByCascade_stepToKeyConst >= 2)
            InitThreeFishByCascade(InitThreeFishByCascade_stepToKeyConst);
    }
}
