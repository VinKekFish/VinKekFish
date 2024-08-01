using static utils.DateTimeStrings;
using utils.console;

namespace builder;

public partial class Program
{
    static string   configuration = "Release";
    static string   testTags      = "";     // example: "fast mandatory -slow" (можно также разделять запятыми)
    static string   output        = "./build";
    static DateTime now           = DateTime.Now;
    static bool     no_restore    = true;
    static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            configuration = args[0];
        }

        if (args.Length > 1)
        {
            testTags = args[1];
        }

        if (args.Length > 2)
        {
            // var flags = args[2].Trim().ToLowerInvariant().Split(new string[] {" ", ",", ", "}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 2; i < args.Length; i++)
            {
                var flag = args[i];
                if (flag.StartsWith("-"))
                    flag = flag[1 ..];

                if (flag == "restore")
                    no_restore = false;
            }
        }

        using (var _ = new NotImportantConsoleOptions())
        {            
            Console.WriteLine($"Builder started at {GetTimeString(DateTime.Now)}");
        }

        // ---------------- Устанавливаем обработчики ошибок ----------------

        SetErrorHandlers();

        // ---------------- Компиляция ----------------

        var (resultCode, WillTests) = MainBuild();
        if (resultCode != ErrorCode.Success)
        {
            using (var _ = new ErrorConsoleOptions())
                Console.Error.Write($"{GetTimeString(DateTime.Now)}. Error during build");

            return (int) resultCode;
        }

        if (!WillTests)
            return (int) resultCode;


        using (var _ = new NotImportantConsoleOptions())
            Console.Write($"Tests started at {GetTimeString(DateTime.Now)}");

        // ---------------- Тесты ----------------
        resultCode = MainTests(testTags);
        if (resultCode != ErrorCode.Success)
        {
            using (var _ = new ErrorConsoleOptions())
            {
                Console.Error.WriteLine($"{GetTimeString(DateTime.Now)}. Error during tests");
                Console.Error.WriteLine($"Working dir {Directory.GetCurrentDirectory()}");
            }

            return (int) resultCode;
        }

        using (var _ = new NotErrorConsoleOptions())
            Console.Write($"Builder successfully ended at {GetTimeString(DateTime.Now)}");

        return (int) ErrorCode.Success;
    }

    public enum ErrorCode
    {
        /// <summary>Успех</summary>
        Success = 0,
        
        /// <summary>Ошибка, которая неверно преобразуется в ErrorCode</summary>
        Unknown = -1,

        /// <summary>Наличие файла builder.lock - это означает, что билд был неуспешным</summary>
        Builder_Lock = 1,

        /// <summary>Не удалось собрать какой-то из проектов по неизвестной ошибке (не ошибка компиляции)</summary>
        DotnetError = 2,

        /// <summary>Неверный файл конфигурации builder.conf</summary>
        InvalidConfigFile = 3,

        /// <summary>Сборка пропущена, т.к. нет изменений (успешный)</summary>
        SuccessActual = 4,

        /// <summary>Во время сборки проекта dotnet publish выдал ошибку</summary>
        ProjectBuildError = 5,

        /// <summary>Тесты не прошли успешно</summary>
        TestError = 6,

        /// <summary>Билдер не выполнил какие-либо специфические для проекта правила</summary>
        SpecificForProjectError = 7
    };

    public static void SetErrorHandlers()
    {
        Builder_lock_event       += Builder_Lock_ErrorHandler;
        Updated_file_found_event += Updated_File_Found_Handler;

        End_build_for_project_event += End_build_for_project_Handler;
    }

    public static void Builder_Lock_ErrorHandler(FileInfo builder_lock_file)
    {
        using (var bg = new ErrorConsoleOptions())
        {
            // Console.Error.Write("builder.lock file exists. ");
            Console.Error.Write("builder was changed. Lets execute build.sh");
        }

        // Console.Error.Write("The build was run twice or got a code error during building");
    }

    public static void Updated_File_Found_Handler(FileInfo updatedFile)
    {
        using (var opt = new NotErrorConsoleOptions())
        {
            Console.Write($"Updated file found: {updatedFile.FullName}");
        }
    }

    public static void End_build_for_project_Handler(ErrorCode code, DirectoryInfo updatedFile)
    {
        var ProjectName = updatedFile.FullName;

        var cd = Directory.GetCurrentDirectory();
        if (ProjectName.StartsWith(cd))
        {
            ProjectName = "." + Path.Combine("./", ProjectName.Substring(cd.Length));
        }

        if (code == ErrorCode.Success)
            using (var opt = new NotErrorConsoleOptions())
                Console.Error.Write($"Project builded: '{ProjectName}'");
        else
        if (code == ErrorCode.SuccessActual)
            using (var opt = new NotImportantConsoleOptions())
                Console.Error.Write($"Project skipped (up to date): '{ProjectName}'");
        else
            using (var opt = new ErrorConsoleOptions())
                Console.Error.Write($"Error '{code}' occured during build for project: '{ProjectName}'");
    }
}


public static class ErrorCode_helper
{
    public static bool IsErrorCode(this Program.ErrorCode code)
    {
        if (code == Program.ErrorCode.Success)
            return false;

        if (code == Program.ErrorCode.SuccessActual)
            return false;

        return true;
    }

    public static bool IsSuccessCode(this Program.ErrorCode code)
    {
        return !IsErrorCode(code);
    }
}
