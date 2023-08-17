// TODO: tests
using static VinKekFish_console.Program;

namespace VinKekFish_console;
using VinKekFish_EXE;

public partial class Program
{
    public static Regime_Service? service = null;
    public static ProgramErrorCode command_service(string[] args)
    {
        isService = true;
        var list  = new List<string>(args);
        list.RemoveAt(0);

//      AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        Console.CancelKeyPress += ProcessExit;

        service = new VinKekFish_EXE.Regime_Service();
        return service.Start(list);
    }

    public static bool is_command_service(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "service")
            return true;

        return false;
    }

    public static void ProcessExit(object? sender, EventArgs e)
    {
        try
        {
            service?.doTerminate(willBlock: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message + "\n" + ex.StackTrace);
        }
    }
}
