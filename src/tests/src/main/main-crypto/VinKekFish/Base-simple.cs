using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;

using static VinKekFish_Utils.Utils;



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
    {/*
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
    */}
}
