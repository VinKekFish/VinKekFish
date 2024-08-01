namespace cryptoprime_tests;

using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;

using static VinKekFish_Utils.Utils;


// [TestTagAttribute("inWork")]
[TestTagAttribute("performance")]
[TestTagAttribute("VinKekFish", duration: 15e3, singleThread: true)]
public unsafe class VinKekFish_test_base_performance1 : TestTask
{
    public VinKekFish_test_base_performance1(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_base_performance1), constructor)
    {
        TaskFunc = this.Test;
    }

    public void Test()
    {
        var keyLen = 128;
        Test(4, 4, 1, 1, keyLen);
    }

    public void Test(int roundsCnt, int RoundsForFinal, int RoundsForFirstKeyBlock, int RoundsForTailsBlock, int keyLen, int min = 100)
    {
        using var k1e   = new VinKekFish_k1_base_20210419();
//        using var k1t1  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 1);
        using var k1t4  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: Environment.ProcessorCount);

        k1t4 .output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);

              var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
        using var key       = allocator.AllocMemory(keyLen);
        using var out1e     = allocator.AllocMemory(VinKekFishBase_etalonK1.BLOCK_SIZE);

        // BytesBuilder.FillByBytes(1, key, key.len);
        for (int i = 0; i < key.len-1; i += 2)
        {
            key[i+0] = (byte) (i*3);
            key[i+1] = (byte) (i*5);
        }

        k1e  .Init1(roundsCnt, PreRoundsForTranspose: roundsCnt);
        k1e  .Init2(key, key.len, RoundsForEnd: RoundsForFinal, RoundsForExtendedKey: RoundsForTailsBlock, Rounds: RoundsForFirstKeyBlock);
        k1t4 .Init1(roundsCnt);
        k1t4 .Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);

        var st1 = new DriverForTestsLib.SimpleTimeMeter();
        k1e  .InputData_Xor(null, 0, 0);
        k1e  .DoStep(roundsCnt);
        st1.Dispose();

        var stm = new DriverForTestsLib.SimpleTimeMeter();
        k1t4 .DoStepAndIO(roundsCnt);
        stm.Dispose();

        k1e .OutputData(out1e , 0, out1e .len, VinKekFishBase_etalonK1.BLOCK_SIZE);
        var sp = new ReadOnlySpan<byte>(out1e, (int) out1e.len);

        using var out1t4 = k1t4 .output.GetBytes();
        sp = new ReadOnlySpan<byte>(out1t4, (int) out1t4.len);

        if (!out1t4.UnsecureCompare(out1e))
        {
            throw new Exception($"!out1t4.UnsecureCompare(out1e) {roundsCnt}, {keyLen}");
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
