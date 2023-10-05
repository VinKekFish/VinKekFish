// TODO: tests
namespace vinkekfish;

using System.Diagnostics.Tracing;
using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using static CascadeSponge_1t_20230905;
using System.Text;

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
    public       object ThreadsStop  = new object();

    public readonly nint ThreadsCount = 0;

    /// <summary>Посылает сигнал на останов всех потоков. Каскадная губка не должна быть в это время в функции step или step_once</summary>
    protected virtual void doThreadsDispose()
    {
        // Блокируем возможность для других потоков начать шаг (хотя если шаг начат, то он может продолжаться)
        ThreadsError = true;

        // Ждём завершения задач потоков; если потоки не хотят завершаться, продолжаем дальше
        if (ThreadsExecuted > 0)
        {
            Record.errorsInDispose = true;
            Console.Error.WriteLine("CascadeSponge_mt_20230930.doThreadsDispose: ThreadsExecuted > 0");

            lock (ThreadsStop)
            {
                Monitor.PulseAll(ThreadsStop);      // Завершаем шаг, если так неповезло, что он, всё-таки, идёт (будит поток шага с установленным флагом ThreadsError)
                if (ThreadsExecuted > 0)
                    Monitor.Wait(ThreadsStop, 70);

                if (ThreadsExecuted > 0)
                {
                    Monitor.PulseAll(ThreadsStop);
                    Monitor.Wait(ThreadsStop, 250);
                }
            }
        }

        // Посылаем потокам нужный сигнал и ждём
        ThreadsExecuted = Threads.Length;
        SetEndTaskForAllThreads();
        Event.Set();

        var cnt = 0;
        while (ThreadsExecuted > 0 && cnt < 5)
        {
            Thread.Sleep(cnt * 50);
            cnt++;

            SetEndTaskForAllThreads();
            Event.Set();
        }

        lock (ThreadsStop)
            Monitor.PulseAll(ThreadsStop);
    }

    private void SetEndTaskForAllThreads()
    {
        for (nint i = 0; i < ThreadsFunc.Length; i += AlignmentMultipler)
            ThreadsFunc[i] = EndTask;
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

    protected ManualResetEvent Event = new ManualResetEvent(false);


    /// <summary>Функция, выполняемая потоками</summary>
    protected virtual void ThreadsFunction(nint ThreadIndex)
    {
        var ATI = ThreadIndex*AlignmentMultipler;
        while (ThreadsFunc[ATI] != EndTask)
        {
            if (ThreadsFunc[ATI] == EmptyTaskSlot)
            {
                Thread.Sleep(0);        // Event внутри цикла никогда не сбрасывается, так что немного экономим мощности
                Event.WaitOne();
            }

            if (ThreadsFunc[ATI] == EndTask)
            {
                Interlocked.Decrement(ref ThreadsExecuted);
                lock (ThreadsStop)
                    Monitor.PulseAll(ThreadsStop);

                return;
            }

            switch (ThreadsFunc[ATI])
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

    public int[] debug_t;
    //                                                        /// <summary>Текущий номер последнего необработанного индекса губки нужного слоя</summary>
    //protected volatile int KeccakNumberForThreads = 0;
    /// <summary>Функция преобразования keccak</summary>
    protected void Thread_keccak(nint ThreadIndex)
    {
        ThreadsFunc[ThreadIndex*AlignmentMultipler] = EmptyTaskSlot;

        nint index = ThreadIndex;

        byte * S, B, C, sb, sb2, buff = stackalloc byte[MaxInputForKeccak];
        byte * st = (byte *) stepBuffer;
        try
        {
            for (; index < wide; index += ThreadsCount)
            {
                // var index = Interlocked.Increment(ref KeccakNumberForThreads) - 1;
                // debug_t[ThreadIndex]++;

                if (curStepBuffer < 0)
                {
                    sb2 = st;
                    sb  = st + ReserveConnectionLen;
                }
                else
                {
                    sb  = st;
                    sb2 = st + ReserveConnectionLen;
                }

                getKeccakS(ThreadsLayer, index, S: out S, B: out B, C: out C);
                if (ThreadsLayer > 0)
                {
                    var si = index*MaxInputForKeccak;
                    nint j = 0, i = 0, bi = 0;

                    j   = si / wide;
                    i   = si % wide;
                    i  *= MaxInputForKeccak;
                    i  += j;

                    for (; bi < MaxInputForKeccak; bi++)
                    {
                        buff[bi] = sb[i];

                        i += MaxInputForKeccak;
                        if (i >= ReserveConnectionLen)
                        {
                            i = ++j;
                        }
                    }

                    input(buff, MaxInputForKeccak, S, regime);
                }

                KeccakPrime.Keccackf(a: (ulong*)S, c: (ulong*)C, b: (ulong*)B);

                if (ThreadsLayer < tall-1)
                KeccakPrime.Keccak_Output_512(sb2 + MaxInputForKeccak*index, MaxInputForKeccak, S: S);
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

    protected virtual void Clear_Debug_t()
    {
        for (nint i = 0; i < debug_t.Length; i++)
            debug_t[i] = 0;
    }

    protected virtual string toString_Debug_t()
    {
        var sb = new StringBuilder();

        foreach (var t in debug_t)
            sb.Append($"{t, 3} ");

        return sb.ToString();
    }
}
