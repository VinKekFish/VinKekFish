// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using maincrypto.keccak;
using vinkekfish;
using System.Xml;
using cryptoprime;
using static vinkekfish.CascadeSponge_1t_20230905;
using cryptoprime.VinKekFish;
using System.ComponentModel;
using static cryptoprime.BytesBuilderForPointers;

public partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "зашифровать"</summary>
    public abstract unsafe class DecEncCommand: Command
    {
                                                                    /// <summary>Имя открытого файла (для зашифрования).</summary>
        public          FileInfo?       DecryptedFileName;          /// <summary>Имя зашифрованного файла, который будет сгенерирован из открытого файла.</summary>
        public          FileInfo?       EncryptedFileName;          /// <summary>Имя файлов ключей.</summary>
        public readonly List<FileInfo>  KeyFiles    = new();        /// <summary>Если true, то команда к шифрованию потребует ввода пароля.</summary>
        public          bool            isHavePwd   = false;        /// <summary>Если false, то команда запросит синхропосылку по пути /dev/vkf/random от сервиса vkf.</summary>
        public          bool            noVKFRandom = false;        /// <summary>Если true, то разрешён необфусцированный ввод пароля.</summary>
        public          bool            isSimplePwd = false;
        public          string          alg         = "std.1.202510";

        public DecEncCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}

        public override void Dispose(bool fromDestructor = false)
        {/*
            if (fromDestructor && Cascade_Key != null)
            {
                var msg = "EncCommand.Dispose executed with a not disposed state.";
                if (BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor)
                    throw new Exception(msg);
                else
                    Console.Error.WriteLine(msg);
            }

            Cascade_Key?   .Dispose();
            VinKekFish_Key?.Dispose();

            Cascade_Key    = null;
            VinKekFish_Key = null;
*/
            base.Dispose(fromDestructor);
        }

        public static bool ParseBool(CommandOption command)
        {
            var val = command.value.Trim().ToLowerInvariant();
            return val == "true" || val == "1" || val == "yes";
        }

        public readonly List<string> algs = new()
        {
            "std.1.202510",
            "std.3.202510",
            "fast.1.202510",
            "short.1.202510",
        };

        public void SelectAlg(string v)
        {
            if (algs.Contains(v))
            {
                alg = v;

                if (isDebugMode)
                    Console.WriteLine($"alg: {alg}");
            }
            else
            {
                if (isDebugMode)
                {
                    Console.WriteLine($"Incorrect algorithm: " + v);
                    Console.WriteLine("Correct algorithms:");
                    foreach (var alg in algs)
                    {
                        Console.WriteLine(alg);
                    }
                }
                else
                    throw new NotImplementedException();
            }
        }
    }
}
