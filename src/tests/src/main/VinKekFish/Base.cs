using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;


//[TestTagAttribute("inWork")]
//[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public class VinKekFish_test_baseK: Keccak_test_parent
{
    public VinKekFish_test_baseK(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}


    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            return lst;
        }
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public unsafe class VinKekFish_test_base_compareToEtalon : TestTask
{
    public VinKekFish_test_base_compareToEtalon(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_base_compareToEtalon), constructor)
    {
        taskFunc = this.Test;
    }

    public void Test()
    {
        var roundsCnt = 128;

        using var k1e   = new VinKekFish_k1_base_20210419();
        using var k1t1  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 1,  TimerIntervalMs: -1);
        using var k1t4  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 4,  TimerIntervalMs: -1);
        using var k1t16 = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 16, TimerIntervalMs: -1);

        k1t1 .output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);
        k1t4 .output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);
        k1t16.output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);

              var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
        using var key       = allocator.AllocMemory(ushort.MaxValue);
        using var out1e     = allocator.AllocMemory(VinKekFishBase_etalonK1.BLOCK_SIZE);

        BytesBuilder.FillByBytes(1, key, key.len);

        k1e .Init1(roundsCnt);
        k1e .Init2(key, key.len);
        k1t1.Init1(roundsCnt);
        k1t1.Init2(key);

        k1e  .DoStep(roundsCnt);
        k1t1 .doStepAndIO(roundsCnt);
        // k1t4 .doStepAndIO(roundsCnt);
        // k1t16.doStepAndIO(roundsCnt);

        k1e .outputData(out1e , 0, out1e .len, VinKekFishBase_etalonK1.BLOCK_SIZE);
        var sp = new ReadOnlySpan<byte>(out1e, (int) out1e.len);
        Console.WriteLine(Convert.ToHexString(sp));

        using var out1t1 = k1t1 .output.getBytes();
        sp = new ReadOnlySpan<byte>(out1t1, (int) out1t1.len);
        Console.WriteLine(Convert.ToHexString(sp));

        k1t1  .output.Dispose();
        k1t4  .output.Dispose();
        k1t16 .output.Dispose();

        if (!out1t1.UnsecureCompare(out1e))
        {
            throw new Exception("!out1t1.UnsecureCompare(out1e)");
        }
    }
}
