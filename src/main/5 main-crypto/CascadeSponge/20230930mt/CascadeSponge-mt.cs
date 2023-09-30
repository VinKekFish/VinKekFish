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
/// Это многопоточная реализация каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_mt_20230930: CascadeSponge_1t_20230905, IDisposable
{
    /// <summary>Создаёт каскадную губку с заданными параметрами</summary>
    /// <param name="_wide">Ширина каскадной губки, не менее MinWide и не менее CalcMinWide. Всегда должна быть чётной. Чем больше ширина, тем больше выход данных губки за один шаг.</param>
    /// <param name="_tall">Высота каскадной губки, не менее MinTall</param>
    /// <param name="_strenghtInBytes">Потребная стойкость губки в байтах (4096 битов стойкости - 512 байтов)</param>
    /// <param name="ThreadsNum">Количество потоков, которое будет обрабатывать губку</param>
    public CascadeSponge_mt_20230930(nint _strenghtInBytes = 192, nint _wide = 0, nint _tall = 0, nint ThreadsNum = -1):
            base(_strenghtInBytes, _wide, _tall)
    {
        if (ThreadsNum <= 0)
        {
            ThreadsNum = Environment.ProcessorCount;
        }

        if (ThreadsNum > wide)
            ThreadsNum = wide;

        this.ThreadsNum = ThreadsNum;
        Threads         = new Thread[ThreadsNum];
        ThreadsFunc     = new nint[ThreadsNum];
        for (nint i = 0; i < Threads.Length; i++)
        {
            var _i = i;
            Threads[i] = new Thread(  () => ThreadsFunction(_i)  );

            ThreadsFunc [i] = EmptyTaskSlot;
        }
        StartThreads();
    }

    public override void Dispose(bool fromDestructor = false)
    {
        if (!isDisposed)
        {
            doThreadsDispose();
        }

        base.Dispose(fromDestructor);
    }
}
