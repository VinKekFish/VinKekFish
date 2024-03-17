using utils.console;
using System.Diagnostics;
using static utils.DateTimeStrings;

namespace builder;

partial class Program
{
    public static (ErrorCode, DirectoryInfo) ExecuteBuildForProject(string currentDirectoryPath, string projectRelativePath, bool inSingleFile = true, bool isActualCheck = true, DateTime lastModified = default, bool SelfContained = false)
    {
        var configurationForDotNet = Program.configuration;
        var output                 = Program.output;
            output                 = Path.Combine(currentDirectoryPath, output);
        var output_di              = new DirectoryInfo(output);

        var pd  = Path.Combine(currentDirectoryPath, projectRelativePath);
        var di  = new DirectoryInfo(pd);

        var csProjects  = di.GetFiles("*.csproj");
        if (csProjects.Length <= 0)
            throw new Exception($"*.csproj not found in {di.FullName}");

        var dllPatterns = new List<string>();
        foreach (var dll in csProjects)
        {
            var dllName = dll.Name.Substring(0, dll.Name.Length - ".csproj".Length);
            dllPatterns.Add(dllName);

            dllName += ".dll";
            dllPatterns.Add(dllName);
        }

        var isActual  = isActualCheck ? isActualVersion(output_di, di, dllPatterns, null, lastModified) : false;

        if (isActual)
        {
            // updated_project_found_event?.Invoke(di);
            return (ErrorCode.SuccessActual, di);
        }
        else
        {
            updated_project_found_event?.Invoke(di);
        }

        var no_restore_string = "";
        if (Program.no_restore)
            no_restore_string = "--no-restore";

        var buildVersion = getDateVersionString(Program.now);
        var inSingleFileString = inSingleFile ? "/p:PublishSingleFile=true" : "";
        var args = $"publish {no_restore_string} --configuration {configurationForDotNet} --output \"{output}\" -p:Version={buildVersion} --self-contained {SelfContained} --use-current-runtime false {inSingleFileString}";

        var psi  = new ProcessStartInfo("dotnet", args);
        psi.WorkingDirectory = di.FullName;

        // Console.WriteLine(args);

        var ps  = Process.Start(psi);
        if (ps == null)
            return (ErrorCode.DotnetError, di);

        ps.WaitForExit();
        if (ps.ExitCode != 0)
            return (ErrorCode.ProjectBuildError, di);

        return (ErrorCode.Success, di);
    }

    protected static string[] isActualVersion_sourcePattern_cs = new string[] { "*.cs", "*.loc" };

    /// <summary>Проверяет, что исходники не старше, чем файлы библиотек</summary>
    /// <param name="output_di">Директория, где мы ищем dll</param>
    /// <param name="sources_di">Директория проекта, которую мы хотим проверить</param>
    /// <param name="sourcePattern">Шаблон для поиска исходников. Если null, то шаблон будет "*.cs"</param>
    /// <param name="patternForProjectFile">Шаблон для поиска dll-файлов, дата создания которых проверяется</param>
    /// <returns>true - если версия актуальна и перестроение не требуется; false - если требуется перестроение</returns>
    public static bool isActualVersion(DirectoryInfo output_di, DirectoryInfo sources_di, List<string> patternForProjectFile, string[]? sourcePattern = null, DateTime lastModified = default)
    {
        if (patternForProjectFile.Count <= 0)
            throw new ArgumentException($"patterForProjectFile must be contain at least one string ({sources_di.FullName})");

        sourcePattern ??= isActualVersion_sourcePattern_cs;

        var files = new List<FileInfo>(16);
        var bins  = new List<FileInfo>(4);

        GetFilesForPatterns(sources_di, sourcePattern,        files);
        GetFilesForPatterns(output_di,  patternForProjectFile, bins);

        if (files.Count <= 0)
            throw new ArgumentException($"isActualVersion: source pattern '{String.Join(", ", sourcePattern)}' is incorrect: 0 source files found");

        if (bins.Count <= 0)
        {
            Console.WriteLine($"No binaries in {output_di.FullName} for {sources_di.FullName} [need to build]");
            return false;
        }

        var first = bins[0].LastWriteTimeUtc;
        if (lastModified > first)
            first = lastModified;

        // Ищем самый старый исполняемый файл
        foreach (var bin in bins)
        {
            var timeOfTheBin = bin.LastWriteTimeUtc;
            if (timeOfTheBin < first)
                first = timeOfTheBin;
        }

        foreach (var file in files)
        {
            // Если хотя бы один из файлов-исходников новее, то версия бинарных файлов не актуальна
            if (file.LastWriteTimeUtc >= first)
            {
                using (var opt = new NotErrorConsoleOptions())
                    Console.Write($"{sources_di.Name}");

                Console.WriteLine($"\nUpdated file found: {file.FullName}");
                return false;
            }
        }

        return true;
    }

    protected static void GetFilesForPatterns(DirectoryInfo di, IEnumerable<string> patterForFile, List<FileInfo> files)
    {
        foreach (var pattern in patterForFile)
        {
            var fl = di.GetFiles(pattern, SearchOption.AllDirectories);
            files.AddRange(fl);
        }
    }
/*
    protected static void TrimStringArray(string[] splitted)
    {
        for (int j = 0; j < splitted.Length; j++)
            splitted[j] = splitted[j].Trim();
    }
*/
}
