using static utils.DateTimeStrings;
using utils.console;

namespace builder;

partial class Program
{
    static int Main(string[] args)
    {
        var builder_config_file_name = args.Length > 0 ? args[0] : "builder.conf";
        var builder_config_fi        = new FileInfo(builder_config_file_name);

        // Если файл с опциями не создан, создаём его
        if (!builder_config_fi.Exists)
        {
            File.WriteAllText
            (
                builder_config_fi.FullName,
                """
                configuration: Debug
                output: build

                [projects]
                """
            );

            using (var _ = new ErrorConsoleOptions())
                Console.Error.WriteLine($"Configuration file is not exists: '{builder_config_fi.FullName}'");

            return (int) ErrorCode.InvalidConfigFile;
        }

        var configFileLines = File.ReadAllLines(builder_config_fi.FullName);
        ParseConfigFile(configFileLines);

        using (var _ = new NotImportantConsoleOptions())
        {            
            Console.WriteLine($"Builder started at {getTimeString(DateTime.Now)}");
            Console.WriteLine($"Config file: '{builder_config_fi.FullName}");
            Console.WriteLine($"Projects count to build: {configuration["projects"].Values.Count}");

            var output_for_configuration = new string[] {"configuration", "output"};
            foreach (var confOptName in output_for_configuration)
            {
                if (!configuration.ContainsKey(confOptName))
                {
                    using (var _2 = new ErrorConsoleOptions())
                        Console.Error.WriteLine($"Is not specified required option: {confOptName}");

                    return (int) ErrorCode.InvalidConfigFile;
                }

                Console.Write($"{confOptName}: '{configuration[confOptName]}'; ");
            }
        }

        // ---------------- Устанавливаем обработчики ошибок ----------------

        SetErrorHandlers();

        // ---------------- Компиляция ----------------

        var ec = MainBuild();
        if (ec.resultCode != ErrorCode.Success)
        {
            using (var _ = new ErrorConsoleOptions())
                Console.Error.WriteLine($"{getTimeString(DateTime.Now)}. Error during build");

            return (int) ec.resultCode;
        }

        if (!ec.WillTests)
            return (int) ec.resultCode;


        using (var _ = new NotImportantConsoleOptions())
            Console.Write($"Tests started at {getTimeString(DateTime.Now)}");

        // ---------------- Тесты ----------------
        ec.resultCode = MainTests();
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

        /// <summary>Не удалось собрать какой-то из проектов</summary>
        DotnetError = 2,

        /// <summary>Неверный файл конфигурации builder.conf</summary>
        InvalidConfigFile = 3,

        /// <summary>Сборка пропущена, т.к. нет изменений</summary>
        SuccessActual = 4
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
            Console.Error.Write("builder.lock file exists. ");
        }

        Console.Error.Write("The build was run twice or got a code error during building");
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
