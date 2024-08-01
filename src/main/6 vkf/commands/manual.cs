// TODO: tests

using static VinKekFish_console.Program;

namespace VinKekFish_console;
using VinKekFish_EXE;

public partial class Program
{
    public static ProgramErrorCode DoCommand_manual(string[] args)
    {
        Console.Error.WriteLine("NOT IMPLEMENTED");
        return ProgramErrorCode.success;
    }

    public static bool Is_command_manual(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "manual")
            return true;

        return false;
    }
}
