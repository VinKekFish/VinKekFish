// TODO: tests
namespace VinKekFish_EXE;

/// <summary>
/// Класс, реализующий функциональность программы в режиме работы сервиса "service"
/// </summary>
public class Regime_Service
{
                                                /// <summary>Полное имя файла конфигурации</summary>
    public string? ConfigFileName = null;       /// <summary>Если true - получен сигнал завершения программы или самого прослушивателя</summary>
    public bool    Terminated     = false;
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
        if (args.Count <= 0)
        {
            PrintHelp();
            return ProgramErrorCode.noArgs_Service;
        }

        vkfListener = new UnixSocketListener("/inRamA/UnixSocketListener-socket");

        while (!Terminated || vkfListener.connections.Count > 0)
        {
            Thread.Sleep(1000);
        }

        Console.WriteLine("Regime_Service.Start: to exited");

        return ProgramErrorCode.success;
    }
}

