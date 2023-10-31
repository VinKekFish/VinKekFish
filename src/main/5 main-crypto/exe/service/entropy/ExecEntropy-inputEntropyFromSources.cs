// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System;
using System.Runtime.Serialization.Json;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;

// В этом файле получаются данные из промежуточных губок и они вводятся в основные губки
public partial class Regime_Service
{
    /// <summary>Это должно быть вызвано в lock (entropy_sync).
    /// Функция принимает данные из промежуточных губок и, если надо, вызывает методы для получения дополнительной энтропии.</summary>
    protected unsafe virtual void InputEntropyFromSources()
    {
        var BlockLen = KeccakPrime.BlockLen;
        lock (continuouslyGetters)
        {
            var  buff = bufferRec!.array;
            nint cur  = 0;
            foreach (var getter in continuouslyGetters)
            {
                if (!getter.isDataReady(BlockLen))
                    continue;

                if (bufferRec.len - cur < BlockLen)
                {
                    InputBuffToSponges(bufferRec!, cur);
                    cur = 0;
                }

                getter.getBytes(buff + cur, BlockLen);
                cur += BlockLen;
            }

            if (cur > 0)
                InputBuffToSponges(bufferRec!, cur);
        }
    }

    /// <summary>Метод вводит данные из record в губки и выполняет шаг расчёта.</summary>
    /// <param name="record">Данные для ввода в губки</param>
    /// <param name="len">Длина данных для ввода из record</param>
    public unsafe void InputBuffToSponges(Record record, nint len)
    {
        checked
        {
            if (record.len < len)
                throw new ArgumentOutOfRangeException("len", $"Regime_Service.InputBuffToSponges: record.len < len ({record.len} < {len})");

            lock (entropy_sync)
            Parallel.Invoke
            (
                () =>
                {
                    CascadeSponge.step
                    (
                        ArmoringSteps: CascadeSponge.countStepsForKeyGeneration - 1,

                        data   : record,
                        dataLen: len,
                        regime : 128
                    );
                },
                () =>
                {
                    VinKekFish.input!.add(record, len);
                    VinKekFish.doStepAndIO
                    (
                        countOfRounds: VinKekFish.EXTRA_ROUNDS_K,
                        outputLen    : 0,
                        regime       : 128
                    );
                }
            );
        }
    }
}
