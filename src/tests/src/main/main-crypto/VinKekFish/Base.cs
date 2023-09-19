using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;

using static VinKekFish_Utils.Utils;


//[TestTagAttribute("inWork")]
//[TestTagAttribute("VinKekFish", duration: 2000, singleThread: false)]
public class VinKekFish_test_baseK: Keccak_test_parent
{
    public VinKekFish_test_baseK(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    public override DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath("src/tests/src/main/main-crypto/VinKekFish/");
    }

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new List<byte[]>();

            return lst;
        }
    }
}

/*
[TestTagAttribute("inWork")]
[TestTagAttribute("VinKekFish", duration: 60e3, singleThread: true)]
public unsafe class VinKekFish_test_simplebase_compareToEtalon : TestTask
{
    public VinKekFish_test_simplebase_compareToEtalon(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_simplebase_compareToEtalon), constructor)
    {
        taskFunc = this.Test;
    }

    public void Test()
    {
        var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
        using var k   = new VinKekFish_k1_base_20210419();

        using var  key = allocator.AllocMemory(128);
        using var @out = allocator.AllocMemory(VinKekFishBase_etalonK1.BLOCK_SIZE);

        BytesBuilder.FillByBytes(1, key, key.len);

        int roundsCnt = 5, RoundsForFinal = 5, RoundsForTailsBlock = 1, RoundsForFirstKeyBlock = 4;

        File.Delete("log-k1.log");
        File.Delete("log-KN.log");

        k.Init1(roundsCnt, PreRoundsForTranspose: roundsCnt);
        k.Init2(key, key.len, RoundsForEnd: RoundsForFinal, RoundsForExtendedKey: RoundsForTailsBlock, Rounds: RoundsForFirstKeyBlock);

        using var k1t1  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 1);

        k1t1 .Init1(roundsCnt);
        k1t1 .Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);
    }
}
*/

[TestTagAttribute("inWork")]
[TestTagAttribute("VinKekFish", duration: 60e3, singleThread: true)]
public unsafe class VinKekFish_test_base_compareToEtalon : TestTask
{
    public VinKekFish_test_base_compareToEtalon(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_base_compareToEtalon), constructor)
    {
        taskFunc = this.Test;
    }

    public void Test()
    {
        File.Delete("log-k1.log");
        File.Delete("log-KN.log");

        testByIncorrect();

        Test(VinKekFishBase_etalonK1.MIN_ROUNDS,     VinKekFishBase_etalonK1.MIN_ROUNDS,     VinKekFishBase_etalonK1.MIN_ABSORPTION_ROUNDS_D, VinKekFishBase_etalonK1.MIN_ABSORPTION_ROUNDS_D);
        Test(VinKekFishBase_etalonK1.MIN_ROUNDS,     VinKekFishBase_etalonK1.MIN_ROUNDS,     1, 1);
        Test(VinKekFishBase_etalonK1.REDUCED_ROUNDS, VinKekFishBase_etalonK1.REDUCED_ROUNDS, 1, 1);
        Test(VinKekFishBase_etalonK1.NORMAL_ROUNDS,  VinKekFishBase_etalonK1.NORMAL_ROUNDS,  1, 1);
        Test(VinKekFishBase_etalonK1.EXTRA_ROUNDS,   VinKekFishBase_etalonK1.EXTRA_ROUNDS,   1, 1);
        Test(VinKekFishBase_etalonK1.MAX_ROUNDS,     VinKekFishBase_etalonK1.MAX_ROUNDS,     1, 1);

        Test(5,   5,   1, 1);
        Test(8,   8,   1, 1);
        Test(64 , 64,  1, 1);
        Test(128, 128, 1, 1);
    }

    public void Test(int roundsCnt, int RoundsForFinal, int RoundsForFirstKeyBlock, int RoundsForTailsBlock)
    {
        using var k1e   = new VinKekFish_k1_base_20210419();
        using var k1t1  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 1);
        using var k1t4  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 4);
        using var k1t16 = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 16);

        k1t1 .output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);
        k1t4 .output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);
        k1t16.output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);

              var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
        using var key       = allocator.AllocMemory(ushort.MaxValue);
        using var out1e     = allocator.AllocMemory(VinKekFishBase_etalonK1.BLOCK_SIZE);

        // BytesBuilder.FillByBytes(1, key, key.len);
        for (int i = 0; i < key.len; i += 2)
        {
            key[i+0] = (byte) (i*3);
            key[i+1] = (byte) (i*5);
        }

        k1e  .Init1(roundsCnt, PreRoundsForTranspose: roundsCnt);
        k1e  .Init2(key, key.len, RoundsForEnd: RoundsForFinal, RoundsForExtendedKey: RoundsForTailsBlock, Rounds: RoundsForFirstKeyBlock);
        k1t1 .Init1(PreRoundsForTranspose: roundsCnt);
        k1t1 .Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);
        k1t4 .Init1(roundsCnt);
        k1t4 .Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);
        k1t16.Init1(roundsCnt);
        k1t16.Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);

        k1e  .DoStep(roundsCnt);
        k1t1 .doStepAndIO(roundsCnt);
        k1t4 .doStepAndIO(roundsCnt);
        k1t16.doStepAndIO(roundsCnt);

        k1e .outputData(out1e , 0, out1e .len, VinKekFishBase_etalonK1.BLOCK_SIZE);
        var sp = new ReadOnlySpan<byte>(out1e, (int) out1e.len);
        Console.WriteLine(Convert.ToHexString(sp));

        using var out1t1 = k1t1 .output.getBytes();
        sp = new ReadOnlySpan<byte>(out1t1, (int) out1t1.len);
        Console.WriteLine(Convert.ToHexString(sp));

        using var out1t4 = k1t4 .output.getBytes();
        sp = new ReadOnlySpan<byte>(out1t4, (int) out1t4.len);
        Console.WriteLine(Convert.ToHexString(sp));

        using var out1t16 = k1t16.output.getBytes();
        sp = new ReadOnlySpan<byte>(out1t16, (int) out1t16.len);
        Console.WriteLine(Convert.ToHexString(sp));

        k1t1  .output.Dispose();
        k1t4  .output.Dispose();
        k1t16 .output.Dispose();

        if (!out1t1.UnsecureCompare(out1e))
        {
            throw new Exception($"!out1t1.UnsecureCompare(out1e) {roundsCnt}");
        }
        if (!out1t4.UnsecureCompare(out1e))
        {
            throw new Exception($"!out1t4.UnsecureCompare(out1e) {roundsCnt}");
        }
        if (!out1t16.UnsecureCompare(out1e))
        {
            throw new Exception($"!out1t16.UnsecureCompare(out1e) {roundsCnt}");
        }
    }

    public void testByIncorrect()
    {
        int incorrect = 0;
        try
        {
            Test(VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MIN_ABSORPTION_ROUNDS_D, VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS);
            incorrect += 1;
            // this.error.Add();
        }
        catch
        {}

        try
        {
            Test(VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS, 1, VinKekFishBase_etalonK1.MAX_ROUNDS);
            incorrect += 2;
        }
        catch
        {}

        try
        {
            Test(VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS, 1);
            incorrect += 4;
        }
        catch
        {}

        if (incorrect != 0)
            throw new Exception($"testByIncorrect ({incorrect})");
    }
}
