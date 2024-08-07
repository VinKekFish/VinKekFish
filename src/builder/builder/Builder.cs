using System.Diagnostics;

namespace builder;

partial class Program
{
    public static (ErrorCode resultCode, bool WillTests) MainBuild()
    {
        End_build_for_project_event += (ErrorCode code, DirectoryInfo updatedDir) =>
        {
            if (code.IsErrorCode())
                Console.Error.WriteLine($"Error during the build process for '{updatedDir.FullName}' with error code {code}");
        };

        if (!CheckVersionOfBuildProgramm())
        {
            return (ExecuteFullBuild(), false);
        }

        var result = ExecuteBuild();

        return (result, true);
    }

    /// <summary>Проверяем, актуальна ли на нынешний момент программа билда</summary>
    /// <param name="di">Директория для проверки</param><param name="fi">Бинарный файл для проверки</param> <summary>
    /// <returns>true - если билдер актуален; иначе false</returns>
    public static bool CheckVersionOfBuildProgramm()
    {
        // var pathToFile = typeof(Program).Assembly.Location;
        // var pathToFile = System.AppContext.BaseDirectory;
        // Console.WriteLine(pathToFile);

        // Запуск должен происходить из директории VinKekFish (корневая папка репозитория)
        var cd          = Directory.GetCurrentDirectory();
        var builderPath = Path.Combine(cd, "src/builder/");

        var di    = new DirectoryInfo(builderPath);                          // Исходники builder
        var fi    = new FileInfo(Path.Combine(cd, "build/builder/builder")); // Файл для запуска
        var last  = fi.LastWriteTimeUtc;
        var files = di.GetFiles("*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (file.LastWriteTimeUtc >= last)
            {
                // Console.WriteLine($"Updated file found: {file.FullName}");
                Updated_file_found_event?.Invoke(file);
                return false;
            }
        }

        return true;
    }

    public delegate void  Builder_Lock_Event         (FileInfo      builder_lock_file);
    public delegate void  Updated_File_Found_Event   (FileInfo      updatedFile);
    public delegate void  Updated_Project_Found_Event(DirectoryInfo updatedDir);
    public delegate void  Build_Project_Event        (ErrorCode code, DirectoryInfo updatedDir);
                                                                                    /// <summary>Вызывается, если обнаружен файл builder.lock</summary>
    public static event Builder_Lock_Event?          Builder_lock_event;            /// <summary>Вызывается, если в проекте builder обнаружено обновление: требуется полная перестройка проекта builder</summary>
    public static event Updated_File_Found_Event?    Updated_file_found_event;      /// <summary>Вызывается, если в любом проекте обнаружено обновление: требуется полная перестройка проекта</summary>
    public static event Updated_Project_Found_Event? Updated_project_found_event;   /// <summary>Вызывается, после того, как мы построили проект</summary>
    public static event Build_Project_Event?         End_build_for_project_event;

    public static ErrorCode ExecuteFullBuild()
    {
        Builder_lock_event?.Invoke(new FileInfo("builder.lock"));
        return ErrorCode.Builder_Lock;
        /*
        var fi = new FileInfo("builder.lock");
        if (fi.Exists)
        {
            var b = builder_lock_event;
            if (b != null)
                b(fi);

            return ErrorCode.Builder_Lock;
        }

        try
        {
            using var fh = File.Open("builder.lock", FileMode.CreateNew, FileAccess.Write, FileShare.None);

            var psi = Process.Start("bash", "./b/build.sh");
            psi.WaitForExit();

            return (ErrorCode) psi.ExitCode;
        }
        finally
        {
            File.Delete("builder.lock");
        }*/
    }
}
