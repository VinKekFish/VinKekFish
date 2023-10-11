// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Runtime.Serialization.Json;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{                                                                                           /// <summary>Этот объект используется для синхронизации доступа к объектам, накапливающим энтропию</summary>
    public readonly object entropy_sync = new object();
    public readonly AllocHGlobal_AllocatorForUnsafeMemory allocator = new AllocHGlobal_AllocatorForUnsafeMemory();

    protected virtual void StopEntropy()
    {
        lock (entropy_sync)
        {
            isInitiated = false;
            VinKekFish   .Dispose();
            CascadeSponge.Dispose();

            Monitor.PulseAll(entropy_sync);
        }
    }

    protected long ExecEntorpy_now = default;
    protected virtual void ExecEntropy()
    {
        lock (entropy_sync)
        {
            ExecEntorpy_now = DateTime.Now.Ticks;

            Monitor.PulseAll(entropy_sync);
        }
    }
}
