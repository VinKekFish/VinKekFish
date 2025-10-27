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
        public          bool            noVKFRandom = false;
        public          string          alg         = "std.1.202510";

        public CascadeSponge_mt_20230930?  Cascade_Key    = null;
        public VinKekFishBase_KN_20210525? VinKekFish_Key = null;

        public DecEncCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {}

        public override void Dispose(bool fromDestructor = false)
        {
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

            base.Dispose(fromDestructor);
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
