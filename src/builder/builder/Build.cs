using utils.console;
using System.Diagnostics;

namespace builder;

partial class Program
{
    public static ErrorCode ExecuteBuild()
    {
        var cd  = Directory.GetCurrentDirectory();

        // var Projects = new string[] {"libs/utils", "tree-AST/lib"};
        var Projects = configuration["projects"].Values;

        foreach (var curProject in Projects)
        {
            var (result, dir) = ExecuteBuildForProject(cd, curProject);
            end_build_for_project_event?.Invoke(result, dir);

            if (result != ErrorCode.Success)
            {
                if (result != ErrorCode.SuccessActual)
                    return result;
            }
        }

        return ErrorCode.Success;
    }

    public static (ErrorCode, DirectoryInfo) ExecuteBuildForProject(string currentDirectoryPath, string projectRelativePath)
    {
        var configurationForDotNet = configuration["configuration"].ToString();
        var output                 = configuration["output"].ToString();
            output                 = Path.Combine(currentDirectoryPath, output);
        var output_di              = new DirectoryInfo(output);

        var pd  = Path.Combine(currentDirectoryPath, projectRelativePath);
        var di  = new DirectoryInfo(pd);

        var projectConfFile = new FileInfo(Path.Combine(di.FullName, $"builder-{configurationForDotNet}.pconf"));
        if (projectConfFile.Exists)
        {
            using (var opt = new NotImportantConsoleOptions())
            {
                Console.Write($"Project configuration file {projectConfFile.Name} exists");
            }
        }

        var csProjects  = di.GetFiles("*.csproj");
        var dllPatterns = new List<string>();
        foreach (var dll in csProjects)
        {
            var dllName = dll.Name.Substring(0, dll.Name.Length - ".csproj".Length);
            dllName += ".dll";

            dllPatterns.Add(dllName);
        }

        var isActual  = isActualVersion(output_di, di,dllPatterns, null);

        if (isActual)
        {
            updated_project_found_event?.Invoke(di);
            return (ErrorCode.SuccessActual, di);
        }
        else
        {
            updated_project_found_event?.Invoke(di);
        }

        var args = $"publish --configuration {configurationForDotNet} --output \"{output}\"";
        var psi  = new ProcessStartInfo("dotnet", args);
        psi.WorkingDirectory = di.FullName;

        // Console.WriteLine(args);

        var ps  = Process.Start(psi);
        if (ps == null)
            return (ErrorCode.DotnetError, di);

        ps.WaitForExit();


        return (ErrorCode.Success, di);
    }

    protected static string[] isActualVersion_sourcePattern_cs = new string[] { "*.cs" };

    /// <summary>Проверяет, что исходники не старше, чем файлы библиотек</summary>
    /// <param name="output_di">Директория, где мы ищем dll</param>
    /// <param name="sources_di">Директория проекта, которую мы хотим проверить</param>
    /// <param name="sourcePattern">Шаблон для поиска исходников. Если null, то шаблон будет "*.cs"</param>
    /// <param name="patterForProjectFile">Шаблон для поиска dll-файлов, дата создания которых проверяется</param>
    /// <returns>true - если версия актуальна и перестроение не требуется; false - если требуется перестроение</returns>
    public static bool isActualVersion(DirectoryInfo output_di, DirectoryInfo sources_di, List<string> patterForProjectFile, string[]? sourcePattern = null)
    {
        if (patterForProjectFile.Count <= 0)
            throw new ArgumentException("patterForProjectFile must be contain at least one string");

        sourcePattern ??= isActualVersion_sourcePattern_cs;

        var files = new List<FileInfo>(16);
        var bins  = new List<FileInfo>(4);

        GetFilesForPatterns(sources_di, sourcePattern,        files);
        GetFilesForPatterns(output_di,  patterForProjectFile, bins);

        if (files.Count <= 0)
            throw new ArgumentException($"isActualVersion: source pattern '{String.Join(", ", sourcePattern)}' is incorrect: 0 source files found");

        if (bins.Count <= 0)
            return false;

        var first = bins[0].LastWriteTimeUtc;

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
                // Console.WriteLine($"Updated file found: {file.FullName}");
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

    public class ConfigOption
    {
        public readonly string       Name;
        public readonly List<string> Values;

        public ConfigOption(string Name, params string[] values)
        {
            this.Name   = Name;

            if (values != null && values.Length > 0)
                this.Values = new List<string>(values);
            else
                this.Values = new List<string>();
        }

        public override string ToString()
        {
            if (Values.Count > 0)
                return Values[0];
            else
                return "";
        }
    }

    public static SortedDictionary<string, ConfigOption> configuration = new SortedDictionary<string, ConfigOption>();
    public static void ParseConfigFile(string[] configFileLines)
    {
        ConfigOption? currentOption = null;
        for (int i = 0; i < configFileLines.Length; i++)
        {
            var curLine = configFileLines[i].Trim();
            if (curLine.Length <= 0)
                continue;
            if (curLine.StartsWith("#"))
                continue;

            // Если декларируется массив, например [projects]
            if (curLine.StartsWith("[") && curLine.EndsWith("]"))
            {
                var Name = curLine  .Substring(1, curLine.Length - 2)
                                    .Trim().ToLowerInvariant();
                currentOption = getConfigurationOptionByName(Name);
            }
            else
            {
                var splitted = curLine.Split(':', 2);
                TrimStringArray(splitted);
                // Если строка вида Name: value
                if (splitted.Length > 1)
                {
                    currentOption = getConfigurationOptionByName(splitted[0].ToLowerInvariant());
                    currentOption.Values.Add(splitted[1]);
                }
                else
                {
                    if (currentOption == null)
                        throw new Exception($"A config file error found at line {i+1}: an array item '{curLine}', but no have the array");

                    currentOption.Values.Add(splitted[0]);
                }
            }
        }
    }

    protected static ConfigOption getConfigurationOptionByName(string Name)
    {
        ConfigOption? currentOption;
        if (configuration.ContainsKey(Name))
        {
            currentOption = configuration[Name];
        }
        else
        {
            currentOption = new ConfigOption(Name);
            configuration.Add(Name, currentOption);
        }

        return currentOption;
    }

    protected static void TrimStringArray(string[] splitted)
    {
        for (int j = 0; j < splitted.Length; j++)
            splitted[j] = splitted[j].Trim();
    }
}
