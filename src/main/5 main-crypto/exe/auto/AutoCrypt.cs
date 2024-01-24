// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;

/// <summary>
/// Класс, реализующий функциональность программы в режиме работы
/// </summary>
public partial class AutoCrypt
{
    public bool isDebugMode = false;

    public readonly Command CurrentCommand;    
    public AutoCrypt()
    {
        start:

        var command = CommandOption.ReadAndParseLine(() => Console.WriteLine("Input 'operation:'.\r\nExamles: debug:, enc:, dec:, gen_key:, end:"), isDebugMode: isDebugMode);

        switch (command.name)
        {
            case "debug":
                    isDebugMode = true;
                goto start;
            case "enc":
                    CurrentCommand = new EncCommand() {isDebugMode = isDebugMode};
                break;
            case "dec":
                    CurrentCommand = new DecCommand() {isDebugMode = isDebugMode};
                break;
            case "gen":
            case "gen_key":
                    CurrentCommand = new GenKeyCommand() {isDebugMode = isDebugMode};
                break;
            case "end":
                    CurrentCommand = new EndCommand();
                return;
            default:
                if (!isDebugMode)
                    throw new CommandException("Command is unknown");
                goto start;
        }
    }

    public ProgramErrorCode Exec()
    {
        return CurrentCommand.Exec();
    }
}
