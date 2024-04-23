using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;

using static VinKekFish_Utils.Utils;


/// [TestTagAttribute("inWork")]
[TestTagAttribute("performance")]
[TestTagAttribute("VinKekFish", duration: 15e3, singleThread: true)]
public unsafe class VinKekFish_test_base_performance11 : TestTask
{
    public VinKekFish_test_base_performance11(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_base_performance11), constructor)
    {
        taskFunc = this.Test;
    }

    public void Test()
    {
        var keyLen = 128;
        Test(30, 30, 30, 30, keyLen);
    }

    public void Test(int roundsCnt, int RoundsForFinal, int RoundsForFirstKeyBlock, int RoundsForTailsBlock, int keyLen, int min = 100)
    {
        using var k1t1 = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, K: 11, ThreadCount: 1);
        using var k1t4 = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, K: 11, ThreadCount: Environment.ProcessorCount);

        k1t1.output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE*11);
        k1t4.output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE*11);

              var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
        using var key       = allocator.AllocMemory(keyLen);

        // BytesBuilder.FillByBytes(1, key, key.len);
        for (int i = 0; i < key.len-1; i += 2)
        {
            key[i+0] = (byte) (i*3);
            key[i+1] = (byte) (i*5);
        }

        k1t1.Init1(roundsCnt);
        k1t1.Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);
        k1t4.Init1(roundsCnt);
        k1t4.Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);

        var st1 = new DriverForTestsLib.SimpleTimeMeter();
        k1t1.doStepAndIO(roundsCnt);
        st1.Dispose();

        var stm = new DriverForTestsLib.SimpleTimeMeter();
        k1t4.doStepAndIO(roundsCnt);
        stm.Dispose();

        using var out1t1 = k1t1.output.getBytes();
        using var out1t4 = k1t4.output.getBytes();

        if (!out1t4.UnsecureCompare(out1t1))
        {
            throw new Exception($"!out1t4.UnsecureCompare(out1t1) {roundsCnt}, {keyLen}");
        }

        var tm = st1.TotalMilliseconds * 100 / stm.TotalMilliseconds;
        var   cntPerSecond = (int) (1 * 1000.0 / stm.TotalMilliseconds);
        var bytePerSecornd = cntPerSecond * 512;
        this.Name += $" {(int)tm,3}% {cntPerSecond,4}, {$"{bytePerSecornd:#,0}",7}";
        // var min = 100; //cascademt.ThreadsCount * 100 / 2;
        var max = Environment.ProcessorCount * 110;
        if (tm < min)   // ??? Производительность плавает постоянно
        {
            var te = new TestError() {Message = $"Low performance: {tm:F0}%"};
            this.error.Add(te);
        }
        if (tm > max)
        {
            var te = new TestError() {Message = $"Very high performance: {tm:F0}%"};
            this.error.Add(te);
        }
    }
}
