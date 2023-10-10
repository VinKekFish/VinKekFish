// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{                                                                                           /// <summary>Этот объект используется для синхронизации доступа к объектам, накапливающим энтропию</summary>
    public object entropy_sync = new object();

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
            ExecEntorpy_now  = DateTime.Now.Ticks;
            VinKekFish.input = new BytesBuilderStatic(MAX_RANDOM_AT_START_FILE_LENGTH);

                var arr = stackalloc byte[sizeof(long)];
            using var rec = new Record() {array = arr, len = sizeof(long)};
            BytesBuilder.ULongToBytes((ulong) ExecEntorpy_now, arr, sizeof(long));

            if (VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0)
                throw new Exception("Regime_Service.StartEntropy: Fatal algorithmic error: VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0");

            Parallel.Invoke
            (
                () =>
                {
                    VinKekFish.Init1
                                    (
                                        keyForPermutations: rec,
                                        PreRoundsForTranspose: VinKekFish.EXTRA_ROUNDS_K - 3
                                    ); // rec является синхропосылкой, но т.к. ключа нет, то rec вводится как ключ
                    VinKekFish.Init2();
                },

                () =>
                {
                    CascadeSponge.InitEmptyThreeFish((ulong) ExecEntorpy_now);
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
