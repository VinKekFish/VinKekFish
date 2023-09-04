using utils.console;
using System.Diagnostics;
using System.Text.RegularExpressions;
using utils;

namespace builder;


partial class Program
{
    public static ErrorCode ExecuteBuild()
    {
        var cd = Directory.GetCurrentDirectory();
        var bb = new SortedSet<string>();


        // ----------------  BytesBuilder and ThreeFish slowly for ThreeFish generator  ----------------
        var (result, dir) = ExecuteBuildForProject(cd, "src/main/1 BytesBuilder/", inSingleFile: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
            else
                bb.Add("1");
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
            else
                bb.Add("2");
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
            File.Copy(threefish_generated_FileName1, threefish_generated_FileName1_copy, true);
            File.Copy(threefish_generated_FileName2, threefish_generated_FileName2_copy, true);
        }

        // ----------------  crypto primes  ----------------
        (result, dir) = ExecuteBuildForProject(cd, "src/main/3 cryptoprime/", inSingleFile: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
            else
                bb.Add("3");
        }

        // ----------------  VinKekFish_Utils  ----------------
        (result, dir) = ExecuteBuildForProject(cd, "src/main/4 utils/", inSingleFile: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
            else
                bb.Add("4");
        }

        // ----------------  VinKekFish_Utils test-dev  ----------------
        (result, dir) = ExecuteBuildForProject(cd, "src/test-dev/SecureCompare/", inSingleFile: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }

        // ----------------  main-crypto ----------------
        (result, dir) = ExecuteBuildForProject(cd, "src/main/5 main-crypto/", inSingleFile: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
            else
                bb.Add("5");
        }


        // ----------------  vkf: главная консольная программа ----------------
        var vkf_folder = "src/main/6 vkf";
        var ckf_vFile = "/Program-version.cs";
        var versionStr = "public static readonly string ProgramVersion_RiWiWak6ObEcc =";
        var vkf_program = File.ReadAllText(vkf_folder + ckf_vFile);
        var index = vkf_program.IndexOf(versionStr);
        var endIndex = vkf_program.IndexOf("\n", startIndex: index);
        vkf_program = vkf_program.Substring(0, length: index)
                    + versionStr + "\"" + DateTimeStrings.getDateVersionString(DateTime.Now) + "\";"
                    + vkf_program.Substring(endIndex);
        File.WriteAllText(vkf_folder + ckf_vFile, vkf_program);

        // Копируем файл с опциями в директорию билда
        CopyFiles("src/main/5 main-crypto/exe/service/", "*.options");
        CopyFiles("src/main/4 utils/languages/locales",  "*.loc", "build/locales");


        // Строим проект
        (result, dir) = ExecuteBuildForProject(cd, vkf_folder, inSingleFile: true, isActualCheck: false, SelfContained: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }




        // ----------------  All tests executor. MUST BE LAST  ----------------
        // Кроме одноразовых тестов test-dev
        (result, dir) = ExecuteBuildForProject(cd, "src/tests/", false, isActualCheck: false);
        end_build_for_project_event?.Invoke(result, dir);

        if (result != ErrorCode.Success)
        {
            if (result != ErrorCode.SuccessActual)
                return result;
        }

        return ErrorCode.Success;
    }

    public static void CopyFiles(string dirPath, string searchPattern, string buildPath = "build")
    {
        var optionsFilePath = new DirectoryInfo(dirPath);
        var fi_options = optionsFilePath.GetFiles(searchPattern, SearchOption.AllDirectories);
        var destDir    = new DirectoryInfo(buildPath); destDir.Refresh();
        if (!destDir.Exists)
            destDir.Create();

        foreach (var optionFile in fi_options)
            optionFile.CopyTo(Path.Combine(destDir.FullName, optionFile.Name), true);
    }
}
