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
        public bool isDebugMode = false;
        public EncCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}
                                                                            /// <summary>Коэффициент стойкости VinKekFish (K=1,3,5,7,...)</summary>
        public int  VinKekFish_K         = 1;                               /// <summary>Количество раундов</summary>
        public int  VinKekFish_Rounds    = 0;                               /// <summary>Количество раундов со стандартными таблицами перестановки</summary>
        public int  VinKekFish_PreRounds = 0;                               /// <summary>Стойкость каскадной губки в байтах</summary>
        public int  Cascade_Bytes        = 512;

        public override ProgramErrorCode Exec()
        {
            ThreadPool.QueueUserWorkItem(   (x) => Connect()      );

            start:

            var command = CommandOption.ReadAndParseLine(() => Console.WriteLine("Commands (not all):\r\nfile:path_to_file\r\nout:path_to_file\r\nregime:1.0\r\ncascade:512\r\n[cascade:bytes of strength]"));
            switch (command.name)
            {
                case "vinkekfish":
                case "vkf":
                        ParseVinKekFishOptions(command.value.Trim());
                    break;
                case "cascade":
                        ParseCascadeOptions(command.value.Trim());
                    break;
                case "file":
                        ParseFileOptions(command.value.TrimStart());
                    break;
                case "out":
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

        /// <summary>Распарсить опции команды vinkekfish</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected void ParseVinKekFishOptions(string value)
        {
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)
            {
                var val = values[0].Trim();
                var K   = int.Parse(val);
                if (K == 0)
                {
                    VinKekFish_K = 0;
                    return;
                }

                if ((K & 1) != 1)
                    throw new Exception("K may be only is 1, 3, 5, 7, 9, 11, 13, 15, 17, 19");

                if (K >= 1 && K <= 19)
                    VinKekFish_K = K;
                else
                    throw new Exception("K may be only is 1, 3, 5, 7, 9, 11, 13, 15, 17, 19");
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
                    Cascade_Bytes = bytes;
                    return;
                }
            }
        }

        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected FileInfo? ParseFileOptions(string value)
        {
            return null;
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

        protected static Regex SpaceRegex = new Regex("[ \t]+", RegexOptions.Compiled | RegexOptions.Singleline);
        protected static Regex CommaRegex = new Regex("[,;]",   RegexOptions.Compiled | RegexOptions.Singleline);
        public static string[] ToSpaceSeparated(string value)
        {
            lock (CommaRegex)
            value = CommaRegex.Replace(value, " ");
            lock (SpaceRegex)
            value = SpaceRegex.Replace(value, " ");

            var values = value.Split(" ");
            return values;
        }
    }
}
