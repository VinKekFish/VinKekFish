// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

/// <summary>
/// Класс, реализующий функциональность программы в режиме работы сервиса "service"
/// </summary>
public partial class Regime_Service
{
                                                         /// <summary>Полное имя файла конфигурации</summary>
    public          string? ConfigFileName = null;       /// <summary>Если true - получен сигнал завершения программы или самого прослушивателя</summary>
    public volatile bool    Terminated     = false;
                                                       /// <summary>Путь к папке, где программой создаётся unix stream. Берётся из конфигурационного файла</summary>
    public DirectoryInfo? UnixStreamDir;               /// <summary>Полное имя файла (с путём) unix stream для получения энтропии</summary>
    public FileInfo?      UnixStreamPath;              /// <summary>Полное имя файла (с путём) unix stream для получения параметров накопления энтропии</summary>
    public FileInfo?      UnixStreamPathParams;
                                                /// <summary>Прослушиватель сокета, предназначенного для выдачи другим приложениям энтропии</summary>
    public UnixSocketListener? vkfListener     = null;  /// <summary>Прослушиватель сокета, предназначенного для выдачи другим приложениям информации о накопленной энтропии</summary>
    public UnixSocketListener? vkfInfoListener = null;  /// <summary>Прослушиватель символьного устройства</summary>
    public CuseStream?         vkfCuseListener = null;
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
        if (!Terminated)
            doTerminate(true);
    }

    public void doTerminate(bool willBlock = false)
    {
        if (!Terminated)
        {
            // Сначала мы вводим остатки данных, потому что иначе они могут потеряться,
            // если потоки завершатся ранее, чем дойдёт дело до впитывания.
            InputEntropyFromSourcesWhile(int.MaxValue, 0);

            Terminated = true;
            TryToDispose(vkfListener);
            TryToDispose(vkfInfoListener);
            TryToDispose(vkfCuseListener);

            Thread.Sleep(250);/*
            lock (continuouslyGetters)
            foreach (var getter in continuouslyGetters)
            {
                try
                {
                    // getter.thread.Interrupt();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(formatException(ex));
                }
            }*/
        }

        var errCnt  = 0;
        var errTime = 0;
        var time    = 0;

        // Первый раз ждём все потоки на завершение
        // Затем cntT уже больше нуля и позволяет нам завершиться,
        // не ожидая завершения потоков, которые, скорее всего, уже и не завершатся,
        // т.к. ждут файлового ввода-вывода
        while (willBlock && continueWaitForExit())
        {
            Thread.Sleep(1000);
            lock (continuouslyGetters)
            {
                // Дополнительно вводим энтропию из потоков, чтобы там был ноль ожидающих данных
                // Когда ноль ожидающих данных, continueWaitForExit может вернуть ноль, даже если поток ещё не завершён
                InputEntropyFromSourcesWhile(int.MaxValue, 0);
                foreach (var getter in continuouslyGetters)
                {
                    if (time > 0 || getter.countOfBytesFromLastOutput > 0)
                        Console.WriteLine(L("Wait for getter") + ": " + getter.inputElement.PathString + $" (isDataReady = {getter.isDataReady(1)}; countOfBytesFromLastOutput = {getter.countOfBytesFromLastOutput})");
                }

                time++;
                if (errCnt != continuouslyGetters.Count)
                {
                    errTime = 0;
                    errCnt  = continuouslyGetters.Count;
                }
                else
                {
                    errTime++;
                }
            }

            if (errTime > 15)
            {
                Console.WriteLine(L("Wait for threads has been terminated by timeout") + $" ({time} s / {errCnt} threads)");
                break;
            }
        }

        if (willBlock)
        {
            for (int i = 0; i < 4; i++)
                if (getCountOfNonStoppedGetterThreads() > 0)
                    Thread.Sleep(500);
                else
                    break;

            lock (continuouslyGetters)
            foreach (var getter in continuouslyGetters)
            {
                if (!getter.thread.ThreadState.HasFlag(ThreadState.Stopped))
                    Console.WriteLine(L("Getter has not be ended") + ": " + getter.inputElement.PathString + $" (isDataReady = {getter.isDataReady(1)}; countOfBytesFromLastOutput = {getter.countOfBytesFromLastOutput}; ThreadState = {getter.thread.ThreadState}); StreamForClose = {getter.StreamForClose} ");
            }

            Console.WriteLine("Regime_Service.doTerminate: ended");
        }
    }

    public int getCountOfNonStoppedGetterThreads()
    {
        int result = 0;
        lock (continuouslyGetters)
        foreach (var getter in continuouslyGetters)
        {
            if (!getter.thread.ThreadState.HasFlag(ThreadState.Stopped))
                result++;
        }

        return result;
    }

    public bool continueWaitForExit()
    {
        if (vkfListener != null)
        if (vkfListener.ConnectionsCount > 0)
            return true;

        if (vkfInfoListener != null)
        if (vkfInfoListener.ConnectionsCount > 0)
            return true;

        lock (continuouslyGetters)
        {
            if (continuouslyGetters.Count > 0)
            {
                foreach (var getter in continuouslyGetters)
                {
                    try
                    {
                        lock (getter)
                        if (getter.GetCountOfReadyBytes() > 0)
                            return true;

                        // Пытаемся ещё раз прервать поток исполнения
                        // и в первый раз прервать поток ввода-вывода
                        // getter.thread.Interrupt();
                        lock (getter)
                            getter.StreamForClose?.Close();
                    }
                    catch (Exception ex)
                    {
                        formatException(ex);
                    }
                }
            }
        }   

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

                if (options_service?.root?.output?.random?.charDevice?.path != null)
                {
                    var crandomPath = options_service?.root?.output?.random?.charDevice?.path!;
                    Console.WriteLine(L("Try to create character device by path /dev/") + crandomPath);
                    vkfCuseListener = new CuseStream(crandomPath, this);
                }

                // Сразу берём источники энтропии, до того, как будем губку инициализировать,
                // т.к. эти источники от инициализации губки не зависят
                StartContinuouslyEntropy();
                StartEntropy();
                ExecEntropy();
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
            Console.WriteLine($"Regime_Service.Start: exiting started at {DateTime.Now.ToString()}");

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

