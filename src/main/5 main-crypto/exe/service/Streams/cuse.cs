// TODO: tests
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using cryptoprime;

namespace VinKekFish_EXE;
using static VinKekFish_Utils.Language;

/// <summary>
/// Это - синглтон
/// Класс для режима работы как службы
/// Представляет собой символьное устройство для вывода энтропии, альтернативное unix-сокетам
/// Принимает входящие запросы и высылает ответы
/// </summary>
public unsafe class CuseStream: IDisposable
{
    public readonly FileInfo fi;
    public          bool     doTerminate = false;

    public readonly Regime_Service service;

    protected static void * session = null;
    public static CuseStream? Cuse {get; protected set;}

    // Для проверки можно использовать cat path_to_socket
    public CuseStream(string path, Regime_Service service)
    {
        if (Cuse != null)
        {
            throw new ArgumentException("CuseStream: Cuse != null");
        }

        Cuse = this;
        this.service = service;

        fi = new FileInfo(Path.Combine("/dev/", path));
        fi.Refresh();
        if (fi.Exists)
        {
            fi.Delete();
        }

        var t = new Thread
        (
            () =>
            {
                var ci  = stackalloc cuse_info[1];
                var ops = stackalloc cuse_lowlevel_ops[1];

                for (var i = 0; i < sizeof(cuse_lowlevel_ops); i++)
                    ((byte*)ops)[i] = 0;

                var stra = stackalloc byte*[1];
                var name = Utf8StringMarshaller.ConvertToUnmanaged("DEVNAME=" + path + "\u0000");
                stra[0] = name;

                ci->dev_info_argc = 1;
                ci->dev_info_argv = stra;
                ci->flags         = 0;  // CUSE_UNRESTRICTED_IOCTL = 1
                ci->dev_major     = 0;
                ci->dev_minor     = 0;

                var A = new string[] {"", "-f"/*, "-d"*/};

                ops->read      = &CuseReadFunc;
                ops->open      = &CuseOpenFunc;
                ops->release   = &Cuse_release;
                ops->init      = &Cuse_init;
                ops->init_done = &Cuse_init_done;

                int multithreaded = 0;
                session = (void *) cuse_lowlevel_setup(A.Length, A, ci, ops, &multithreaded, null);

                var r = fuse_session_loop(session);

                // Завершаем работу программы
                cuse_lowlevel_teardown(session);
                GC.KeepAlive(A);
            }
        );

        t.IsBackground = true;
        t.Start();

        var cnt = 0;
        while (!fi.Exists)
        {
            Thread.Sleep(100);
            fi.Refresh();
            cnt++;
            if (cnt > 10)
            {
                Console.Error.WriteLine($"CuseStream: {L("character device")} '{fi.FullName}' {L("failed to create")}");
                return;
            }
        }

        Process.Start("chmod", $"a+r \"{fi.FullName}\"");
    }

    ~CuseStream()
    {
        if (!isDisposed)
            Close(true);
    }

    public void Dispose()
    {
        Close(false);
    }

    public bool isDisposed = false;
    public virtual void Close(bool fromDestructor = false)
    {
        if (isDisposed)
        {
            if (fromDestructor)
                return;

            BytesBuilderForPointers.Record.errorsInDispose = true;
            Console.Error.WriteLine("CuseStream.Close: Close executed twice. " + fi.FullName);

            return;
        }
        else
        if (fromDestructor)
        {
            BytesBuilderForPointers.Record.errorsInDispose = true;
            Console.Error.WriteLine("CuseStream.Close: !isDisposed from destructor. " + fi.FullName);
        }

        // Блокируем объект на случай повторных вызовов
        // Блокируем connections, т.к. мы его сейчас очищать будем
        lock (this)
        {
            if (session != null)
            {
                fuse_session_exit(session);
                try
                {
                    File.ReadAllText(fi.FullName);  // Читаем данные из символьного устройства, чтобы дать событие для завершения сессии
                }
                catch
                {}
            }

            isDisposed = true;
        }
    }
                                                                                        /// <summary>Последний выданный дескриптор для открытого файла</summary>
    public    static ulong fileHandleLast = 0;                                          /// <summary>Список выданных дескрипторов</summary>
    protected static List<ulong> fhs = new List<ulong>(128);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Cuse_init(void * a, fuse_conn_info * b)
    {
        b->max_write = 0;
        b->max_read  = 4096;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Cuse_init_done(void * user_data)
    {
    }

    // [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void CuseOpenFunc(fuse_req* request, fuse_file_info * fileInfo)
    {
        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.direct_io; // direct_io
        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.keep_cache; // keep_cache
        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.noflush; // noflush
        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.nonseekable; // nonseekable

        // Создаём дескриптор файла
        fileInfo->fh = Interlocked.Increment(ref fileHandleLast);

        // Регистрируем дескриптор файла в списке открытых файлов
        lock (fhs)
            fhs.Add(fileInfo->fh);

        fuse_reply_open(request, fileInfo);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Cuse_release(fuse_req* request, fuse_file_info * fileInfo)
    {
        // Удаляем дескриптор из открытых.
        // Скорее всего, его уже тут нет, т.к. он удаляется при первом чтении.
        // Это не требование библиотеки fuse, это просто конкретная операция,
        // чтобы понять, что по дескриптору нужно выводить данные или данные уже выведены
        lock (fhs)
            fhs.Remove(fileInfo->fh);

        fuse_reply_err(request, PosixErrorCode.Success);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void CuseReadFunc(fuse_req* request, nint size, nint offset, fuse_file_info * fileInfo)
    {
        if (offset > 0)
        {
            fuse_reply_err(request, PosixErrorCode.EOPNOTSUPP);
            return;
        }

        // Удаляем дескриптор файла из списка открытых дескрипторов.
        // Так как мы выводим информацию только один раз,
        // дальше мы будем обозначать конец файла, если дескриптора уже нет.
        bool exists = false;
        lock (fhs)
            exists = fhs.Remove(fileInfo->fh);

        // Если дескпритора не было в списке открытых - обозначить конец файла.
        // Он обозначается выводом нулевого количества байтов.
        if (!exists)
        {
            fuse_reply_buf(request, null, 0);
            return;
        }

        nint blockSize = Cuse!.service.getMinBlockSize();
        try
        {
            using (var buff = Cuse!.service.getEntropyForOut(blockSize))
            {
                // Здесь из буффера информация будет скопирована в отдельно выделенный внутри fuse объект
                fuse_reply_buf(request, buff, (int) buff.len);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("CuseStream.CuseReadFunc error");
            Console.Error.WriteLine(VinKekFish_Utils.Memory.formatException(ex));

            fuse_reply_err(request, PosixErrorCode.ENOMEM);
        }
    }


    public const string LibFuseName = "libfuse3.so.3";
    [DllImport(LibFuseName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe nint cuse_lowlevel_setup(int argc, [In, MarshalAs(UnmanagedType.LPArray)] string[] argv, cuse_info * cuseInfo, cuse_lowlevel_ops * ll_ops, int * multithreaded , void * userdata);
    [DllImport(LibFuseName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe nint fuse_session_loop(void * session);
    [DllImport(LibFuseName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_open(void * request, fuse_file_info * fileInfo);
    [DllImport(LibFuseName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_buf(void * request, byte * buf, int size);
    [DllImport(LibFuseName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_err(void * request, PosixErrorCode err);
    [DllImport(LibFuseName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_session_exit(void * fuse_session);
    [DllImport(LibFuseName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int cuse_lowlevel_teardown(void * fuse_session);


    [StructLayout(LayoutKind.Sequential/*, Pack = 4*/)]
    public struct fuse_file_info
    {
        public int   flags;
        public ulong bitFlags;
        public ulong fh, lock_owner;
        public uint  poll_events;
    }

    [StructLayout(LayoutKind.Sequential/*, Pack = 4*/)]
    public struct cuse_info
    {
        public int	dev_major;
        public int	dev_minor;
        public int	dev_info_argc;
        public byte** dev_info_argv;
        public int	flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct cuse_lowlevel_ops
    {
        public cuse_lowlevel_ops()
        {
        }

        public delegate* unmanaged[Cdecl]<void*, fuse_conn_info *, void>                   init      = null;
        public delegate* unmanaged[Cdecl]<void*, void>                                     init_done = null;
        public void *                                                               destroy   = null;
        public delegate* unmanaged[Cdecl]<fuse_req*, fuse_file_info *, void>               open;
        public delegate* unmanaged[Cdecl]<fuse_req*, nint, nint, fuse_file_info *, void>   read;
        public void *                                                               write   = null;
        public void *                                                               flush   = null;
        public delegate* unmanaged[Cdecl]<fuse_req*, fuse_file_info *, void>               release = null;
        public void *                                                               fsync   = null;
        public delegate* unmanaged[Cdecl]<fuse_req*, int, void *, fuse_file_info *, int, void *, nint, nint, void>
                                                                                    ioctl   = null;
        public void *                                                               poll    = null;

        public void * r1, r2, r3;   // Этого нет в наличии в структуре: просто доп. поля на всякий случай
    }

    [StructLayout(LayoutKind.Sequential/*, Pack = 4*/)]
    public struct fuse_conn_info
    {
        public int proto_major;
        public int proto_minor;
        public int max_write;
        public int max_read;
        public int max_readahead;
        public int capable;
        public int want;
        public int max_background;
        public int congestion_threshold;
        public int time_gran;
        public fixed int reserved[22];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct fuse_req
    {
        public void * session;
        // Здесь есть ещё и другие поля!
    }

    // https://ru.wikipedia.org/wiki/Errno.h
    // https://github.com/LTRData/FuseDotNet
    public enum PosixErrorCode : int
    {                                       /// <summary>No error</summary>
        Success = 0,
                                            /// <summary>Operation not permitted</summary>
        EPERM   = 1,                        /// <summary>No such file or directory</summary>
        ENOENT  = 2,                        /// <summary>No such process</summary>
        ESRCH   = 3,                        /// <summary>Interrupted system call</summary>
        EINTR   = 4,                        /// <summary>Input/output error</summary>
        EIO     = 5,                        /// <summary>Device not configured</summary>
        ENXIO   = 6,                        /// <summary>Argument list too long</summary>
        E2BIG   = 7,                        /// <summary>Exec format error</summary>
        ENOEXEC = 8,                        /// <summary>Bad file descriptor</summary>
        EBADF   = 9,                        /// <summary>No child processes</summary>
        ECHILD  = 10,                       /// <summary>Resource deadlock avoided</summary>
        EDEADLK = 11,                       /// <summary>Cannot allocate memory</summary>
        ENOMEM  = 12,                       /// <summary>Permission denied</summary>
        EACCES  = 13,                       /// <summary>Bad address</summary>
        EFAULT  = 14,                       /// <summary>Block device required</summary>
        ENOTBLK = 15,                       /// <summary>Device busy</summary>
        EBUSY   = 16,                       /// <summary>File exists</summary>
        EEXIST  = 17,                       /// <summary>Cross-device link</summary>
        EXDEV   = 18,                       /// <summary>Operation not supported by device</summary>
        ENODEV  = 19,                       /// <summary>Not a directory</summary>
        ENOTDIR = 20,                       /// <summary>Is a directory</summary>
        EISDIR  = 21,                       /// <summary>Invalid argument</summary>
        EINVAL  = 22,                       /// <summary>Too many open files in system</summary>
        ENFILE  = 23,                       /// <summary>Too many open files</summary>
        EMFILE  = 24,                       /// <summary>Inappropriate ioctl for device</summary>
        ENOTTY  = 25,                       /// <summary>Text file busy</summary>
        ETXTBSY = 26,                       /// <summary>File too large</summary>
        EFBIG   = 27,                       /// <summary>No space left on device</summary>
        ENOSPC  = 28,                       /// <summary>Illegal seek</summary>
        ESPIPE  = 29,                       /// <summary>Read-only filesystem</summary>
        EROFS   = 30,                       /// <summary>Too many links</summary>
        EMLINK  = 31,                       /// <summary>Broken pipe</summary>
        EPIPE   = 32,                       /// <summary>Numerical argument out of domain</summary>

        EDOM   = 33,                       /// <summary>Result too large</summary>
        ERANGE = 34,                       /// <summary>Resource temporarily unavailable</summary>

        EAGAIN      = 35,                       /// <summary>Operation now in progress</summary>
        EINPROGRESS = 36,                       /// <summary>Operation already in progress</summary>

        EALREADY    = 37,                       /// <summary>Socket operation on non-socket</summary>

        ENOTSOCK        = 38,                       /// <summary>Destination address required</summary>
        EDESTADDRREQ    = 39,                       /// <summary>Message too long</summary>
        EMSGSIZE        = 40,                       /// <summary>Protocol wrong type for socket</summary>
        EPROTOTYPE      = 41,                       /// <summary>Protocol not available</summary>
        ENOPROTOOPT     = 42,                       /// <summary>Protocol not supported</summary>
        EPROTONOSUPPORT = 43,                       /// <summary>Socket type not supported</summary>
        ESOCKTNOSUPPORT = 44,                       /// <summary>Operation not supported</summary>
        EOPNOTSUPP      = 45,                       /// <summary>Operation not supported</summary>
        ENOTSUP         = EOPNOTSUPP,               /// <summary>Protocol family not supported</summary>
        EPFNOSUPPORT    = 46,                       /// <summary>Address family not supported by protocol family</summary>
        EAFNOSUPPORT    = 47,                       /// <summary>Address already in use</summary>
        EADDRINUSE      = 48,                       /// <summary>Can't assign requested address</summary>
        EADDRNOTAVAIL   = 49,                       /// <summary>Network is down</summary>

        ENETDOWN        = 50,                       /// <summary>Network is unreachable</summary>   /* Network is down */
        ENETUNREACH     = 51,                       /// <summary>Network dropped connection on reset</summary>
        ENETRESET       = 52,                       /// <summary>Software caused connection abort</summary>
        ECONNABORTED    = 53,                       /// <summary>Connection reset by peer</summary>
        ECONNRESET      = 54,                       /// <summary>No buffer space available</summary>
        ENOBUFS         = 55,                       /// <summary>Socket is already connected</summary>
        EISCONN         = 56,                       /// <summary>Socket is not connected</summary>
        ENOTCONN        = 57,                       /// <summary>Can't send after socket shutdown</summary>
        ESHUTDOWN       = 58,                       /// <summary>Too many references: can't splice</summary>
        ETOOMANYREFS    = 59,                       /// <summary>Operation timed out</summary>
        ETIMEDOUT       = 60,                       /// <summary>Connection refused</summary>
        ECONNREFUSED    = 61,                       /// <summary>Too many levels of symbolic links/summary>

        ELOOP        = 62,                       /// <summary>File name too long</summary>
        ENAMETOOLONG = 63,                       /// <summary>Host is down</summary>

        EHOSTDOWN    = 64,                       /// <summary>No route to host</summary>
        EHOSTUNREACH = 65,                       /// <summary>Directory not empty</summary>
        ENOTEMPTY    = 66,                       /// <summary>Too many processes</summary>

        EPROCLIM    = 67,                       /// <summary>Too many users</summary>
        EUSERS      = 68,                       /// <summary>Disc quota exceeded</summary>
        EDQUOT      = 69,                       /// <summary>Stale NFS file handle</summary>

        ESTALE          = 70,                       /// <summary>Too many levels of remote in path</summary>
        EREMOTE         = 71,                       /// <summary>RPC struct is bad</summary>
        EBADRPC         = 72,                       /// <summary>RPC version wrong</summary>
        ERPCMISMATCH    = 73,                       /// <summary>RPC prog. not avail</summary>
        EPROGUNAVAIL    = 74,                       /// <summary>Program version wrong</summary>
        EPROGMISMATCH   = 75,                       /// <summary>Bad procedure for program</summary>
        EPROCUNAVAIL    = 76,                       /// <summary>No locks available</summary>

        ENOLCK          = 77,                       /// <summary>Function not implemented</summary>
        ENOSYS          = 78,                       /// <summary>Inappropriate file type or format</summary>

        EFTYPE          = 79,                       /// <summary>Authentication error</summary>
        EAUTH           = 80,                       /// <summary>Need authenticator</summary>
        ENEEDAUTH       = 81,                       /// <summary>Identifier removed</summary>
        EIDRM           = 82,                       /// <summary>No message of desired type</summary>
        ENOMSG          = 83,                       /// <summary>Value too large to be stored in data type</summary>
        EOVERFLOW       = 84,                       /// <summary>Operation canceled</summary>
        ECANCELED       = 85,                       /// <summary>Illegal byte sequence</summary>
        EILSEQ          = 86,                       /// <summary>Attribute not found</summary>
        ENOATTR         = 87,                       /// <summary>Programming error</summary>

        EDOOFUS         = 88,                       /// <summary>Bad message</summary>

        EBADMSG         = 89,                       /// <summary>Multihop attempted</summary>
        EMULTIHOP       = 90,                       /// <summary>Link has been severed</summary>
        ENOLINK         = 91,                       /// <summary>Protocol error</summary>
        EPROTO          = 92,                       /// <summary>Capabilities insufficient</summary>

        ENOTCAPABLE     = 93,                       /// <summary>Not permitted in capability mode</summary>
        ECAPMODE        = 94,                       /// <summary>State not recoverable</summary>
        ENOTRECOVERABLE = 95,                       /// <summary>Previous owner died</summary>
        EOWNERDEAD      = 96,                       /// <summary>Integrity check failed</summary>
        EINTEGRITY      = 97
    }

    [Flags]
    public enum FuseFileInfoOptions : ulong
    {                                       /// <summary>Нет опций</summary>
        none = 0x0,

        /// <summary>In case of a write operation indicates if this was caused by a
        ///    writepage </summary>
        write_page = 0x1,

        /// <summary>Can be filled in by open, to use direct I/O on this file.
        ///     Introduced in version 2.4 </summary>
        direct_io = 0x2,

        /// <summary>Can be filled in by open, to indicate, that cached file data
        ///     need not be invalidated.  Introduced in version 2.4 </summary>
        keep_cache = 0x4,

        /// <summary>Indicates a flush operation.  Set in flush operation, also
        ///     maybe set in highlevel lock operation and lowlevel release
        ///     operation.  Introduced in version 2.6 </summary>
        flush = 0x8,

        /// <summary>Can be filled in by open, to indicate that the file is not
        /// seekable.  Introduced in version 2.8 </summary>
        nonseekable = 0x10,

        /// <summary>Indicates that flock locks for this file should be
        /// released.  If set, lock_owner shall contain a valid value.
        /// May only be set in ->release().  Introduced in version
        /// 2.9 </summary>
        flock_release = 0x20,

        /// <summary>Can be filled in by opendir. It signals the kernel to
        ///     enable caching of entries returned by readdir().  Has no
        ///     effect when set in other contexts (in particular it does
        ///     nothing when set by open()). </summary>
        cache_readdir = 0x40,

        /// <summary>Can be filled in by open, to indicate that flush is not needed
        ///     on close. </summary>
        noflush = 0x80,
    }
}
