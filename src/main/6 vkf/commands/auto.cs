using static VinKekFish_console.Program;

namespace VinKekFish_console;
using VinKekFish_EXE;

public partial class Program
{
    /// <summary>true - режим работы программы, где клиент - другая вызывающая программа</summary>
    public static bool isAutomaticProgram = false;  /// <summary>true - программа работает из-под root в режиме сервиса</summary>
    public static bool isService = false;
    public static ProgramErrorCode command_auto(string[] args)
    {
        isAutomaticProgram = true;
        using var ac = new AutoCrypt();

        return ac.Exec();
    }

    public static bool is_command_auto(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "auto")
            return true;

        return false;
    }
}
