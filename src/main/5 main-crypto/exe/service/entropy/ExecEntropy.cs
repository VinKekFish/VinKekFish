// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Runtime.Serialization.Json;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public partial class Regime_Service
{                                                                                           /// <summary>Этот объект используется для синхронизации доступа к объектам, накапливающим энтропию</summary>
    public readonly object entropy_sync = new();
    public readonly AllocHGlobal_AllocatorForUnsafeMemory allocator = new();

    protected virtual void StopEntropy()
    {
        lock (entropy_sync)
        {
            InputEntropyFromSourcesWhile(int.MaxValue, 0);
            ConditionalInputEntropyToMainSponges(nint.MaxValue, true);

            if (IsInitiated)
                MandatoryWriteCurrentFile();

            IsInitiated = false;
            TryToDispose(VinKekFish);
            TryToDispose(CascadeSponge);
            TryToDispose(bufferRec);

            Monitor.PulseAll(entropy_sync);
        }
    }

    public const long ticksPerSecond = 1000 * 10000;
    public const long ticksPerHour   = 3600 * ticksPerSecond;

    protected long ExecEntorpy_now = default;
    protected long LastCurrentFile = default;
    protected unsafe virtual void ExecEntropy()
    {
        // Эта функция сама получает блокировку entropy_sync
        if (InputEntropyFromSourcesWhile() <= 0)
            return;

        lock (entropy_sync)
        {
            ExecEntorpy_now = DateTime.Now.Ticks;

            MayBeWriteCurrentFile();

            Monitor.PulseAll(entropy_sync);
        }
    }
}
