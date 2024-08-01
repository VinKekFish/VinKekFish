// TODO: tests
using static VinKekFish_Utils.Language;

namespace VinKekFish_console;
using VinKekFish_EXE;
using static VinKekFish_Utils.Memory;

public partial class Program
{
    public static ProgramErrorCode Command_check(string[] args)
    {
        isService = true;
        var list  = new List<string>(args);
        list.RemoveAt(0);

        if (args.Length < 2)
        {
            Console.Error.WriteLine("vkf chech conf.file");
            Console.Error.WriteLine(L("Have no enough arguments"));
            return ProgramErrorCode.noArgs;
        }
        try
        {
            var fileString = File.ReadAllLines(args[1]);
            var opt = new VinKekFish_Utils.ProgramOptions.Options(new List<string>(fileString));
            var options_service = new VinKekFish_Utils.ProgramOptions.Options_Service(opt);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return ProgramErrorCode.success;
    }

    public static bool Is_command_check(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "check")
            return true;

        return false;
    }
}
