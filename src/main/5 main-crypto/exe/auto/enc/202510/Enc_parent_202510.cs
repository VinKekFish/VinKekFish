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

public unsafe partial class Enc_parent_202510
{
    public static void PrintErrorInCatchMsg(Exception ex)
    {
        DoFormatException(ex);
        Console.WriteLine();
        using (new VinKekFish_Utils.console.ErrorConsoleOptions())
            Console.Write(L("May be the incorrect file or a programm error. This may be when either a wrong file, a fake file, an incorrect key, an incorrect key file order, or an incorrect 'alg' name") + ".");
        Console.WriteLine();
    }

    public static void PrintIncorrectFileMsg(bool IncorrectHash = false)
    {
        Console.WriteLine();

        using (new VinKekFish_Utils.console.ErrorConsoleOptions())
        {
            if (IncorrectHash)
                Console.Write(L("Incorrect hash of file: this is either a wrong file, a fake file, an incorrect key, an incorrect key file order, or an incorrect 'alg' name") + ".");
            else
                Console.Write(L("Incorrect file: this is either a wrong file, a fake file, an incorrect key, an incorrect key file order, or an incorrect 'alg' name") + ".");
        }
        Console.WriteLine();
    }
}
