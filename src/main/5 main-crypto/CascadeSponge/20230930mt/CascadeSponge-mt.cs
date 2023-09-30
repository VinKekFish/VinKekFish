// TODO: tests
namespace vinkekfish;

using System.Diagnostics.Tracing;
using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using static CascadeSponge_1t_20230905;

// code::docs:rQN6ZzeeepyOpOnTPKAT:  Это главный файл многопоточной реализации

// ::cp:alg:a7L6XjXsuwWGVxwJSN1x.main:20230930

/// <summary>
/// Это многопоточная реализация каскадной губки. Почти не имеет смысла использовать при wide < 32. Не стоит устанавливать ThreadsCount в слишком большую величину, т.к. производительность от этого может даже ухудшиться.
/// </summary>
public unsafe partial class CascadeSponge_mt_20230930: CascadeSponge_1t_20230905, IDisposable
{
    protected Record? stepBuffer;
    protected int     curStepBuffer = 1;

    /// <summary>Создаёт каскадную губку с заданными параметрами</summary>
    /// <param name="_wide">Ширина каскадной губки, не менее MinWide и не менее CalcMinWide. Всегда должна быть чётной. Чем больше ширина, тем больше выход данных губки за один шаг.</param>
    /// <param name="_tall">Высота каскадной губки, не менее MinTall</param>
    /// <param name="_strenghtInBytes">Потребная стойкость губки в байтах (4096 битов стойкости - 512 байтов)</param>
    /// <param name="ThreadsCount">Количество потоков, которое будет обрабатывать губку. Не стоит устанавливать ThreadsCount в слишком большую величину, т.к. производительность от этого может даже ухудшиться.</param>
    public CascadeSponge_mt_20230930(nint _strenghtInBytes = 192, nint _wide = 0, nint _tall = 0, nint ThreadsCount = -1):
            base(_strenghtInBytes, _wide, _tall)
    {
        if (ThreadsCount <= 0)
        {
            ThreadsCount = Environment.ProcessorCount;
        }

        if (ThreadsCount > wide)
            ThreadsCount = wide;

        debug_t = new int[ThreadsCount];

        this.ThreadsCount = ThreadsCount;
        Threads         = new Thread[ThreadsCount-1];   // Основной поток тоже занят вычислениями, поэтому мы создаём на 1 поток меньше. Основной поток имеет последний индекс
        ThreadsFunc     = new nint[ThreadsCount];
        for (nint i = 0; i < Threads.Length; i++)
        {
            var _i = i;
            Threads[i] = new Thread(  () => ThreadsFunction(_i)  );

            ThreadsFunc [i] = EmptyTaskSlot;
        }
        StartThreads();

        stepBuffer = Keccak_abstract.allocator.AllocMemory(ReserveConnectionLen*2, "CascadeSponge_mt_20230930: stepBuffer");
    }

    public override void Dispose(bool fromDestructor = false)
    {
        try
        {
            if (!isDisposed)
            {
                doThreadsDispose();
                stepBuffer?.Dispose();
                stepBuffer = null;
            }
        }
        finally
        {
            base.Dispose(fromDestructor);
        }
    }
}
