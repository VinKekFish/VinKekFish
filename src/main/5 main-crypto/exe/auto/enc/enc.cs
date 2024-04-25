// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "зашифровать"</summary>
    public class EncCommand: Command
    {
        public EncCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}
        public override ProgramErrorCode Exec(StreamReader? sr)
        {
            ThreadPool.QueueUserWorkItem(   (x) => Connect()      );

            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine(sr, () => Console.WriteLine("Commands (not all):\r\nfile:path_to_file\r\nkey:path_to_file"));
            switch (command.name)
            {
                case "file":
                        ParseFileOptions(command.value.TrimStart());
                    break;
                case "out":
                        ParseFileOptions(command.value.TrimStart());
                    break;
                case "key":
                        ParseFileOptions(command.value.TrimStart());
                    break;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;
                    // TODO: стартовать шифрование
                    // InitSponges();
                    break;
                case "end":
                    return ProgramErrorCode.AbandonedByUser;
                default:
                    if (!isDebugMode)
                        throw new CommandException(L("Command is unknown"));
                    goto start;
            }

            return ProgramErrorCode.success;
        }
    }
}
