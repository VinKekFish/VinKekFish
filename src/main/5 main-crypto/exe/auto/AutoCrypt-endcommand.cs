// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет собой пустую команду, ведущую к завершению работы программы</summary>
    public class EndCommand: Command
    {
        public EndCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}

        public override ProgramErrorCode Exec()
        {
            return ProgramErrorCode.success;
        }
    }
}
