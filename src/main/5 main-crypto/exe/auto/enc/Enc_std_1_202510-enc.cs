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

public unsafe sealed partial class Enc_std_1_202510: IDisposable
{
    public CascadeSponge_mt_20230930?  Cascade_Key    = null;
    public VinKekFishBase_KN_20210525? VinKekFish_Key = null;
    public Record?                     decFile        = null;
    public Record?                     encFile        = null;

    public readonly AutoCrypt.DecEncCommand command;
    public Enc_std_1_202510(AutoCrypt.DecEncCommand enc_dec_command)
    {
        this.command = enc_dec_command;
    }

    public void Dispose()
    {
        Cascade_Key?   .Dispose();
        VinKekFish_Key?.Dispose();
        decFile?       .Dispose();
        encFile?       .Dispose();

        Cascade_Key    = null;
        VinKekFish_Key = null;
        decFile        = null;
        encFile        = null;
    }

    public const nint OIV_Length = 64;
    // Определяем стойкость шифрования
    public const byte VKF_K = 3;
    public const int  KeyStrenght = VKF_K*VinKekFishBase_etalonK1.BLOCK_SIZE;

    public const string Position_END        = "END";
    public const string Position_IOV        = "OIV";
    public const string Position_DataLength = "DataLength";
    public const string Position_Hash       = "Hash";

    /// <summary>Шифрует файл, пользуясь уже установленными параметрами. Все примитивы создаёт сам.</summary>
    public ProgramErrorCode Encrypt()
    {
        var allocator = Keccak_abstract.allocator;
        var Offsets = new Dictionary<string, Int64>();
        if (command.isDebugMode)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString());
            Console.WriteLine(L("Encryption started. Wait random data from") + " " + command.autoCrypt.RandomSocketPoint.ToString());
        }

        CreateKeyCascadeSponge();

        lock (command)
            try
            {
                while (command.bbp.Count == 0)
                {
                    if (command.isDebugMode)
                        Console.WriteLine("Waiting for random from /dev/vkf/random");

                    Monitor.Wait(this);
                }

                // Выделяем массив под синхропосылку
                // FileShare.Read не нужен, но, почему-то, иногда возникает исключение "file being used by another process".
                using var OIV = command.bbp.GetBytesAndRemoveIt(allocator.AllocMemory(OIV_Length, "InitSpongesFirst.OIV"));
                using var encF = File.Open(command.EncryptedFileName!.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

                nint DecFileLength;
                using (var decFileStream = File.Open(command.DecryptedFileName!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    DecFileLength = (nint)command.DecryptedFileName!.Length;
                    if (DecFileLength <= 0)
                    {
                        Console.WriteLine(L("ERROR") + ": " + L("The file for encryption have zero length") + ".");
                        return ProgramErrorCode.Abandoned;
                    }

                    decFile = allocator.AllocMemory((nint)command.DecryptedFileName!.Length, "dec-file-1");

                    var readedBytes = decFileStream.Read(decFile);
                    if (readedBytes != DecFileLength)
                    {
                        throw new Exception($"readedBytes != DecFileLength [{readedBytes} != {DecFileLength}]");
                    }
                }

                // Записываем открытый вектор инициализации
                Offsets.Add(Position_END, 0);
                Offsets.Add(Position_IOV, 0);
                encF.Write(OIV);

                // Записываем длину исходного файла для шифрования
                Offsets.Add(Position_DataLength, encF.Position);
                byte[]? DecFileLenData = null;
                BytesBuilder.VariableULongToBytes((ulong)DecFileLength, ref DecFileLenData);
                encF.Write(DecFileLenData);
                Offsets.Add(Position_END, encF.Position);

                InitSpongesFirst(allocator, Offsets, VKF_K, KeyStrenght, OIV);

                Offsets.Add(Position_Hash, encF.Position);
                encF.Write(decFile);

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

    private void CreateKeyCascadeSponge()
    {
        // +BLOCK_SIZE - т.к. эта губка генерирует ключи, она должна быть чуть более стойкая, чем те ключи, что она генерирует
        Cascade_Key = new
        (
            KeyStrenght + VinKekFishBase_etalonK1.BLOCK_SIZE * 2,
            ThreadsCount: Environment.ProcessorCount - 1
        )
        {
            StepTypeForAbsorption = TypeForShortStepForAbsorption.effective
        };
    }
}
