// dotnet publish --output ./build.dev -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ConsoleTest;
class Program
{
    // [{( - скобки убираем, проще запоминать просто слово "скобка"
    // < - меньше и больше легко перепутать, оставляем только один из них
    // ! слишком похож на черту и имеет сложное наименование
    // Ч - похоже на 4. ± - сложное наименование, к тому же, похожее на два занака "+-", идущие подряд
    static string GrantedSymbols = "qwertyuiopasdfghjkLzxcvbnm1234567890,.<?;':(+-*/=|&^%$#@ΣΔΨλШЫЭЯ";
    static void Main(string[] args)
    {
        // 24bit or truecolor
        var tc = System.Environment.GetEnvironmentVariable("COLORTERM");
        Console.WriteLine(tc);
        Console.WriteLine($"isTrueColor: {isTrueColor(tc)}");
        Console.WriteLine($"Эти цвета явно различны, если TrueColor поддерживается:");
        // \x1b то же, что и echo -e "\033" в терминале
        Console.WriteLine("\x1b[1m\x1b[48;2;255;0;0m\x1b[38;2;0;0;255mTRUECOLOR\x1b[0m\x1b[0m");
        Console.WriteLine("\x1b[1m\x1b[48;2;224;0;0m\x1b[38;2;0;0;224mTRUECOLOR\x1b[0m\x1b[0m");
        Console.WriteLine("\x1b[1m\x1b[48;2;192;0;0m\x1b[38;2;0;0;192mTRUECOLOR\x1b[0m\x1b[0m");

        Console.WriteLine("Символ лямбда: λ");
        Console.WriteLine("Другие символы");
        foreach (var c in GrantedSymbols)
            Console.Write(c + " ");
        Console.WriteLine();
        Console.WriteLine("Нажмите любую клавишу...");
        var key = Console.ReadKey(true);
        Console.WriteLine("Введена клавиша " + key.Key.ToString());

        // entry::warn:onlylinux:sOq1JvFKRxQyw7FQ:
        using var sc = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var path = "/inRamA/socket";
        var un = new UnixDomainSocketEndPoint(path);
        sc.Bind(un);
        // sc.Connect(un);
        Console.WriteLine($"Создан unix-сокет {path}");
        var owner_id = GetFileOwner(path);
        Console.WriteLine($"Его владелец {owner_id}");
        Console.WriteLine("Нажмите любую клавишу...");
        Console.ReadLine();
    }

    //  sudo apt-get install libc6-dev - это уже установлено
    //  find /usr/include -name "stat.h"
    public struct StatBuf
    {
        public long    st_dev;         /* ID of device containing file */
        public long    st_ino;         /* Inode number */
        public long    st_nlink;       /* Number of hard links */
        public int     st_mode;        /* File type and mode */
        public int     st_uid;         /* User ID of owner */
        public int     st_gid;         /* Group ID of owner */
        public int     st_pad0;
        public long    st_rdev;        /* Device ID (if special file) */
        public long    st_size;        /* Total size, in bytes */
        public long    st_blksize;     /* Block size for filesystem I/O */
        public long    st_blocks;      /* Number of 512B blocks allocated */

        /* Since Linux 2.6, the kernel supports nanosecond
            precision for the following timestamp fields.
            For the details before Linux 2.6, see NOTES. */

        public long st_atime;  /* Time of last access */
        public long st_atime_nsec;
        public long st_mtime;  /* Time of last modification */
        public long st_mtime_nsec;
        public long st_ctime;  /* Time of last status change */
        public long st_ctime_nsec;
        public long __unused3;
        public long __unused4;
        public long __unused5;

        // Это на всякий случай, добавлено вне того, что есть в Linux
        public long tmp1, tmp2, tmp3, tmp4, tmp5, tmp6, tmp7, tmp8;
    }
    /*
    Это из /usr/include/x86_64-linux-gnu/asm/stat.h
    Похоже, это неверная структура, потому что для i386
    struct stat {
	unsigned long  st_dev;
	unsigned long  st_ino;
	unsigned short st_mode;
	unsigned short st_nlink;
	unsigned short st_uid;
	unsigned short st_gid;
	unsigned long  st_rdev;
	unsigned long  st_size;
	unsigned long  st_blksize;
	unsigned long  st_blocks;
	unsigned long  st_atime;
	unsigned long  st_atime_nsec;
	unsigned long  st_mtime;
	unsigned long  st_mtime_nsec;
	unsigned long  st_ctime;
	unsigned long  st_ctime_nsec;
	unsigned long  __unused4;
	unsigned long  __unused5;
    };

    Вот это - верная
    struct stat {
	__kernel_ulong_t	st_dev;
	__kernel_ulong_t	st_ino;
	__kernel_ulong_t	st_nlink;

	unsigned int		st_mode;
	unsigned int		st_uid;
	unsigned int		st_gid;
	unsigned int		__pad0;
	__kernel_ulong_t	st_rdev;
	__kernel_long_t		st_size;
	__kernel_long_t		st_blksize;
	__kernel_long_t		st_blocks;

	__kernel_ulong_t	st_atime;
	__kernel_ulong_t	st_atime_nsec;
	__kernel_ulong_t	st_mtime;
	__kernel_ulong_t	st_mtime_nsec;
	__kernel_ulong_t	st_ctime;
	__kernel_ulong_t	st_ctime_nsec;
	__kernel_long_t		__unused[3];
};

    */
// TODO: вот здесь нужно будет вставить проверку в реальном VinKekFish, что это функция реально работает и возвращает верный результат
    // entry::warn:onlylinux:sOq1JvFKRxQyw7FQ:
    [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
    public static extern int stat(string path, ref StatBuf sb);

    public static int GetFileOwner(string path)
    {
        var a = new StatBuf();
        a.st_dev = -1;
        var errcode = stat(path, ref a);
        if (errcode != 0)
            throw new Exception($"stat in libc return error {errcode}");
/*
        Console.WriteLine(a.st_dev);
        Console.WriteLine(a.st_ino);
        Console.WriteLine(a.st_mode);
        Console.WriteLine(a.st_nlink);
        Console.WriteLine(a.st_uid);
        Console.WriteLine(a.st_gid);
        Console.WriteLine(a.st_rdev);
        Console.WriteLine(a.st_size);
        Console.WriteLine(a.st_blksize);
        Console.WriteLine(a.st_blocks);
        */
        return a.st_uid;
    }

    public static bool isTrueColor(string? COLORTERM)
    {
        if (COLORTERM == null)
            return false;

        COLORTERM = COLORTERM.ToLowerInvariant().Trim();
        if (COLORTERM == "24bit" || COLORTERM == "truecolor")
            return true;

        return false;
    }
}
