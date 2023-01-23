using static utils.DateTimeStrings;
using utils.console;

namespace builder;

public partial class Program
{
    static string   configuration = "Release";
    static string   testTags      = "";     // example: "fast mandatory -slow" (можно также разделять запятыми)
    static string   output        = "./build";
    static DateTime now           = DateTime.Now;
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

        using (var _ = new NotImportantConsoleOptions())
        {            
            Console.WriteLine($"Builder started at {getTimeString(DateTime.Now)}");
        }

        // ---------------- Устанавливаем обработчики ошибок ----------------

        SetErrorHandlers();

        // ---------------- Компиляция ----------------

        var ec = MainBuild();
        if (ec.resultCode != ErrorCode.Success)
        {
            using (var _ = new ErrorConsoleOptions())
                Console.Error.Write($"{getTimeString(DateTime.Now)}. Error during build");

            return (int) ec.resultCode;
        }

        if (!ec.WillTests)
            return (int) ec.resultCode;


        using (var _ = new NotImportantConsoleOptions())
            Console.Write($"Tests started at {getTimeString(DateTime.Now)}");

        // ---------------- Тесты ----------------
        ec.resultCode = MainTests(testTags);
        if (ec.resultCode != ErrorCode.Success)
        {
            using (var _ = new ErrorConsoleOptions())
                Console.Error.WriteLine($"{getTimeString(DateTime.Now)}. Error during tests");

            return (int) ec.resultCode;
        }

        using (var _ = new NotErrorConsoleOptions())
            Console.Write($"Builder successfully ended at {getTimeString(DateTime.Now)}");

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
        builder_lock_event       += Builder_Lock_ErrorHandler;
        updated_file_found_event += Updated_File_Found_Handler;

        end_build_for_project_event += end_build_for_project_Handler;
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

    public static void end_build_for_project_Handler(ErrorCode code, DirectoryInfo updatedFile)
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
    public static bool isErrorCode(this Program.ErrorCode code)
    {
        if (code == Program.ErrorCode.Success)
            return false;

        if (code == Program.ErrorCode.SuccessActual)
            return false;

        return true;
    }

    public static bool isSuccessCode(this Program.ErrorCode code)
    {
        return !isErrorCode(code);
    }
}
