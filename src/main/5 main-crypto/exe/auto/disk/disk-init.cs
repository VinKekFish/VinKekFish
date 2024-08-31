// TODO: tests
using System.Runtime;

#pragma warning disable CA1416
// Весь класс DiskCommand зависит от libfuse3-3 и рассчитан исключительно на применение в составе Linux с установленным losetup
// ::warn:onlylinux:sOq1JvFKRxQyw7FQ:

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
using static AutoCrypt.Import;
using System.Data;
using System.Diagnostics;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public partial class DiskCommand : Command, IDisposable
    {
        protected DiskCommand(AutoCrypt autoCrypt) : base(autoCrypt)
        {
            if (geteuid() != 0)
                Console.WriteLine(L("ERROR") + ": " + L("Disk command required root (sudo) user!"));
        }

        private static DiskCommand? dc = null;
        public static DiskCommand CreateDiskCommand(AutoCrypt autoCrypt, bool isDebugMode)
        {
            if (dc != null)
                throw new InvalidOperationException("DiskCommand");

            dc = new DiskCommand(autoCrypt) { isDebugMode = isDebugMode };
            return dc;
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

            dc = null;
        }

        #pragma warning disable CS0103
        #pragma warning disable CA2211

        /// <summary>Это поле принадлежит объекту, оно не статическое. Полный декларируемый размер диска.</summary>
        public static ulong FileSize = 68_719_476_736L; // 64 Gib
        /// <summary>Это поле принадлежит объекту, оно не статическое. Путь к /dev/loop устройству, на которое смонтирован диск.</summary>
        public static string loopDev = "";

        public static uint uid = uint.MaxValue;
        public static uint gid = uint.MaxValue;

        public static DirectoryInfo? DataDir = null, tmpDir = null, UserDir = null;
        public static FileInfo? OpenKeyFileInfo = null;

        public static bool   isCreatedDir = false;
        public static string Rights       = "0:0";

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
                        data:encrypted_data_dev
                        tmp:tmp_path
                        user:mount_path
                        unencrypted-key:key_file
                        size:number_size_in_bytes
                        r:user:group
                        start:

                        Example:
                        data:/sync_folder/
                        tmp:/tmp/mount_data/
                        user:/inRamS/user_data/
                        key:/inRamA/decrypted.key
                        size:64G
                        r:admin:all
                        start:
                        """
                    )
            );

            switch (command.name)
            {
                case "r":
                        Rights = command.value.Trim();
                        goto start;
                case "size":
                        FileSize = (ulong) ParseUtils.ParseSize(command.value);
                        goto start;
                case "data":
                        DataDir = ParseDirOptions(command.value.TrimStart(), isDebugMode, FileMustExists.Indifferent);

                        if (isDebugMode)
                        {
                            if (DataDir is not null)
                                Console.WriteLine("data: " + DataDir.FullName);
                            else
                                Console.WriteLine(L("File error"));
                        }
                        else
                        if (DataDir is null)
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
                            if (tmpDir is not null)
                                Console.WriteLine("tmp: " + tmpDir.FullName);
                            else
                                Console.WriteLine("File error");
                        }
                        else
                        if (tmpDir is null)
                        {
                            Console.Error.WriteLine("tmp: ERROR");
                            return ProgramErrorCode.Abandoned;
                        }

                    goto start;
                case "user":
                        UserDir = ParseDirOptions(command.value.TrimStart(), isDebugMode, FileMustExists.Indifferent);

                        if (isDebugMode)
                        {
                            if (UserDir is not null)
                                Console.WriteLine("user: " + UserDir.FullName);
                            else
                                Console.WriteLine(L("File error"));
                        }
                        else
                        if (UserDir is null)
                        {
                            Console.Error.WriteLine("user: ERROR");
                            return ProgramErrorCode.Abandoned;
                        }

                    goto start;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;
                    if (DataDir is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("ServiceDir is null");
                        else
                            Console.WriteLine("ServiceDir is null");

                        goto start;
                    }
                    if (tmpDir is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("tmpDir is null");
                        else
                            Console.WriteLine("tmpDir is null");

                        goto start;
                    }
                    if (UserDir is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("userDir is null");
                        else
                            Console.WriteLine("userDir is null");

                        goto start;
                    }
                    if (OpenKeyFileInfo is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("OpenKey is null");
                        else
                            Console.WriteLine("OpenKey is null");

                        goto start;
                    }

                    OpenKeyFileInfo.Refresh();
                    using (var key = Keccak_abstract.allocator.AllocMemory((nint) OpenKeyFileInfo.Length, "disk command: fileForOpenKey"))
                    {
                        using (var fileForOpenKey = File.OpenRead(OpenKeyFileInfo.FullName))
                        {
                            fileForOpenKey.Read(key);
                        }

                        if (isDebugMode)
                            Console.WriteLine(L("Initialization started"));

                        InitSponges(key);
                    }

                    if (isDebugMode)
                        Console.WriteLine(L("Try to mount volume"));

                    MountVolume();


                    break;
                case "end":
                    Terminated = true;
                    return ProgramErrorCode.AbandonedByUser;
                default:
                    if (!isDebugMode)
                        throw new CommandException(L("Command is unknown"));

                    Console.WriteLine(L("Command is unknown"));

                    goto start;
            }

            return ProgramErrorCode.success;
        }

        /// <summary>Полная длина двойной синхропосылки на блок файловой системы.</summary>
        public const int FullBlockSyncLen = 128;
        public const int SemiBlockSyncLen = 64;

        public KeyDataGenerator? KeyGenerator;
        public static Keccak_20200918?  keccak1, keccak2, keccakOIV;
        public static Threefish1024?    ThreeFish1, ThreeFish2, ThreeFish3;
        public void InitSponges(Record key)
        {
            CascadeSponge_mt_20230930?  Cascade_Key    = null;
            VinKekFishBase_KN_20210525? VinKekFish_Key = null;
            try
            {
                if (KeyGenerator != null)
                    throw new InvalidOperationException();

                if (!Directory.Exists(DataDir!.FullName))
                {
                    isCreatedDir = true;
                    Directory.CreateDirectory(DataDir!.FullName, UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    Process.Start("chmod", $"a+rX {DataDir.FullName}");
                }

                syncPath  = Path.Combine(DataDir.FullName, SyncName);
                var synFI = new FileInfo(syncPath); synFI.Refresh();
                if (!synFI.Exists)
                {
                    isCreatedDir = true;
                    do
                    {
                        this.Connect();
                    }
                    while (bbp.Count < 128);        // Так как длина стандартного блока, возвращаемого /dev/vkf/random, равна 404-ём байтам, то, скорее всего, реальная длина синхропосылки будет 404 байта. Остальное будет дополнено нулями.

                    using (var syncBytes = Keccak_abstract.allocator.AllocMemory(blockSize))
                    {
                        syncBytes.Clear();
                        bbp.GetBytesAndRemoveIt(syncBytes, bbp.Count);
                        bbp.Clear();

                        using (var syncFileDescriptor = synFI.OpenWrite())
                        {
                            syncFileDescriptor.Write(syncBytes);
                        }

                        using (var cpi = Process.Start("chmod", $"a-wx \"{synFI.FullName}\""))
                        {
                            cpi.WaitForExit();
                        }
                        using (var cpi = Process.Start("chmod", $"a+r \"{synFI.FullName}\""))
                        {
                            cpi.WaitForExit();
                        }
                        using (var cpi = Process.Start("chattr", $"+i \"{synFI.FullName}\""))
                        {
                            cpi.WaitForExit();
                        }
                    }

                    // Обновляем содержимое записи, так как файл мы только что создали
                    synFI.Refresh();
                }

                Cascade_Key = new CascadeSponge_mt_20230930(512) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.elevated };
                using (var syncBytes = Keccak_abstract.allocator.AllocMemory((nint) synFI.Length))
                {
                    using (var syncFileDescriptor = File.OpenRead(syncPath))
                    {
                        syncFileDescriptor.Read(syncBytes);
                    }

                    Cascade_Key.InitKeyAndOIV(key, OIV: syncBytes);
                }

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

#pragma warning restore CS0103
#pragma warning restore CA2211
#pragma warning restore CA1416