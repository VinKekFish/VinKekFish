// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.IO;
using System.Text;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using Approximation = FileParts.Approximation;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public partial class DiskCommand : Command, IDisposable
    {
        public DiskCommand(AutoCrypt autoCrypt) : base(autoCrypt)
        {

        }

        public override void Dispose(bool fromDestructor = false)
        {
            TryToDispose(KeyGenerator); KeyGenerator = null;
            TryToDispose(keccak1);      keccak1      = null;
            TryToDispose(keccak2);      keccak2      = null;
            TryToDispose(keccakOIV);    keccakOIV    = null;
            TryToDispose(ThreeFish1);   ThreeFish1   = null;
            TryToDispose(ThreeFish2);   ThreeFish2   = null;
            TryToDispose(ThreeFish3);   ThreeFish3   = null;

            base.Dispose(fromDestructor);
        }

        protected DirectoryInfo? ServiceDir = null, tmpDir = null, userDir = null;
        protected FileInfo? OpenKeyFileInfo = null;
        public override ProgramErrorCode Exec(StreamReader? sr)
        {
            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine
            (
                sr,
                () => Console.WriteLine
                    (
                        """
                        512 bit encryption
                        Need root user

                        Commands (not all):
                        service:data_dir
                        tmp:tmp_path
                        user:mount_path
                        unencrypted-key:key_file
                        start:

                        Example:
                        enc:/sync_folder/data
                        tmp:/tmp/mount_data
                        mount:/inRamS/user_data
                        key:/inRamA/decrypted.key
                        start:
                        """
                    )
            );

            var value = command.value.Trim().ToLowerInvariant();

            switch (command.name)
            {
                case "service":
                        ServiceDir = ParseDirOptions(command.value.TrimStart(), isDebugMode, FileMustExists.Indifferent);

                        if (isDebugMode)
                        {
                            if (ServiceDir is not null)
                                Console.WriteLine("service: " + ServiceDir.FullName);
                            else
                                Console.WriteLine(L("File error"));
                        }
                        else
                        if (ServiceDir is null)
                        {
                            Console.Error.WriteLine("service: ERROR");
                            return ProgramErrorCode.Abandoned;
                        }

                    goto start;
                case "unencrypted-key":
                case "key":
                    OpenKeyFileInfo = ParseFileOptions(command.value.TrimStart(), isDebugMode, FileMustExists.Exists);
                    goto start;
                case "tmp":
                        tmpDir = ParseDirOptions(command.value.TrimStart(), isDebugMode, FileMustExists.Indifferent);

                        if (isDebugMode)
                        {
                            if (ServiceDir is not null)
                                Console.WriteLine("tmp: " + ServiceDir.FullName);
                            else
                                Console.WriteLine(L("File error"));
                        }
                        else
                        if (ServiceDir is null)
                        {
                            Console.Error.WriteLine("tmp: ERROR");
                            return ProgramErrorCode.Abandoned;
                        }

                    goto start;
                case "user":
                        userDir = ParseDirOptions(command.value.TrimStart(), isDebugMode, FileMustExists.Indifferent);

                        if (isDebugMode)
                        {
                            if (ServiceDir is not null)
                                Console.WriteLine("user: " + ServiceDir.FullName);
                            else
                                Console.WriteLine(L("File error"));
                        }
                        else
                        if (ServiceDir is null)
                        {
                            Console.Error.WriteLine("user: ERROR");
                            return ProgramErrorCode.Abandoned;
                        }

                    goto start;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;
                    if (ServiceDir is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("ServiceDir is null");
                        else
                            Console.WriteLine(L("ServiceDir is null"));

                        goto start;
                    }
                    if (tmpDir is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("tmpDir is null");
                        else
                            Console.WriteLine(L("tmpDir is null"));

                        goto start;
                    }
                    if (userDir is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("userDir is null");
                        else
                            Console.WriteLine(L("userDir is null"));

                        goto start;
                    }
                    if (OpenKeyFileInfo is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("OpenKey is null");
                        else
                            Console.WriteLine(L("OpenKey is null"));

                        goto start;
                    }

                    OpenKeyFileInfo.Refresh();
                    using (var key = Keccak_abstract.allocator.AllocMemory((nint) OpenKeyFileInfo.Length, "disk command: fileForOpenKey"))
                    {
                        using (var fileForOpenKey = File.OpenRead(OpenKeyFileInfo.FullName))
                        {
                            fileForOpenKey.Read(key);
                        }
                        InitSponges(key);
                    }


                    break;
                case "end":
                    Terminated = true;
                    return ProgramErrorCode.AbandonedByUser;
                default:
                    if (!isDebugMode)
                        throw new CommandException(L("Command is unknown"));

                    goto start;
            }

            return ProgramErrorCode.success;
        }

        public KeyDataGenerator? KeyGenerator;
        public Keccak_20200918?  keccak1, keccak2, keccakOIV;
        public Threefish1024?    ThreeFish1, ThreeFish2, ThreeFish3;
        public void InitSponges(Record key)
        {
            CascadeSponge_mt_20230930?  Cascade_Key    = null;
            VinKekFishBase_KN_20210525? VinKekFish_Key = null;
            try
            {
                if (KeyGenerator != null)
                    throw new InvalidOperationException();

                Cascade_Key    = new CascadeSponge_mt_20230930(512) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.elevated };
                Cascade_Key.InitKeyAndOIV(key);

                VinKekFish_Key = new VinKekFishBase_KN_20210525
                (
                    K: 1,
                    CountOfRounds: VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(1),
                    ThreadCount: 1
                );
                VinKekFish_Key.Init1
                (
                    PreRoundsForTranspose: VinKekFish_Key.CountOfRounds - 4,
                    prngToInit: Cascade_Key
                );
                VinKekFish_Key.Init2
                (
                    key: key,
                    RoundsForFirstKeyBlock: VinKekFish_Key.CountOfRounds,
                    RoundsForTailsBlock:    VinKekFish_Key.CountOfRounds
                );

                KeyGenerator = new KeyDataGenerator(VinKekFish_Key, Cascade_Key, Cascade_Key.countStepsForKeyGeneration, "DiskCommand.InitSponges.KeyDataGenerator")
                {
                    KeyLenCsc = cryptoprime.KeccakPrime.b_size,
                    KeyLenVkf = cryptoprime.KeccakPrime.b_size,
                    willDisposeSponges = false
                };

                keccak1   = new Keccak_20200918();
                keccak2   = new Keccak_20200918();
                keccakOIV = new Keccak_20200918();

                keccak1  .DoInitFromKey(KeyGenerator.GetBytes(cryptoprime.KeccakPrime.b_size, 1, "keccak1"), 1);
                keccak2  .DoInitFromKey(KeyGenerator.GetBytes(cryptoprime.KeccakPrime.b_size, 2, "keccak2"), 1);
                keccakOIV.DoInitFromKey(KeyGenerator.GetBytes(cryptoprime.KeccakPrime.b_size, 3, "keccak3"), 1);

                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 1, "ThreeFish1"))
                {
                    ThreeFish1 = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 2, "ThreeFish2"))
                {
                    ThreeFish2 = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 3, "ThreeFish3"))
                {
                    ThreeFish3 = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
            }
            finally
            {
                TryToDispose(Cascade_Key);
                TryToDispose(VinKekFish_Key);
                TryToDispose(KeyGenerator); KeyGenerator   = null;
            }
        }
    }
}
