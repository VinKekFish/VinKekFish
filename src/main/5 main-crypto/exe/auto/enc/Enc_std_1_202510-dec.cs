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

public unsafe partial class Enc_std_1_202510: IDisposable
{
    /// <summary>Расшифровывает файл, пользуясь уже установленными параметрами. Все примитивы создаёт сам.</summary>
    public ProgramErrorCode Decrypt()
    {
        var allocator = Keccak_abstract.allocator;
        var Offsets   = new Dictionary<string, Int64>();
        if (command.isDebugMode)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString());
            Console.WriteLine(L("Decryption started. Wait random data from") + " " + command.autoCrypt.RandomSocketPoint.ToString());
        }

        CreateKeyCascadeSponge();

        lock (this)
        try
        {
            nint EncFileLength;
            using (var decFileStream = File.Open(command.DecryptedFileName!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                EncFileLength = (nint)command.DecryptedFileName!.Length;
                if (EncFileLength <= 256)
                {
                    Console.WriteLine(L("ERROR") + ": " + L("The file for decryption have zero length") + ".");
                    return ProgramErrorCode.Abandoned;
                }

                decFile = allocator.AllocMemory((nint)command.DecryptedFileName!.Length, "dec-file-1");

                var readedBytes = decFileStream.Read(decFile);
                if (readedBytes != EncFileLength)
                {
                    throw new Exception($"readedBytes != DecFileLength [{readedBytes} != {EncFileLength}]");
                }
            }
            // Выделяем массив под синхропосылку
            // FileShare.Read не нужен, но, почему-то, иногда возникает исключение "file being used by another process".
            using var OIV  = command.bbp.GetBytesAndRemoveIt(allocator.AllocMemory(OIV_Length, "InitSpongesFirst.OIV"));
            InitSpongesFirst(allocator, Offsets, VKF_K, KeyStrenght, OIV);

            if (command.isDebugMode)
            {
                Console.WriteLine(L("First step of the initialization ended") + ". " + DateTime.Now.ToLongTimeString());
            }

            if (command.isHavePwd)
            {
                Console.WriteLine(L("Enter password:"));
                // _ = new PasswordEnter(Cascade_KeyGenerator!, VinKekFish_KeyGenerator!, regime: 1, doErrorMessage: true, countOfStepsForPermitations: (nint) Cascade_KeyOpts.ArmoringSteps, ArmoringSteps: (nint) Cascade_KeyOpts.ArmoringSteps);
            }

            Console.WriteLine("Encrypted");
        }
        finally
        {
            // Завершение работы программы
            Dispose();
        }

        return ProgramErrorCode.success;
    }
}
