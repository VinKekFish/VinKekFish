// #define CAN_CREATEFILE_FOR_KECCAK

namespace cryptoprime_tests;

using System.Collections.Generic;
using System.Text;
using cryptoprime;
using DriverForTestsLib;

/// <summary>Общий класс для задач-наследников AutoSaveTestTask</summary>
public class ParentAutoSaveTask: AutoSaveTestTask
{
    public ParentAutoSaveTask(TaskResultSaver executer_and_saver, TestConstructor constructor, bool canCreateFile = false):
                    base
                    (
                        name:               "",
                        dirForFiles:        getDirectoryPath(),
                        executer_and_saver: executer_and_saver,
                        constructor:        constructor
                    )
    {
        this.executer_and_saver.canCreateFile = canCreateFile;

        this.Name   = this.GetType().FullName ?? throw new System.ArgumentNullException();
        dirForFiles = setDirForFiles();
    }

    public static DirectoryInfo getDirectoryPath(string ProjectDir = "", string DirName = "autotests")
    {
        var pathToFile = new DirectoryInfo(System.AppContext.BaseDirectory)?.Parent ?? throw new Exception();
        var dir        = new DirectoryInfo(Path.Combine(pathToFile.FullName, ProjectDir));
        if (dir == null)
            throw new Exception();

        return new DirectoryInfo(  Path.Combine(dir.FullName, DirName)  );
    }

    public virtual DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath();
    }
}

[TestTagAttribute("keccak")]
[TestTagAttribute("mandatory", duration: 25d)]
public class Keccak_sha_3_512_test: ParentAutoSaveTask
{
    #if CAN_CREATEFILE_FOR_KECCAK
    public static readonly bool canCreateFile = true;
    #warning CAN_CREATEFILE_FOR_KECCAK
    #else
    public static readonly bool canCreateFile = false;
    #endif

    public Keccak_sha_3_512_test(TestConstructor constructor): base
    (
        executer_and_saver: new Saver(),
        constructor:        constructor,
        canCreateFile:      Keccak_sha_3_512_test.canCreateFile
    )
    {
        // Console.WriteLine("Keccak_sha_3_512_test test task created");
    }

    public override DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath("src/tests/src/main/cryptoprime/keccak/tests/");
    }

    unsafe protected class Saver: TaskResultSaver
    {
        public Saver()
        {
        }

        public override object ExecuteTest(AutoSaveTestTask task)
        {
            // Console.WriteLine("Keccak_sha_3_512_test start executing");

            var lst = new List<object>(16);

            // openssl dgst -sha3-515 fileForHashing
            Dictionary<string, string> hashes = new Dictionary<string, string>
            {
                {
                    "",
                    "a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a615b2123af1f5f94c11e3e9402c3ac558f500199d95b6d3e301758586281dcd26"
                },
                {
                    "The quick brown fox jumps over the lazy dog",
                    "01dedd5de4ef14642445ba5f5b97c15e47b9ad931326e4b0727cd94cefc44fff23f07bf543139939b49128caf436dc1bdee54fcb24023a08d9403f9b4bf0d450"
                },
                {
                    "The quick brown fox jumps over the lazy dog.",
                    "18f4f4bd419603f95538837003d9d254c26c23765565162247483f65c50303597bc9ce4d289f21d1c2f1f458828e33dc442100331b35e7eb031b5d38ba6460f8"
                },
                {
                    "0123456789\n",
                    "fb7815c242d98a7991c09d717c5420afcb69dda70f08fad455759004e1daa8c62424771a1f2864820ea811c6072cdc04804cccb6307685783005760467cc2656"
                },
                {
                    "01234567890123456789012345678901234567890123456789012345678901234567890",
                    "8bdcb85e6b52c29fafac0d3daf65492f2e3499e066da1a095a65eb1144849a26b2790a8b39c2a7fb747456f749391d953841a61cb13289f9806f04981c180a86"
                },
                {
                    "012345678901234567890123456789012345678901234567890123456789012345678901",
                    "bce9da5b408846edd5bec9f26c2dee9bd835215c3f2b3876197067d87bc4d1af0cd97f94fda59761a0d804fe82383be2c6c4886fbb82e005fcf899449029f221"
                },
                {
                    "0123456789012345678901234567890123456789012345678901234567890123456789012",
                    "f0586f85ed2379fa489d34ddf8f34d12e8d69711a0949928eb473774699f5444010392cb807dab76cad524e88080ee9a4c10de9f4cfa8ace248371c948f6a364"
                },
                {
                    """
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890

                    """,
                    "027de595a1c7cbc502d712c68a609d82be688541cc51ff39625f1be6f4d756d27fa31e9e5fd1231e9464d70b01b0f7b9d0d202402342661fcf794ee47b5f6ae1"
                },
                {
                    """
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    e
                    """,
                    "1400b324dff542f38c9c079379dda55ac8d2a61fcdcf5d8ba0b74917278ba3af2a05588547317185cf0c932d0044ee2331671046fd97ee5a8e543bafdc3d11dc"
                },
                {
                    """
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    en
                    """,
                    "12ef68ddfc4ea201bbad0201058b491286d5f8dd005aef1cd8620baf3a85fb8c2d38db8466719b4685f353fcb0e18304a5fa5c4b86f2f35725398f58118de6c6"
                },
                {
                    """
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    01234567890123456789012345678901234567890123456789012345678901234567890
                    end
                    """,
                    "e06743cd083c0fd214c051f57393b54e3cc68253080a3d5e1898d341cd596252852455642df9079fb6fbb54f68afd61a402f871d3fb894d181305fc90bdf10bb"
                }
            };

            foreach (var str in hashes)
            {
                var a = KeccakPrime.getSHA3_512(Encoding.ASCII.GetBytes(str.Key));
                lst.Add(a);

                if 
                (
                    Convert.ToHexString(a).ToLowerInvariant()
                    !=
                    str.Value
                )
                    throw new Exception($"string '{str.Key}' have incorrect hash " + Convert.ToHexString(a).ToLowerInvariant());
            }

            return lst;
        }
    }
}


// [TestTagAttribute("inWork")]
/// <summary>Этот тест проверяет производительность алгоритма keccak (на случай, если она ухудшилась)</summary>
[TestTagAttribute("keccak")]
[TestTagAttribute("performance", duration: 2500d, singleThread: true)]
public class Keccak_sha_3_512_test_performance: TestTask
{
    #if CAN_CREATEFILE_FOR_KECCAK
    public static readonly bool canCreateFile = true;
    #warning CAN_CREATEFILE_FOR_KECCAK
    #else
    public static readonly bool canCreateFile = false;
    #endif

    public Keccak_sha_3_512_test_performance(TestConstructor constructor):
                                        base(nameof(Keccak_sha_3_512_test_performance), constructor: constructor)
    {
        // Console.WriteLine("Keccak_sha_3_512_test test task created");

        taskFunc = () =>
        {
            const int blocksCount = 1024;
            const int iterCount   = 1024;

            var msg = new byte[KeccakPrime.r_512b * blocksCount];  // blocksCount вхождений блока keccak

            var countBlocksForOneSecond = 0d;
            var st        = new DriverForTestsLib.SimpleTimeMeter();
            var st_etalon = new DriverForTestsLib.SimpleTimeMeter();
            Parallel.Invoke
            (
                () =>
                {
                    using (st)
                    for (int i = 0; i < iterCount; i++)
                    {
                        KeccakPrime.getSHA3_512(msg);
                    }

                    countBlocksForOneSecond = blocksCount * iterCount * 1000 / st.TotalMilliseconds;
                },
                () =>
                {
                    long a = 0, b = 1;
                    using (st_etalon)
                    for (int i = 0; i < 128*1024*1024; i++)
                    {
                        a = f(a, b, i);
                    }

                    static long f(long a, long b, int i)
                    {
                        a -= b;
                        b += i;
                        a += b;

                        return a;
                    }
                }
            );

            var k = st_etalon.TotalMilliseconds / st.TotalMilliseconds;
            // Console.WriteLine($"{k}");
            Console.WriteLine($"keccak: countBlocksForOneSecond = {countBlocksForOneSecond:N0}");

            // Нормальная производительность блока keccak составляет порядка 400-500 тысяч блоков в секунду на больших объёмах блоков.
            // Сравниваем с эталоном: операции сложения примерно в 1.07-1.18
            var errStr = $"countBlocksForOneSecond = {countBlocksForOneSecond:N0} (normal 400-500 thousands per second on 2.8 GHz)";
            if (k < 1.07)
                throw new Exception($"Keccak_sha_3_512_test_performance: k < 1.10; k = {k}; {errStr}");
            if (countBlocksForOneSecond < 400_000)
                throw new Exception($"Keccak_sha_3_512_test_performance: countBlocksForOneSecond < 400_000; {errStr}");
        };
    }
}
