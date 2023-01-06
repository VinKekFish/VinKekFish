using utils.console;
using System.Diagnostics;

namespace builder;


partial class Program
{
    public static ErrorCode ExecuteBuild()
    {
        var cd  = Directory.GetCurrentDirectory();
/*
        // ----------------    ----------------
        var (result, dir) = ExecuteBuildForProject(cd, "src/builder/builder/");
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }
*/
        return ErrorCode.Success;
    }
}
