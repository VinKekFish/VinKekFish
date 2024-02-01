// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Net.Sockets;
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
    public          Socket  RandomSocket;

    public          UnixDomainSocketEndPoint RandomSocketPoint;

    public string  RandomStreamName = "/dev/vkf/random";
    public string  RandomNameFromOS = "/dev/random";


    public AutoCrypt()
    {
        RandomSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        RandomSocketPoint = new UnixDomainSocketEndPoint(RandomStreamName);
        // RandomSocket.Connect(un);

        start:

        var command = CommandOption.ReadAndParseLine(() => Console.WriteLine("Input 'operation_name:'.\r\nExamles: debug:, enc:, dec:, key:, pwd:, end:"), isDebugMode: isDebugMode);

        switch (command.name)
        {
            case "debug":
                    isDebugMode = true;
                goto start;
            case "enc":
                    CurrentCommand = new EncCommand(this) {isDebugMode = isDebugMode};
                break;
            case "dec":
                    CurrentCommand = new DecCommand(this) {isDebugMode = isDebugMode};
                break;
            case "key":
            case "key_gen":
                    CurrentCommand = new GenKeyCommand(this) {isDebugMode = isDebugMode};
                break;
            case "pwd":
            case "pwd_gen":
                    CurrentCommand = new GenPwdCommand(this) {isDebugMode = isDebugMode};
                break;
            case "end":
                    CurrentCommand = new EndCommand(this);
                return;
            default:
                if (!isDebugMode)
                    throw new CommandException(L("Command is unknown (enter 'debug:' at the vkf start for more bit information)"));
                goto start;
        }
    }

    public ProgramErrorCode Exec()
    {
        return CurrentCommand.Exec();
    }
}
