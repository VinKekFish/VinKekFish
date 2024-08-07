// #define CAN_CREATEFILE_FOR_KECCAK

namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;

using CodeGenerated.Cryptoprimes;

// ::test:O0s1QcshQ7zCGVMMKZtf:

// [TestTagAttribute("inWork")]
/// <summary>Этот тест проверяет производительность алгоритма ThreeFish (на случай, если она ухудшилась)</summary>
[TestTagAttribute("ThreeFish")]
[TestTagAttribute("performance", duration: 3000d, singleThread: true)]
public class ThreeFish_test_performance: TestTask
{
    #if CAN_CREATEFILE_FOR_KECCAK
    public static readonly bool canCreateFile = true;
    #warning CAN_CREATEFILE_FOR_KECCAK
    #else
    public static readonly bool canCreateFile = false;
    #endif

    public unsafe ThreeFish_test_performance(TestConstructor constructor):
                                        base(nameof(ThreeFish_test_performance), constructor: constructor)
    {
        // Console.WriteLine("Keccak_sha_3_512_test test task created");

        TaskFunc = () =>
        {
            const int iterCount   = 1024 * 1024;

            var allocator   = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
            using var key   = allocator.AllocMemory(128);
            using var text  = allocator.AllocMemory(128 * iterCount);
            using var tweak = allocator.AllocMemory(3*8);

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

                        Threefish_Static_Generated.Threefish1024_step
                                ((ulong *) key.array, (ulong *) tweak.array, (ulong *) (text.array + (i << 7)));
/*
                        Threefish_Static_Generated2.Threefish1024_step
                                (key.array, tweak.array, (text.array + (i << 7)));
*/
                    }

                    countBlocksForOneSecond = iterCount * 1000 / st.TotalMilliseconds;
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
            this.Name += $" ({countBlocksForOneSecond:N0}; {k/0.9:F3})";
            // Console.WriteLine($"{k}");
            // Console.WriteLine($"ThreeFish: countBlocksForOneSecond = {countBlocksForOneSecond:N0}");

            // Нормальная производительность блока ThreeFish составляет порядка 300-400 тысяч блоков в секунду
            // Сравниваем с эталоном: операции сложения примерно в 0.97
            var errStr = $"countBlocksForOneSecond = {countBlocksForOneSecond:N0} (normal 300-400 thousands per second on 2.8 GHz)";
            if (k < 0.90)
                throw new Exception($"ThreeFish_test_performance: k < 0.90; k = {k}; {errStr}");
            if (countBlocksForOneSecond < 340_000)
                throw new Exception($"ThreeFish_test_performance: countBlocksForOneSecond < 340_000; {errStr}");
        };
    }
}
