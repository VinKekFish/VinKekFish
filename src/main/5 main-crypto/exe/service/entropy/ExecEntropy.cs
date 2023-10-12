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
            if (isInitiated)
                MandatoryWriteCurrentFile();

            isInitiated = false;
            VinKekFish   .Dispose();
            CascadeSponge.Dispose();

            Monitor.PulseAll(entropy_sync);
        }
    }

    public const long ticksPerSecond = 1000 * 10000;
    public const long ticksPerHour   = 3600 * ticksPerSecond;

    protected long ExecEntorpy_now = default;
    protected long LastCurrentFile = default;
    protected unsafe virtual void ExecEntropy()
    {
        lock (entropy_sync)
        {
            ExecEntorpy_now = DateTime.Now.Ticks;
            MayBeWriteCurrentFile();

            Monitor.PulseAll(entropy_sync);
        }
    }

    protected unsafe void MayBeWriteCurrentFile()
    {
        var lcf = ExecEntorpy_now - LastCurrentFile;
        if (lcf > 3600 * ticksPerSecond)
        {
            MandatoryWriteCurrentFile();
        }
    }
// TODO: Надо бы сюда время впихнуть как доп. энтропию. Пусть даже лишний бит, но чтобы был
    protected unsafe void MandatoryWriteCurrentFile()
    {
        using (var ws = RandomAtFolder_Current!.OpenWrite())
        {
            using (Record output = getEntropyForOut(OutputStrenght))
            {
                WriteRecordToFileStream(ws, output);
            }
        }
        LastCurrentFile = ExecEntorpy_now;
    }

    public static unsafe void WriteRecordToFileStream(FileStream ws, Record output, int size = 0)
    {
        checked
        {
            if (size == 0)
                size = (int) output.len;

            var span = new Span<byte>(output, size);
            ws.Write(span);
        }
    }

    public unsafe Record getEntropyForOut(int outputStrenght)
    {
        using var bb = new BytesBuilderStatic(outputStrenght + CascadeSponge.maxDataLen, allocator);

        var rec = allocator.AllocMemory(outputStrenght + sizeof(long));

        Parallel.Invoke
        (
            () =>
            {
                do
                {
                    BytesBuilder.ULongToBytes((ulong) DateTime.Now.Ticks, rec.array, sizeof(long));
                    CascadeSponge.step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, data: rec, dataLen: sizeof(long));
                    bb.add(CascadeSponge.lastOutput, CascadeSponge.maxDataLen >> 1);
                }
                while (bb.Count < outputStrenght);

                bb.getBytesAndRemoveIt(rec, outputStrenght);
            }
        );

        return rec;
    }
}
