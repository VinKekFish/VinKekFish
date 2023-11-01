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
    {                                                                   /// <summary>Значения максимального количества накопленных байтов энтропии (min), минимального количества (max), среднего (avg) и количества для противодействия атакам по побочным каналам (EME)</summary>
        protected double _min = 0d, _max = 0d, _avg = 0d, _EME = 0d;    /// <summary>Значение "удалённых" байтов - количества байтов, которые были получены из губки.</summary>
        protected nint removedBytes = 0;

        public nint min => (nint) Math.Max(_min - removedBytes, 0);
        public nint max => (nint) Math.Max(_max - removedBytes, 0);
        public nint avg => (nint) Math.Max(_avg - removedBytes, 0);
        public nint EME => (nint) Math.Max(_EME - removedBytes, 0);


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

        /// <summary>Логически "удаляет" часть байтов из значения накопленной энтропии</summary>
        /// <param name="bytes">Количество удаляемых байтов энтропии (положительное значение, равное количеству изъятых из губки байтов)</param>
        public void removeBytes(nint bytes)
        {
            if (bytes < 0)
                throw new ArgumentOutOfRangeException($"Regime_Service.CountOfBytesCounter.removeBytes: bytes < 0 ({bytes})");

            removedBytes += bytes;
        }

        public void Clear()
        {
            _min = 0d;
            _max = 0d;
            _avg = 0d;
            _EME = 0d;

            removedBytes = 0;
        }

        public CountOfBytesCounter Clone()
        {
            var result = new CountOfBytesCounter()
            {
                _min = this._min,
                _max = this._max,
                _avg = this._avg,
                _EME = this._EME,

                removedBytes = this.removedBytes
            };

            return result;
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
                removed
                    {removedBytes}

            """;
        }
    }
                                                                                                                /// <summary>Количество собранных байтов энтропии - всего; эта переменная учитывает собранные, но ещё не введённые в главную губку байты.</summary>
    protected readonly CountOfBytesCounter countOfBytesCounterTotal_h = new CountOfBytesCounter();              /// <summary>Количество собранных байтов энтропии - с учётом выведенной энтропии; эта переменная учитывает собранные, но ещё не введённые в главную губку байты.</summary>
    protected readonly CountOfBytesCounter countOfBytesCounterCorr_h  = new CountOfBytesCounter();
                                                                                                                /// <summary>Количество собранных байтов энтропии - всего.</summary>
    public CountOfBytesCounter countOfBytesCounterTotal { get; protected set; } = new CountOfBytesCounter();    /// <summary>Количество собранных байтов энтропии - с учётом выведенной энтропии.</summary>
    public CountOfBytesCounter countOfBytesCounterCorr  { get; protected set; } = new CountOfBytesCounter();

    /// <summary>Это должно быть вызвано в lock (entropy_sync).
    /// Функция принимает данные из промежуточных губок и, если надо, вызывает методы для получения дополнительной энтропии.</summary>
    protected unsafe virtual void InputEntropyFromSources()
    {
        var BlockLen = KeccakPrime.BlockLen;
        lock (continuouslyGetters)
        {
            var buff = bufferRec!.array;
            foreach (var getter in continuouslyGetters)
            {
                if (!getter.isDataReady(BlockLen))
                    continue;

                ConditionalInputEntropyToMainSponges(BlockLen);

                getter.getBytes(buff + bufferRec_current, BlockLen);
                bufferRec_current += BlockLen;

                countOfBytesCounterTotal_h.addNumberToBytes(BlockLen, getter);
                countOfBytesCounterCorr_h .addNumberToBytes(BlockLen, getter);
            }

            // Отрабатываем, если длина вводимых байтов больше, чем один шаг губки
            ConditionalInputEntropyToMainSponges(bufferRec.len - Math.Max(VinKekFish.BLOCK_SIZE_K, CascadeSponge.maxDataLen));
        }
    }

    /// <summary>Ввести накопленную в bufferRec энтропию в основную губку и выполнить вспомогательные операции. Может быть вызвано пользователем для принудительного сброса накопленной энтропии в губку.</summary>
    /// <param name="EmptyRemainder">Максимальное количество незаполненного места, которое может остаться в bufferRec (если незаполненного места больше, то ввод в губку производиться не будет). Если нужно срабатывание всегда, то можно подать nint.MaxValue; чем больше эта величина, тем больше вероятность срабатывания.</param>
    public unsafe void ConditionalInputEntropyToMainSponges(nint EmptyRemainder)
    {
        lock (entropy_sync)
        {
            if (bufferRec_current > 0)
            if (bufferRec!.len - bufferRec_current < EmptyRemainder)
            {
                var enteredBytesCount = bufferRec_current;
                InputBuffToSponges(bufferRec!, bufferRec_current);
                SetCountOfBytesCounters_and_ClearBufferRec();

                if (options_service?.root?.Options?.doLogEveryInputEntropyToSponge ?? false)
                    Console.WriteLine(L("Entropy bytes entered to the main sponge (\"do log every input entropy to sponge\" option is setted). Entered") + $" {enteredBytesCount}");
            }
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
