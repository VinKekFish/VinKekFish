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

                                                                                            /// <summary>Указывает папку, где содержатся данные, хранящиеся между запусками программы. В том числе, данные по рандомизации на старте</summary>
    public DirectoryInfo? RandomAtFolder;
    public DirectoryInfo? RandomAtFolder_Static;

    public VinKekFishBase_KN_20210525 VinKekFish    = new VinKekFishBase_KN_20210525(VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(11), 11, 1);   // 275 == inKekFish.EXTRA_ROUNDS_K, K = 11
    public CascadeSponge_mt_20230930  CascadeSponge = new CascadeSponge_mt_20230930(10*1024);

    public bool isInitiated { get; protected set; } = false;

    /// <summary>Функция вызывается для инициализации всех губок, накапливающих энтропию</summary>
    protected unsafe virtual void StartEntropy()
    {
        lock (entropy_sync)
        {
            if (VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0)
                throw new Exception("Regime_Service.StartEntropy: Fatal algorithmic error: VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0");

            ExecEntorpy_now  = DateTime.Now.Ticks;
            VinKekFish.input = new BytesBuilderStatic(MAX_RANDOM_AT_START_FILE_LENGTH);

            using var bufferRec = allocator.AllocMemory(MAX_RANDOM_AT_START_FILE_LENGTH);
            using var rndbytes  = new BytesBuilderForPointers();

            CascadeSponge.InitEmptyThreeFish((ulong) ExecEntorpy_now);
            CascadeSponge.step();
            CascadeSponge.InitThreeFishByCascade(1);

            checked
            {
                try
                {
                    var rndList  = options_service!.root!.input!.entropy!.os!.random;
                    foreach (var rnd in rndList)
                    {
                        var intervals = rnd.intervals!.intervals;
                        foreach (var interval in intervals)
                        {
                            if (interval.time == -1 || interval.time == 0)
                            {
                                if (string.IsNullOrEmpty(rnd.PathString))
                                    throw new Exception($"Regime_Service.StartEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': file name is empty. The random file name is required.");

                                var rndFileInfo = new FileInfo(rnd.PathString); rndFileInfo.Refresh();
                                var len         = (nint) rndFileInfo.Length;

                                if (interval.Length > 0)
                                    len = (nint) interval.Length;

                                if (len > MAX_RANDOM_AT_START_FILE_LENGTH)
                                    throw new Exception($"Regime_Service.StartEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': the file length too match. The length ({len}) of the random file must be lowest ${MAX_RANDOM_AT_START_FILE_LENGTH}.");

                                var bufferSpan = new Span<byte>(bufferRec, (int) len);
                                using (var rs = rndFileInfo.OpenRead())
                                {
                                    rs.Read(bufferSpan);
                                    rndbytes.addWithCopy(bufferRec << bufferRec.len - len);
                                }
Console.WriteLine($"len = {len}");
                                CascadeSponge.step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, data: bufferRec, dataLen: len, regime: 2);
                            }
                        }
                    }
                }
                catch
                {}
            }

            // Делаем первичную инициализацию временем при старте
                  var arr = stackalloc byte[sizeof(long) + (int) CascadeSponge.maxDataLen];
            using var rec = new Record() {array = arr, len = sizeof(long) + (int) CascadeSponge.maxDataLen};
            BytesBuilder.ULongToBytes((ulong) ExecEntorpy_now, arr, sizeof(long));

            CascadeSponge.step(CascadeSponge.countStepsForKeyGeneration, regime: 7);
            BytesBuilder.CopyTo(CascadeSponge.maxDataLen, rec.len, CascadeSponge.lastOutput, arr, sizeof(long));


            Parallel.Invoke
            (
                () =>
                {
                    VinKekFish.Init1
                    (
                        keyForPermutations: rec,
                        PreRoundsForTranspose: VinKekFish.EXTRA_ROUNDS_K - VinKekFish.Calc_OptimalRandomPermutationCount(rec.len),
                        ThreeFishInitSteps:    1
                    ); // rec является синхропосылкой, но т.к. ключа нет, то rec вводится как ключ
                    VinKekFish.Init2();
                },

                () =>
                {
                    CascadeSponge.step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, regime: 3, data: rec.array, dataLen: rec.len, inputRegime: CascadeSponge_1t_20230905.InputRegime.overwrite);
                    CascadeSponge.InitThreeFishByCascade(1);
                },

                CreateFolders
            );

            GetStartupEntropy();
            isInitiated = true;

            Monitor.PulseAll(entropy_sync);
        }
    }

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
