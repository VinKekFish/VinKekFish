// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;

/// <summary>
/// Класс, реализующий функциональность программы в режиме работы сервиса "service"
/// </summary>
public partial class Regime_Service
{
                                                /// <summary>Полное имя файла конфигурации</summary>
    public string? ConfigFileName = null;       /// <summary>Если true - получен сигнал завершения программы или самого прослушивателя</summary>
    public bool    Terminated     = false;
                                                       /// <summary>Путь к папке, где программой создаётся unix stream. Берётся из конфигурационного файла</summary>
    public DirectoryInfo? UnixStreamDir;               /// <summary>Полное имя файла (с путём) unix stream для получения энтропии</summary>
    public FileInfo?      UnixStreamPath;              /// <summary>Полное имя файла (с путём) unix stream для получения параметров накопления энтропии</summary>
    public FileInfo?      UnixStreamPathParams;        /// <summary>Путь к производителю энтропии (/dev/random)</summary>
    public string         OS_Entropy_path = "/dev/random";
                                                /// <summary>Прослушиватель сокета, предназначенного для выдачи другим приложениям энтропии</summary>
    public UnixSocketListener? vkfListener     = null;  /// <summary>Прослушиватель сокета, предназначенного для выдачи другим приложениям информации о накопленной энтропии</summary>
    public UnixSocketListener? vkfInfoListener = null;
    public Regime_Service()
    {
    }
                                                /// <summary>Вывод на экран справочную информацию по параметрам командной строки режима service</summary>
    private static void PrintHelp()
    {
        Console.WriteLine(L("Usage:"));
        Console.WriteLine("vkf service pathToConfigFile");
        Console.WriteLine("");
        Console.WriteLine(L("See an example of a config file in the program packet"));
    }

    ~Regime_Service()
    {
        doTerminate(true);
    }

    public void doTerminate(bool willBlock = false)
    {
        if (!Terminated)
        {
            Terminated = true;
            vkfListener?.Close();
            vkfInfoListener?.Close();

            Thread.Sleep(250);
            lock (continuouslyGetters)
            foreach (var getter in continuouslyGetters)
            {
                try
                {
                    getter.thread.Interrupt();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(VinKekFish_Utils.Memory.formatException(ex));
                }
            }
        }

        var errCnt  = 0;
        var errTime = 0;
        while (willBlock && continueWaitForExit())
        {
            Thread.Sleep(1000);
            lock (continuouslyGetters)
            {
                foreach (var getter in continuouslyGetters)
                {
                    Console.WriteLine(L("Wait for getter") + ": " + getter.inputElement.PathString);
                }

                if (errCnt != continuouslyGetters.Count)
                {
                    errTime = 0;
                    errCnt  = continuouslyGetters.Count;
                }
                else
                    errTime++;
            }

            if (errTime > 15)
                break;
        }

        if (willBlock)
            Console.WriteLine("Regime_Service.doTerminate: exited");
    }

    public bool continueWaitForExit()
    {
        lock (continuouslyGetters)
            if (continuouslyGetters.Count > 0)
                return true;

        if (vkfListener != null)
        if (vkfListener.ConnectionsCount > 0)
            return true;

        if (vkfInfoListener != null)
        if (vkfInfoListener.ConnectionsCount > 0)
            return true;

        return false;
    }


    /// <summary>Исполнение программы в режиме сервиса</summary>
    /// <param name="args">Аргументы командной строки, идущие после описателя режима service (имя файла конфигурации)</param>
    /// <returns>Код возврата сервиса</returns>
    public ProgramErrorCode Start(List<string> args)
    {
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        Thread.CurrentThread.IsBackground = false;

        try
        {
            var poResult = ParseOptions(args);
            if (poResult != ProgramErrorCode.success)
                return poResult;

            Console.WriteLine($"{L("initialization started at")} {DateTime.Now.ToString()}");

            // Мы сначала создаём прослушиватель, но делаем это сразу в блокировке, чтобы не было возможности получить данные ещё из непроинициализированных губок
            lock (entropy_sync)
            {
                vkfListener     = new UnixSocketListener(UnixStreamPath      !.FullName, this, UnixSocketListener.SocketinformationType.entropy);
                vkfInfoListener = new UnixSocketListener(UnixStreamPathParams!.FullName, this, UnixSocketListener.SocketinformationType.entropyParams);
                StartEntropy();
                StartContinuouslyEntropy();
            }

            Console.WriteLine($"{L("service started at")} {DateTime.Now.ToString()}");

            try
            {
                while (!Terminated)
                {
                    ExecEntropy();
                    Thread.Sleep(270);
                }
            }
            finally
            {
                doTerminate(true);
            }
        }
        finally
        {
            Console.WriteLine($"Regime_Service.Start: exiting at {DateTime.Now.ToString()}");

            StopEntropy();

            Console.WriteLine($"Regime_Service.Start: exited at {DateTime.Now.ToString()}");
        }

        return ProgramErrorCode.success;
    }

    // Парсим файл конфигурации для сервисного режима
    public Options_Service? options_service = null;
    /// <summary>Вызывается при старте службы для парсинга настроек</summary>
    /// <param name="args">Аргументы командной строки, идущие после описателя режима service (имя файла конфигурации)</param>
    /// <returns>0, если успех</returns>
    public ProgramErrorCode ParseOptions(List<string> args)
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

            var out_random       = options_service!.root!.output!.out_random!;
            UnixStreamDir        = out_random.dir;
            UnixStreamPath       = out_random.file;
            UnixStreamPathParams = out_random.fileForParams;

            // Это первое, что пишет программа при запуске
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine($"UnixStreamPath = {UnixStreamPath!.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return ProgramErrorCode.noOptions_Service;
        }

        return ProgramErrorCode.success;
    }

    public string? GetStringFromOptions(string path, Options opt, Options.Block? block = null)
    {
        var foundedBlock = opt.SearchBlock(path, block?.blockHeaderIndent ?? 0, block);
        if (foundedBlock is null)
            return null;

        return foundedBlock.blocks[0].Name;
    }
}

