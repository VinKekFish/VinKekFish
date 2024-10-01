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
            TryToDispose(KeyGenerator);  KeyGenerator  = null;
            TryToDispose(keccak1);       keccak1       = null;
            TryToDispose(keccak2);       keccak2       = null;
            TryToDispose(keccakOIV);     keccakOIV     = null;
            TryToDispose(keccakA);       keccakA       = null;
            TryToDispose(ThreeFish1s);   ThreeFish1s   = null;
            TryToDispose(ThreeFish2s);   ThreeFish2s   = null;
            TryToDispose(ThreeFish3s);   ThreeFish3s   = null;
            TryToDispose(ThreeFish1b);   ThreeFish1b   = null;
            TryToDispose(ThreeFish2b);   ThreeFish2b   = null;
            TryToDispose(ThreeFish3b);   ThreeFish3b   = null;
            TryToDispose(ThreeFishHash); ThreeFishHash = null;

            DisposeBuffers();

            base.Dispose(fromDestructor);

            dc = null;
        }

        private static void DisposeBuffers()
        {
            if (block128.array == null)
                return;

            TryToDispose(bytesFromFile);
            TryToDispose(catBytes);
            TryToDispose(sync1);
            TryToDispose(sync2);
            TryToDispose(sync3);
            TryToDispose(sync4);
            TryToDispose(block64);
            TryToDispose(syncNumber1);
            TryToDispose(syncNumber2);
            TryToDispose(syncNumber3);
            TryToDispose(blockSync1);
            TryToDispose(blockSync2);
            TryToDispose(blockSyncH);
            TryToDispose(block128);

            System.Runtime.InteropServices.Marshalling.Utf8StringMarshaller.Free
                (ptr_vinkekfish_file_name);
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

        /// <summary>Если true, то директория с диском была создана программой в этом запуске, а не существовала ранее.</summary>
        public static bool   isFirstTimeCreatedDir = false;                     /// <summary>Отформатировать раздел, даже если он проинициализирован.</summary>
        public static bool   ForcedFormatFlag      = false;                     /// <summary>Удаление без перезатирания.</summary>
        public static bool   FastDeleteFlag        = false;
        public static string Rights                = "#0:#0";

        public override ProgramErrorCode Exec(ref StreamReader? sr)
        {
            string val = "";
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
                        fast-delete:true
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
                case "fast-delete":
                        val = command.value.Trim().ToLowerInvariant();
                        FastDeleteFlag = val == "true" || val == "1" || val == "yes";

                        if (isDebugMode)
                        {
                            if (FastDeleteFlag)
                                Console.WriteLine("fast-delete: true");
                            else
                                Console.WriteLine("fast-delete: false");
                        }

                        goto start;
                case "forced-format":
                        val = command.value.Trim().ToLowerInvariant();
                        ForcedFormatFlag = val == "true" || val == "1" || val == "yes";

                        if (isDebugMode)
                        {
                            if (ForcedFormatFlag)
                                Console.WriteLine("forced-format: true");
                            else
                                Console.WriteLine("forced-format: false");
                        }

                        goto start;
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

                    TryToDispose(sr); sr = null;
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

        /// <summary>Полная длина двойной синхропосылки на блок файловой системы. При изменении этого нужно также изменить расчёт номера файла категории (он завязан на эту константу, но эта константа там не используется).</summary>
        public const int FullBlockSyncLen = 128;
        public const int SemiBlockSyncLen = 64; // Это должно совпадать с блоком keccak.
        // Много чего завязано на то, что блок синхропосылки совпадает с временным массивом block.

        public KeyDataGenerator? KeyGenerator;
        public static Keccak_20200918?  keccak1, keccak2, keccakOIV, keccakA;
        public static Threefish1024?    ThreeFish1s, ThreeFish2s, ThreeFish3s;  // Для синхропосылок
        public static Threefish1024?    ThreeFish1b, ThreeFish2b, ThreeFish3b;  // Непосредственно для блоков с обратной связью. ThreeFish3b, кажется, нигде не используется
        public static Threefish1024?    ThreeFishHash;
        public void InitSponges(Record key)
        {
            CascadeSponge_mt_20230930?  Cascade_Key    = null;
            VinKekFishBase_KN_20210525? VinKekFish_Key = null;
            try
            {
                if (KeyGenerator != null)
                    throw new InvalidOperationException();

                var di = new DirectoryInfo(DataDir!.FullName); di.Refresh();
                if (!di.Exists)
                {
                    isFirstTimeCreatedDir = true;
                    Directory.CreateDirectory(DataDir!.FullName, UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    // Читать директорию могут все. Это нужно, если директория синхронизируется другими программами.
                    using (var pi = Process.Start("chmod", $"a+rX \"{DataDir.FullName}\""))
                    {
                        pi.WaitForExit();
                    }
                    // Ставим невозможность удалять из директории файлы кому-то, кроме владельцев этих файлов
                    using (var pi = Process.Start("chmod", $"a+t  \"{DataDir.FullName}\""))
                    {
                        pi.WaitForExit();
                    }
                    // Делаем требования синхронности для обновлений директории, на всякий случай
                    using (var pi = Process.Start("chattr", $"+D  \"{DataDir.FullName}\""))
                    {
                        pi.WaitForExit();
                    }
                }

                syncPath  = Path.Combine(DataDir!.FullName, SyncName);
                var synFI = new FileInfo(syncPath); synFI.Refresh();
                if (!synFI.Exists)
                {
                    isFirstTimeCreatedDir = true;
                    Console.WriteLine(L("Starting the generation of the main sync of the disk") + ". " + L("It may take a couple of tens of seconds") + ".");
                    do
                    {
                        Console.Write($"{bbp.Count*100/SyncRandomLength, 3}% ");
                        // К сожалению, если запускать vkf ... & , почему-то виснет на попытке переставить курсор
                        this.Connect();
                    }
                    while (bbp.Count < SyncRandomLength);
                    Console.WriteLine("     ");

                    using (var syncBytes = Keccak_abstract.allocator.AllocMemory(SyncRandomLength))
                    {
                        syncBytes.Clear();
                        bbp.GetBytesAndRemoveIt(syncBytes);
                        bbp.Clear();

                        using (var syncFileDescriptor = synFI.OpenWrite())
                        {
                            syncFileDescriptor.Write(nullBlock);
                            syncFileDescriptor.Seek(0, SeekOrigin.Begin);
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
                    // synFI.Refresh();
                    synFI = null;
                    Console.WriteLine(L("Initialization continue"));
                }

                Cascade_Key = new CascadeSponge_mt_20230930(512) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.elevated };
                using (var syncBytes = Keccak_abstract.allocator.AllocMemory(SyncRandomLength))
                {
                    using (var syncFileDescriptor = File.OpenRead(syncPath))
                    {
                        var readed = syncFileDescriptor.Read(syncBytes);
                        if (readed != SyncRandomLength)
                            throw new InvalidOperationException("readed != SyncRandomLength for " + syncPath);
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
                keccakA   = new Keccak_20200918();

                keccak1  .DoInitFromKey(KeyGenerator.GetBytes(cryptoprime.KeccakPrime.b_size, 1, "keccak1"), 1, true);
                keccak2  .DoInitFromKey(KeyGenerator.GetBytes(cryptoprime.KeccakPrime.b_size, 2, "keccak2"), 1, true);
                keccakOIV.DoInitFromKey(KeyGenerator.GetBytes(cryptoprime.KeccakPrime.b_size, 3, "keccak3"), 1, true);

                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 1, "ThreeFish1s"))
                {
                    ThreeFish1s = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 2, "ThreeFish2s"))
                {
                    ThreeFish2s = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 3, "ThreeFish3s"))
                {
                    ThreeFish3s = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 1, "ThreeFish1b"))
                {
                    ThreeFish1b = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 2, "ThreeFish2b"))
                {
                    ThreeFish2b = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 3, "ThreeFish3b"))
                {
                    ThreeFish3b = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }
                using (var tkey = KeyGenerator.GetBytes(Threefish_slowly.keyLen + Threefish_slowly.twLen, 4, "ThreeFishHash"))
                {
                    ThreeFishHash = new Threefish1024
                    (tkey, Threefish_slowly.keyLen, tkey >> Threefish_slowly.keyLen, Threefish_slowly.twLen);
                }

                // Заполняем значение syncNumber неизвестными по умолчанию числами, чтобы было сложнее проводить криптоанализ ThreeFish.
                using (var tkey = KeyGenerator.GetBytes(syncNumber1.len, 0, "syncNumber.tkey"))
                {
                    BytesBuilder.CopyTo(tkey, syncNumber1);
                }
                // Заполняем значение syncNumber неизвестными по умолчанию числами, чтобы было сложнее проводить криптоанализ ThreeFish.
                using (var tkey = KeyGenerator.GetBytes(syncNumber2.len, 2, "syncNumber2.tkey"))
                {
                    BytesBuilder.CopyTo(tkey, syncNumber2);
                }
                // Заполняем значение syncNumber неизвестными по умолчанию числами, чтобы было сложнее проводить криптоанализ ThreeFish.
                using (var tkey = KeyGenerator.GetBytes(syncNumber3.len, 3, "syncNumber3.tkey"))
                {
                    BytesBuilder.CopyTo(tkey, syncNumber3);
                }
                // Заполняем значение blockSync неизвестными по умолчанию числами, чтобы было сложнее проводить криптоанализ ThreeFish.
                using (var tkey = KeyGenerator.GetBytes(blockSync1.len, 4, "blockSync.tkey"))
                {
                    BytesBuilder.CopyTo(tkey, blockSync1);
                }
                // Заполняем значение blockSync неизвестными по умолчанию числами, чтобы было сложнее проводить криптоанализ ThreeFish.
                using (var tkey = KeyGenerator.GetBytes(blockSync2.len, 5, "blockSync2.tkey"))
                {
                    BytesBuilder.CopyTo(tkey, blockSync2);
                }
                // Заполняем значение blockSync неизвестными по умолчанию числами, чтобы было сложнее проводить криптоанализ ThreeFish.
                using (var tkey = KeyGenerator.GetBytes(blockSyncH.len, 6, "blockSync3.tkey"))
                {
                    BytesBuilder.CopyTo(tkey, blockSyncH);
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