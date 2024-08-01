// TODO: tests

namespace VinKekFish_console;

using System.Runtime;
using VinKekFish_EXE;
using Memory = VinKekFish_Utils.Memory;
using static VinKekFish_Utils.Memory;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using System.Text;

public partial class Program
{
    public static int Main(string[] args)
    {
        // Ниже, после возврата из программы, просто проверим Record.errorsInDispose
        // Если надо - выдадим ошибку. Плюс, в stderr всё равно выдастся сообщение, если где-то забыта память
        cryptoprime.BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor = false;
        cryptoprime.BytesBuilderForPointers.Record.doExceptionOnDisposeTwiced       = false;

        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

        // Переходим к выполнению основной программы и обработке ошибок
        try
        {
            return (int) Main_ec(args);
        }
        finally
        {
            try
            {
                // Пытаемся искусственно спровоцировать вызов деструкторов
                GCSettings.LatencyMode = GCLatencyMode.Batch;
                int cnt = 0;
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        GC.Collect();
                        GC.WaitForFullGCComplete();
                        GC.WaitForFullGCApproach();
                        GC.WaitForPendingFinalizers();
                    }
                    catch (Exception iex)
                    {
                        FormatException(iex);
                        if (cnt > 0)
                            i++;
                        cnt++;
                    }
                }
            }
            catch (VinKekFish_Utils.ProgramOptions.Options_Service.Options_Service_Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                FormatException(ex);
            }

            if (cryptoprime.BytesBuilderForPointers.Record.ErrorsInDispose)
            {
                Console.Error.WriteLine(L("Error at the end of the program: cryptoprime.BytesBuilderForPointers.Record.errorsInDispose"));

                foreach (var error in cryptoprime.BytesBuilderForPointers.Record.errorsInDispose_List)
                    Console.Error.WriteLine(error);
            }

            if (VinKekFish_Utils.Memory.AllocatedMemory != 0)
            {
                Console.Error.WriteLine(L("Error: leaked memory in mmap: ") + VinKekFish_Utils.Memory.AllocatedMemory.ToString("#,0") + " (" + VinKekFish_Utils.Memory.AllocatedRegionsCount.ToString("#,0") + ")");
                DeallocateAtBreakage();
            }
        }
    }

    protected static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex)
            Console.Error.WriteLine(L("Unhandled Exception occured") + ".\n" + e.ExceptionObject.ToString());
        else
            Console.Error.WriteLine(L("Unhandled Exception occured") + ".\n" + FormatException(ex, false));

        if (e.IsTerminating)
            service?.DoTerminate(true);
    }

    public static ProgramErrorCode Main_ec(string[] args)
    {
        L("");
        // Инициализация аллокатора памяти: проверяем, что он вообще работает
        VinKekFish_Utils.Memory.Init();
        if (Memory.memoryLockType.IsError())
        {
            Console.Error.WriteLine(L("Not have right memory allocator") + $" ({Memory.memoryLockType})");
            return ProgramErrorCode.wrongMemoryAllocator;
        }
        if (!Memory.memoryLockType.IsCorrect())
        {
            Console.Error.WriteLine(L("Not have right memory allocator") + $" ({Memory.memoryLockType}). " + L("You must disable swap file, if you have"));
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

        if (Is_command_auto(args))
        {
            return Command_auto(args);
        }

        if (Is_command_check(args))
        {
            return Command_check(args);
        }

        if (Is_command_service(args))
        {
            return Command_service(args);
        }

        if (Is_command_install(args))
        {
            return DoCommand_install(args);
        }

        if (!isAutomaticProgram)
        if (!Is_command_manual(args))
        {
            PrintVersionHeader();
            PrintHelp();

            return ProgramErrorCode.errorRegimeArgs;
        }

        return DoCommand_manual(args);

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

    public static string GenVersionNumber()
    {
        // "2024.03.19.1112"
        var now = DateTime.Now;
        var str =    now.Year  .ToString("D4");
        str += "." + now.Month .ToString("D2");
        str += "." + now.Day   .ToString("D2");
        str += "." + now.Hour  .ToString("D2");
        str +=       now.Minute.ToString("D2");

        return str;
    }

    public static void PrintHelp()
    {
        Console.WriteLine(L("Full help see at the program url (above)"));
        Console.WriteLine();
        Console.WriteLine("vkf service conf.file");
        Console.WriteLine("\t" + L("for execute as service"));
        Console.WriteLine("vkf check conf.file");
        Console.WriteLine("\t" + L("for check service file"));
        Console.WriteLine("vkf auto command.file");
        Console.WriteLine("\t" + L("for execute without human"));
        Console.WriteLine("vkf manual");
        Console.WriteLine("\t" + L("for execute for human friendly"));
        Console.WriteLine("vkf version");
        Console.WriteLine("\t" + L("for get version information"));
        Console.WriteLine();
    }
}
