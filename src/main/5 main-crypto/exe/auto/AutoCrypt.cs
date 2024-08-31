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

    protected StreamReader? fs = null;

    public AutoCrypt(string[] args)
    {
        cryptoprime.BytesBuilderForPointers.Record.DoRegisterDestructor(this);

        // RandomSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        RandomSocketPoint = new UnixDomainSocketEndPoint(RandomStreamName);
        // RandomSocket.Connect(un);
        DoCorrectRandomStreamName();

        if (args.Length > 1)
        {
            var FileName = args[1];
            fs = new StreamReader(new FileStream(FileName, FileMode.Open, FileAccess.Read));
        }

    start:

        var command = (CommandOption)CommandOption.ReadAndParseLine
        (
            fs,
            PrintHelpForMainLevelAutoCommand,
            isDebugMode: isDebugMode
        );

        switch (command.name)
        {
            case "debug":
                isDebugMode = true;
                Console.WriteLine(L("Debug mode enabled"));
                goto start;
            case "enc":
                CurrentCommand = new EncCommand(this) { isDebugMode = isDebugMode };
                break;
            case "dec":
                CurrentCommand = new DecCommand(this) { isDebugMode = isDebugMode };
                break;
            case "key-main":
            case "key-primary":
            case "main-key":
            case "primary-key":
            case "key_gen_main":
            case "key-gen-main":
                CurrentCommand = new GenKeyCommand(this) { isDebugMode = isDebugMode };
                break;
            case "pwd":
            case "pwd_gen":
                CurrentCommand = new GenPwdCommand(this) { isDebugMode = isDebugMode };
                break;
            case "end":
                CurrentCommand = new EndCommand(this);
                return;
            case "disk":
                CurrentCommand = new DiskCommand(this);
                return;
            default:
                if (!isDebugMode)
                    throw new CommandException(L("Command is unknown (enter 'debug:' at the vkf start for more bit information)"));

                Console.WriteLine(L("Command is unknown"));
                PrintHelpForMainLevelAutoCommand();

                goto start;
        }
    }

    private static void PrintHelpForMainLevelAutoCommand()
    {
        Console.WriteLine(L("Input 'name:value'")
                           + ":\r\nExamles: debug:, enc:, dec:, key-main:, pwd:, end:, start:");
    }

    /// <summary>Взять из конфигурации имя файла, через который сервис vkf даёт энтропию</summary>
    public void DoCorrectRandomStreamName()
    {
        try
        {
            var fileString = File.ReadAllLines("options/service.options");
            var opt = new Options(new List<string>(fileString));
            // Console.WriteLine(opt.ToString());
            var options_service = new Options_Service(opt);

            var out_random = options_service!.root!.output!.out_random!;
            RandomStreamName = Path.Combine(out_random.dir!.FullName, "random");
        }
        catch
        {}

        Console.WriteLine("The vkf random stream: " + RandomStreamName);
    }

    /// <summary>Запускает команду.</summary>
    /// <param name="sr">Поток, с которого будет считываться набор команд</param>
    /// <returns>Код ошибки</returns>
    public ProgramErrorCode Exec()
    {
        return CurrentCommand!.Exec(fs);
    }

    private bool isDisposed = false;
    protected virtual void Dispose(bool fromDestructor = false)
    {
        var id = isDisposed;
        if (!isDisposed)
        {
            TryToDispose(CurrentCommand);
            TryToDispose(fs);
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
