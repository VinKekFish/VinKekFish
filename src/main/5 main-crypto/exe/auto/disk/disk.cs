// TODO: tests
using System.Runtime;

#pragma warning disable CS0103
#pragma warning disable IDE1006
#pragma warning disable CA1416
#pragma warning disable CA2211

namespace VinKekFish_EXE;

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
using System.Runtime.InteropServices.Marshalling;
using CodeGenerated.Cryptoprimes;
using System.Runtime.Intrinsics.X86;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public partial class DiskCommand : Command, IDisposable
    {
        /// <summary>Задаёт длину случайного вхождения в файле главой синхропосылки (SyncName).</summary>
        public const  int    SyncRandomLength = 4096;
        public const  string SyncName = "sync";
        public static string syncPath = "";
        public const  string SyncBackupName  = "backup-";     // Файл для бэкапа текущих изменений синхропосылок блока
        public const  string LockFile = "lock";
        /// <summary>Метод вызывается автоматически из метода Exec. Осуществляет непосредственное монтирование и вход в цикл обработки сообщений файловой системы.</summary>
        public void MountVolume()
        {
            // Параметр -s очень важен, т.к. bytesFromFile является статическим и не может быть разделён.
            var A = new string[] {"", "-s", "-f", "-o", "noexec,nodev,nosuid,auto_unmount,noatime", tmpDir!.FullName};

            Console.CancelKeyPress += (o, e) =>
            {
                e.Cancel = !destroyed;
                ProcessExit();
            };
            AppDomain.CurrentDomain.UnhandledException += 
            delegate
            {
                ProcessExit();  
            };
            PosixSignalRegistration.Create(PosixSignal.SIGINT,  ProcessExit);
            PosixSignalRegistration.Create(PosixSignal.SIGQUIT, ProcessExit);
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, ProcessExit);

            // DataDir создаётся при инициализации

            if (!Directory.Exists(tmpDir!.FullName))
            {
                Directory.CreateDirectory(tmpDir!.FullName, UnixFileMode.None);
            }

            if (!Directory.Exists(UserDir!.FullName))
            {
                Directory.CreateDirectory(UserDir!.FullName, UnixFileMode.None);
            }

            FuseOperations* fuseOperations = stackalloc FuseOperations[1];
            fuseOperations->read    = &fuse_read;
            fuseOperations->write   = &fuse_write;
            fuseOperations->getattr = &fuse_getattr;
            fuseOperations->readdir = &fuse_readDir;
            fuseOperations->statfs  = &fuse_statfs;
            fuseOperations->init    = &fuse_init;
            fuseOperations->destroy = &fuse_destroy;

            uid = geteuid();
            gid = getegid();

            FileNumberFormatString = "D" + (int) Math.Ceiling( Math.Log10(FileSize) );

            var r = fuse_main_real(A.Length, A, fuseOperations, Marshal.SizeOf(fuseOperations[0]), 0);
        }

        public static string FileNumberFormatString = "";

        private static void ProcessExit(PosixSignalContext context)
        {
            context.Cancel = !destroyed;
            ProcessExit();
        }

        private static void ProcessExit()
        {
            ThreadPool.QueueUserWorkItem
            (
                delegate
                {
                    var pus = Process.Start("umount", $"\"{UserDir!.FullName}\"");
                    pus.WaitForExit();

                    if (!string.IsNullOrEmpty(loopDev))
                    {
                        var args = $"-d {loopDev}";
                        using var pi = Process.Start("losetup", args);
                        pi.WaitForExit();
                    }

                    Process.Start("umount", "\"" + tmpDir!.FullName + "\"").Dispose();
                }
            );
        }

        const string vinkekfish_file_name = "vinkekfish_file";
        const string vinkekfish_file_path = "/" + vinkekfish_file_name;
        readonly static byte * ptr_vinkekfish_file_name = Utf8StringMarshaller.ConvertToUnmanaged(vinkekfish_file_name);

        // Если это изменить, то старые диски перестанут корректно открываться (или надо проверять их размер блока)
        // От этого также зависит форматирование диска (размер кластера), хотя открываться, при этом, будут любые размеры кластеров.
        const int blockSizeShift = 16;
        const int blockSize      =  1 << blockSizeShift;
        const int blockSizeMask  = (1 << blockSizeShift) - 1;
        public static (nint file, nint position, nint size, nint catFile, nint catPos) getPosition(nint position, nint size)
        {
            var positionInFile = position & blockSizeMask;
            var file           = position >> blockSizeShift;
            var catFile        = file >> 9;                 // Это количество FullBlockSyncLen в blockSize
            if (size > blockSize - positionInFile)
                size = blockSize - positionInFile;

            var catPos = file & 511;

            return (file, positionInFile, size, catFile, catPos);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static nint fuse_read(byte*  path, byte*  buffer, nint size, long position, FuseFileInfo * fileInfo)
        {
            if (destroyed)
                throw new InvalidOperationException();

            var fileName = Utf8StringMarshaller.ConvertToManaged(path);

            if (fileName != vinkekfish_file_path)
            {
                if (fileName == "/")
                    return - (int) PosixResult.EOPNOTSUPP;

                return - (int) PosixResult.ENOENT;
            }

            if (position + size > (long) FileSize)
                size = (nint) ((long) FileSize - position);

            CorrectLockFileIfExists();

            for (nint i = 0; i < size;)
            {
                var pos = getPosition(i + (nint) position, size - i);

                var (fn, bfn) = GetFileNumberName   (pos);
                var (cf, bcf) = GetCatFileNumberName(pos);

                try
                {
                    using (var file = File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        file.Read(bytesFromFile);
                    }
                    using (var catFile = File.Open(cf, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        catFile.Seek(pos.catPos, SeekOrigin.Begin);
                        catFile.Read(sync1);
                        catFile.Read(sync2);
                    }

                    // Расшифрование данных
                    DoDecrypt(pos);

                    GetHash(block64, pos.file);
                    if (!SecureCompareFast(sync2, block64))
                    {
                        Console.WriteLine("Hash is incorrect for block: " + fn);
                        return -(nint)PosixResult.EINTEGRITY;
                    }

                    for (nint j = 0; j < pos.size; j++, i++)
                    {
                        buffer[i] = bytesFromFile[pos.position + j];
                    }
                }
                catch (FileNotFoundException)
                {
                    BytesBuilder.ToNull(pos.size, buffer + i);
                    i += pos.size;
                }
            }

            bytesFromFile.Clear();

            return size;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static nint fuse_write(byte* path, byte* buffer, nint size, long position, FuseFileInfo * fileInfo)
        {
            if (destroyed)
                throw new InvalidOperationException();

            var fileName = Utf8StringMarshaller.ConvertToManaged(path);
            if (fileName != vinkekfish_file_path)
            {
                return - (nint) PosixResult.ENOENT;
            }

            if (position + size > (long) FileSize)
                size = (nint) ((long) FileSize - position);

            CorrectLockFileIfExists();

            for (nint i = 0; i < size;)
            {
                var pos = getPosition(i + (nint)position, size - i);

                var (fn, bfn) = GetFileNumberName   (pos);
                var (cf, bcf) = GetCatFileNumberName(pos);
                var isNull = false;
                var notExists = false;

                try
                {
                    using (var file = File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        file.Read(bytesFromFile);
                    }
                    using (var catFile = File.Open(cf, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        catFile.Seek(pos.catPos, SeekOrigin.Begin);
                        catFile.Read(sync1);
                        catFile.Read(sync2);
                    }

                    // Расшифрование данных
                    DoDecrypt(pos);

                    GetHash(block64, pos.file);
                    if (!SecureCompareFast(sync2, block64))
                    {
                        Console.WriteLine("Hash is incorrect (in write function) for block: " + fn);
                        return -(nint)PosixResult.EINTEGRITY;
                    }

                    for (nint j = 0; j < pos.size; j++, i++)
                    {
                        bytesFromFile[pos.position + j] = buffer[i];
                    }

                    // Копируем содержимое файла категорий
                    File.Copy(cf, bcf);

                    isNull = IsNull(bytesFromFile);
                    if (!isNull)
                    {
                        GenerateNewSync(pos);
                        DoEncrypt(pos, sync3, sync4);

#warning !
                        File.WriteAllText(LockFile, "");
                        // Новый файл с новым содержимым файла
                        using (var file = File.Open(bfn, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            file.Write(bytesFromFile);
                        }
                        // Готовим новый файл категорий
                        using (var catFile = File.Open(bcf, FileMode.Open, FileAccess.Write, FileShare.None))
                        {
                            catFile.Seek(pos.catPos, SeekOrigin.Begin);
                            catFile.Write(sync3);
                            catFile.Write(sync4);
                        }
                    }
                    else
                    {
                        using (var catFile = File.Open(bcf, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            catFile.Seek (pos.catPos, SeekOrigin.Begin);
                            catFile.Write(nullBlock, 0, FullBlockSyncLen);

                            catFile.Seek(0, SeekOrigin.Begin);
                            catFile.Read(bytesFromFile);
                        }

                        if (IsNull(bytesFromFile))
                        {
                            File.Delete(bcf);
Console.WriteLine("DELETED bcf: " + bcf);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    notExists = true;
                    bytesFromFile.Clear();                      // Из несуществующего файла мы считали одни нули
                    for (nint j = 0; j < pos.size; j++, i++)
                    {
                        bytesFromFile[pos.position + j] = buffer[i];
                    }

                    isNull = IsNull(bytesFromFile);

                    if (!isNull)
                    {
                        File.WriteAllText(LockFile, "");
                        if (File.Exists(cf))
                            File.Copy(cf, bcf);

                        using (var catFile = File.Open(bcf, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                        {
                            if (catFile.Length == 0)
                                catFile.Write(nullBlock);

                            catFile.Seek(pos.catPos, SeekOrigin.Begin);
                            catFile.Read(sync1);
                            catFile.Read(sync2);

#warning убрать очистку
                            GenerateNewSync(pos);
                            DoEncrypt(pos, sync3, sync4);

                            catFile.Seek(pos.catPos, SeekOrigin.Begin);
                            catFile.Write(sync3);
                            catFile.Write(sync4);
                        }

                        using (var file = File.Open(bfn, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            file.Write(bytesFromFile);
                        }
                    }
                    else
                    {

                    }
                }

                if (!isNull || !notExists)
                {
                    if (destroyed)
                        throw new InvalidOperationException();

                    File.WriteAllText(LockFile, "1");

                    SafelyDeleteBlockFile(fn);
                    SafelyDeleteBlockFile(cf);

                    if (!isNull)
                    {
                        File.Move(bfn, fn);
                    }

                    if (File.Exists(bcf))
                        File.Move(bcf, cf);

                    File.Delete(LockFile);
                }

#warning Вставить проверку на то, что файл cat также является весь нулевым. И вставить обнуление ячеек файла cat при удалении этого файла.
            }

            bytesFromFile.Clear();

            return size;
        }

        public static void SafelyDeleteBlockFile(string fn)
        {
            if (File.Exists(fn))
            {
                using (var file = File.OpenWrite(fn))
                {
                    file.Write(nullBlock);
                }
                File.Delete(fn);
            }
        }

        private static void CorrectLockFileIfExists()
        {
            if (!File.Exists(LockFile))
                return;

            Console.WriteLine(L("Lock file detected") + ".");
            Console.WriteLine(L("An attempt is being made to restore the file system") + ".");

            var lf    = new FileInfo(LockFile); lf.Refresh();
            var files = DataDir!.GetFiles(SyncBackupName + "*");
            if (lf.Length > 0)
            {
                foreach (var file in files)
                {
                    // Вычисляем имя основного файла, после чего удалим основной файл и заменим его файлом бэкапа
                    var fn = Path.Combine(DataDir.FullName, file.Name.Substring(SyncBackupName.Length));
                    if (File.Exists(fn))
                    {
                        File.WriteAllBytes(fn, nullBlock);
                        File.Delete(fn);
                    }

                    file.MoveTo(fn, false);

                    Console.WriteLine(L("File resored") + ": " + fn);
                }
            }
            else
            {
                foreach (var file in files)
                {
                    file.Delete();
                    Console.WriteLine(L("An uninitialized file has been deleted") + ": " + file.FullName);
                }
            }

            File.Delete(LockFile);
        }

        /// <summary>Рассчитывает и возвращает имена файла и backup-файла</summary>
        /// <param name="pos">Задаёт позицию на диске, для которой необходимо найти файл. Используется только pos.file.</param>
        /// <returns>(Имя файла данных, имя backup-файла данных)</returns>
        public static (string, string) GetFileNumberName((nint file, nint position, nint size, nint catFile, nint catPos) pos)
        {
            var baseFileName = pos.file.ToString(FileNumberFormatString);
            return
            (
                Path.Combine(DataDir!.FullName,                  baseFileName),
                Path.Combine(DataDir!.FullName, SyncBackupName + baseFileName)
            );
        }

        /// <summary>Рассчитывает и возвращает имена файла и backup-файла</summary>
        /// <param name="pos">Задаёт позицию на диске, для которой необходимо найти файл. Используется только pos.file.</param>
        /// <returns>(имя файла категорий, имя backup-файла категорий)</returns>
        public static (string, string) GetCatFileNumberName((nint file, nint position, nint size, nint catFile, nint catPos) pos)
        {
            var baseFileName = "cat" + pos.catFile.ToString(FileNumberFormatString);
            return
            (
                Path.Combine(DataDir!.FullName,                  baseFileName),
                Path.Combine(DataDir!.FullName, SyncBackupName + baseFileName)
            );
        }

        /// <summary>Безопасно (с точки зрения тайминг-атак) узнаёт, не является ли блок состоящим из одних нулей.</summary>
        /// <param name="bytes">Блок для проверки. Размер должен быть кратен 8-ми байтам.</param>
        /// <returns>true, если блок состоит из одних нулей.</returns>
        private static bool IsNull(Record bytes)
        {
            if ((bytes.len & 7) > 0)
                throw new ArgumentOutOfRangeException("IsNull: (bytes.len & 7) > 0");

            var  a  = (long *) bytes.array;
            var  ln = bytes.len >> 3;
            long v  = 0;
            for (int i = 0; i < ln; i++, a++)
            {
                v |= *a;
            }

            return v == 0;
        }

        private static void DoDecrypt((nint file, nint position, nint size, nint catFile, nint catPos) pos)
        {
            // Первый проход расшифрования (начинаем со второй губки и второго ключа)
            keccak2!.CloneStateTo(keccakA!);
            keccakA!.DoInitFromKey(sync1, 0);
            keccakA!.DoInitFromKey(sync2, 1);
            BytesBuilder.CopyTo(syncNumber2, block128);
            BytesBuilder.ULongToBytes((ulong)pos.file, block128, (pos.file & 1) * 8);
            Threefish_Static_Generated.Threefish1024_step(ThreeFish2s!.key, ThreeFish2s.tweak, block128);
            keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, 2);

            for (int j = 0, k = 0; j < bytesFromFile.len; j += KeccakPrime.BlockLen, k++)
            {
                keccakA.DoXor      (bytesFromFile, KeccakPrime.BlockLen, j);
                BytesBuilder.CopyTo(bytesFromFile, block64, index: j);
                BytesBuilder.CopyTo(blockSync2,    block128);
                BytesBuilder.CopyTo(block64,       block128, (k & 1) * 64);
                Threefish_Static_Generated.Threefish1024_step(ThreeFish2b!.key, ThreeFish2b.tweak, block128);
                keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, (byte) k);
            }

            BytesBuilder.ReverseBytes(bytesFromFile.len, bytesFromFile);

            // Второй проход расшифрования
            keccak1!.CloneStateTo(keccakA!);
            keccakA!.DoInitFromKey(sync1, 0);
            BytesBuilder.CopyTo(syncNumber1, block128);
            BytesBuilder.ULongToBytes((ulong)pos.file, block128, (pos.file & 1) * 8);
            Threefish_Static_Generated.Threefish1024_step(ThreeFish1s!.key, ThreeFish1s.tweak, block128);
            keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, 1);

            for (int j = 0, k = 0; j < bytesFromFile.len; j += KeccakPrime.BlockLen, k++)
            {
                keccakA.DoXor      (bytesFromFile, KeccakPrime.BlockLen, j);
                BytesBuilder.CopyTo(bytesFromFile, block64, index: j);
                BytesBuilder.CopyTo(blockSync1,    block128);
                BytesBuilder.CopyTo(block64,       block128, (k & 1) * 64);
                Threefish_Static_Generated.Threefish1024_step(ThreeFish1b!.key, ThreeFish1b.tweak, block128);
                keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, (byte) k);
            }
        }

        private static void DoEncrypt((nint file, nint position, nint size, nint catFile, nint catPos) pos, Record sync1, Record sync2)
        {
            // Первый проход шифрования
            keccak1!.CloneStateTo(keccakA!);
            keccakA!.DoInitFromKey(sync1, 0);
            BytesBuilder.CopyTo(syncNumber1, block128);
            BytesBuilder.ULongToBytes((ulong)pos.file, block128, (pos.file & 1) * 8);
            Threefish_Static_Generated.Threefish1024_step(ThreeFish1s!.key, ThreeFish1s.tweak, block128);
            keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, 1);

            for (int j = 0, k = 0; j < bytesFromFile.len; j += KeccakPrime.BlockLen, k++)
            {
                BytesBuilder.CopyTo(bytesFromFile, block64, index: j);
                keccakA.DoXor      (bytesFromFile, KeccakPrime.BlockLen, j);
                BytesBuilder.CopyTo(blockSync1, block128);
                BytesBuilder.CopyTo(block64,    block128, (k & 1) * 64);
                Threefish_Static_Generated.Threefish1024_step(ThreeFish1b!.key, ThreeFish1b.tweak, block128);
                keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, (byte)k);
            }

            GetHash(sync2, pos.file);

            BytesBuilder.ReverseBytes(bytesFromFile.len, bytesFromFile);

            // Второй проход шифрования
            keccak2!.CloneStateTo(keccakA!);
            keccakA!.DoInitFromKey(sync1, 0);
            keccakA!.DoInitFromKey(sync2, 1);
            BytesBuilder.CopyTo(syncNumber2, block128);
            BytesBuilder.ULongToBytes((ulong)pos.file, block128, (pos.file & 1) * 8);
            Threefish_Static_Generated.Threefish1024_step(ThreeFish2s!.key, ThreeFish2s.tweak, block128);
            keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, 2);

            for (int j = 0, k = 0; j < bytesFromFile.len; j += KeccakPrime.BlockLen, k++)
            {
                BytesBuilder.CopyTo(bytesFromFile, block64, index: j);
                keccakA.DoXor      (bytesFromFile, KeccakPrime.BlockLen, j);
                BytesBuilder.CopyTo(blockSync2,    block128);
                BytesBuilder.CopyTo(block64,       block128, (k & 1) * 64);
                Threefish_Static_Generated.Threefish1024_step(ThreeFish2b!.key, ThreeFish2b.tweak, block128);
                keccakA.DoInitFromKey(block128, KeccakPrime.BlockLen, (byte)k);
            }
        }

        private static void GetHash(Record sync2, nint file)
        {
            keccakA!.DoOutput  (sync2,      KeccakPrime.BlockLen);          // Выдать первичный хеш
            BytesBuilder.CopyTo(blockSyncH, block128);                      // Зашифровать первичный хещ
            BytesBuilder.CopyTo(sync2,      block128, (file & 1) * 64);
            Threefish_Static_Generated.Threefish1024_step(ThreeFishHash!.key, ThreeFishHash.tweak, block128);
            BytesBuilder.CopyTo(block128,   sync2);
                                                                            // Получить от зашифрованного хеша главный хеш
            keccakA.DoInitFromKey(sync2, KeccakPrime.BlockLen, 2);
            keccakA.DoOutput     (sync2, KeccakPrime.BlockLen);
        }

        /// <summary>Генерирует новую случайную синхропосылку для блока pos. Результат выдаётся в статический массив sync3.</summary>
        /// <param name="pos">Описатель позиции на диске, для которой генерируется новая синхропосылка</param>
        private static void GenerateNewSync((nint file, nint position, nint size, nint catFile, nint catPos) pos)
        {
            keccakOIV!.CloneStateTo(keccakA!);
            BytesBuilder.CopyTo(syncNumber3, block128);
            BytesBuilder.ULongToBytes((ulong)pos.file, block128);
            BytesBuilder.ULongToBytes((ulong) DateTime.Now.Ticks, block128, 8);
            Threefish_Static_Generated.Threefish1024_step(ThreeFish3s!.key, ThreeFish3s.tweak, block128);
            keccakA!.DoInitFromKey(block128, KeccakPrime.BlockLen, 0);

            keccakA.DoInitFromKey(sync1, 1);
            keccakA.DoInitFromKey(sync2, 2);
            keccakA.DoOutput(sync3, KeccakPrime.BlockLen);

            // Очень маловероятное событие.
            // Но нули являются служебными, поэтому допустить их появление нельзя.
            if (IsNull(sync3))
                sync3[0] = 1;

        }

        private static Record bytesFromFile = Keccak_abstract.allocator.AllocMemory(blockSize);
        private static Record sync1         = Keccak_abstract.allocator.AllocMemory(KeccakPrime.BlockLen);
        private static Record sync2         = Keccak_abstract.allocator.AllocMemory(KeccakPrime.BlockLen);
        private static Record sync3         = Keccak_abstract.allocator.AllocMemory(KeccakPrime.BlockLen);
        private static Record sync4         = Keccak_abstract.allocator.AllocMemory(KeccakPrime.BlockLen);
        private static Record block64       = Keccak_abstract.allocator.AllocMemory(KeccakPrime.BlockLen);
        private static Record syncNumber1   = Keccak_abstract.allocator.AllocMemory(Threefish_slowly.keyLen);
        private static Record syncNumber2   = Keccak_abstract.allocator.AllocMemory(Threefish_slowly.keyLen);
        private static Record syncNumber3   = Keccak_abstract.allocator.AllocMemory(Threefish_slowly.keyLen);
        private static Record blockSync1    = Keccak_abstract.allocator.AllocMemory(Threefish_slowly.keyLen);
        private static Record blockSync2    = Keccak_abstract.allocator.AllocMemory(Threefish_slowly.keyLen);
        private static Record blockSyncH    = Keccak_abstract.allocator.AllocMemory(Threefish_slowly.keyLen);
        private static Record block128      = Keccak_abstract.allocator.AllocMemory(Threefish_slowly.keyLen);

        private static byte[] nullBlock = new byte[blockSize];

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static int fuse_getattr(byte * fileNamePtr, FuseFileStat* stat, FuseFileInfo * fileInfo)
        {
            var dirName = Utf8StringMarshaller.ConvertToManaged(fileNamePtr);

            var st = (byte *) stat;
            for (int i = 0; i < sizeof(FuseFileStat); i++, st++)
                *st = 0;

            (*stat).blksize = blockSize;

            if (dirName == "/")
            {
                stat->uid = uid;
                stat->gid = gid;

                stat->nlink = 2;    // Это минимум,
                stat->size = 0;
                stat->mode = PosixFileMode.Directory | PosixFileMode.OwnerAll | PosixFileMode.GroupAll | PosixFileMode.OthersAll;

                return (int) PosixResult.Success;
            }
            else
            if (dirName == vinkekfish_file_path)
            {
                stat->uid = uid;
                stat->gid = gid;

                stat->nlink = 1;    // Это минимум,
                stat->size  = (long) FileSize;
                stat->mode  = PosixFileMode.Regular | PosixFileMode.OwnerRead | PosixFileMode.OwnerWrite;

                return (int) PosixResult.Success;
            }

            return - (int) PosixResult.ENOENT;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static unsafe int fuse_readDir(byte * pDirName, void * buf, delegate*unmanaged[Cdecl]<void*, byte*, void*, nint, int, int> filler, nint offset, FuseFileInfo * fi, FuseReadDirFlags flags)
        {
            var dirName = Utf8StringMarshaller.ConvertToManaged(pDirName);

            if (dirName == "/")
            {
                // Нам здесь нужно убрать лидирующий "/", чтобы вывести верные имена
                filler(buf, ptr_vinkekfish_file_name, null, 0, 0); // FUSE_FILL_DIR_DEFAULTS == 0 - это последний параметр; FUSE_READDIR_PLUS == 1
            }
            else
            {
                return - (int) PosixResult.ENOENT;
            }

            return (int) PosixResult.Success;
        }


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static unsafe nint fuse_statfs(byte * path, StatVFS * stat)
        {
            var st = (byte *) stat;
            for (int i = 0; i < sizeof(StatVFS); i++, st++)
                *st = 0;

            (*stat).blocks  = FileSize / blockSize;
            (*stat).frsize  = blockSize;
            (*stat).bsize   = blockSize;
            (*stat).files   = 2;
            (*stat).namemax = (ulong) vinkekfish_file_path.Length;

            return (int) PosixResult.Success;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static void * fuse_init(void * connect, FuseConfig * config)
        {
            var st = (byte *) config;
            for (int i = 0; i < sizeof(FuseConfig); i++, st++)
                *st = 0;

            config->direct_io     = 0;
            config->kernel_cache  = 1;
            (*config).hard_remove = 1;

            // losetup отказывается работать в самом коллбэке
            // Возможно, он виснет из-за того, что init ещё не завершился, а он уже посылает сигналы файловой системе
            ThreadPool.QueueUserWorkItem
            (
                delegate
                {
                    try
                    {
                        var psi = new ProcessStartInfo();
                        psi.UseShellExecute = false;
                        psi.RedirectStandardOutput = true;
                        psi.FileName  = "losetup";
                        var tmpFile   = Path.Combine(tmpDir!.FullName, vinkekfish_file_name);
                        psi.Arguments = $"-f --show -- \"{tmpFile}\"";

                        var pi = Process.Start(psi);
                        if (!pi!.WaitForExit(10_000))
                        {
                            try{ pi.Kill(true); } catch{}
                            Console.Error.WriteLine("ERROR: losetup is hung. This is an unexpected error.");
                            return;
                        }
                        // Здесь всё равно может подвиснуть на чтении незакрытого потока стандартного вывода
                        loopDev = pi.StandardOutput.ReadToEnd().Trim();     // Может содержать перевод строки

                        var exists = true;
                        if (loopDev.Length < 2 || loopDev.Contains('\n'))
                            exists = false;
                        else
                        {
                            var fi = new FileInfo(loopDev);
                            if (!fi.Exists)
                                exists = false;
                        }

                        if (exists)
                        {
                            Process? pif = null;
                            if (isCreatedDir || ForcedFormatFlag)
                            {
                                Console.WriteLine(L("Program begin formatting the section..."));

                                // Проверить верность форматирования можно с помощью команды file -s /dev/loop
                                // В команде подставить верный номер loop устройства
                                var iN = FileSize >> 16;
                                // Это форматирование файловой системы пользователя.
                                // pif = Process.Start("mke2fs", $"-t ext4 -b 4096 -I 1024 -N {iN} -C 64k -m 0 -J size=4 -O extent,bigalloc,inline_data,flex_bg,resize_inode,sparse_super2,dir_nlink,^dir_index,^metadata_csum" + " " + loopDev);
                                pif = Process.Start("mke2fs", $"-t ext4 -b 1024 -I 256 -N {iN} -m 0 -J size=1 -O extent,flex_bg,resize_inode,sparse_super2,dir_nlink,^dir_index,^metadata_csum" + " " + loopDev);
                                pif.WaitForExit();
                            }
                            pif = Process.Start("chown", $"{Rights} {loopDev}");
                            pif.WaitForExit();
                            // noexec, nosuid ???? Опции надо бы добавить???
                            pif = Process.Start("mount", $"-o discard,relatime,lazytime {loopDev} \"{UserDir!.FullName}\"");
                            pif.WaitForExit();
                            if (Rights.Length > 0)
                            {
                                pif = Process.Start("chown", $"{Rights} \"{UserDir!.FullName}\"");
                                pif.WaitForExit();
                            }

                            Console.WriteLine($"Started with loop device\r\n" + loopDev);
                        }
                        else
                            Console.WriteLine("ERROR: loop device not mounted: " + loopDev);
                    }
                    finally
                    {
                    }
                }
            );

            return null;
        }

        private static bool destroyed = false;

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static void fuse_destroy(void * data)
        {
            TryToDispose(bytesFromFile);
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
            TryToDispose(block128);

            Utf8StringMarshaller.Free(ptr_vinkekfish_file_name);

            destroyed = true;
        }
    }
}


#pragma warning restore CS0103
#pragma warning restore IDE1006
#pragma warning restore CA1416
#pragma warning restore CA2211
