// TODO: tests

namespace VinKekFish_console;

using System.Runtime;
using VinKekFish_EXE;
using Memory = VinKekFish_Utils.Memory;
using static VinKekFish_Utils.Memory;
using static VinKekFish_Utils.Language;

public partial class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return (int) Main_ec(args);
        }
        finally
        {
            // Пытаемся искусственно спровоцировать вызов деструкторов
            GCSettings.LatencyMode = GCLatencyMode.Batch;
            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForFullGCComplete();
                GC.WaitForFullGCApproach();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    public static ProgramErrorCode Main_ec(string[] args)
    {
        // Инициализация аллокатора памяти: проверяем, что он вообще работает
        VinKekFish_Utils.Memory.Init();
        if (Memory.memoryLockType.IsError())
        {
            Console.Error.WriteLine($"Not have right memory allocator ({Memory.memoryLockType})");
            return ProgramErrorCode.wrongMemoryAllocator;
        }
        if (!Memory.memoryLockType.isCorrect())
        {
            Console.Error.WriteLine($"Not have right memory allocator ({Memory.memoryLockType}). You must disable swap file, if you have");
        }

        // Далее проверяем аргументы и т.п.
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h" || args[0] == "-?")
        {
            PrintVersionHeader();
            PrintHelp();

            return ProgramErrorCode.noArgs;
        }

        if (args[0] == "-v" || args[0] == "--version" || args[0] == "-version" || args[0] == "version")
        {
            PrintVersionHeader();
            return ProgramErrorCode.version;
        }

        if (is_command_auto(args))
        {
            return command_auto(args);
        }

        if (is_command_service(args))
        {
            return command_service(args);
        }

        if (!isAutomaticProgram)
        if (!is_command_manual(args))
        {
            PrintVersionHeader();
            PrintHelp();

            return ProgramErrorCode.errorRegimeArgs;
        }

        return command_manual(args);

        // return ProgramErrorCode.success;
    }

    public static void PrintVersionHeader()
    {
        Console.WriteLine($"VinKekFish (ВинКекФиш)");
        Console.WriteLine($"Version: {ProgramVersion_RiWiWak6ObEcc}");
        Console.WriteLine("url: https://github.com/VinKekFish/VinKekFish");
        Console.WriteLine(L("Current culture: ") + culture.ToString());
        Console.WriteLine(L("AuthorPresentationString"));
        Console.WriteLine();
    }

    public static void PrintHelp()
    {
        Console.WriteLine(L("Full help see at the program url (above)"));
        Console.WriteLine();
        Console.WriteLine("vkf service");
        Console.WriteLine("\t" + L("for execute as service"));
        Console.WriteLine("vkf auto");
        Console.WriteLine("\t" + L("for execute without human"));
        Console.WriteLine("vkf manual");
        Console.WriteLine("\t" + L("for execute for human friendly"));
        Console.WriteLine();
    }
}
