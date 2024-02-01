// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public class GenKeyCommand: Command
    {
        public bool isDebugMode = false;
        public GenKeyCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}

        public override ProgramErrorCode Exec()
        {
            return ProgramErrorCode.success;
        }
    }
}
