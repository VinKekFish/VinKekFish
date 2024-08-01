using static VinKekFish_console.Program;

namespace VinKekFish_console;
using VinKekFish_EXE;

public partial class Program
{                                               
                                                #pragma warning disable CA2211
    /// <summary>true - режим работы программы, где клиент - другая вызывающая программа</summary>
    public static bool isAutomaticProgram = false;  /// <summary>true - программа работает из-под root в режиме сервиса</summary>
    public static bool isService = false;
                                                #pragma warning restore CA2211

    public static ProgramErrorCode Command_auto(string[] args)
    {
        isAutomaticProgram = true;
        using var ac = new AutoCrypt(args);

        return ac.Exec();
    }

    public static bool Is_command_auto(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "auto")
            return true;

        return false;
    }
}
