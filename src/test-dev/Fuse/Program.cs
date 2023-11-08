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
    static void Main(string[] args)
    {
        var A = new string[] {"", "-f", "-d", "/inRamA/ttt"};
        Console.CancelKeyPress += (o, e) =>
        {
            Process.Start("umount", "/inRamA/ttt");
        };

        var fuseOperations = new FuseOperations()
        {
            open     = &fuse_open,
            read     = &fuse_read,
            access   = &fuse_access,
            getattr  = &GetAttr,
            //getxattr = &GetXAttr
        };

        // fuse_main_real(args.Length, args, fuseOperations, Marshal.SizeOf(fuseOperations), 0);
        var r = fuse_main_real(A.Length, A, fuseOperations, Marshal.SizeOf(fuseOperations), 0);

        Console.WriteLine("end with result: " + r);
    }

    // https://github.com/vzabavnov/dotnetcore.fuse/
    // https://github.com/PlasticSCM/FuseSharp
    // https://github.com/libfuse/libfuse/blob/master/example/cuse.c



    [DllImport("libfuse3.so.3", EntryPoint = "fuse_opt_parse", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int FuseOptParse(FuseArgs* args, void* data, FuseOpt* opts, FuseOptProc proc);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern PosixResult fuse_main_real(int argc,
        [In, MarshalAs(UnmanagedType.LPArray)] string[] argv,
        [In] FuseOperations? operations, nint operationsSize, nint userData);


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
    public static extern unsafe int fuse_session_exit(void * fuse_session);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_exit(void * fuse);

    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe fuse_context * fuse_get_context();
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void * fuse_get_session(fuse_context * context);
    

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

    public unsafe delegate int FuseOptProc(void* data, [MarshalAs(UnmanagedType.LPStr), In] string arg, int key, FuseArgs * outargs);

    public static unsafe int FuseOptFunc(void* data, [MarshalAs(UnmanagedType.LPStr), In] string arg, int key, FuseArgs * outargs)
    {
        return 1;
    }

    // static void cusexmp_read(fuse_req_t req, size_t size, off_t off, struct fuse_file_info *fi)


    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static int fuse_open(byte* path, FuseFileInfo * fileInfo)
    {
        Console.WriteLine("fuse_open !!!!!!!!!!!!");
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static int fuse_read(byte*  path, byte*  buffer, nint size, long position, FuseFileInfo * fileInfo)
    {
        Console.WriteLine("fuse_read !!!!!!!!!!!!");
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static int fuse_access(nint path, PosixAccessMode mask)
    {
        Console.WriteLine("fuse_access !!!!!!!!!!!!");
        return (int) PosixResult.Success;
    }

//    public static int GetAttr(byte * fileNamePtr, [Out] out FuseFileStat stat, ref FuseFileInfo fileInfo)
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static int GetAttr(byte * fileNamePtr, nint* stat, FuseFileInfo * fileInfo)
    {
        Console.WriteLine("GetAttr !!!!!!!!!!!!");
        var f = new FuseFileStat()
            {
                st_size = 4,
                st_birthtim = new TimeSpec(),
                st_mtim = new TimeSpec(),
                st_ctim = new TimeSpec(),
                st_atim = new TimeSpec(),
                st_mode = PosixFileMode.OthersRead | PosixFileMode.GroupRead | PosixFileMode.OwnerRead
            };
        
        var gch = GCHandle.Alloc(f, GCHandleType.Pinned);
        *stat = gch.AddrOfPinnedObject();

        return (int) PosixResult.Success;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static int GetXAttr(byte * fileNamePtr, byte * stat, byte * a, nint size)
    {
        Console.WriteLine("GetXAttr !!!!!!!!!!!!");

        return (int) PosixResult.Success;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct fuse_context
    {
        public void * fuse;
        public void * uid;
        public void * gid;
        public void * pid;
        public void * private_data;
        public void * umask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FuseOpt
    {
        public nint  templ;
        public ulong offset;
        public int   value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FuseArgs
    {
        public int    argc;
        public char** argv;
        public int    allocated;
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
    public struct fuse_file_info
    {
        public int   flags;
        public uint  writepage, direct_io, keep_cache, parallel_direct_writes, flush, nonseekable, flock_release, cache_readdir, noflush, padding, padding2;
        public ulong fh, lock_owner;
        public uint  poll_events;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct fuse_cmdline_opts
    {
        int singlethread;
        int foreground;
        int debug;
        int nodefault_subtype;
        char *mountpoint;
        int show_version;
        int show_help;
        int clone_fd;
        uint max_idle_threads; /* discouraged, due to thread
                                        * destruct overhead */

        /* Added in libfuse-3.12 */
        uint max_threads;
    }
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
public enum FuseFileInfoOptions : long
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

[Flags]
public enum PosixAccessMode
{
    Exists = 0x00,
    Execute = 0x01,
    Write = 0x02,
    Read = 0x04
}

public enum FuseReadDirFlags
{

}

public struct FuseFileStat
{
    unsafe static FuseFileStat()
    {
    }

    private unsafe delegate void fMarshalToNative(void* pNative, in FuseFileStat stat);

    private static readonly fMarshalToNative pMarshalToNative;

    public static int NativeStructSize { get; }

    public readonly unsafe void MarshalToNative(void* pNative) => pMarshalToNative(pNative, this);

    public readonly unsafe void MarshalToNative(nint pNative) => pMarshalToNative((void*)pNative, this);

    public long st_size { get; set; }
    public long st_nlink { get; set; }
    public PosixFileMode st_mode { get; set; }
    public long st_gen { get; set; }
    public TimeSpec st_birthtim { get; set; }
    public TimeSpec st_atim { get; set; }
    public TimeSpec st_ctim { get; set; }
    public TimeSpec st_mtim { get; set; }
    public long st_ino { get; set; }
    public long st_dev { get; set; }
    public long st_rdev { get; set; }
    public uint st_uid { get; set; }
    public uint st_gid { get; set; }
    public int st_blksize { get; set; }
    public long st_blocks { get; set; }

    public override readonly string ToString() => $"{{ Size = {st_size}, Mode = {st_mode}, Inode = {st_ino}, Uid = {st_uid}, Gid = {st_gid} }}";
}
[StructLayout(LayoutKind.Sequential)]
public readonly struct TimeSpec
{
    public TimeSpec()
    {
        tv_sec = -1; tv_nsec = -1;
    }

    public readonly nint tv_sec;       /* seconds */
    public readonly nint tv_nsec;        /* and nanoseconds */
}
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal sealed class FuseOperations
    {
        public FuseOperations()
        {
            this.access = null;
            this.bmap = null;
            this.chmod = null;
            this.chown = null;
            this.copy_file_range = null;
            this.create = null;
            this.destroy = null;
            this.fallocate = null;
            this.flock = null;
            this.flush = null;
            this.fsync = null;
            this.fsyncdir = null;
            this.getattr = null;
            this.getxattr = null;
            this.init = null;
            this.ioctl = null;
            this.link = null;
            this.listxattr = null;
            this.@lock = null;
            this.lseek = null;
            this.mkdir = null;
            this.mknod = null;
            this.open = null;
            this.opendir = null;
            this.poll = null;
            this.read = null;
            this.read_buf = null;
            this.readdir = null;
            this.readlink = null;
            this.release = null;
            this.releasedir = null;
            this.removexattr = null;
            this.rename = null;
            this.rmdir = null;
            this.setxattr = null;
            this.statfs = null;
            this.symlink = null;
            this.truncate = null;
            this.unlink = null;
            this.utimens = null;
            this.write = null;
            this.write_buf = null;
        }

        public delegate* unmanaged[Cdecl]<byte*, nint*, FuseFileInfo*, int> getattr;
        public delegate* unmanaged[Cdecl]<nint, nint, nint, int> readlink;
        public delegate* unmanaged[Cdecl]<nint, PosixFileMode, int, int> mknod;
        public delegate* unmanaged[Cdecl]<nint, PosixFileMode, int> mkdir;
        public delegate* unmanaged[Cdecl]<nint, int> unlink;
        public delegate* unmanaged[Cdecl]<nint, int> rmdir;
        public delegate* unmanaged[Cdecl]<nint, nint, int> symlink;
        public delegate* unmanaged[Cdecl]<nint, nint, int> rename;
        public delegate* unmanaged[Cdecl]<nint, nint, int> link;
        public delegate* unmanaged[Cdecl]<nint, PosixFileMode, int> chmod;
        public delegate* unmanaged[Cdecl]<nint, int, int, int> chown;
        public delegate* unmanaged[Cdecl]<nint, long, int> truncate;
        public delegate* unmanaged[Cdecl]<byte*, FuseFileInfo*, int> open;
        public delegate* unmanaged[Cdecl]<byte*, byte*, nint, long, FuseFileInfo*, int> read;
        public delegate* unmanaged[Cdecl]<nint, nint, nint, long, FuseFileInfo*, int> write;
        public delegate* unmanaged[Cdecl]<nint, void *, int> statfs;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int> flush;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int> release;
        public delegate* unmanaged[Cdecl]<nint, int, FuseFileInfo*, int> fsync;
        public delegate* unmanaged[Cdecl]<nint, nint, nint, nint, int, int> setxattr;
        public delegate* unmanaged[Cdecl]<byte*, byte*, byte*, nint, int> getxattr;
        public delegate* unmanaged[Cdecl]<nint, nint, nint, int> listxattr;
        public delegate* unmanaged[Cdecl]<nint, nint, int> removexattr;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int> opendir;
        public delegate* unmanaged[Cdecl]<nint, nint, nint, long, FuseFileInfo*, FuseReadDirFlags, int> readdir;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int> releasedir;
        public delegate* unmanaged[Cdecl]<nint, int, FuseFileInfo*, int> fsyncdir;
        public delegate* unmanaged[Cdecl]<nint, int> init;
        public delegate* unmanaged[Cdecl]<nint> destroy;
        public delegate* unmanaged[Cdecl]<nint, PosixAccessMode, int> access;
        public delegate* unmanaged[Cdecl]<nint, PosixAccessMode, FuseFileInfo*, int> create;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int, nint, int> @lock;
        public delegate* unmanaged[Cdecl]<nint, nint, FuseFileInfo*, int> utimens;
        public delegate* unmanaged[Cdecl]<nint, nint, ulong *, int> bmap;
        public delegate* unmanaged[Cdecl]<byte*, int, void *, FuseFileInfo*, int, void *, int> ioctl;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, nint, uint *, int> poll;
        public delegate* unmanaged[Cdecl]<nint, nint, long, FuseFileInfo *, int> write_buf;
        public delegate* unmanaged[Cdecl]<nint, nint, nint, long, FuseFileInfo *, int> read_buf;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo *, int, int> flock;
        public delegate* unmanaged[Cdecl]<nint, PosixFileMode, long, long, FuseFileInfo*, int> fallocate;
        public delegate* unmanaged[Cdecl]<nint, FuseFileInfo *, long, nint, FuseFileInfo*, long, nint, int, int> copy_file_range;
        public delegate* unmanaged[Cdecl]<nint, long, int, FuseFileInfo*, int> lseek;
    }
}
