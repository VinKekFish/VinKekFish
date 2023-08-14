// TODO: tests

namespace VinKekFish_console;

public partial class Program
{
    public enum ProgramErrorCode { success = 0, version = 100, noArgs = 101 };

    public static int Main(string[] args)
    {
        return (int) Main_ec(args);
    }

    static ProgramErrorCode Main_ec(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h" || args[0] == "-?")
        {
            PrintVersionHeader();
            PrintHelp();

            return ProgramErrorCode.noArgs;
        }

        if (args[0] == "-v" || args[0] == "--version")
        {
            PrintVersionHeader();
            return ProgramErrorCode.version;
        }

        if (is_command_auto(args))
        {
            command_auto(args);
        }

        return ProgramErrorCode.success;
    }

    public static void PrintVersionHeader()
    {
        Console.WriteLine($"VinKekFish");
        Console.WriteLine($"Version: {ProgramVersion_RiWiWak6ObEcc}");
        Console.WriteLine("url: https://github.com/VinKekFish/VinKekFish");
        Console.WriteLine("Author: Vinogradov Sergey Vasilievich, Mitistchy, Moscow Region, Russia; @2020-2020+");
        Console.WriteLine();
    }

    public static void PrintHelp()
    {
        Console.WriteLine("Full help see at the program url (above)");
        Console.WriteLine();
        Console.WriteLine("1. Password, file or key generation");
        Console.WriteLine("vkf gen");
        Console.WriteLine("see 'vkf gen help' for get help about command");
        Console.WriteLine();
    }
}
