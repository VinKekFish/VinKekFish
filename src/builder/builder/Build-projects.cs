using utils.console;
using System.Diagnostics;

namespace builder;


partial class Program
{
    public static ErrorCode ExecuteBuild()
    {
        var cd  = Directory.GetCurrentDirectory();


        // ----------------  BytesBuilder and ThreeFish slowly for ThreeFish generator  ----------------
        var (result, dir) = ExecuteBuildForProject(cd, "src/main/1 BytesBuilder/", inSingleFile: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }


        // ----------------  generator for crypto primes  ----------------
        var threefish_generated_FileName1 = "./build/Threefish_Static_Generated.cs";
        var threefish_generated_FileName2 = "./build/Threefish_Static_Generated2.cs";
        var threefish_generated_FileName1_copy = "src/main/3 cryptoprime/ThreeFish/Threefish_Static_Generated.cs";
        var threefish_generated_FileName2_copy = "src/main/3 cryptoprime/ThreeFish/Threefish_Static_Generated2.cs";

        var dt_threefish1 = new FileInfo(threefish_generated_FileName1).LastWriteTime;
        var dt_threefish2 = new FileInfo(threefish_generated_FileName2).LastWriteTime;
        if (dt_threefish1 < dt_threefish2)
            dt_threefish1 = dt_threefish2;

        (result, dir) = ExecuteBuildForProject(cd, "src/main/2 generator/", inSingleFile: true, lastModified: dt_threefish1);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }

        if (result != ErrorCode.SuccessActual)
        {
            File.Delete(threefish_generated_FileName1);
            File.Delete(threefish_generated_FileName2);

            var pi = Process.Start("build/generator", threefish_generated_FileName1 + " " + threefish_generated_FileName2);
            pi.WaitForExit(); 

            if (!File.Exists(threefish_generated_FileName1))
            {
                return ErrorCode.SpecificForProjectError;
            }
            if (!File.Exists(threefish_generated_FileName2))
            {
                return ErrorCode.SpecificForProjectError;
            }

            File.Delete(threefish_generated_FileName1_copy);
            File.Delete(threefish_generated_FileName2_copy);
            File.Copy(threefish_generated_FileName1, threefish_generated_FileName1_copy,  true);
            File.Copy(threefish_generated_FileName2, threefish_generated_FileName2_copy, true);
        }

        // ----------------  crypto primes  ----------------
        (result, dir) = ExecuteBuildForProject(cd, "src/main/3 cryptoprime/", inSingleFile: false);
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
