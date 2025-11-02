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
    public unsafe class EncCommand: DecEncCommand
    {
        public EncCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}
        public override ProgramErrorCode Exec(ref StreamReader? sr)
        {
            ThreadPool.QueueUserWorkItem(   (x) => Connect()      );

            if (isDebugMode)
                Console.WriteLine(L("Enter options for the file encryption"));


            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine(sr, () => Console.WriteLine("Commands (not all):\r\nfile:path_to_file\r\nkey:path_to_file"));
            switch (command.name)
            {
                case "novkfrandom":
                    noVKFRandom = ParseBool(command);

                    if (isDebugMode)
                    {
                        if (noVKFRandom)
                            Console.WriteLine("novkfrandom: true");
                        else
                            Console.WriteLine("novkfrandom: false");
                    }
                    // TODO:
                    throw new NotImplementedException();

                //                    goto start;
                case "in":
                    DecryptedFileName = ParseFileOptions(command.value.TrimStart(), isDebugMode, mustExists: FileMustExists.Exists);

                    if (isDebugMode)
                    {
                        if (DecryptedFileName == null)
                            Console.WriteLine($"File name for input is incorrect: {command.value.TrimStart()}");
                        else
                            Console.WriteLine($"in: {DecryptedFileName?.FullName}");
                    }

                    goto start;
                case "out":
                    var dateString  = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
                    var outval      = command.value.TrimStart();
                    if (outval.Length <= 0 && DecryptedFileName != null)
                    {
                        outval = DecryptedFileName.FullName + "." + dateString + ".vkf";
                    }

                    var OutFileName   = outval.Replace("$date$", dateString);
                    EncryptedFileName = ParseFileOptions(OutFileName, isDebugMode, mustExists: FileMustExists.NotExists);

                    if (isDebugMode)
                    {
                        if (EncryptedFileName == null)
                            Console.WriteLine($"File name for out is incorrect: {OutFileName}");
                        else
                            Console.WriteLine($"out: {EncryptedFileName?.FullName}");
                    }

                    goto start;
                case "key":
                    var keyFile = ParseFileOptions(command.value.TrimStart(), isDebugMode, mustExists: FileMustExists.Exists);

                    if (keyFile == null)
                    {
                        Console.WriteLine(L("Incorrect key file name for file") + ": " + command.value.TrimStart());
                        return ProgramErrorCode.wrongCryptoParams;
                    }

                    if (keyFile.Length <= 0)
                    {
                        Console.WriteLine(L("Incorrect key file length for file") + ": " + keyFile.FullName);
                        return ProgramErrorCode.wrongCryptoParams;
                    }

                    KeyFiles.Add(keyFile!);
                    goto start;
                case "pwd":
                    isHavePwd = ParseBool(command);
                    goto start;
                case "pwd-simple":
                    isSimplePwd = ParseBool(command);
                    goto start;
                case "alg":
                    SelectAlg(command.value.Trim());
                    goto start;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;

                    if (EncryptedFileName == null)
                    {
                        Console.WriteLine("Command 'out' expected");
                        goto start;
                    }
                    if (DecryptedFileName == null)
                    {
                        Console.WriteLine("Command 'in' expected");
                        goto start;
                    }
                    if (KeyFiles.Count == 0 && !isHavePwd)
                    {
                        Console.WriteLine("Command 'key' or 'pwd' expected");
                        goto start;
                    }

                    if (isDebugMode)
                        Console.WriteLine(L("Try to start with algorithm") + $" {alg}");

                    return alg switch
                    {
                        "std.1.202510"   => new Enc_std_1_202510  (this, 1).Encrypt(),
                        "std.3.202510"   => new Enc_std_1_202510  (this, 3).Encrypt(),
                        "short.1.202510" => new Enc_short_1_202510(this)   .Encrypt(),
                        _ => throw new CommandException(L("The algorithm is unknown") + ": " + alg),
                    };

                case "end":
                    return ProgramErrorCode.AbandonedByUser;
                default:
                    if (!isDebugMode)
                        throw new CommandException(L("Command is unknown"));
                    else
                        Console.WriteLine(L("Command is unknown"));
                    goto start;
            }
        }
    }
}
