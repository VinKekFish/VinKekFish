// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{
    public DirectoryInfo? RandomAtFolder;
    public DirectoryInfo? RandomAtFolder_Static;

    public VinKekFishBase_KN_20210525 VinKekFish    = new VinKekFishBase_KN_20210525(-1, 11, 1);
    public CascadeSponge_mt_20230930  CascadeSponge = new CascadeSponge_mt_20230930(10*1024);
    protected virtual void StartEntropy()
    {
        VinKekFish.input = new BytesBuilderStatic(MAX_RANDOM_AT_START_FILE_LENGTH);
        VinKekFish.Init1();
        VinKekFish.Init2();

        CreateFolders();
        GetStartupEntropy();
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
