// TODO: tests
namespace VinKekFish_EXE;

/// <summary>
/// 
/// </summary>
public class Regime_Service
{
                                                /// <summary>Полное имя файла конфигурации</summary>
    public string? ConfigFileName = null;
    public Regime_Service()
    {
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

        return ProgramErrorCode.success;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Use:");
        Console.WriteLine("vkf service pathToConfigFile");
        Console.WriteLine("");
        Console.WriteLine("See an example of a config file in program packet");
    }
}
