// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.Options;

/// <summary>
/// Класс, реализующий функциональность программы в режиме работы сервиса "service"
/// </summary>
public class Regime_Service
{
                                                /// <summary>Полное имя файла конфигурации</summary>
    public string? ConfigFileName = null;       /// <summary>Если true - получен сигнал завершения программы или самого прослушивателя</summary>
    public bool    Terminated     = false;
                                                /// <summary>Путь к создаваемому программой unix stream. Берётся из конфигурационного файла</summary>
    public string? UnixStreamPath;              /// <summary>Путь к производителю энтропии (/dev/random)</summary>
    public string  OS_Entropy_path = "/dev/random";
                                                /// <summary>Прослушиватель сокета, предназначенного для выдачи другим приложениям чего-либо</summary>
    public UnixSocketListener? vkfListener = null;
    public Regime_Service()
    {
    }
                                                /// <summary>Вывод на экран справочную информацию по параметрам командной строки режима service</summary>
    private static void PrintHelp()
    {
        Console.WriteLine("Use:");
        Console.WriteLine("vkf service pathToConfigFile");
        Console.WriteLine("");
        Console.WriteLine("See an example of a config file in program packet");
    }

    ~Regime_Service()
    {
        doTerminate(true);
    }

    public void doTerminate(bool willBlock = false)
    {
        Terminated = true;
        vkfListener?.Close();
        while (willBlock && vkfListener != null && vkfListener?.ConnectionsCount > 0)
        {
            Thread.Sleep(100);
        }

        if (willBlock)
            Console.WriteLine("Regime_Service.doTerminate: exited");
    }

    /// <summary>Исполнение программы в режиме сервиса</summary>
    /// <param name="args">Аргументы командной строки, идущие после описателя режима service (имя файла конфигурации)</param>
    /// <returns>Код возврата сервиса</returns>
    public ProgramErrorCode Start(List<string> args)
    {
        GCSettings.LatencyMode = GCLatencyMode.Batch;

        var poResult = ParseOptions(args);
        if (poResult is not null)
            return poResult.Value;

        vkfListener = new UnixSocketListener(UnixStreamPath!);

        while (!Terminated || vkfListener.connections.Count > 0)
        {
            Thread.Sleep(1000);
        }

        Console.WriteLine("Regime_Service.Start: exited");

        return ProgramErrorCode.success;
    }

    private ProgramErrorCode? ParseOptions(List<string> args)
    {
        if (args.Count <= 0)
        {
            PrintHelp();
            return ProgramErrorCode.noArgs_Service;
        }

        var fileString = File.ReadAllLines(args[0]);
        var opt = new Options(new List<string>(fileString));
        // Console.WriteLine(opt.ToString());

        var pathBlock = opt.SearchBlock("unix stream.path");
        if (pathBlock is null)
        {
            Console.Error.WriteLine("In options not occur unix stream path");
            return ProgramErrorCode.noArgs_Service;
        }

        UnixStreamPath = pathBlock.blocks[0].Name;
        Console.WriteLine($"UnixStreamPath = {UnixStreamPath}");

        var entropyBlock = opt.SearchBlock("entropy");
        var block        = opt.SearchBlock("OS");
        if (block is not null)
            OS_Entropy_path  = block.Name;

        Console.WriteLine($"OS_Entropy_path = {OS_Entropy_path}");

        return null;
    }
}

