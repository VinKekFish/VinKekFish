// TODO: tests
using static VinKekFish_console.Program;

namespace VinKekFish_console;
using VinKekFish_EXE;

public partial class Program
{
    public static ProgramErrorCode command_service(string[] args)
    {
        isService = true;
        var list  = new List<string>(args);
        list.RemoveAt(0);

        return new VinKekFish_EXE.Regime_Service().Start(list);
    }

    public static bool is_command_service(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "service")
            return true;

        return false;
    }
}
