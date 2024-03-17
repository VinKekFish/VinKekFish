// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Net.Sockets;
using cryptoprime;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

/// <summary>
/// Класс, реализующий функциональность программы в режиме работы
/// </summary>
public partial class AutoCrypt: IDisposable
{
    public bool isDebugMode = false;

    public readonly Command? CurrentCommand;

    public          UnixDomainSocketEndPoint RandomSocketPoint;

    public string  RandomStreamName = "/dev/vkf/random";    // TODO: прочитать из конфига
    public string  RandomNameFromOS = "/dev/random";

    public AutoCrypt()
    {
        cryptoprime.BytesBuilderForPointers.Record.doRegisterDestructor(this);

        // RandomSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        RandomSocketPoint = new UnixDomainSocketEndPoint(RandomStreamName);
        // RandomSocket.Connect(un);

        start:

        var command = (CommandOption) CommandOption.ReadAndParseLine(() => Console.WriteLine(L("Input 'name:value'") + ":\r\nExamles: debug:, enc:, dec:, key:, pwd:, end:"), isDebugMode: isDebugMode);

        switch (command.name)
        {
            case "debug":
                    isDebugMode = true;
                    Console.WriteLine(L("Debug mode enabled"));
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
        return CurrentCommand!.Exec();
    }

    private bool isDisposed = false;
    protected virtual void Dispose(bool fromDestructor = false)
    {
        var id = isDisposed;
        if (!isDisposed)
        {
            TryToDispose(CurrentCommand);
            isDisposed = true;
        }

        if (!id)
        if (fromDestructor)
        {
            var msg = $"AutoCrypt.Dispose ~AutoCrypt() executed with a not disposed state.";
            if (BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor)
                throw new Exception(msg);
            else
                Console.Error.WriteLine(msg);
        }
    }

    ~AutoCrypt()
    {
        Dispose(fromDestructor: true);
    }

    void IDisposable.Dispose()
    {
        Dispose(fromDestructor: false);
        GC.SuppressFinalize(this);
    }
}
