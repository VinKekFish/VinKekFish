// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{
    public DirectoryInfo? RandomAtFolder;
    public DirectoryInfo? RandomAtFolder_Static;

    public VinKekFishBase_KN_20210525 VinKekFish    = new VinKekFishBase_KN_20210525(VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(11), 11, 1);   // 275 == inKekFish.EXTRA_ROUNDS_K, K = 11
    public CascadeSponge_mt_20230930  CascadeSponge = new CascadeSponge_mt_20230930(10*1024);

    public bool isInitiated { get; protected set; } = false;

    protected unsafe virtual void StartEntropy()
    {
        ExecEntorpy_now  = DateTime.Now.Ticks;
        VinKekFish.input = new BytesBuilderStatic(MAX_RANDOM_AT_START_FILE_LENGTH);

        var arr = stackalloc byte[sizeof(long)];
        var rec = new Record() {array = arr, len = sizeof(long)};
        BytesBuilder.ULongToBytes((ulong) ExecEntorpy_now, arr, sizeof(long));

        if (VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0)
            throw new Exception("Regime_Service.StartEntropy: Fatal algorithmic error: VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0");

        VinKekFish.Init1
                        (
                            keyForPermutations: rec,
                            PreRoundsForTranspose: VinKekFish.EXTRA_ROUNDS_K - 3
                        ); // rec является синхропосылкой, но т.к. ключа нет, то rec вводится как ключ
        VinKekFish.Init2();

        CascadeSponge.InitEmptyThreeFish((ulong) ExecEntorpy_now);

        CreateFolders();
        GetStartupEntropy();
        isInitiated = true;
    }

    protected virtual void StopEntropy()
    {
        VinKekFish   .Dispose();
        CascadeSponge.Dispose();
    }

    protected long ExecEntorpy_now = default;
    protected virtual void ExecEntropy()
    {
        ExecEntorpy_now = DateTime.Now.Ticks;
    }
}
