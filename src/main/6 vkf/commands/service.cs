using static VinKekFish_console.Program;

namespace VinKekFish_console;

public partial class Program
{
    public static void command_service(string[] args)
    {
        isService = true;
    }

    public static bool is_command_service(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "service")
            return true;

        return false;
    }
}
