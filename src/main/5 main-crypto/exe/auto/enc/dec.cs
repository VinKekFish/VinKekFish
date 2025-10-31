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
using System.Text.RegularExpressions;

public partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public unsafe class DecCommand: DecEncCommand
    {
        public readonly Regex DateFileString = new(@"\.[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9][0-9]$", RegexOptions.Compiled);

        public DecCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}
        public override ProgramErrorCode Exec(ref StreamReader? sr)
        {
            if (isDebugMode)
                Console.WriteLine(L("Enter options for the file decryption"));


            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine(sr, () => Console.WriteLine("Commands (not all):\r\nfile:path_to_file\r\nkey:path_to_file"));
            switch (command.name)
            {
                case "out":
                    var outval = command.value.TrimStart();
                    // Убираем от файла расширение .vkf и расширение с датой файла
                    if (outval.Length <= 0 && EncryptedFileName != null)
                    {
                        var encFullName = EncryptedFileName.FullName;
                        if (encFullName.EndsWith(".vkf"))
                        {
                            encFullName = encFullName.Substring(0, encFullName.Length - ".vkf".Length);
                        }

                        var matchOfDateFileString = DateFileString.Match(encFullName);
                        if (matchOfDateFileString.Success)
                        {
                            encFullName = encFullName.Substring(0, encFullName.Length - matchOfDateFileString.Value.Length);
                        }

                        outval = encFullName;
                        if (File.Exists(outval))
                        {
                            outval += ".decrypted";
                        }
                    }

                    DecryptedFileName = ParseFileOptions(outval, isDebugMode, mustExists: FileMustExists.NotExists);

                    if (isDebugMode)
                    {
                        if (DecryptedFileName == null)
                            Console.WriteLine($"File name for output is incorrect: {command.value.TrimStart()}");
                        else
                            Console.WriteLine($"out: {DecryptedFileName?.FullName}");
                    }

                    goto start;
                case "in":
                    outval = command.value.TrimStart();

                    EncryptedFileName = ParseFileOptions(outval, isDebugMode, mustExists: FileMustExists.Exists);

                    if (isDebugMode)
                    {
                        if (EncryptedFileName == null)
                            Console.WriteLine($"File name for input is incorrect: {outval}");
                        else
                            Console.WriteLine($"in: {EncryptedFileName?.FullName}");
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
                    isHavePwd = true;
                    goto start;
                case "alg":
                    SelectAlg(command.value.Trim());
                    goto start;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;

                    if (EncryptedFileName == null)
                    {
                        Console.WriteLine("Command 'in' expected");
                        goto start;
                    }
                    if (DecryptedFileName == null)
                    {
                        Console.WriteLine("Command 'out' expected");
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
                        "std.1.202510" => new Enc_std_1_202510(this, 1).Decrypt(),
                        "std.3.202510" => new Enc_std_1_202510(this, 3).Decrypt(),
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
