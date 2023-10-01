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
[TestTagAttribute("CascadeSponge", duration: 1500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_PerformanceTest : TestTask
{
    public CascadeSponge_mt_20230930_PerformanceTest(TestConstructor constructor) :
                                            base("", constructor)
    {
        taskFunc  = Test;
        this.Name = this.GetType().Name;
    }

    public virtual void Test()
    {
        // В первый раз, почему-то, очень медленно работает шаг - делаем это вне измерений производительности
        var cascade1t = new CascadeSponge_1t_20230905(_tall: 4, _wide: 4);
        cascade1t.step(countOfSteps: 1);
        cascade1t.Dispose();
    }

    public void Test(int min, int tall, int wide, int cnt)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var cascade1t = new CascadeSponge_1t_20230905(_tall: tall, _wide: wide);
        var cascademt = new CascadeSponge_mt_20230930(_tall: tall, _wide: wide, ThreadsCount: 0);

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
            this.Name += $"  {tm:F0}% {cnt/stm.TotalMilliseconds:F0}";
            // var min = 100; //cascademt.ThreadsCount * 100 / 2;
            var max = Environment.ProcessorCount * 110;
            if (tm < min)   // ??? Производительность плавает постоянно
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


[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 1500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_PerformanceTest_9 : CascadeSponge_mt_20230930_PerformanceTest
{
    public CascadeSponge_mt_20230930_PerformanceTest_9(TestConstructor constructor) :
                                            base(constructor)
    {}

    public override void Test()
    {
        Test( 90, 9,  8, 96);
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 1500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_PerformanceTest_16 : CascadeSponge_mt_20230930_PerformanceTest
{
    public CascadeSponge_mt_20230930_PerformanceTest_16(TestConstructor constructor) :
                                            base(constructor)
    {}

    public override void Test()
    {
        Test(100, 16, 16, 27);
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 1500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_PerformanceTest_29 : CascadeSponge_mt_20230930_PerformanceTest
{
    public CascadeSponge_mt_20230930_PerformanceTest_29(TestConstructor constructor) :
                                            base(constructor)
    {}

    public override void Test()
    {
        Test(100, 29, 28, 27);
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 1500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_PerformanceTest_88 : CascadeSponge_mt_20230930_PerformanceTest
{
    public CascadeSponge_mt_20230930_PerformanceTest_88(TestConstructor constructor) :
                                            base(constructor)
    {}

    public override void Test()
    {
        Test(130, 88, 88, 15);
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 1500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_PerformanceTest_176 : CascadeSponge_mt_20230930_PerformanceTest
{
    public CascadeSponge_mt_20230930_PerformanceTest_176(TestConstructor constructor) :
                                            base(constructor)
    {}

    public override void Test()
    {
        Test(250, 176, 176, 15);
    }
}
