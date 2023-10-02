using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;

using static VinKekFish_Utils.Utils;

/*
Конкретная проверка развёрнутой реализацией для конкретных значений параметров
*/


[TestTagAttribute("inWork")]
[TestTagAttribute("VinKekFish", duration: 60e3, singleThread: true)]
public unsafe class VinKekFish_test_simplebase : TestTask
{
    public VinKekFish_test_simplebase(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_simplebase), constructor)
    {
        taskFunc = this.Test;
    }

    public void Test()
    {
        var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
        using var k   = new VinKekFish_k1_base_20210419();

        using var  key = allocator.AllocMemory(1022);   // Здесь должно быть чётное значение, т.к. ниже инициализация этого требует
        using var @out = allocator.AllocMemory(VinKekFishBase_etalonK1.BLOCK_SIZE);

        // BytesBuilder.FillByBytes(1, key, key.len);
        var uskey = (ushort *) key;
        var len2  = key.len / 2;
        for (int i = 0; i < len2; i += 2)
        {
            uskey[i] = (ushort) (i * 331 + i);
        }

        int roundsCnt = 9, RoundsForFinal = 9, RoundsForTailsBlock = 2, RoundsForFirstKeyBlock = 4;

        using var k1t1  = new VinKekFishBase_KN_20210525(K: 3, CountOfRounds: roundsCnt, ThreadCount: 4);

        if (k1t1.MIN_ROUNDS_K != roundsCnt)
            throw new Exception("k1t1.MIN_ROUNDS_K != roundsCnt");

        k1t1.Init1(roundsCnt);
        k1t1.Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);
        k1t1.step(k1t1.MIN_ROUNDS_K);

        Init2(key, RoundsForFinal, RoundsForTailsBlock, RoundsForFirstKeyBlock);
    }

    protected void Init2(Record key, int roundsForFinal, int roundsForTailsBlock, int roundsForFirstKeyBlock)
    {
        
    }
}
