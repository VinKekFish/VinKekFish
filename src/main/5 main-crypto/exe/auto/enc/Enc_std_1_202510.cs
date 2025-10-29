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
    public CascadeSponge_mt_20230930?  Cascade_Key    = null;
    public VinKekFishBase_KN_20210525? VinKekFish_Key = null;

    public readonly AutoCrypt.DecEncCommand command;
    public Enc_std_1_202510(AutoCrypt.DecEncCommand enc_dec_command)
    {
        this.command = enc_dec_command;
    }

    public void Dispose()
    {
        Cascade_Key?   .Dispose();
        VinKekFish_Key?.Dispose();

        Cascade_Key    = null;
        VinKekFish_Key = null;
    }

    public const nint OIV_Length = 64;
    // Определяем стойкость шифрования
    public const byte VKF_K = 3;
    public const int  KeyStrenght = VKF_K*VinKekFishBase_etalonK1.BLOCK_SIZE;

    /// <summary>Шифрует файл, пользуясь уже установленными параметрами. Все примитивы создаёт сам.</summary>
    public ProgramErrorCode Encrypt()
    {
        var allocator = Keccak_abstract.allocator;
        var Offsets   = new Dictionary<string, Int64>();
        if (command.isDebugMode)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString());
            Console.WriteLine(L("Encryption started. Wait random data from") + " " + command.autoCrypt.RandomSocketPoint.ToString());
        }

        Cascade_Key = new(KeyStrenght, ThreadsCount: Environment.ProcessorCount - 1) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };

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
            using var OIV  = command.bbp.GetBytesAndRemoveIt(allocator.AllocMemory(OIV_Length, "InitSpongesFirst.OIV"));
            using var encF = File.Open(command.EncryptedFileName!.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

            encF.Write(OIV);
            Offsets.Add("OIV", 0);

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
            TryToDispose(Cascade_Key);   Cascade_Key    = null;
            TryToDispose(VinKekFish_Key);VinKekFish_Key = null;
        }

        return ProgramErrorCode.success;
    }

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

        // Определяем стойкость шифрования
        const byte VKF_K = 3; const int KeyStrenght = VKF_K*VinKekFishBase_etalonK1.BLOCK_SIZE;
        Cascade_Key = new(KeyStrenght, ThreadsCount: Environment.ProcessorCount - 1) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };

        lock (this)
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
            TryToDispose(Cascade_Key);   Cascade_Key    = null;
            TryToDispose(VinKekFish_Key);VinKekFish_Key = null;
        }

        return ProgramErrorCode.success;
    }


    private void InitSpongesFirst(AllocHGlobal_AllocatorForUnsafeMemory allocator, Dictionary<string, long> Offsets, byte VKF_K, int KeyStrenght, Record OIV)
    {
        if (command.isDebugMode)
            Console.WriteLine(L("Initialization started (Cascade sponge)") + ". " + DateTime.Now.ToLongTimeString());

        // Инициализируем губку с помощью синхропосылки
        Cascade_Key!.Step(0, 0, OIV, OIV.len, regime: 254);
        Cascade_Key!.Step(Cascade_Key.countStepsForHardening, regime: 0);

        nint maxInputLen = KeyStrenght * 4; // *4 - это просто запас

        var KeyArrays = new List<Record>(command.KeyFiles.Count);

        try
        {
            byte regime = 3;
            // Вводим в каскадную губку ключи
            // Аналогичный ввод ниже
            foreach (var KeyFileName in command.KeyFiles)
            {
                using (var KeyFile = File.Open(command.EncryptedFileName!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var mem = allocator.AllocMemory((nint)KeyFileName.Length, "InitSpongesFirst.KeyFile");

                    KeyFile.Read(mem);
                    KeyArrays.Add(mem);

                    Cascade_Key.Step(data: mem, dataLen: mem.len,
                                        StepsForAbsorption: Cascade_Key.GetCountOfStepsForAbsorption(TypeForShortStepForAbsorption.log),
                                        regime: regime++);

                    // Вычисляем размер приёмника для VinKekFish
                    if (maxInputLen < mem.len)
                        maxInputLen = mem.len;
                }
            }

            // На всякий случай избегаем повторного режима 1
            // Он первый в InitThreeFishByCascade
            if (Cascade_Key.LastRegime == 1)
                Cascade_Key.Step();

            // Завершаем инициализацию каскадной губки
            Cascade_Key.InitThreeFishByCascade();

            if (command.isDebugMode)
                Console.WriteLine(L("Initialization continued (VinKekFish)") + ". " + DateTime.Now.ToLongTimeString());

            VinKekFish_Key = new VinKekFishBase_KN_20210525
            (
                K: VKF_K,
                CountOfRounds: VinKekFishBase_KN_20210525.Calc_NORMAL_ROUNDS_K(VKF_K),
                ThreadCount: 1
            );
            VinKekFish_Key.Init1
            (
                PreRoundsForTranspose: VinKekFish_Key.CountOfRounds - 4,
                prngToInit: Cascade_Key
            );
            VinKekFish_Key.Init2
            (
                RoundsForFirstKeyBlock: VinKekFish_Key.CountOfRounds,
                RoundsForTailsBlock: VinKekFish_Key.CountOfRounds,
                noInputKey: true
            );

            VinKekFish_Key.input  = new BytesBuilderStatic(maxInputLen);
            VinKekFish_Key.output = new BytesBuilderStatic(maxInputLen);

            // Вводим в губку VinKekFish ключи
            // Аналогичный ввод выше
            regime = 3;
            foreach (var KeyArray in KeyArrays)
            {
                VinKekFish_Key.input.Add(KeyArray);

                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.DoStepAndIO(regime: regime++);
            }
        }
        finally
        {
            // Удаляем все ключи, так как они больше не нужны
            foreach (var KeyArray in KeyArrays)
            {
                KeyArray.Dispose();
            }
            KeyArrays.Clear();
        }
    }
}
