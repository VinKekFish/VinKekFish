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
    {                                                                               /// <summary>Опции шифрования ключа</summary>
        public VinKekFishOptions VinKekFish_Key    = new VinKekFishOptions();       /// <summary>Опции шифрования открытого текста</summary>
        public VinKekFishOptions VinKekFish_Cipher = new VinKekFishOptions();       /// <summary>Опции шифрования ключа</summary>
        public CascadeOptions    Cascade_Key       = new CascadeOptions();          /// <summary>Опции шифрования открытого текста</summary>
        public CascadeOptions    Cascade_Cipher    = new CascadeOptions();

        public GenKeyCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}

        public override ProgramErrorCode Exec()
        {
            ThreadPool.QueueUserWorkItem(   (x) => Connect()      );

            start:

            var command = CommandOption.ReadAndParseLine(() => Console.WriteLine("Commands (not all):\r\nfile:path_to_file\r\nout:path_to_file\r\nregime:1.0\r\ncascade:512\r\n[cascade:bytes of strength]"));
            switch (command.name)
            {
                case "vinkekfish-k":
                case "vkf-k":
                        ParseVinKekFishOptions(command.value.Trim(), VinKekFish_Key);
                    break;
                case "cascade-k":
                        ParseCascadeOptions(command.value.Trim());
                    break;
                case "vinkekfish-c":
                case "vkf-c":
                        ParseVinKekFishOptions(command.value.Trim(), VinKekFish_Cipher);
                    break;
                case "cascade-c":
                        ParseCascadeOptions(command.value.Trim());
                    break;
                case "key":
                        ParseFileOptions(command.value.TrimStart());
                    break;
                case "file":
                        ParseFileOptions(command.value.TrimStart());
                    break;
                case "regime":
                        ParseRegimeOptions(command.value.Trim());
                    break;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;
                    // TODO: стартовать шифрование
                    InitSponges();
                    break;
                case "end":
                    return ProgramErrorCode.AbandonedByUser;
                default:
                    if (!isDebugMode)
                        throw new CommandException("Command is unknown");
                    goto start;
            }

            return ProgramErrorCode.success;
        }


        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected void ParseRegimeOptions(string value)
        {
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)
            {
                var val = values[0].Trim();
            }
        }

        /// <summary>Распарсить опции команды vinkekfish</summary>
        /// <param name="value">Опции, разделённые пробелом. K Rounds PreRounds KOut</param>
        protected void ParseVinKekFishOptions(string value, VinKekFishOptions opts)
        {
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)     // K
            {
                var val = values[0].Trim();
                var K   = int.Parse(val);
                if (K == 0)
                    return;

                if ((K & 1) != 1)
                    throw new Exception("K may be only is 1, 3, 5, 7, 9, 11, 13, 15, 17, 19");

                if (K >= 1 && K <= 19)
                {
                    opts.K = K;
                    opts.SetK(K);
                }
                else
                    throw new Exception("K may be only is 1, 3, 5, 7, 9, 11, 13, 15, 17, 19");
            }

            if (values.Length >= 2)     // Rounds
            {
                var val    = values[1].Trim();
                var Rounds = int.Parse(val);
                if (Rounds == 0)
                    return;

                opts.Rounds = Rounds;
            }

            if (values.Length >= 3)     // PreRounds
            {
                var val = values[2].Trim();
                var PR  = int.Parse(val);
                if (PR == 0)
                    return;

                opts.PreRounds = PR;
            }

            if (values.Length >= 4)     // KOut
            {
                var val = values[3].Trim();
                var KO  = float.Parse(val, System.Globalization.NumberStyles.Float);
                if (KO == 0)
                    return;

                opts.KOut = KO;
            }
        }

        /// <summary>Распарсить опции команды cascade</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected void ParseCascadeOptions(string value)
        {
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)
            {
                var val   = values[0].Trim();
                var bytes = int.Parse(val);
                if (bytes > 0)
                {
                    // Cascade_Bytes = bytes;
                    return;
                }
            }
        }

    }
}
