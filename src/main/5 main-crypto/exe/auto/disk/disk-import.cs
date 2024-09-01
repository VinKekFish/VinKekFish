// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

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

public unsafe partial class AutoCrypt
{
    public static class Import
    {
        [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint geteuid();
        [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint getegid();

        [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern PosixResult fuse_main_real
        (
                int argc,
                [In, MarshalAs(UnmanagedType.LPArray)] string[] argv,
                FuseOperations * operations,
                nint operationsSize,
                nint userData
        );


        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct FuseOperations
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

            public delegate* unmanaged[Cdecl]<byte*, FuseFileStat*, FuseFileInfo*, int> getattr;
            public delegate* unmanaged[Cdecl]<nint, nint, nint, int> readlink;
            public delegate* unmanaged[Cdecl]<nint, PosixFileMode, int, int> mknod;
            public delegate* unmanaged[Cdecl]<byte*, PosixFileMode, int> mkdir;
            public delegate* unmanaged[Cdecl]<nint, int> unlink;
            public delegate* unmanaged[Cdecl]<nint, int> rmdir;
            public delegate* unmanaged[Cdecl]<nint, nint, int> symlink;
            public delegate* unmanaged[Cdecl]<nint, nint, int> rename;
            public delegate* unmanaged[Cdecl]<nint, nint, int> link;
            public delegate* unmanaged[Cdecl]<byte*, PosixFileMode, FuseFileInfo*, int> chmod;
            public delegate* unmanaged[Cdecl]<byte *, int, int, FuseFileInfo*, int> chown;
            public delegate* unmanaged[Cdecl]<nint, long, int> truncate;
            public delegate* unmanaged[Cdecl]<byte*, FuseFileInfo*, int> open;
            public delegate* unmanaged[Cdecl]<byte*, byte*, nint, long, FuseFileInfo*, nint> read;
            public delegate* unmanaged[Cdecl]<byte*, byte*, nint, long, FuseFileInfo*, nint> write;
            public delegate* unmanaged[Cdecl]<byte*, StatVFS *, nint> statfs;
            public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int> flush;
            public delegate* unmanaged[Cdecl]<byte*, FuseFileInfo*, int> release;
            public delegate* unmanaged[Cdecl]<nint, int, FuseFileInfo*, int> fsync;
            public delegate* unmanaged[Cdecl]<nint, nint, nint, nint, int, int> setxattr;
            public delegate* unmanaged[Cdecl]<byte*, byte*, byte*, nint, int> getxattr;
            public delegate* unmanaged[Cdecl]<nint, nint, nint, int> listxattr;
            public delegate* unmanaged[Cdecl]<nint, nint, int> removexattr;
            public delegate* unmanaged[Cdecl]<byte*, FuseFileInfo*, int> opendir;
            public delegate* unmanaged[Cdecl]<byte*, void*, delegate*unmanaged[Cdecl]<void*, byte*, void*, nint, int, int>, nint, FuseFileInfo*, FuseReadDirFlags, int> readdir;
            public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int> releasedir;
            public delegate* unmanaged[Cdecl]<nint, int, FuseFileInfo*, int> fsyncdir;
            public delegate* unmanaged[Cdecl]<void*, FuseConfig*, void*> init;
            public delegate* unmanaged[Cdecl]<void *, void> destroy;
            public delegate* unmanaged[Cdecl]<nint, PosixAccessMode, int> access;
            public delegate* unmanaged[Cdecl]<nint, PosixAccessMode, FuseFileInfo*, int> create;
            public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, int, nint, int> @lock;
            public delegate* unmanaged[Cdecl]<nint, nint, FuseFileInfo*, int> utimens;
            public delegate* unmanaged[Cdecl]<nint, nint, ulong *, int> bmap;
            public delegate* unmanaged[Cdecl]<byte *, int, void *, FuseFileInfo *, uint, void *, int> ioctl;
            public delegate* unmanaged[Cdecl]<nint, FuseFileInfo*, nint, uint *, int> poll;
            public delegate* unmanaged[Cdecl]<nint, nint, long, FuseFileInfo *, int> write_buf;
            public delegate* unmanaged[Cdecl]<nint, nint, nint, long, FuseFileInfo *, int> read_buf;
            public delegate* unmanaged[Cdecl]<nint, FuseFileInfo *, int, int> flock;
            public delegate* unmanaged[Cdecl]<nint, PosixFileMode, long, long, FuseFileInfo*, int> fallocate;
            public delegate* unmanaged[Cdecl]<nint, FuseFileInfo *, long, nint, FuseFileInfo*, long, nint, int, int> copy_file_range;
            public delegate* unmanaged[Cdecl]<nint, long, int, FuseFileInfo*, int> lseek;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FuseConfig
        {
            public FuseConfig()
            {
                Clear();
            }

            public void Clear()
            {
                this.set_gid = 0; this.gid = 0; this.gid = 0; this.uid = 0; this.set_mode = 0; this.umask = 0;
                this.entry_timeout = 0; this.negative_timeout = 0; this.attr_timeout = 0;
                this.intr = 0; this.intr_signal = 0; this.remember = 0; this.hard_remove = 0;
                this.use_ino = 0; this.readdir_ino = 0;
            }

                                            /// <summary>If `set_gid` is non-zero, the st_gid attribute of each file is overwritten with the value of `gid`.</summary>
            public int  set_gid;
            public uint gid;
                                            /// <summary>If `set_uid` is non-zero, the st_uid attribute of each file is overwritten with the value of `uid</summary>
            public int  set_uid;
            public uint uid;
                                            /// <summary>If `set_mode` is non-zero, the any permissions bits set in `umask` are unset in the st_mode attribute of each file</summary>
            public int  set_mode;
            public uint umask;
                                                    /// <summary>The timeout in seconds for which name lookups will be cached.</summary>
            public double entry_timeout;            /// <summary>The timeout in seconds for which a negative lookup will be cached. This means, that if file did not exist (lookupreturned ENOENT), the lookup will only be redone after the timeout, and the file/directory will be assumed to not exist until then. A value of zero means that negative lookups are not cached.</summary>
            public double negative_timeout;         /// <summary>The timeout in seconds for which file/directory attributes (as returned by e.g. the `getattr` handler) are cached.</summary>
            public double attr_timeout;
                                                    /// <summary>Allow requests to be interrupted</summary>
            public int intr;                        /// <summary>Specify which signal number to send to the filesystem when a request is interrupted.  The default is hardcoded to USR1.</summary>
            public int intr_signal;                 /// <summary>Normally, FUSE assigns inodes to paths only for as long as the kernel is aware of them. With this option inodes are instead remembered for at least this many seconds.  This will require more memory, but may be necessary when using applications that make use of inode numbers. A number of -1 means that inodes will be remembered for the entire life-time of the file-system process.</summary>
            public int remember;                    /// <summary>The default behavior is that if an open file is deleted, the file is renamed to a hidden file (.fuse_hiddenXXX), and only removed when the file is finally released.  This relieves the filesystem implementation of having to deal with this problem. This option disables the hiding behavior, and files are removed immediately in an unlink operation (or in a rename operation which overwrites an existing file). It is recommended that you not use the hard_remove option. When hard_remove is set, the following libc functions fail on unlinked files (returning errno of xattr(2), ftruncate(2), fstat(2), fchmod(2), fchown(2)</summary>
            public int hard_remove;
                                                    /// <summary>Honor the st_ino field in the functions getattr() and fill_dir(). This value is used to fill in the st_ino field in the stat(2), lstat(2), fstat(2) functions and the d_ino field in the readdir(2) function. The filesystem does not have to guarantee uniqueness, however some applications rely on this value being unique for the whole filesystem. affect the inode that libfuse  and the kernel use internally (also called the "nodeid").</summary>
            public int use_ino;                     /// <summary>If use_ino option is not given, still try to fill in the d_ino field in readdir(2). If the name was previously looked up, and is still in the cache, the inode number found there will be used.  Otherwise it will be set to -1. If use_ino option is given, this option is ignored.</summary>
            public int readdir_ino;
                                                    /// <summary>This option disables the use of page cache (file content cache)
                                                    /// in the kernel for this filesystem. This has several affects:
                                                    ///
                                                    /// 1. Each read(2) or write(2) system call will initiate one
                                                    ///    or more read or write operations, data will not be
                                                    ///    cached in the kernel.
                                                    ///
                                                    /// 2. The return value of the read() and write() system calls
                                                    ///    will correspond to the return values of the read and
                                                    ///    write operations. This is useful for example if the
                                                    ///    file size is not known in advance (before reading it).
                                                    ///
                                                    /// Internally, enabling this option causes fuse to set the
                                                    /// `direct_io` field of `struct fuse_file_info` - overwriting
                                                    /// any value that was put there by the file system.
                                                    /// </summary>
            public int direct_io;                   /// <summary>This option disables flushing the cache of the file contents on every open(2).  This should only be enabled on filesystems where the file data is never changed externally (not through the mounted FUSE filesystem).  Thus it is not suitable for network filesystems and other intermediate filesystems. NOTE: if this option is not specified (and neither direct_io) data is still cached after the open(2), so a read(2) system call will not always initiate a read operation. Internally, enabling this option causes fuse to set the `keep_cache` field of `struct fuse_file_info` - overwriting any value that was put there by the file system.</summary>
            public int kernel_cache;                /// <summary>This option is an alternative to `kernel_cache`. Instead of unconditionally keeping cached data, the cached data is invalidated on open(2) if if the modification time or the size of the file has changed since it was last opened.</summary>
            public int auto_cache;                  /// <summary>By default, fuse waits for all pending writes to complete and calls the FLUSH operation on close(2) of every fuse fd. With this option, wait and FLUSH are not done for read-only fuse fd, similar to the behavior of NFS/SMB clients.</summary>
            public int no_rofd_flush;
                                                    /// <summary>The timeout in seconds for which file attributes are cached for the purpose of checking if auto_cache should flush the file data on open.</summary>
            public int ac_attr_timeout_set;
            public double ac_attr_timeout;
                                                    /// <summary>If this option is given the file-system handlers for the following operations will not receive path information: read, write, flush, release, fallocate, fsync, readdir, releasedir, fsyncdir, lock, ioctl and poll. For the truncate, getattr, chmod, chown and utimens operations the path will be provided only if the struct fuse_file_info argument is NULL.</summary>
            public int nullpath_ok;
                                                    /// <summary>The remaining options are used by libfuse internally and should not be touched.</summary>
            public int show_help;
            public char *modules;
            public int debug;
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


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FuseFileInfo
        {
            static unsafe FuseFileInfo()
            {
                if (IntPtr.Size == 8 && sizeof(FuseFileInfo) != 40)
                {
                    throw new PlatformNotSupportedException($"Invalid packing of structure FuseFileInfo. Should be 40 bytes, is {sizeof(FuseFileInfo)} bytes");
                }
            }

            public readonly PosixOpenFlags flags;
            public FuseFileInfoOptions options;
            public ulong fileHandle;
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FuseFileStat
        {
            public FuseFileStat()
            {
                this = default;
            }

                                                /// <summary>Device</summary>
            public long dev;                     /// <summary>File serial number</summary>
            public long ino;                     /// <summary>Link count</summary>
            public long nlink;                   /// <summary>File mode</summary>
            public PosixFileMode mode;           /// <summary>User ID of the file's owner</summary>
            public uint uid;                     /// <summary>Group ID of the file's group</summary>
            public uint gid;                     /// <summary>Выравнивающее поле</summary>
            public int  pad0;                    /// <summary>Device number, if device.</summary>
            public long rdev;                    /// <summary>Size of file, in bytes</summary>
            public long size;                    /// <summary>Optimal block size for I/O</summary>
            public long blksize;                 /// <summary>Number 512-byte blocks allocated</summary>
            public long blocks;
                                                /// <summary>Time of last access</summary>
            public TimeSpec atime;              /// <summary>Time of last modification</summary>
            public TimeSpec mtime;              /// <summary>Time of last status change</summary>
            public TimeSpec ctime;

            // !!!! Здесь должен быть указатель
            public fixed long reserved[3];

        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct TimeSpec
        {
            public TimeSpec()
            {
                // tv_sec = -1; tv_nsec = -1;
            }

            public readonly long tv_sec   = -1;         // seconds
            public readonly long tv_nsec  = -1;        // and nanoseconds
        }

        [Flags]
        public enum PosixFileMode : int
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

        [StructLayout(LayoutKind.Sequential)]
        public struct StatVFS
        {
                                        /// <summary>Filesystem block size</summary>
            public ulong bsize;            /// <summary>Fragment size</summary>
            public ulong frsize;           /// <summary>Size of fs in frsize units</summary>
            public ulong blocks;           /// <summary>Number of free blocks</summary>
            public ulong bfree;            /// <summary>Number of free blocks</summary>
            public ulong bavail;           /// <summary>Number of inodes</summary>
            public ulong files;            /// <summary>Number of free inodes</summary>
            public ulong ffree;            /// <summary>Number of free inodes for unprivileged users</summary>
            public ulong favail;           /// <summary>Filesystem ID</summary>
            public ulong fsid;             /// <summary>Mount flags</summary>
            public ulong flag;             /// <summary>Maximum filename length</summary>
            public ulong namemax;
        }

    }
}
