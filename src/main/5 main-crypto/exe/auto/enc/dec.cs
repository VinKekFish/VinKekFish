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
                Console.WriteLine(L("Enter options for the file encryption"));


            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine(sr, () => Console.WriteLine("Commands (not all):\r\nfile:path_to_file\r\nkey:path_to_file"));
            switch (command.name)
            {
                case "dec":
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
                            Console.WriteLine($"File name for input is incorrect: {command.value.TrimStart()}");
                        else
                            Console.WriteLine($"dec: {DecryptedFileName?.FullName}");
                    }

                    goto start;
                case "enc":
                    outval = command.value.TrimStart();

                    EncryptedFileName = ParseFileOptions(outval, isDebugMode, mustExists: FileMustExists.Exists);

                    if (isDebugMode)
                    {
                        if (EncryptedFileName == null)
                            Console.WriteLine($"File name for out is incorrect: {outval}");
                        else
                            Console.WriteLine($"enc: {EncryptedFileName?.FullName}");
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
                    // TODO: 
                    if (isDebugMode)
                        Console.WriteLine("Alg selection is not implemented");
                    goto start;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;

                    if (EncryptedFileName == null)
                    {
                        Console.WriteLine("Command 'enc' expected");
                        goto start;
                    }
                    if (DecryptedFileName == null)
                    {
                        Console.WriteLine("Command 'dec' expected");
                        goto start;
                    }
                    if (KeyFiles.Count == 0 && !isHavePwd)
                    {
                        Console.WriteLine("Command 'key' or 'pwd' expected");
                        goto start;
                    }

                    return alg switch
                    {
                        "std.1.202510" => Decrypt_std_1_202510(),
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

        /// <summary>Расшифровывает файл, пользуясь уже установленными параметрами. Все примитивы создаёт сам.</summary>
        public ProgramErrorCode Decrypt_std_1_202510()
        {
            var allocator = Keccak_abstract.allocator;
            var Offsets   = new Dictionary<string, Int64>();
            if (isDebugMode)
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString());
                Console.WriteLine(L("Decryption started. Wait random data from") + " " + autoCrypt.RandomSocketPoint.ToString());
            }

            // Определяем стойкость шифрования
            const byte VKF_K = 3; const int KeyStrenght = VKF_K*VinKekFishBase_etalonK1.BLOCK_SIZE;
            Cascade_Key = new(KeyStrenght, ThreadsCount: Environment.ProcessorCount - 1) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };

            lock (this)
            try
            {
                while (bbp.Count == 0)
                {
                    if (isDebugMode)
                        Console.WriteLine("Waiting for random from /dev/vkf/random");

                    Monitor.Wait(this);
                }
                InitSpongesFirst(allocator, Offsets, VKF_K, KeyStrenght);

                if (isDebugMode)
                {
                    Console.WriteLine(L("First step of the initialization ended") + ". " + DateTime.Now.ToLongTimeString());
                }

                if (isHavePwd)
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

        private void InitSpongesFirst(AllocHGlobal_AllocatorForUnsafeMemory allocator, Dictionary<string, long> Offsets, byte VKF_K, int KeyStrenght)
        {
            const nint OIV_Length = 64;
            // Выделяем массив под синхропосылку
            // FileShare.Read не нужен, но, почему-то, иногда возникает исключение "file being used by another process".
            using var OIV  = bbp.GetBytesAndRemoveIt(allocator.AllocMemory(OIV_Length, "InitSpongesFirst.OIV"));
            using var encF = File.Open(EncryptedFileName!.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

            encF.Write(OIV);
            Offsets.Add("OIV", 0);

            if (isDebugMode)
                Console.WriteLine(L("Initialization started (Cascade sponge)") + ". " + DateTime.Now.ToLongTimeString());

            // Инициализируем губку с помощью синхропосылки
            Cascade_Key!.Step(0, 0, OIV, OIV.len, regime: 254);
            Cascade_Key!.Step(Cascade_Key.countStepsForHardening, regime: 0);

            nint maxInputLen = KeyStrenght * 4; // *4 - это просто запас

            var KeyArrays = new List<Record>(KeyFiles.Count);

            try
            {
                byte regime = 3;
                // Вводим в каскадную губку ключи
                // Аналогичный ввод ниже
                foreach (var KeyFileName in KeyFiles)
                {
                    using (var KeyFile = File.Open(EncryptedFileName!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
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

                if (isDebugMode)
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
}
