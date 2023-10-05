// TODO: tests
namespace vinkekfish;

using cryptoprime;
using static cryptoprime.BytesBuilderForPointers;

using static CascadeSponge_1t_20230905.InputRegime;
using static CascadeSponge_1t_20230905;

// code::docs:rQN6ZzeeepyOpOnTPKAT:

// ::cp:alg:a7L6XjXsuwWGVxwJSN1x.step:20230930

// Описание шага шифрования, инициализации ключей ThreeFish через губку, а также инициализации губки ключом шифрования и синхропосылкой (открытым вектором инициализации)

/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_mt_20230930: IDisposable
{
    protected volatile byte regime = 0;

    public override nint step(nint countOfSteps = 0, nint ArmoringSteps = 0, byte * data = null, nint dataLen = 0, byte regime = 0, InputRegime inputRegime = xor, StepProgress? progress = null)
    {
        nint result = -1;
        try
        {
            Event.Set();    // Сразу же предлагаем потокам работать, чтобы потом не тратить время на синхронизацию
            result = base.step(countOfSteps: countOfSteps, ArmoringSteps: ArmoringSteps, data: data, dataLen: dataLen, regime: regime, inputRegime: inputRegime, progress: progress);
        }
        finally
        {
            Event.Reset();
        }

        return result;
    }

    /// <summary>Выполняет одиночный шаг. Двойной шаг при вводе данных этот алгоритм не выполняет! Event.Set() и Event.Reset() выполняются вызывающей функцией "step"</summary>
    /// <param name="data">Дата для ввода</param>
    /// <param name="dataLen">Данные для ввода. не более чем maxDataLen</param>
    /// <param name="regime">Логический режим ввода (определяемой схемой шифрования)</param>
    /// <param name="inputRegime">Режим ввода данных в губку: xor или overwrite (перезапись)</param>
    /// <param name="calcOut">Если false, то выход не рассчитывается</param>
    protected override void step_once(byte * data = null, nint dataLen = 0, byte regime = 0, InputRegime inputRegime = xor)
    {
        if (ThreadsError)
            throw new CascadeSpongeException("CascadeSponge_mt_20230930.step_once: ThreadsError is setted (in the function start)");

        // Вводим данные, включая обратную связь, в верхний слой губки
        InputData(data, dataLen, regime, rcOutput, inputRegime);

        var buffer  = (byte *) stepBuffer;
        this.regime = regime;
        if (inputRegime == overwrite)
            input = KeccakPrime.Keccak_InputOverwrite64_512;
        else
            input = KeccakPrime.Keccak_Input64_512;

        curStepBuffer = 1;

        for (nint layer = 0; layer < tall; layer++)
        {
            // Рассчитываем для данного уровня все данные
            ThreadsLayer           = layer;
            ThreadsExecuted        = (int) ThreadsCount; // На один больше, чем Threads.Length
            // KeccakNumberForThreads = 0;                 // Устанавливаем счётчик выполненной работы: это текущий номер последнего необработанного индекса губки нужного слоя

            // Clear_Debug_t();
            if (ThreadsError)
                throw new CascadeSpongeException("CascadeSponge_mt_20230930.step_once: ThreadsError is setted (in a start of tasks)");

            // После того, как подготовили данные для заданий, ставим задания потокам
            // Если поставим перед - потоки сразу начнут выполнение и сделают всё некорректно
            for (nint i = 0; i < ThreadsFunc.Length; i += AlignmentMultipler)
                ThreadsFunc[i] = 1;       // Ставим на выполнение keccak потокам

            // lock (ThreadsStop)      // Ждём выполнения задач (и запускаем задачи)
            {
                Thread_keccak(ThreadsCount-1);

                while (ThreadsExecuted > 0 && !ThreadsError)
                {
                    // Monitor.Wait(ThreadsStop);
                    // Thread.Sleep(0);
                }

                // Event.Reset();  // Именно здесь, т.к. потоки могут не начаться до того, как будет ожидание ThreadsStop
                curStepBuffer *= -1;
            }

            if (ThreadsError)
                throw new CascadeSpongeException("CascadeSponge_mt_20230930.step_once: ThreadsError is setted");

            // Console.WriteLine(  toString_Debug_t()  );
        }

        // Последний уровень губки, включая преобразование обратной связи
        outputAllData();

        BytesBuilder.ToNull(ReserveConnectionLen, buffer);
        _countOfProcessedSteps++;
        lastRegime = regime;
    }
}
