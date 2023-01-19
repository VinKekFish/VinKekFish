using utils.console;
using System.Diagnostics;

namespace builder;


partial class Program
{
    public static ErrorCode ExecuteBuild()
    {
        var cd  = Directory.GetCurrentDirectory();

        // ----------------  crypto primes  ----------------
        var (result, dir) = ExecuteBuildForProject(cd, "src/main/cryptoprime/", false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }







        // ----------------  All tests executor. MUST BE LAST  ----------------
        (result, dir) = ExecuteBuildForProject(cd, "src/tests/", false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }

        return ErrorCode.Success;
    }
}
