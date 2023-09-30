// TODO: tests
namespace vinkekfish;

using System.Diagnostics.Tracing;
using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using static CascadeSponge_1t_20230905;

// code::docs:rQN6ZzeeepyOpOnTPKAT:

public unsafe partial class CascadeSponge_mt_20230930: IDisposable
{
                                                                /// <summary>Потоки, выделенные для этой губки. Не более, чем wide</summary>
    protected Thread[]   Threads;
                                                                /// <summary>Общий номер задачи, который надо выполнить. -1 (EmptyTaskSlot) - нет задачи. EndTask - завершаем потоки.</summary>
    protected volatile nint[] ThreadsFunc;                      /// <summary>Номер слоя для обработки keccak</summary>
    protected volatile nint   ThreadsLayer    = EmptyTaskSlot;  /// <summary>Количество запущенных потоков</summary>
    protected volatile  int   ThreadsExecuted = 0;              /// <summary>Флаг произошедшей внутри потока ошибки</summary>
    protected volatile bool   ThreadsError    = false;

    public const nint   EndTask       = -7;
    public const nint   EmptyTaskSlot = -1;
    public       object ThreadsStart = new object();
    public       object ThreadsStop  = new object();

    public readonly nint ThreadsNum = 0;

    /// <summary>Посылает сигнал на останов всех потоков. Каскадная губка не должна быть в это время в функции step или step_once</summary>
    protected virtual void doThreadsDispose()
    {
        ThreadsError = true;
        for (nint i = 0; i < ThreadsFunc.Length; i++)
            ThreadsFunc[i] = EndTask;

        lock (ThreadsStart)
        Monitor.PulseAll(ThreadsStart);

        ThreadsExecuted = 0;
        lock (ThreadsStop)
        Monitor.PulseAll(ThreadsStop);
    }
                                                            /// <summary>Запускает все потоки</summary>
    protected virtual void StartThreads()
    {
        foreach (var t in Threads!)
        {
            if (t.ThreadState != ThreadState.Running && t.ThreadState != ThreadState.WaitSleepJoin)
                t.Start();
        }

        // Ожидаем запуска потоков
        // lock (this)
    }

    /// <summary>Функция, выполняемая потоками</summary>
    protected virtual void ThreadsFunction(nint ThreadIndex)
    {
        while (ThreadsFunc[ThreadIndex] != EndTask)
        {
            lock (ThreadsStart)
            {
                if (ThreadsFunc[ThreadIndex] == EmptyTaskSlot)
                    Monitor.Wait(ThreadsStart);      // Здесь может быть получение сигнала в холостую
            }

            if (ThreadsFunc[ThreadIndex] == EndTask)
                return;

            switch (ThreadsFunc[ThreadIndex])
            {
                case 1:
                        Thread_keccak(ThreadIndex);
                    break;
                case 2:
                        break;
                default:            // Пустая функция
                        break;
            }
        }
    }

    protected volatile int KeccakThreadNumber = 0;
    /// <summary>Функция преобразования keccak</summary>
    protected void Thread_keccak(nint ThreadIndex)
    {
        ThreadsFunc[ThreadIndex] = EmptyTaskSlot;
Console.WriteLine("start: " + ThreadIndex);
        byte * S, B, C;
        try
        {
            while (true)
            {
                var index = Interlocked.Increment(ref KeccakThreadNumber) - 1;

                if (index >= wide)
                    break;
Console.WriteLine(ThreadIndex);
                getKeccakS(ThreadsLayer, index, S: out S, B: out B, C: out C);
                KeccakPrime.Keccackf(a: (ulong*)S, c: (ulong*)C, b: (ulong*)B);
            }
        }
        catch
        {
            ThreadsError = true;
        }
        finally
        {
            var te = Interlocked.Decrement(ref ThreadsExecuted);

            if (te <= 0)
            lock (ThreadsStop)
                Monitor.PulseAll(ThreadsStop);
        }
    }
}
