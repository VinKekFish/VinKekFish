using utils.console;
using System.Diagnostics;

namespace builder;


partial class Program
{
    public static ErrorCode ExecuteBuild()
    {
        var cd  = Directory.GetCurrentDirectory();


        // ----------------  generator for crypto primes  ----------------
        var (result, dir) = ExecuteBuildForProject(cd, "src/main/generator/", inSingleFile: true);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }

        if (result != ErrorCode.SuccessActual)
        {
            var threefish_generated_FileName = "./build/Threefish_Static_Generated.cs";
            File.Delete(threefish_generated_FileName);

            var pi = Process.Start("build/generator", threefish_generated_FileName);
            pi.WaitForExit();

            if (!File.Exists(threefish_generated_FileName))
            {
                return ErrorCode.SpecificForProjectError;
            }

            File.Copy(threefish_generated_FileName, "src/main/cryptoprime/ThreeFish/Threefish_Static_Generated.cs", true);
        }

        // ----------------  crypto primes  ----------------
        (result, dir) = ExecuteBuildForProject(cd, "src/main/cryptoprime/", inSingleFile: false);
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
