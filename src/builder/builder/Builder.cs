using System.Diagnostics;

namespace builder;

partial class Program
{
    public static (ErrorCode resultCode, bool WillTests) MainBuild()
    {
        if (!checkVersionOfBuildProgramm())
        {
            return (ExecuteFullBuild(), false);
        }

        var result = ExecuteBuild();

        return (result, true);
    }

    /// <summary>Проверяем, актуальна ли на нынешний момент программа билда</summary>
    /// <param name="di">Директория для проверки</param><param name="fi">Бинарный файл для проверки</param> <summary>
    /// <returns>true - если билдер актуален; иначе false</returns>
    public static bool checkVersionOfBuildProgramm()
    {
        var pathToFile = typeof(Program).Assembly.Location;
        // Console.WriteLine(pathToFile);

        var builderPath = Directory.GetCurrentDirectory();
            builderPath = Path.Combine(builderPath, "builder");

        var di    = new DirectoryInfo(builderPath);
        var fi    = new FileInfo(pathToFile);
        var last  = fi.LastWriteTimeUtc;
        var files = di.GetFiles("*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (file.LastWriteTimeUtc >= last)
            {
                // Console.WriteLine($"Updated file found: {file.FullName}");
                updated_file_found_event?.Invoke(file);
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
    public static event Builder_Lock_Event?          builder_lock_event;            /// <summary>Вызывается, если в проекте builder обнаружено обновление: требуется полная перестройка проекта builder</summary>
    public static event Updated_File_Found_Event?    updated_file_found_event;      /// <summary>Вызывается, если в любом проекте обнаружено обновление: требуется полная перестройка проекта</summary>
    public static event Updated_Project_Found_Event? updated_project_found_event;   /// <summary>Вызывается, после того, как мы построили проект</summary>
    public static event Build_Project_Event?         end_build_for_project_event;

    public static ErrorCode ExecuteFullBuild()
    {
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

            var psi = Process.Start("bash", "build.sh");
            psi.WaitForExit();

            return (ErrorCode) psi.ExitCode;
        }
        finally
        {
            File.Delete("builder.lock");
        }
    }
}
