// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public class GenPwdCommand: Command
    {
        public GenPwdCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}

        public override ProgramErrorCode Exec(StreamReader? sr)
        {
            return ProgramErrorCode.success;
        }
    }
}
