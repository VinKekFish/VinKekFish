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
using System.IO.Compression;
using static VinKekFish_EXE.AutoCrypt;

public unsafe partial class Enc_std_1_202510: IDisposable
{
    private void CreateKeyCascadeSponge()
    {
        // +BLOCK_SIZE - т.к. эта губка генерирует ключи, она должна быть чуть более стойкая, чем те ключи, что она генерирует
        Cascade_Key = new
        (
            KeyKeyStrenght,
            ThreadsCount: Environment.ProcessorCount - 1
        )
        {
            StepTypeForAbsorption = TypeForShortStepForAbsorption.elevated
        };
    }

    /// <summary>Создаёт губку для непосредственного шифрования.</summary>
    /// <param name="key">Ключ для инициализации губки.</param>
    /// <param name="steps">Количество шагов для впитывания.</param>
    /// <returns>Возвращает созданную и проинициализированную ключом key губку.</returns>
    private CascadeSponge_mt_20230930 CreateAndInitCascadeSponge(Record key, TypeForShortStepForAbsorption steps = TypeForShortStepForAbsorption.weak)
    {
        var result = new CascadeSponge_mt_20230930
        (
            KeyStrenght,
            ThreadsCount: Environment.ProcessorCount - 1
        )
        {
            StepTypeForAbsorption = steps
        };

        result.InitKeyAndOIV(key);

        return result;
    }
    
    /// <summary>Генерирует губку vkf</summary>
    /// <param name="key">Ключ для инициализации</param>
    /// <param name="PermutationGenerator">Каскадная губка для инициализации таблиц перестановок</param>
    /// <returns></returns>
    private VinKekFishBase_KN_20210525 CreateAndInitVkfSponge(Record key, CascadeSponge_mt_20230930 PermutationGenerator, int ThreadCount = 1)
    {
        var result = new VinKekFishBase_KN_20210525
        (
            CountOfRounds: VinKekFishBase_KN_20210525.Calc_NORMAL_ROUNDS_K(VKF_K),
            K: VKF_K,
            ThreadCount: ThreadCount
        );

        result.Init1
        (
            PreRoundsForTranspose: VinKekFishBase_KN_20210525.Calc_NORMAL_ROUNDS_K(VKF_K) - 4,
            prngToInit: PermutationGenerator
        );

        result.Init2(key: key);

        return result;
    }

    private void InitSpongesFirst(AllocHGlobal_AllocatorForUnsafeMemory allocator, Record OIV, nint decFileLength = 0, int decFileLengthFieldLength = 0)
    {
        if (command.isDebugMode)
            Console.WriteLine(L("Initialization started (Cascade sponge)") + ". " + DateTime.Now.ToLongTimeString());

        // Инициализируем губку с помощью синхропосылки
        Cascade_Key!.Step(0, 0, OIV, OIV.len, regime: 254);
        Cascade_Key!.Step(Cascade_Key.countStepsForHardening, regime: 0);

        nint maxInputLen = KeyKeyStrenght * 4; // *4 - это просто запас

        var KeyArrays = new List<Record>(command.KeyFiles.Count);

        try
        {
            byte regime = 3;
            // Вводим в каскадную губку ключи
            // Аналогичный ввод ниже
            foreach (var KeyFileFi in command.KeyFiles)
            {
                using (var KeyFile = File.Open(KeyFileFi!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var mem = allocator.AllocMemory((nint)KeyFileFi.Length, "InitSpongesFirst.KeyFile");

                    var readed = KeyFile.Read(mem);
                    KeyArrays.Add(mem);

                    if (readed != mem.len)
                        throw new Exception($"Enc_std_1_202510.InitSpongesFirst: readed != mem.len in KeyFile ({readed} != {mem.len})");

                    Cascade_Key.Step(data: mem, dataLen: mem.len,
                                        StepsForAbsorption: Cascade_Key.GetCountOfStepsForAbsorption(TypeForShortStepForAbsorption.log),
                                        regime: regime++);

                    // Вычисляем размер приёмника для VinKekFish
                    if (maxInputLen < mem.len)
                        maxInputLen = mem.len;
                }
            }

            // Дополнительно перемешиваем ключевую информацию,
            // чтобы распределить энтропию более равномерно
            Cascade_Key.Step(regime: (byte) (regime - 127), countOfSteps: Cascade_Key.countStepsForKeyGeneration);

            // На всякий случай избегаем повторного режима 1
            // Он первый в InitThreeFishByCascade
            if (Cascade_Key.LastRegime == 1)
                Cascade_Key.Step();

            // Завершаем инициализацию каскадной губки
            Cascade_Key.InitThreeFishByCascade();

            if (command.isDebugMode)
                Console.WriteLine(L("Initialization continuing (VinKekFish)") + ". " + DateTime.Now.ToLongTimeString());

            VinKekFish_Key = new VinKekFishBase_KN_20210525
            (
                K: VKF_KEY_K,
                CountOfRounds: VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(VKF_KEY_K),
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
                RoundsForTailsBlock:    VinKekFish_Key.CountOfRounds,
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
                VinKekFish_Key.DoStepAndFullInput(regime: regime++);
            }

            // Завершаем ввод ключей комбинацией режимов,
            // которая точно не будет встречена при вводе ключей
            VinKekFish_Key.DoStepAndIO(regime: 3);
            VinKekFish_Key.DoStepAndIO(regime: 2);

            if (command.isHavePwd)
            {
                _ = new PasswordEnter(Cascade_Key!, VinKekFish_Key!, regime: 1, doErrorMessage: true, countOfStepsForPermitations: 0, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
            }

            // -------------------------------------------
            //  Обмен данными инициализации между губками
            // -------------------------------------------
            if (command.isDebugMode)
                Console.WriteLine(L("Initialization continuing (VinKekFish <=> Cascade sponge)") + ". " + DateTime.Now.ToLongTimeString());

            // Делаем вывод в каскадную губку из VKF
            VinKekFish_Key.DoStepAndFullOutput(regime: 0, DataLen: KeyKeyStrenght, ByLen: VinKekFish_Key.BLOCK_SIZE_KEY_K);
            if (VinKekFish_Key.output.Count < KeyKeyStrenght)
                throw new Exception("FATAL ERROR: VinKekFish_Key.output.Count < KeyKeyStrenght");

            using var memKey = allocator.AllocMemory(KeyKeyStrenght, "InitSpongesFirst.KeyPermutation");
            VinKekFish_Key.output.GetBytesAndRemoveIt(memKey);
            VinKekFish_Key.output.Clear();

            // Вырабатываем ввод в VKF из каскадной губки в усиленном режиме
            // (только половина вывода с каждого шага)
            while (VinKekFish_Key.input.Count < KeyKeyStrenght)
            {
                Cascade_Key.Step(regime: 255, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                VinKekFish_Key.input.Add(Cascade_Key.lastOutput, Cascade_Key.lastOutput.len >> 1);
                Cascade_Key.Step(regime: 1, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                VinKekFish_Key.input.Add(Cascade_Key.lastOutput, Cascade_Key.lastOutput.len >> 1);
            }
            Cascade_Key.Step(regime: 127);

            // Вводим в каскадную губку данные из VKF
            Cascade_Key.Step(data: memKey, dataLen: memKey.len, regime: 0);
            memKey.Clear();

            // Вводим рандомизирующий ввод из каскадной губки
            // с необратимой перезаписью для защиты введённых ранее ключей
            VinKekFish_Key.DoStepAndFullInput(regime: 127, Overwrite: true, nullPadding: false);

            Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, doCheckSafty: false);

            // -------------------------------------------
            //  Генерация сессионных ключей
            // -------------------------------------------
            if (command.isDebugMode)
                Console.WriteLine(L("Initialization continuing (session keys creation)") + ". " + DateTime.Now.ToLongTimeString());

            using var KeyGenerator = new KeyDataGenerator(VinKekFish_Key, Cascade_Key, Cascade_Key.countStepsForKeyGeneration, "Enc_std_1_202510.InitSpongesFirst.KeyGenerator")
            {
                KeyLenCsc = KeyStrenght,
                KeyLenVkf = KeyStrenght,
                willDisposeSponges = false
            };

            if (decFileLength > 0)
                KeyGenerator.Generate(7);
            else
                KeyGenerator.Generate(6);

            if (command.isDebugMode)
                Console.WriteLine(L("Initialization continuing (cascade sponges creation)") + ". " + DateTime.Now.ToLongTimeString());

            Cascade_p     = CreateAndInitCascadeSponge(KeyGenerator.keys[0].csc!);
            Cascade_1f    = CreateAndInitCascadeSponge(KeyGenerator.keys[1].csc!);
            Cascade_1r    = CreateAndInitCascadeSponge(KeyGenerator.keys[2].csc!);
            Cascade_2f    = CreateAndInitCascadeSponge(KeyGenerator.keys[3].csc!);
            Cascade_2r    = CreateAndInitCascadeSponge(KeyGenerator.keys[4].csc!);
            Cascade_vkf   = CreateAndInitCascadeSponge(KeyGenerator.keys[5].csc!);
            if (decFileLength > 0)
            Cascade_noise = CreateAndInitCascadeSponge(KeyGenerator.keys[6].csc!);

            if (command.isDebugMode)
                Console.WriteLine(L("Initialization continuing (vkf sponges creation)") + ". " + DateTime.Now.ToLongTimeString());

            VinKekFish_1f = CreateAndInitVkfSponge(KeyGenerator.keys[0].vkf!, Cascade_vkf, Environment.ProcessorCount - 1);
            VinKekFish_1r = CreateAndInitVkfSponge(KeyGenerator.keys[1].vkf!, Cascade_vkf, Environment.ProcessorCount - 1);
            VinKekFish_2f = CreateAndInitVkfSponge(KeyGenerator.keys[2].vkf!, Cascade_vkf, Environment.ProcessorCount - 1);
            VinKekFish_2r = CreateAndInitVkfSponge(KeyGenerator.keys[3].vkf!, Cascade_vkf, Environment.ProcessorCount - 1);
            if (decFileLength > 0)
            VinKekFish_n  = CreateAndInitVkfSponge(KeyGenerator.keys[4].vkf!, Cascade_vkf, Environment.ProcessorCount - 1);

            if (decFileLength > 0)
            {
                if (command.isDebugMode)
                    Console.WriteLine(L("Initialization continuing (noise generation)") + ". " + DateTime.Now.ToLongTimeString());

                // Выравнивание идёт всего файла на нужную границу, а не только самого открытого текста
                var FilePaddingsLen = CalcFilePaddingsLen
                (
                    decFileLength*2 +
                    OIV_Length + HashLength + decFileLengthFieldLength, FileAlignment
                );

                NoiseGenerator = new KeyDataGenerator(VinKekFish_n!, Cascade_noise!, 0, "Enc_std_1_202510.InitSpongesFirst.NoiseGenerator")
                {
                    KeyLenCsc = decFileLength + FilePaddingsLen,
                    KeyLenVkf = 0,
                    willDisposeSponges = false
                };

                // Здесь мы не генерируем ключи, значит можно брать блоки целиком
                NoiseGenerator.vkf.BlockLen = VinKekFish_n !.BLOCK_SIZE_K;
                NoiseGenerator.csc.BlockLen = Cascade_noise!.lastOutput.len;

                CascadeSponge_1t_20230905.StepProgress? progressCsc = new();
                progressCsc.allSteps = NoiseGenerator.KeyLenCsc;
                ThreadPool.QueueUserWorkItem
                (
                    delegate
                    {
                        NoiseGenerator.Generate(1, progressCsc: progressCsc);

                        lock (progressCsc)
                            Monitor.PulseAll(progressCsc);
                    }
                );

                lock (progressCsc)
                while (progressCsc.processedSteps < progressCsc.allSteps)
                {
                    if (Monitor.Wait(progressCsc, 30_000))
                    {
                        Console.WriteLine();
                        break;
                    }
                    else
                        Console.Write($"{progressCsc.processedSteps*100.0/progressCsc.allSteps:f1}% " + DateTime.Now.ToLongTimeString() + "\t");
                }
            }

            if (command.isDebugMode)
            {
                Console.WriteLine(L("Step of the initialization ended") + ". " + DateTime.Now.ToLongTimeString());
            }
        }
        finally
        {
            // Удаляем все ключи, так как они больше не нужны
            foreach (var KeyArray in KeyArrays)
            {
                TryToDispose(KeyArray);
            }
            KeyArrays.Clear();
        }
    }
}
