// TODO: tests
using static VinKekFish_Utils.Language;

namespace VinKekFish_console;
using static VinKekFish_Utils.Utils;

using System.Runtime.InteropServices;
using VinKekFish_EXE;

public partial class Program
{
                                                                    #pragma warning disable CA2211
    public static Regime_Service? service = null;
                                                                    #pragma warning restore CA2211
    public static ProgramErrorCode Command_service(string[] args)
    {
        isService = true;
        var list  = new List<string>(args);
        list.RemoveAt(0);

        // https://en.wikipedia.org/wiki/Signal_(IPC)
//      AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        Console.CancelKeyPress += ProcessExit;
        //PosixSignalRegistration.Create(PosixSignal.SIGTSTP, ProcessExit);
        PosixSignalRegistration.Create(PosixSignal.SIGINT,  ProcessExit);
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, ProcessExit);
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, ProcessExit);


        service = new VinKekFish_EXE.Regime_Service();
        return service.Start(list);
    }

    public static bool Is_command_service(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "service")
            return true;

        return false;
    }

    public static void ProcessExit(PosixSignalContext context)
    {
        context.Cancel = true;
        Console.WriteLine(L("A signal for end has been received") + ": " + context.Signal.ToString());

        try
        {
            service?.DoTerminate(willBlock: true);
        }
        catch (Exception ex)
        {
            DoFormatException(ex);
        }
    }

    public static void ProcessExit(object? sender, ConsoleCancelEventArgs e)
    {
        try
        {
            e.Cancel = true;
            Console.WriteLine(L("A signal for end has been received"));
            ThreadPool.QueueUserWorkItem
            (
                (param) =>
                {
                    service?.DoTerminate(willBlock: false);
                }
            );
        }
        catch (Exception ex)
        {
            DoFormatException(ex);
        }
    }
}
