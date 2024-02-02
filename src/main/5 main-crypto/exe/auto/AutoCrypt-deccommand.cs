// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public class DecCommand: Command
    {
        public DecCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}

        public override ProgramErrorCode Exec()
        {
            return ProgramErrorCode.success;
        }
    }
}
