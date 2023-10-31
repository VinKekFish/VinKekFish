// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization.Json;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;

// В этом файле получаются данные из промежуточных губок и они вводятся в основные губки
public partial class Regime_Service
{
    public class CountOfBytesCounter
    {
        protected double _min = 0d, _max = 0d, _avg = 0d, _EME = 0d;

        public nint min => (nint) _min;
        public nint max => (nint) _max;
        public nint avg => (nint) _avg;
        public nint EME => (nint) _EME;

        public void addNumberToBytes(nint bytes, ContinuouslyGetterRecord getter)
        {
            if (getter.inputElement.intervals!.entropy.min > 0)
            _min += (double) bytes / (double) getter.inputElement.intervals!.entropy.min;

            if (getter.inputElement.intervals!.entropy.max > 0)
            _max += (double) bytes / (double) getter.inputElement.intervals!.entropy.max;

            if (getter.inputElement.intervals!.entropy.avg > 0)
            _avg += (double) bytes / (double) getter.inputElement.intervals!.entropy.avg;

            if (getter.inputElement.intervals!.entropy.EME > 0)
            _EME += (double) bytes / (double) getter.inputElement.intervals!.entropy.EME;
        }

        public void Clear()
        {
            _min = 0d;
            _max = 0d;
            _avg = 0d;
            _EME = 0d;
        }

        public override string ToString()
        {
            return
            $"""
                min
                    {min}
                max
                    {max}
                avg
                    {avg}
                EME
                    {EME}

            """;
        }
    }
                                                                                                                /// <summary>Количество собранных битов энтропии - всего</summary>
    public readonly CountOfBytesCounter countOfBytesCounterTotal = new CountOfBytesCounter();                   /// <summary>Количество собранных битов энтропии - с учётом выведенной энтропии</summary>
    public readonly CountOfBytesCounter countOfBytesCounterCorr  = new CountOfBytesCounter();

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

                countOfBytesCounterTotal.addNumberToBytes(BlockLen, getter);
                countOfBytesCounterCorr .addNumberToBytes(BlockLen, getter);
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
