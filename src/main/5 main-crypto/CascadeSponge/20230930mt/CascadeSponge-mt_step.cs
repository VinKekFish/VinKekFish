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
    public class StepProgress
    {                                           /// <summary>Сколько шагов нужно выполнить всего</summary>
        public nint allSteps   = 0;              /// <summary>Сколько шагов уже закончено</summary>
        public nint endedSteps = 0;
    }

    /// <summary>Выполняет одиночный шаг. Двойной шаг при вводе данных этот алгоритм не выполняет!</summary>
    /// <param name="data">Дата для ввода</param>
    /// <param name="dataLen">Данные для ввода. не более чем maxDataLen</param>
    /// <param name="regime">Логический режим ввода (определяемой схемой шифрования)</param>
    /// <param name="inputRegime">Режим ввода данных в губку: xor или overwrite (перезапись)</param>
    /// <param name="calcOut">Если false, то выход не рассчитывается</param>
    protected override void step_once(byte * data = null, nint dataLen = 0, byte regime = 0, InputRegime inputRegime = xor)
    {
        // Вводим данные, включая обратную связь, в верхний слой губки
        InputData(data, dataLen, regime, rcOutput, inputRegime);

        var buffer = stackalloc byte[(int) ReserveConnectionLen];
        var input = KeccakPrime.Keccak_Input64_512;
        if (inputRegime == overwrite)
            input = KeccakPrime.Keccak_InputOverwrite64_512;

        byte * S, B, C;
        for (nint layer = 0; layer < tall; layer++)
        {
            // Рассчитываем для данного уровня все данные
            ThreadsLayer       = layer;
            ThreadsExecuted    = Threads.Length;
            KeccakThreadNumber = 0;                 // Устанавливаем счётчик выполненной работы

            // После того, как подготовили данные для заданий, ставим задания потокам
            // Если поставим перед - потоки сразу начнут выполнение и сделают всё некорректно
            for (nint i = 0; i < ThreadsFunc.Length; i++)
                ThreadsFunc[i] = 1;       // Ставим на выполнение keccak потокам
Console.WriteLine("start");
            lock (ThreadsStop)      // Ждём выполнения задач (и запускаем задачи)
            {
                lock (ThreadsStart)
                    Monitor.PulseAll(ThreadsStart);

                Thread.Sleep(0);

                while (ThreadsExecuted > 0)
                    Monitor.Wait(ThreadsStop);
            }

            if (ThreadsError)
            {
                throw new CascadeSpongeException("CascadeSponge_mt_20230930.step_once: ThreadsError is setted");
            }


            // Если это не последний уровень губки
            if (layer == tall - 1)
                break;

            var buff = buffer;
            // Выводим во временный буфер выход со всех губок этого уровня
            for (nint i = 0; i < wide; i++)
            {
                getKeccakS(layer, i, S: out S, B: out B, C: out C);
                KeccakPrime.Keccak_Output_512(buff, MaxInputForKeccak, S: S);
                buff += MaxInputForKeccak;
            }

            transposeOutput(buffer);

            // Вводим данные на уровень ниже
            buff = buffer;
            for (nint i = 0; i < wide; i++)
            {
                getKeccakS(layer+1, i, S: out S, B: out B, C: out C);

                input(buff, MaxInputForKeccak, S, regime);
                buff += MaxInputForKeccak;
            }
        }

        // Последний уровень губки, включая преобразование обратной связи
        outputAllData();

        BytesBuilder.ToNull(ReserveConnectionLen, buffer);
        _countOfProcessedSteps++;
        lastRegime = regime;
    }
}
