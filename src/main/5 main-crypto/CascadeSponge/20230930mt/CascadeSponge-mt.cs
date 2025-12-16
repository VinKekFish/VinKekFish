// TODO: tests
namespace vinkekfish;

using System.Diagnostics.Tracing;
using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using static CascadeSponge_1t_20230905;

// code::docs:rQN6ZzeeepyOpOnTPKAT:  Это главный файл многопоточной реализации
// code::docs:zp7BtuFYcRWs29lwOSWY:  Это главный файл многопоточной реализации

// ::cp:alg:a7L6XjXsuwWGVxwJSN1x.main:20230930

/// <summary>
/// Это многопоточная реализация каскадной губки. Не имеет смысла использовать при wide < 8.
/// <remarks>См. также класс GetDataFromCascadeSponge (обёртка для удобного получения данных из этой губки).</remarks>
/// </summary>
public unsafe partial class CascadeSponge_mt_20230930: CascadeSponge_1t_20230905, IDisposable
{
    protected Record? stepBuffer;
    protected int     curStepBuffer = 1;

    protected const nint AlignmentMultipler = 16;

    /// <summary>Создаёт каскадную губку с заданными параметрами</summary>
    /// <param name="_wide">Ширина каскадной губки, не менее MinWide и не менее CalcMinWide. Всегда должна быть чётной. Чем больше ширина, тем больше выход данных губки за один шаг.</param>
    /// <param name="_tall">Высота каскадной губки, не менее MinTall</param>
    /// <param name="_strenghtInBytes">Потребная стойкость губки в байтах (4096 битов стойкости - 512 байтов)</param>
    /// <param name="ThreadsCount">Количество потоков, которое будет обрабатывать губку.</param>
    public CascadeSponge_mt_20230930(nint _strenghtInBytes = 192, nint _wide = 0, nint _tall = 0, nint ThreadsCount = -1):
            base(_strenghtInBytes, _wide, _tall)
    {
        if (ThreadsCount <= 0)
        {
            ThreadsCount = Environment.ProcessorCount - 1;
        }

        if (ThreadsCount > wide >> 1)
            ThreadsCount = wide >> 1;
        if (ThreadsCount > Environment.ProcessorCount)
            ThreadsCount = Environment.ProcessorCount - 1;

        debug_t = new int[ThreadsCount];

        this.ThreadsCount = ThreadsCount;
        Threads           = new Thread[ThreadsCount-1];   // Основной поток тоже занят вычислениями, поэтому мы создаём на 1 поток меньше. Основной поток имеет последний индекс
        ThreadsFunc       = new nint[ThreadsCount * AlignmentMultipler];
        for (nint i = 0; i < Threads.Length; i++)
        {
            var _i = i;
            Threads[i] = new Thread(  () => ThreadsFunction(_i)  );
            Threads[i].IsBackground = true;
            Threads[i].Priority     = ThreadPriority.Highest;

            ThreadsFunc[i*AlignmentMultipler] = EmptyTaskSlot;
        }
        StartThreads();

        stepBuffer = Keccak_abstract.allocator.AllocMemory(ReverseConnectionLen*2, "CascadeSponge_mt_20230930: stepBuffer");
    }

    public override void Dispose(bool fromDestructor = false)
    {
        try
        {
            if (!isDisposed)
            {
                DoThreadsDispose();

                VinKekFish_Utils.Utils.TryToDispose(stepBuffer);
                stepBuffer = null;

                if (ThreadsExecuted <= 0)
                    Event.Close();
                else
                {
                    Record .ErrorsInDispose = true;
                    Console.Error.WriteLine($"CascadeSponge_mt_20230930.Dispose: ThreadsExecuted > 0 ({ThreadsExecuted})");
                }
            }
        }
        finally
        {
            base.Dispose(fromDestructor);
        }
    }
}
