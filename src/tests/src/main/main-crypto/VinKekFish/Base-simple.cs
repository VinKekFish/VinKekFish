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
        File.Delete("log-KN.log");
        File.Delete("log-KNe.log");

        var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();

        using var  key = allocator.AllocMemory(8192); // 2048*3+2048   // Здесь должно быть чётное значение, т.к. ниже инициализация этого требует
        using var  OIV = allocator.AllocMemory(3444);   // 1148*3 == MAX_OIV_K
        using var @out = allocator.AllocMemory(512*3);  // VinKekFishBase_etalonK1.BLOCK_SIZE*K

        // BytesBuilder.FillByBytes(1, key, key.len);
        var uskey = (ushort *) key;
        var len2  = key.len / 2;
        for (int i = 0; i < len2; i += 2)
        {
            uskey[i] = (ushort) (i * 331 + i);
        }

        var usOIV = (ushort *) OIV;
            len2  = OIV.len / 2;
        for (int i = 0; i < len2; i += 2)
        {
            usOIV[i] = (ushort) (i * 181 + i);
        }

        int roundsCnt = 9, RoundsForFinal = 9, RoundsForTailsBlock = 2, RoundsForFirstKeyBlock = 4;

        using var k  = new VinKekFishBase_KN_20210525(K: 3, CountOfRounds: roundsCnt, ThreadCount: 4);

        var StateLen = 25 * 128 * 3;    // 9600
        if (k.Len != StateLen)
            throw new Exception("k.Len != StateLen");

        using var state = allocator.AllocMemory(StateLen);
        using var tweak = allocator.AllocMemory(24);
        
        var twu = (ulong *) tweak;
        var TW  = stackalloc ulong[3];  // Для текущего значения tweak
        twu[0] = 4843377040061051741;
        twu[1] = 6387661195837588921;
        twu[2] = 123456789; // Это нигде не участвует, т.к. расширение твика вычисляется из первых двух слов твика
        TW[0]  = 0; TW[1] = 0; TW[2] = 0;

        k.Init1(roundsCnt);
        k.Init2
        (
            key,
            OpenInitializationVector: OIV,
            TweakInit: tweak,
            RoundsForTailsBlock:    RoundsForTailsBlock,
            RoundsForFinal:         RoundsForFinal,
            RoundsForFirstKeyBlock: RoundsForFirstKeyBlock
        );


        Init2(state, OIV, tweak, TW, key, RoundsForFinal, RoundsForTailsBlock, RoundsForFirstKeyBlock);


        k.step(k.MIN_ROUNDS_K);
    }

    protected void Init2(Record state, Record oiv, Record tweak, ulong * TW, Record key, int roundsForFinal, int roundsForTailsBlock, int roundsForFirstKeyBlock)
    {
        state.Clear();

        // Максимальная длина ключа 2048*3
        state[0] = 0;
        state[1] = 24;

        state[6146] = 116;
        state[6147] = 13;

        if (oiv.len != 3444)
            throw new Exception("Init2: oiv.len != 3444");

        for (int i = 0; i < 3444; i++)
            state[6148+i] = oiv[i];
        
        for (int i = 0; i < 6144; i++)
            state[2+i] = key[i];

        TW[0]  = ((ulong *) tweak)[0];
        TW[1]  = ((ulong *) tweak)[1];
        TW[0] += 1253539379;
        TW[1] += 8192;

        TW[2]  = TW[0] ^ TW[1];

        VinKekFish_Utils.Utils.MsgToFile($"round started 4", "KNe");
        VinKekFish_Utils.Utils.ArrayToFile((byte *) TW, 16, "KNe");
        transpose128(state);
    }

    private void transpose128(Record state)
    {
        VinKekFish_Utils.Utils.ArrayToFile(state, 9600, "KNe");

        var buff = stackalloc byte[9600];

        buff[0]   = state[0];
        buff[128] = state[1];
        buff[256] = state[2];
        buff[384] = state[3];
        buff[512] = state[4];
        buff[640] = state[5];

        int j = 768;
        for (int i = 6; i < 9600; i++)
        {
            buff[i] = state[j];
            j += 128;
            if (j >= 9600)
            {
                j -= 9600;
                j++;
            }
        }

        BytesBuilder.CopyTo(9600, 9600, buff, state);
        VinKekFish_Utils.Utils.ArrayToFile(state, 9600, "KNe");
    }
}
