// dotnet publish --output ./build.dev -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

// Возникает ошибка сегментирования, ничего не работает

namespace ConsoleTest;
unsafe class Program
{
    static void * session = null;
    // Без аргументов вызывается
    static void Main(string[] args)
    {
        var ci  = stackalloc cuse_info[1];
        var ops = stackalloc cuse_lowlevel_ops[1];

        for (var i = 0; i < sizeof(cuse_lowlevel_ops); i++)
            ((byte*)ops)[i] = 0;

        AppDomain.CurrentDomain.UnhandledException += (a, ex) => { Console.WriteLine((ex.ExceptionObject as Exception)!.Message); };

        var stra = stackalloc byte*[1];
        // Здесь определяется настоящий путь к устройству: /dev/vkf/crandom
        var name = Utf8StringMarshaller.ConvertToUnmanaged("DEVNAME=vkf-test/crandom\u0000");
        var nlen = strlen(name);

        stra[0] = name;

        ci->dev_info_argc = 1;
        ci->dev_info_argv = stra;
        ci->flags = 0;  // CUSE_UNRESTRICTED_IOCTL = 1
        ci->dev_major = 0;
        ci->dev_minor = 0;

        // var A = new string[] {"", "-f", "-d"};
        var A = new string[] {"", "-f"};
        GC.KeepAlive(A);

        ops->read    = &CuseReadFunc;
        ops->open    = &CuseOpenFunc;
        ops->release = &Cuse_release;
        // ops->ioctl   = &Cuse_ioctl;
        ops->init    = &Cuse_init;
        ops->init_done  = &Cuse_init_done;
        //var r = cuse_lowlevel_main(A.Length, A, ci, ops, null);
        int multithreaded = 0;
        session = (void *) cuse_lowlevel_setup(A.Length, A, ci, ops, &multithreaded, null);
        Console.WriteLine("session: " + (nint)session);
        var r = fuse_session_loop(session);

        cuse_lowlevel_teardown(session);

        Console.WriteLine("end with result: " + r);
    }

    // https://github.com/libfuse/libfuse/blob/master/example/cuse.c

// int cuse_lowlevel_main(int argc, char *argv[], const struct cuse_info *ci, const struct cuse_lowlevel_ops *clop, void *userdata);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe nint cuse_lowlevel_main(int argc, [In, MarshalAs(UnmanagedType.LPArray)] string[] argv, cuse_info * cuseInfo, cuse_lowlevel_ops * ll_ops, void * userdata);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe nint cuse_lowlevel_setup(int argc, [In, MarshalAs(UnmanagedType.LPArray)] string[] argv, cuse_info * cuseInfo, cuse_lowlevel_ops * ll_ops, int * multithreaded , void * userdata);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe nint fuse_session_loop(void * session);


    // int fuse_reply_open(fuse_req_t req, const struct fuse_file_info *fi);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_open(void * request, fuse_file_info * fileInfo);

    // int fuse_reply_buf(fuse_req_t req, const char *buf, size_t size);    
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_buf(void * request, byte * buf, int size);

    // int fuse_reply_err(fuse_req_t req, int err)
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_err(void * request, int err);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_iov(void * req, nint * iov, int count);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_session_exit(void * fuse_session);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_session_unmount(void * fuse_session);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_remove_signal_handlers(void * fuse_session);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_session_destroy(void * fuse_session);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int cuse_lowlevel_teardown(void * fuse_session);


    public static int strlen(byte * str)
    {
        int len = 0;
        while (*str != 0)
        {
            str++;
            len++;
        }

        return len;
    }

    // CUSE

    // [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallingConvention.Cdecl) })]
    // delegate* unmanaged[Cdecl]<..., returnType> 
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void CuseReadFunc(fuse_req* request, nint size, nint offset, fuse_file_info * fileInfo)
    {// Console.WriteLine("CuseReadFunc !!!!!!!!!!!!");
        if (offset > 0)
        {
            // https://learn.microsoft.com/en-us/cpp/c-runtime-library/errno-constants?view=msvc-170
            // fuse_reply_err(request, 22);    // EINVAL = 22; ENOSYS = 40
            fuse_reply_buf(request, null, 0);
            return;
        }
/*Console.WriteLine("read " + fileInfo->fh);
Console.WriteLine(fhs.Count);*/
        bool exists = false;
        lock (fhs)
            exists = fhs.Remove(fileInfo->fh);
/*Console.WriteLine(fhs.Count);
Console.WriteLine(exists);*/
        if (!exists)
        {
            Console.WriteLine("STOP for read");
            fuse_reply_buf(request, null, 0);
            return;
        }

        Console.WriteLine("start for read " + size);
        var a = stackalloc byte[] {48, 49, 50, 51, (byte) 'e', 10};
        fuse_reply_buf(request, a, 6);

        // Завершаем сессию и размонтируем устройство
        if (fileInfo->fh == 2)
        ThreadPool.QueueUserWorkItem
        (
            (p) =>
            {
                Console.WriteLine("EXIT");
                Thread.Sleep(10000);
                fuse_session_exit(session); // Fuse будет ждать обращения к устройству и только потом размонтируется
                File.ReadAllText("/dev/vkf-test/crandom");  // А вот и обращене, которого он будет ждать
                // fuse_session_unmount(session);
                // Остальное сделает cuse_lowlevel_teardown
                // Console.WriteLine("session: " + (nint)session);
                // fuse_remove_signal_handlers(session);
                // fuse_session_destroy(session);
            }
        );
    }

    public static ulong fileHandleLast = 0;

    static List<ulong> fhs = new List<ulong>(128);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void CuseOpenFunc(fuse_req* request, fuse_file_info * fileInfo)
    {//Console.WriteLine("CuseOpenFunc !!!!!!!!!!!!");

        // https://github.com/libfuse/libfuse/blob/master/include/fuse_common.h#L50
        // https://github.com/libfuse/libfuse/blob/master/include/fuse_lowlevel.h#L198

        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.direct_io; // direct_io
        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.keep_cache; // keep_cache
        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.noflush; // noflush
        fileInfo->bitFlags |= (ulong) FuseFileInfoOptions.nonseekable; // nonseekable
        fileInfo->fh          = Interlocked.Increment(ref fileHandleLast);

        lock (fhs)
            fhs.Add(fileInfo->fh);
//Console.WriteLine("added " + fileInfo->fh);
        fuse_reply_open(request, fileInfo);
    }

    // static void cusexmp_ioctl(fuse_req_t req, int cmd, void *arg, struct fuse_file_info *fi, unsigned flags, const void *in_buf, size_t in_bufsz, size_t out_bufsz)
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Cuse_ioctl(fuse_req* request, int cmd, void * arg, fuse_file_info * fileInfo, int flags, void * in_buf, nint in_buf_size, nint out_buff_size)
    {Console.WriteLine("Cuse_ioctl !!!!!!!!!!!!");
        Console.WriteLine(cmd);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Cuse_release(fuse_req* request, fuse_file_info * fileInfo)
    {Console.WriteLine("Cuse_release !!!!!!!!!!!!");
        lock (fhs)
            fhs.Remove(fileInfo->fh);
        // Console.WriteLine("remove " + fileInfo->fh);
        fuse_reply_err(request, 0);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Cuse_init(void * a, fuse_conn_info * b)
    {
        b->max_write = 0;
        b->max_read  = 4096;
        Console.WriteLine("Cuse_init !!!!!!!!!!!!");
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Cuse_init_done(void * user_data)
    {
        Console.WriteLine("Cuse_init_done !!!!!!!!!!!!");
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct fuse_req
    {
        public void * session;
        // Здесь есть ещё и другие поля!
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct cuse_info
    {
        public int	dev_major;
        public int	dev_minor;
        public int	dev_info_argc;
        public byte** dev_info_argv;
        public int	flags;
    }

    [StructLayout(LayoutKind.Sequential)]
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
    public struct cuse_lowlevel_ops
    {
        public cuse_lowlevel_ops()
        {
        }

        public delegate* unmanaged[Cdecl]<void*, fuse_conn_info *, void> init = null;
        public delegate* unmanaged[Cdecl]<void*, void>        init_done = null;
        public void *           destroy = null;
        public delegate* unmanaged[Cdecl]<fuse_req*,           fuse_file_info *, void> open;
        public delegate* unmanaged[Cdecl]<fuse_req*, nint, nint, fuse_file_info *, void> read;
        public void *           write = null;
        public void *           flush = null;
        public delegate* unmanaged[Cdecl]<fuse_req*, fuse_file_info *, void> release = null;
        public void *           fsync = null;
        public delegate* unmanaged[Cdecl]<fuse_req*, int, void *, fuse_file_info *, int, void *, nint, nint, void>       ioctl = null;
        public void *           poll = null;

        public void * r1, r2, r3;   // Этого нет в наличии в структуре: просто доп. поля на всякий случай
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct fuse_file_info
    {
        public int   flags;
        public ulong bitFlags;
        public ulong fh, lock_owner;
        public uint  poll_events;
    }


// Далее - не нужно

public enum PosixResult : int
{
    Success = 0,   /* No error */

    EPERM = 1,   /* Operation not permitted */
    ENOENT = 2,   /* No such file or directory */
    ESRCH = 3,   /* No such process */
    EINTR = 4,   /* Interrupted system call */
    EIO = 5,   /* Input/output error */
    ENXIO = 6,   /* Device not configured */
    E2BIG = 7,   /* Argument list too long */
    ENOEXEC = 8,   /* Exec format error */
    EBADF = 9,   /* Bad file descriptor */
    ECHILD = 10,   /* No child processes */
    EDEADLK = 11,   /* Resource deadlock avoided */
    ENOMEM = 12,   /* Cannot allocate memory */
    EACCES = 13,   /* Permission denied */
    EFAULT = 14,   /* Bad address */
    ENOTBLK = 15,   /* Block device required */
    EBUSY = 16,   /* Device busy */
    EEXIST = 17,   /* File exists */
    EXDEV = 18,   /* Cross-device link */
    ENODEV = 19,   /* Operation not supported by device */
    ENOTDIR = 20,   /* Not a directory */
    EISDIR = 21,   /* Is a directory */
    EINVAL = 22,   /* Invalid argument */
    ENFILE = 23,   /* Too many open files in system */
    EMFILE = 24,   /* Too many open files */
    ENOTTY = 25,   /* Inappropriate ioctl for device */
    ETXTBSY = 26,   /* Text file busy */
    EFBIG = 27,   /* File too large */
    ENOSPC = 28,   /* No space left on device */
    ESPIPE = 29,   /* Illegal seek */
    EROFS = 30,   /* Read-only filesystem */
    EMLINK = 31,   /* Too many links */
    EPIPE = 32,   /* Broken pipe */

    /* math software */
    EDOM = 33,   /* Numerical argument out of domain */
    ERANGE = 34,   /* Result too large */

    /* non-blocking and interrupt i/o */
    EAGAIN = 35,   /* Resource temporarily unavailable */
    EINPROGRESS = 36,   /* Operation now in progress */

    EALREADY = 37,   /* Operation already in progress */

    /* ipc/network software -- argument errors */
    ENOTSOCK = 38,   /* Socket operation on non-socket */
    EDESTADDRREQ = 39,   /* Destination address required */
    EMSGSIZE = 40,   /* Message too long */
    EPROTOTYPE = 41,   /* Protocol wrong type for socket */
    ENOPROTOOPT = 42,   /* Protocol not available */
    EPROTONOSUPPORT = 43,   /* Protocol not supported */
    ESOCKTNOSUPPORT = 44,   /* Socket type not supported */
    EOPNOTSUPP = 45,   /* Operation not supported */
    ENOTSUP = EOPNOTSUPP, /* Operation not supported */
    EPFNOSUPPORT = 46,   /* Protocol family not supported */
    EAFNOSUPPORT = 47,   /* Address family not supported by protocol family */
    EADDRINUSE = 48,   /* Address already in use */
    EADDRNOTAVAIL = 49,   /* Can't assign requested address */

    /* ipc/network software -- operational errors */
    ENETDOWN = 50,   /* Network is down */
    ENETUNREACH = 51,   /* Network is unreachable */
    ENETRESET = 52,   /* Network dropped connection on reset */
    ECONNABORTED = 53,   /* Software caused connection abort */
    ECONNRESET = 54,   /* Connection reset by peer */
    ENOBUFS = 55,   /* No buffer space available */
    EISCONN = 56,   /* Socket is already connected */
    ENOTCONN = 57,   /* Socket is not connected */
    ESHUTDOWN = 58,   /* Can't send after socket shutdown */
    ETOOMANYREFS = 59,   /* Too many references: can't splice */
    ETIMEDOUT = 60,   /* Operation timed out */
    ECONNREFUSED = 61,   /* Connection refused */

    ELOOP = 62,   /* Too many levels of symbolic links */
    ENAMETOOLONG = 63,   /* File name too long */

    /* should be rearranged */
    EHOSTDOWN = 64,   /* Host is down */
    EHOSTUNREACH = 65,   /* No route to host */
    ENOTEMPTY = 66,   /* Directory not empty */

    /* quotas & mush */
    EPROCLIM = 67,   /* Too many processes */
    EUSERS = 68,   /* Too many users */
    EDQUOT = 69,   /* Disc quota exceeded */

    /* Network File System */
    ESTALE = 70,   /* Stale NFS file handle */
    EREMOTE = 71,   /* Too many levels of remote in path */
    EBADRPC = 72,   /* RPC struct is bad */
    ERPCMISMATCH = 73,   /* RPC version wrong */
    EPROGUNAVAIL = 74,   /* RPC prog. not avail */
    EPROGMISMATCH = 75,   /* Program version wrong */
    EPROCUNAVAIL = 76,   /* Bad procedure for program */

    ENOLCK = 77,   /* No locks available */
    ENOSYS = 78,   /* Function not implemented */

    EFTYPE = 79,   /* Inappropriate file type or format */
    EAUTH = 80,   /* Authentication error */
    ENEEDAUTH = 81,   /* Need authenticator */
    EIDRM = 82,   /* Identifier removed */
    ENOMSG = 83,   /* No message of desired type */
    EOVERFLOW = 84,   /* Value too large to be stored in data type */
    ECANCELED = 85,   /* Operation canceled */
    EILSEQ = 86,   /* Illegal byte sequence */
    ENOATTR = 87,   /* Attribute not found */

    EDOOFUS = 88,   /* Programming error */

    EBADMSG = 89,   /* Bad message */
    EMULTIHOP = 90,   /* Multihop attempted */
    ENOLINK = 91,   /* Link has been severed */
    EPROTO = 92,   /* Protocol error */

    ENOTCAPABLE = 93,   /* Capabilities insufficient */
    ECAPMODE = 94,   /* Not permitted in capability mode */
    ENOTRECOVERABLE = 95,   /* State not recoverable */
    EOWNERDEAD = 96,   /* Previous owner died */
    EINTEGRITY = 97   /* Integrity check failed */
}

[Flags]
public enum PosixFileMode : ushort
{
    None = 0x0,
    OthersExecute = 0x1,
    OthersWrite = 0x2,
    OthersRead = 0x4,
    OthersReadExecute = 0x5,
    OthersReadWrite = 0x6,
    OthersAll = 0x7,
    GroupExecute = 0x8,
    GroupWrite = 0x10,
    GroupRead = 0x20,
    GroupReadExecute = 0x28,
    GroupReadWrite = 0x30,
    GroupAll = 0x38,
    OwnerExecute = 0x40,
    OwnerWrite = 0x80,
    OwnerRead = 0x100,
    OwnerReadExecute = 0x140,
    OwnerReadWrite = 0x180,
    OwnerAll = 0x1C0,

    Sticky = 0x200,
    SetGroupId = 0x400,
    SetUserId = 0x800,

    Fifo = 0x1000,
    Character = 0x2000,
    Directory = 0x4000,
    Block = 0x6000,
    Regular = 0x8000,
    SymbolikLink = 0xA000,
    Socket = 0xC000,
    Whiteout = 0xE000
}

[Flags]
public enum PosixOpenFlags : int
{
    Read = 0x0000,   /*open for reading only */
    Write = 0x0001,   /*open for writing only */
    ReadWrite = 0x0002,   /*open for reading and writing */
    AccessModes = 0x0003,   /*mask for selecting access modes */
    NonBlocking = 0x0004,   /*no delay */
    Append = 0x0008,   /*set append mode */
    SharedLock = 0x0010,   /*open with shared file lock */
    ExclusiveLock = 0x0020,   /*open with exclusive file lock */
    Async = 0x0040,   /*signal pgrp when data ready */
    SynchronousWrites = 0x0080,   /*synchronous writes */
    NoFollowLinks = 0x0100,   /*don't follow symlinks */
    Create = 0x0200,   /*create if nonexistent */
    Truncate = 0x0400,   /*truncate to zero length */
    CreateNew = 0x0800,   /*error if already exists */
    NoControllingTerminal = 0x8000,   /*don't assign controlling terminal */
    Direct = 0x00010000,
    Directory = 0x00020000,   /*Fail if not directory */
    Execute = 0x00040000,   /*Open for execute only */
    Search = Execute,
    TtyInit = 0x00080000,   /*Restore default termios attributes */
    Verify = 0x00200000,   /*open only after verification */
    ResolveBeneath = 0x00800000,
    DirSync = 0x01000000
}
[Flags]
public enum FuseFileInfoOptions : ulong
{
    none = 0x0,

    /** In case of a write operation indicates if this was caused by a
        writepage */
    write_page = 0x1,

    /** Can be filled in by open, to use direct I/O on this file.
           Introduced in version 2.4 */
    direct_io = 0x2,

    /** Can be filled in by open, to indicate, that cached file data
        need not be invalidated.  Introduced in version 2.4 */
    keep_cache = 0x4,

    /** Indicates a flush operation.  Set in flush operation, also
        maybe set in highlevel lock operation and lowlevel release
        operation.  Introduced in version 2.6 */
    flush = 0x8,

    /** Can be filled in by open, to indicate that the file is not
    seekable.  Introduced in version 2.8 */
    nonseekable = 0x10,

    /** Indicates that flock locks for this file should be
       released.  If set, lock_owner shall contain a valid value.
       May only be set in ->release().  Introduced in version
       2.9 */
    flock_release = 0x20,

    /** Can be filled in by opendir. It signals the kernel to
        enable caching of entries returned by readdir().  Has no
        effect when set in other contexts (in particular it does
        nothing when set by open()). */
    cache_readdir = 0x40,

    /** Can be filled in by open, to indicate that flush is not needed
	    on close. */
    noflush = 0x80,
}
[StructLayout(LayoutKind.Sequential)]
public struct FuseFileInfo
{
    static unsafe FuseFileInfo()
    {
        if (IntPtr.Size == 8 && sizeof(FuseFileInfo) != 40)
        {
            throw new PlatformNotSupportedException($"Invalid packing of structure FuseFileInfo. Should be 40 bytes, is {sizeof(FuseFileInfo)} bytes");
        }
        else if (IntPtr.Size == 4 && sizeof(FuseFileInfo) != 32)
        {
            throw new PlatformNotSupportedException($"Invalid packing of structure FuseFileInfo. Should be 32 bytes, is {sizeof(FuseFileInfo)} bytes");
        }
    }

    public readonly PosixOpenFlags flags;
    public FuseFileInfoOptions options;
    public long fileHandle;
    public ulong lock_owner;
    public readonly uint poll_events;
}

}
