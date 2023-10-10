// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{
    protected virtual void StopEntropy()
    {
    }

    protected virtual void StartEntropy()
    {
    }

    protected long ExecEntorpy_now = default;
    protected virtual void ExecEntropy()
    {
        ExecEntorpy_now = DateTime.Now.Ticks;
    }
}
