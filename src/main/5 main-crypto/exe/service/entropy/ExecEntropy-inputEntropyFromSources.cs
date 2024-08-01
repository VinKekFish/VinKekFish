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

        public nint Min => (nint) Math.Max(_min - removedBytes, 0);
        public nint Max => (nint) Math.Max(_max - removedBytes, 0);
        public nint Avg => (nint) Math.Max(_avg - removedBytes, 0);
        public nint EME => (nint) Math.Max(_EME - removedBytes, 0);


        public void AddNumberToBytes(nint bytes, ContinuouslyGetterRecord getter)
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
        public void RemoveBytes(nint bytes)
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
                max (bytes)
                    {Min}
                avg (bytes)
                    {Avg}
                min (bytes)
                    {Max}
                EME (bytes)
                    {EME}
                removed (bytes)
                    {removedBytes}

            """;
        }
    }
                                                                                                                /// <summary>Количество собранных байтов энтропии - всего; эта переменная учитывает собранные, но ещё не введённые в главную губку байты.</summary>
    protected readonly CountOfBytesCounter countOfBytesCounterTotal_h = new();              /// <summary>Количество собранных байтов энтропии - с учётом выведенной энтропии; эта переменная учитывает собранные, но ещё не введённые в главную губку байты.</summary>
    protected readonly CountOfBytesCounter countOfBytesCounterCorr_h  = new();
                                                                                                                /// <summary>Количество собранных байтов энтропии - всего.</summary>
    public CountOfBytesCounter CountOfBytesCounterTotal { get; protected set; } = new CountOfBytesCounter();    /// <summary>Количество собранных байтов энтропии - с учётом выведенной энтропии.</summary>
    public CountOfBytesCounter CountOfBytesCounterCorr  { get; protected set; } = new CountOfBytesCounter();

    /// <summary>Функция принимает данные из промежуточных губок и, если надо, вызывает методы для получения дополнительной энтропии. Функция принимает данные в цикле, так что вводит неограниченное количество данных.</summary>
    /// <param name="BlockLen">Минмальное количество данных, которое будет введено из промежуточной губки. Если ноль, то это говорит о том, что любое количество данных должно быть введено из промежуточной губки, даже если они не дотягивают до одного байта энтропии.</param>
    protected unsafe virtual long InputEntropyFromSourcesWhile(int count = 1024, int BlockLen = KeccakPrime.BlockLen)
    {
        var  cnt    = count;
        long result = 0, lastResult;
        bool isMandatory = false;
        do
        {
            lock (entropy_sync)
            {
                lastResult = InputEntropyFromSources(out isMandatory, BlockLen);
            }
            result += lastResult;
            cnt--;
            Thread.Sleep(0);
        }
        while (cnt >= 0 && lastResult > 0);

        // Если вводилось MandatoryUse данные, то всегда вызываем губку
        // (здесь может быть лишний холостой вызов губки)
        if (isMandatory)
        {
            ConditionalInputEntropyToMainSponges(nint.MaxValue, true);
            // Console.WriteLine("isMandatory (debug)");
        }

        return result;
    }

    /// <summary>Это должно быть вызвано в lock (entropy_sync). Вместо этой функции нужно вызывать InputEntropyFromSourcesWhile.
    /// Функция принимает данные из промежуточных губок и, если надо, вызывает методы для получения дополнительной энтропии.</summary>
    /// <param name="isMandatory">Если был ввод данных из промежуточной губки, помеченной как MandatoryUse, данная переменная будет установлена в true.</param>
    /// <param name="BlockLen">Минмальное количество данных, которое будет введено из промежуточной губки</param>
    protected unsafe virtual long InputEntropyFromSources(out bool isMandatory, int BlockLen = KeccakPrime.BlockLen)
    {
        long result    = 0;
           isMandatory = false;
        lock (continuouslyGetters)
        {
            var buff = bufferRec!.array;
            foreach (var getter in continuouslyGetters)
            {
                lock (getter)
                {
                    if (!getter.IsDataReady(BlockLen))
                        continue;

                    int curLen = (int) getter.GetCountOfReadyBytes();
                    if (curLen > KeccakPrime.BlockLen)
                        curLen = KeccakPrime.BlockLen;

                    if (curLen <= 0)
                    {
                        if (getter.CountOfBytesFromLastOutput <= 0)
                            Console.WriteLine($"ERROR in InputEntropyFromSources: curLen = {curLen} ({getter.CountOfBytesFromLastOutput}); BlockLen = {BlockLen}; MandatoryUseGet = {getter.MandatoryUseGet}");

                        curLen = 1; // Чисто на всякий случай берём один байт (там дата чтения из файла появляется и, возможно, какие-то небольшие байты из файла, количество которых округлилось до нуля при переводе его в количество энтропии)
                    }

                    ConditionalInputEntropyToMainSponges(curLen);

                    if (getter.MandatoryUseGet)
                    {
                        isMandatory = true;
                        Monitor.PulseAll(getter);
                    }

                    var readed = getter.GetBytes(buff + bufferRec_current, curLen, BlockLen == 0);
                    bufferRec_current += readed;
                    result            += readed;

                    countOfBytesCounterTotal_h.AddNumberToBytes(readed, getter);
                    countOfBytesCounterCorr_h .AddNumberToBytes(readed, getter);
                }
            }

            ConditionalInputEntropyToMainSponges(KeccakPrime.BlockLen);
        }

        return result;
    }

    /// <summary>Ввести накопленную в bufferRec энтропию в основную губку и выполнить вспомогательные операции. Может быть вызвано пользователем для принудительного сброса накопленной энтропии в губку.</summary>
    /// <param name="EmptySpaceAcceptableRemainder">Максимальное количество незаполненного места, которое может остаться в bufferRec при запуске губки (если незаполненного места больше, то ввод в губку производиться не будет). Если нужно срабатывание всегда, то можно подать nint.MaxValue; чем больше эта величина, тем больше вероятность срабатывания.</param>
    public unsafe void ConditionalInputEntropyToMainSponges(nint EmptySpaceAcceptableRemainder, bool isMandatory = false)
    {
        lock (entropy_sync)
        {
            if (bufferRec_current > 32 || (isMandatory && bufferRec_current > 0)) // 32 - это 256-битов энтропии; если меньше, то можно пробовать перебирать байты, если ты уже знаешь предыдущие; так что мы не будем вводить слишком малую порцию данных; реально ввод всегда не менее 64-х байтов, т.к. запрос идёт сразу одного блока через isDataReady
            {
                var EmptySpace = GetMaxBlockSize() - bufferRec_current;
                if (EmptySpace < EmptySpaceAcceptableRemainder || isMandatory)    // Высчитываем пустое место в буффере и сравниваем его с допустимым
                {
                    var enteredBytesCount = bufferRec_current;
                    InputBuffToSponges(bufferRec!, bufferRec_current);
                    SetCountOfBytesCounters_and_ClearBufferRec();

                    if (options_service?.root?.Options?.doLogEveryInputEntropyToSponge ?? false)
                        Console.WriteLine(L("Entropy bytes entered to the main sponge (\"do log every input entropy to sponge\" option is setted). Entered") + $" {enteredBytesCount}");
                }
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
                throw new ArgumentOutOfRangeException(nameof(len), $"Regime_Service.InputBuffToSponges: record.len < len ({record.len} < {len})");

            lock (entropy_sync)
            Parallel.Invoke
            (
                () =>
                {
                    CascadeSponge.Step
                    (
                        ArmoringSteps: CascadeSponge.countStepsForKeyGeneration - 1,

                        data   : record,
                        dataLen: len,
                        regime : 128
                    );
                },
                () =>
                {
                    VinKekFish.input!.Add(record, len);
                    VinKekFish.DoStepAndIO
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
