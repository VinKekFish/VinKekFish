// TODO: tests
namespace vinkekfish;

using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using System.Threading.Tasks;

using static CascadeSponge_1t_20230905.InputRegime;
using System.Text;

// code::docs:rQN6ZzeeepyOpOnTPKAT:  Это главный файл реализации

/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_1t_20230905: IDisposable
{
    /// <summary>Осуществить шаг алгоритма (полный шаг каскадной губки - все губки делают по одному шагу)</summary>
    /// <param name="countOfSteps">Количество шагов алгоритма</param>
    /// <param name="data">Данные для ввода, не более maxDataLen на один шаг</param>
    /// <param name="dataLen">Количество данных для ввода</param>
    /// <param name="regime">Режим ввода (логический параметр, декларируемый схемой шифрования; может быть любым однобайтовым значением)</param>
    /// <param name="inputRegime">Режим ввода данных в губку: либо обычный xor, либо режим overwrite для обеспечения необратимости шифрования и защиты ключа перед его использованием</param>
    public nint step(nint countOfSteps = 1, byte * data = null, nint dataLen = 0, byte regime = 0, InputRegime inputRegime = xor)
    {
        if (countOfSteps < 1)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.step: countOfSteps < 1");
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
        }

        return dataUsedLen;
    }

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
    }
}
