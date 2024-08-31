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

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public partial class DiskCommand : Command, IDisposable
    {
        public const  string SyncName = "sync";
        public static string syncPath = "";
        /// <summary>Метод вызывается автоматически из метода Exec. Осуществляет непосредственное монтирование и вход в цикл обработки сообщений файловой системы.</summary>
        public void MountVolume()
        {
            // Параметр -s очень важен, т.к. bytesFromFile является статическим и не может быть разделён.
            var A = new string[] {"", "-s", "-f", "-o", "noexec,nodev,nosuid,auto_unmount,noatime", tmpDir!.FullName};

            Console.CancelKeyPress += (o, e) =>
            {
                e.Cancel = true;
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

            uid = geteuid();
            gid = getegid();

            var r = fuse_main_real(A.Length, A, fuseOperations, Marshal.SizeOf(fuseOperations[0]), 0);
        }

        private static void ProcessExit(PosixSignalContext context)
        {
            context.Cancel = true;
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
        public static (nint file, nint position, nint size) getPosition(nint position, nint size)
        {
            var positionInFile = position & blockSizeMask;
            var file           = position >> blockSizeShift;
            if (size > blockSize - positionInFile)
                size = blockSize - positionInFile;

            return (file, positionInFile, size);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static nint fuse_read(byte*  path, byte*  buffer, nint size, long position, FuseFileInfo * fileInfo)
        {
            var fileName = Utf8StringMarshaller.ConvertToManaged(path);

            if (position + size > (long) FileSize)
                size = (nint) ((long) FileSize - position);

            for (nint i = 0; i < size;)
            {
                var pos = getPosition(i + (nint) position, size - i);

                var fn = GetFileNumberName(pos);
                if (File.Exists(fn))
                {
                    #warning !!!!!!!!!
                    var bytes = File.ReadAllBytes(fn);
                    for (nint j = 0; j < pos.size; j++, i++)
                    {
                        buffer[i] = bytes[pos.position + j];
                    }
                }
                else
                {
                    for (nint j = 0; j < pos.size; j++, i++)
                    {
                        buffer[i] = 0;
                    }
                }
            }

            return size;
        }

        private static Record bytesFromFile = Keccak_abstract.allocator.AllocMemory(blockSize);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static nint fuse_write(byte*  path, byte*  buffer, nint size, long position, FuseFileInfo * fileInfo)
        {
            var fileName = Utf8StringMarshaller.ConvertToManaged(path);

            if (fileName != vinkekfish_file_path)
            {
                return - (nint) PosixResult.ENOENT;
            }

            if (position + size > (long) FileSize)
                size = (nint) ((long) FileSize - position);

            for (nint i = 0; i < size;)
            {
                var pos = getPosition(i + (nint)position, size - i);

                var fn     = GetFileNumberName(pos);
                var isNull = false;
                using (var file = File.Open(fn, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    if (file.Length > 0)
                    {
                        file.Read(bytesFromFile);
                    }

                    for (nint j = 0; j < pos.size; j++, i++)
                    {
                        bytesFromFile[pos.position + j] = buffer[i];
                    }
#warning ВСТАВИТЬ ШИФРОВАНИЕ
                    isNull = IsNull(bytesFromFile);
                    file.Write(bytesFromFile);
                }

                if (isNull)
                    File.Delete(fn);
            }

            return size;
        }

        public static string GetFileNumberName((nint file, nint position, nint size) pos)
        {
            return Path.Combine(DataDir!.FullName, pos.file.ToString("D16"));
        }

        /// <summary>Безопасно узнаёт, не является ли блок состоящим из одних нулей.</summary>
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

            (*stat).blocks = FileSize / blockSize;
            (*stat).frsize = blockSize;
            (*stat).bsize  = blockSize;

            return (int) PosixResult.Success;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static void * fuse_init(void * connect, FuseConfig * config)
        {
            var st = (byte *) config;
            for (int i = 0; i < sizeof(FuseConfig); i++, st++)
                *st = 0;

            config->direct_io    = 0;
            config->kernel_cache = 1;

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
                        pi!.WaitForExit(10_000);    // Здесь всё равно может подвиснуть на чтении незакрытого потока стандартного вывода
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
                            if (isCreatedDir)
                            {
                                Console.WriteLine(L("Program begin formatting the section..."));
                                var iSize = 4096;
                                if (FileSize > 1024*1024*1024)
                                    iSize = 16384;
                                else
                                if (FileSize > 64*1024*1024)
                                    iSize = 8192;

                                // Это форматирование файловой системы пользователя.
                                pif = Process.Start("mke2fs", $"-t ext4 -b 4096 -I 1024 -i {iSize} -C 64k -m 0 -J size=4 -O extent,bigalloc,inline_data,flex_bg,^resize_inode,^dir_index,^dir_nlink,^metadata_csum" + " " + loopDev);
                                pif.WaitForExit();
                            }
                            pif = Process.Start("chown", $"{Rights} {loopDev}");
                            pif.WaitForExit();
                            pif = Process.Start("mount", $"-o discard,noexec,nodev,nosuid {loopDev} \"{UserDir!.FullName}\"");
                            pif.WaitForExit();
                            pif = Process.Start("chown", $"{Rights} \"{UserDir!.FullName}\"");
                            pif.WaitForExit();

                            Console.WriteLine($"Started with loop device " + loopDev);
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
    }
}


#pragma warning restore CS0103
#pragma warning restore IDE1006
#pragma warning restore CA1416
#pragma warning restore CA2211
