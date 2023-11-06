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
        var fuseOperations = new FuseOperations()
        {
            open = fuse_open,
            read = fuse_read,
            access = fuse_access,
            getattr = GetAttr
        };

        fuse_main_real(args.Length, args, fuseOperations, Marshal.SizeOf(fuseOperations), 0);
        /*
        var ci  = stackalloc cuse_info[1];
        var ops = stackalloc cuse_lowlevel_ops[1];

        var stra = stackalloc byte*[1];
        var str  = stackalloc byte[256];
        stra[0]  = str;
        var name = Utf8StringMarshaller.ConvertToUnmanaged("DEVNAME=/inRamA/vkf\u0000");
        var nlen = strlen(name);

        for (int i = 0; i < 256; i++)
            str[i] = 0;

        for (int i = 0; i < nlen; i++)
            str[i] = (byte) name[i];
        stra[0] = name;

        ci->dev_info_argc = 1;
        ci->dev_info_argv = (char**) stra;

        ops->read = &FuseReadFunc;
        ops->open = &FuseOpenFunc;
        cuse_lowlevel_main(0, null, ci, ops, null);

        Console.WriteLine("end");

        while (true);*/
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


// int cuse_lowlevel_main(int argc, char *argv[], const struct cuse_info *ci, const struct cuse_lowlevel_ops *clop, void *userdata);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe nint cuse_lowlevel_main(nint argc, char** argv, cuse_info * cuseInfo, cuse_lowlevel_ops * ll_ops, void * userdata);


    // int fuse_reply_open(fuse_req_t req, const struct fuse_file_info *fi);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_open(void * request, fuse_file_info * fileInfo);

    // int fuse_reply_buf(fuse_req_t req, const char *buf, size_t size);    
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_buf(void * request, byte * buf, int size);

    // int fuse_reply_err(fuse_req_t req, int err)
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int fuse_reply_err(void * request, int err);

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


    public static int fuse_open(nint path, ref FuseFileInfo fileInfo)
    {
        Console.WriteLine("fuse_open !!!!!!!!!!!!");
        return 0;
    }

    public static int fuse_read(nint path, nint buffer, nint size, long position, ref FuseFileInfo fileInfo)
    {
        Console.WriteLine("fuse_read !!!!!!!!!!!!");
        return 0;
    }

    public static int fuse_access(nint path, PosixAccessMode mask)
    {
        Console.WriteLine("fuse_access !!!!!!!!!!!!");
        return (int) PosixResult.Success;
    }

//    public static int GetAttr(byte * fileNamePtr, [Out] out FuseFileStat stat, ref FuseFileInfo fileInfo)
    public static int GetAttr(byte * fileNamePtr, nint* stat, ref FuseFileInfo fileInfo)
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

    // [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallingConvention.Cdecl) })]
    // delegate* unmanaged[Cdecl]<..., returnType> 
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void FuseReadFunc(void* request, nint size, nint offset, fuse_file_info * fileInfo)
    {Console.WriteLine("FuseOpenFunc !!!!!!!!!!!! read");
        if (offset > 0)
        {
            fuse_reply_err(request, 0);
            return;
        }

        var a = stackalloc byte[] {48, 49, 50, 51, (byte) 'e'};
        fuse_reply_buf(request, a, 5);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void FuseOpenFunc(void* request, fuse_file_info * fileInfo)
    {Console.WriteLine("FuseOpenFunc !!!!!!!!!!!!");
        if (fileInfo->writepage != 0)
        {
            fuse_reply_err(request, 0);
            return;
        }

        fileInfo->direct_io   = 1;
        fileInfo->keep_cache  = 0;
        fileInfo->noflush     = 1;
        fileInfo->nonseekable = 1;

        fuse_reply_open(request, fileInfo);
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
        public nint	dev_major;
        public nint	dev_minor;
        public nint	dev_info_argc;
        public char	**dev_info_argv;
        public nint	flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct cuse_lowlevel_ops
    {
        public cuse_lowlevel_ops()
        {
        }
        public void *           init = null;
        public void *           init_done = null;
        public void *           destroy = null;
        public delegate* unmanaged[Cdecl]<void*,           fuse_file_info *, void> open;
        public delegate* unmanaged[Cdecl]<void*, nint, nint, fuse_file_info *, void> read;
        public void *           write = null;
        public void *           flush = null;
        public void *           release = null;
        public void *           fsync = null;
        public void *           ioctl = null;
        public void *           poll = null;

        public void * r1, r2, r3;   // Этого нет в наличии в структуре: просто доп. поля на всякий случай
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct fuse_file_info
    {
        public nint   flags;
        public nuint  writepage, direct_io, keep_cache, parallel_direct_writes, flush, nonseekable, flock_release, cache_readdir, noflush, padding, padding2;
        public ulong fh, lock_owner;
        public nuint  poll_events;
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

    #region Delegates

    public delegate int empty();

    public delegate int fuse_f_getattr(byte * fileNamePtr, nint* stat, ref FuseFileInfo fileInfo); //(nint path, nint stat, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_readlink(nint path, nint target, nint size);
	public delegate int fuse_f_mknod(nint path, PosixFileMode mode, int dev);
	public delegate int fuse_f_mkdir(nint path, PosixFileMode mode);
	public delegate int fuse_f_unlink(nint path);
	public delegate int fuse_f_rmdir(nint path);
	public delegate int fuse_f_symlink(nint path, nint target);
	public delegate int fuse_f_rename(nint path, nint target);
	public delegate int fuse_f_link(nint path, nint target);
	public delegate int fuse_f_chmod(nint path, PosixFileMode mode);
	public delegate int fuse_f_chown(nint path, int uid, int gid);
	public delegate int fuse_f_truncate(nint path, long size);
	public delegate int fuse_f_open(nint path, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_read(nint path, nint buffer, nint size, long position, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_write(nint path, nint buffer, nint size, long position, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_flush(nint path, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_release(nint path, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_fsync(nint path, int datasync, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_setxattr(nint path, nint name, nint value, nint size, int flags);
	public delegate int fuse_f_getxattr(nint path, nint name, nint value, nint size);
	public delegate int fuse_f_listxattr(nint path, nint list, nint size);
	public delegate int fuse_f_removexattr(nint path, nint target);
	public delegate int fuse_f_opendir(nint path, ref FuseFileInfo fileInfo);
// 	public delegate int fuse_f_readdir(nint path, nint buf, nint fuse_fill_dir_t, long offset, ref FuseFileInfo fileInfo, FuseReadDirFlags flags);
	public delegate int fuse_f_releasedir(nint path, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_fsyncdir(nint path, int datasync, ref FuseFileInfo fileInfo);
	public delegate void fuse_f_destroy(nint context);
    public delegate int fuse_f_access(nint path, PosixAccessMode mask);
	public delegate int fuse_f_create(nint path, PosixFileMode mode, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_lock(nint path, ref FuseFileInfo fileInfo, int cmd, nint flock);
	public delegate int fuse_f_utimens(nint path, nint timespec, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_bmap(nint path, nint blocksize, out ulong idx);
	public delegate int fuse_f_poll(nint path, ref FuseFileInfo fileInfo, nint fuse_pollhandle, ref uint reventsp);
	public delegate int fuse_f_write_buf(nint path, nint fuse_bufvec, long off, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_read_buf(nint path, nint ppbuf, nint size, long off, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_flock(nint path, ref FuseFileInfo fileInfo, int op);
	public delegate int fuse_f_fallocate(nint path, PosixFileMode mode, long offset, long length, ref FuseFileInfo fileInfo);
	public delegate int fuse_f_copy_file_range(nint path_in, ref FuseFileInfo fi_in, long offset_in, nint path_out, ref FuseFileInfo fi_out, long offset_out, nint size, int flags);
	public delegate int fuse_f_lseek(nint path, long offset, int whence, ref FuseFileInfo fileInfo);
	#endregion Delegates

	public fuse_f_getattr? getattr;
	public fuse_f_readlink? readlink;
	public fuse_f_mknod? mknod;
	public fuse_f_mkdir? mkdir;
	public fuse_f_unlink? unlink;
	public fuse_f_rmdir? rmdir;
	public fuse_f_symlink? symlink;
	public fuse_f_rename? rename;
	public fuse_f_link? link;
	public fuse_f_chmod? chmod;
	public fuse_f_chown? chown;
	public fuse_f_truncate? truncate;
	public fuse_f_open? open;
	public fuse_f_read? read;
	public fuse_f_write? write;
	public empty? statfs;
	public fuse_f_flush? flush;
	public fuse_f_release? release;
	public fuse_f_fsync? fsync;
	public fuse_f_setxattr? setxattr;
	public fuse_f_getxattr? getxattr;
	public fuse_f_listxattr? listxattr;
	public fuse_f_removexattr? removexattr;
	public fuse_f_opendir? opendir;
	public empty? readdir;
	public fuse_f_releasedir? releasedir;
	public fuse_f_fsyncdir? fsyncdir;
	public empty? init;
	public fuse_f_destroy? destroy;
	public fuse_f_access? access;
	public fuse_f_create? create;
	public fuse_f_lock? @lock;
	public fuse_f_utimens? utimens;
	public fuse_f_bmap? bmap;
	public empty? ioctl;
	public fuse_f_poll? poll;
	public fuse_f_write_buf? write_buf;
	public fuse_f_read_buf? read_buf;
	public fuse_f_flock? flock;
	public fuse_f_fallocate? fallocate;
	public fuse_f_copy_file_range? copy_file_range;
	public fuse_f_lseek? lseek;
}
}
