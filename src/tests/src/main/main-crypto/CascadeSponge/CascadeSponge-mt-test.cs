// #define CAN_CREATEFILE_FOR_CascadeSponge_1t_tests
namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using maincrypto.keccak;
using vinkekfish;

using static CodeGenerated.Cryptoprimes.Threefish_Static_Generated;
using static VinKekFish_Utils.Utils;

using static cryptoprime.KeccakPrime;
using static cryptoprime.BytesBuilderForPointers;
using System.Data.Common;
using System.Data;

// example::docs:rQN6ZzeeepyOpOnTPKAT:

// Ниже можно посмотреть на простейший способ создания каскада


[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_PerformanceTest : TestTask
{
    public CascadeSponge_mt_20230930_PerformanceTest(TestConstructor constructor) :
                                            base(nameof(CascadeSponge_mt_20230930_PerformanceTest), constructor)
    {
        taskFunc = Test;
    }

    public void Test()
    {
        Test(4, 4, 1);
        Test(8, 7, 24);
        Test(176, 176, 5);
    }

    public void Test(int tall, int wide, int cnt)
    {
        var cascade1t = new CascadeSponge_1t_20230905(_tall: tall, _wide: wide);
        var cascademt = new CascadeSponge_mt_20230930(_tall: tall, _wide: wide, ThreadsCount: 1);

        // var dlen = cascade1t.maxDataLen*5 - 1;
        var dlen = cascade1t.maxDataLen;
        var data = Keccak_abstract.allocator.AllocMemory(dlen);
        try
        {
            byte* a = data;
            // Инициализируем массивы данных - имитируем синхропосылку и ключ простыми значениями
            for (int i = 0; i < dlen; i++)
                a[i] = (byte) (i*7);

            var st1 = new DriverForTestsLib.SimpleTimeMeter();
            cascade1t.step(data: data, dataLen: dlen, countOfSteps: cnt);
            st1.Dispose();

            var stm = new DriverForTestsLib.SimpleTimeMeter();
            cascademt.step(data: data, dataLen: dlen, countOfSteps: cnt);
            stm.Dispose();

            if (!BytesBuilder.UnsecureCompare(cascade1t.maxDataLen, cascademt.maxDataLen, cascade1t.lastOutput, cascademt.lastOutput))
                throw new Exception($"cascade1t != cascademt for {tall}/{wide}");

            // Console.WriteLine(st1.TotalMilliseconds);
            // Console.WriteLine(stm.TotalMilliseconds);

            var tm = st1.TotalMilliseconds * 100 / stm.TotalMilliseconds;
            this.Name += $"   {tm:F0}%";
            var min = (Environment.ProcessorCount - 1) * 100;
            var max = Environment.ProcessorCount * 110;
            if (tm < min)
            {
                var te = new TestError() {Message = $"Low performance for {tall}/{wide}: {tm:F0}%"};
                this.error.Add(te);
            }
            if (tm > max)
            {
                var te = new TestError() {Message = $"Very high performance for {tall}/{wide}: {tm:F0}%"};
                this.error.Add(te);
            }
        }
        finally
        {
            cascade1t.Dispose();
            cascademt.Dispose();
            data     .Dispose();
        }
    }
}

