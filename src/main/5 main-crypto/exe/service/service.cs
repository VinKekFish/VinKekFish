// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;

/// <summary>
/// Класс, реализующий функциональность программы в режиме работы сервиса "service"
/// </summary>
public class Regime_Service
{
                                                /// <summary>Полное имя файла конфигурации</summary>
    public string? ConfigFileName = null;       /// <summary>Если true - получен сигнал завершения программы или самого прослушивателя</summary>
    public bool    Terminated     = false;
                                                       /// <summary>Путь к папке, где программой создаётся unix stream. Берётся из конфигурационного файла</summary>
    public DirectoryInfo? UnixStreamDir;               /// <summary>Полное имя файла (с путём) unix stream</summary>
    public FileInfo?      UnixStreamPath;              /// <summary>Путь к производителю энтропии (/dev/random)</summary>
    public string         OS_Entropy_path = "/dev/random";
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

        vkfListener = new UnixSocketListener(UnixStreamPath!.FullName);

        while (!Terminated || vkfListener.connections.Count > 0)
        {
            Thread.Sleep(1000);
        }

        Console.WriteLine("Regime_Service.Start: exited");

        return ProgramErrorCode.success;
    }

    // Парсим файл конфигурации для сервисного режима
    public Options_Service? options_service = null;
    public ProgramErrorCode? ParseOptions(List<string> args)
    {
        if (args.Count <= 0)
        {
            PrintHelp();
            return ProgramErrorCode.noArgs_Service;
        }

        try
        {
            var fileString = File.ReadAllLines(args[0]);
            var opt = new Options(new List<string>(fileString));
            // Console.WriteLine(opt.ToString());
            options_service = new Options_Service(opt);

            var out_random = options_service!.root!.output!.out_random!;
            UnixStreamDir  = out_random.dir;
            UnixStreamPath = out_random.file;
            Console.WriteLine($"UnixStreamPath = {UnixStreamPath!.FullName}");


            // OS_Entropy_path
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return ProgramErrorCode.noOptions_Service;
        }

        return null;
    }

    public string? GetStringFromOptions(string path, Options opt, Options.Block? block = null)
    {
        var foundedBlock = opt.SearchBlock(path, block?.blockHeaderIndent ?? 0, block);
        if (foundedBlock is null)
            return null;

        return foundedBlock.blocks[0].Name;
    }
}

