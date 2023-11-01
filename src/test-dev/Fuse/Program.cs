// dotnet publish --output ./build.dev -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

// Возникает ошибка сегментирования, ничего не работает

namespace ConsoleTest;
unsafe class Program
{
    static void Main(string[] args)
    {
        var ci  = new cuse_info();
        var ops = new cuse_lowlevel_ops();
/*
        var opts = new fuse_cmdline_opts();
        opts.foreground = 1;
        opts.debug      = 1;
*/

        var stra = stackalloc byte*[1];
        var str  = stackalloc byte[256];
        stra[0]  = str;
        var name = "DEVNAME=/inRamA/vkf\u0000";

        for (int i = 0; i < name.Length; i++)
            str[i] = (byte) name[i];
/*
        opts.mountpoint = (char**) str;
*/


        ci.dev_info_argc = 1;
        ci.dev_info_argv = (char**) stra;

        ops.read = &FuseReadFunc;
        ops.open = &FuseOpenFunc;
        cuse_lowlevel_main(0, null, &ci, &ops, null);

        while (true);
    }

    // https://github.com/vzabavnov/dotnetcore.fuse/
    // https://github.com/libfuse/libfuse/blob/master/example/cuse.c



    [DllImport("libfuse3.so.3", EntryPoint = "fuse_opt_parse", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int FuseOptParse(FuseArgs* args, void* data, FuseOpt* opts, FuseOptProc proc);

// int cuse_lowlevel_main(int argc, char *argv[], const struct cuse_info *ci, const struct cuse_lowlevel_ops *clop, void *userdata);
    [DllImport("libfuse3.so.3", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int cuse_lowlevel_main(int argc, char** argv, cuse_info * cuseInfo, cuse_lowlevel_ops * ll_ops, void * userdata);


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

    // [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallingConvention.Cdecl) })]
    // delegate* unmanaged[Cdecl]<..., returnType> 
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void FuseReadFunc(void* request, int size, int offset, fuse_file_info * fileInfo)
    {
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
        public byte	dev_major;
        public byte	dev_minor;
        public byte	dev_info_argc;
        public char	**dev_info_argv;
        public byte	flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct cuse_lowlevel_ops
    {
        public void *           init;
        public void *           init_done;
        public void *           destroy;
        public delegate* unmanaged[Cdecl]<void*,           fuse_file_info *, void> open;
        public delegate* unmanaged[Cdecl]<void*, int, int, fuse_file_info *, void> read;
        public void *           write;
        public void *           flush;
        public void *           release;
        public void *           fsync;
        public void *           ioctl;
        public void *           poll;
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
}
