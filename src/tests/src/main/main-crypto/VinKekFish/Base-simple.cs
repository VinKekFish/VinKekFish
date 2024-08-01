namespace cryptoprime_tests;

using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;

using static VinKekFish_Utils.Utils;
using CodeGenerated.Cryptoprimes;
using System.Text.Json.Serialization;

/*
Конкретная проверка развёрнутой реализацией для конкретных значений параметров
*/


// [TestTagAttribute("inWork")]
[TestTagAttribute("Mandatory")]
[TestTagAttribute("VinKekFish", duration: 500, singleThread: false)]
public unsafe class VinKekFish_test_simplebase : TestTask
{
    public VinKekFish_test_simplebase(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_simplebase), constructor)
    {
        TaskFunc = this.Test;
    }

    public void Test()
    {
        File.Delete("log-KN.log");
        File.Delete("log-KNe.log");

        var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();

        using var  key = allocator.AllocMemory(8190,  "key"); // 2048*3+2046   // Здесь должно быть чётное значение, т.к. ниже инициализация этого требует
        using var  OIV = allocator.AllocMemory(3444,  "OIV");   // 1148*3 == MAX_OIV_K
        using var @out = allocator.AllocMemory(512*3, "out");  // VinKekFishBase_etalonK1.BLOCK_SIZE*K

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

        using var state = allocator.AllocMemory(StateLen, "state");
        using var tweak = allocator.AllocMemory(24,       "base tweak");
        
        var twu = (ulong *) tweak;
        var TW  = stackalloc ulong[3];  // Для текущего значения tweak
        twu[0] = 4843377040061051741;
        twu[1] = 6387661195837588921;
        twu[2] = 123456789; // Это нигде не участвует, т.к. расширение твика вычисляется из первых двух слов твика
        TW[0]  = 0; TW[1] = 0; TW[2] = 0;       // Делаем заготовку для tweak непосредственно при вычислениях

        k.Init1(roundsCnt);
        k.Init2
        (
            key,
            OpenInitializationVector: OIV,
            TweakInit:                tweak,
            RoundsForTailsBlock:      RoundsForTailsBlock,
            RoundsForFinal:           RoundsForFinal,
            RoundsForFirstKeyBlock:   RoundsForFirstKeyBlock
        );


        Init2(state, OIV, tweak, TW, key, RoundsForFinal, RoundsForTailsBlock, RoundsForFirstKeyBlock);

        // Выполняем вычислительный шаг, например, для генерации гаммы
        k.output = new BytesBuilderStatic(512*3);
        k.DoStepAndIO(k.MIN_ROUNDS_K, regime: 4);

        state[0] ^= 0;
        state[1] ^= 0;
        state[2] ^= 4;

        TW[0] += 1253539379;
        TW[1] += 4UL << 40;
        TW[2]  = TW[0] ^ TW[1];

        CalcRound(state, TW, 9);

        var r = stackalloc byte[512*3];

        k.output.GetBytesAndRemoveIt(r, 512*3);

        if (!BytesBuilder.UnsecureCompare(512*3, 512*3, state, r))
            throw new Exception("!BytesBuilder.UnsecureCompare(512*3, 512*3, state, r)");
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

        // Вводим ключ и синхропосылку
        for (int i = 0; i < 3444; i++)
            state[6148 + i] = oiv[i];

        for (int i = 0; i < 6144; i++)
            state[2 + i] = key[i];

        TW[0] = ((ulong*)tweak)[0];
        TW[1] = ((ulong*)tweak)[1];
        TW[0] += 1253539379;
        TW[1] += 8190;
        TW[2] = TW[0] ^ TW[1];

        CalcRound(state, TW, 4);

        // Вводим остаток ключа (весь остаток не умещается)
        BytesBuilder.CopyTo(512*3, 9600, key.array + 2048*3, state.array + 3);
        state[0] ^= 0;
        state[1] ^= 6 | 0xC0;

        TW[0] += 1253539379;
        TW[1] += 512*3 + 0x0100_0000_0000_0000;
        TW[2] = TW[0] ^ TW[1];

        CalcRound(state, TW, 2);

        // Окончание ключа
        BytesBuilder.CopyTo(510, 9600, key.array + 2048*3 + 512*3, state.array + 3);
        state[0] ^= 254;
        state[1] ^= 1 | 0xC0;

        TW[0] += 1253539379;
        TW[1] += 510 + 0x0100_0000_0000_0000;
        TW[2] = TW[0] ^ TW[1];

        CalcRound(state, TW, 2);

        // Отбивка ключа
        BytesBuilder.ToNull(512*3, state.array + 3);
        state[0] ^= 0;
        state[1] ^= 0x80;
        state[2] ^= 255;

        TW[0] += 1253539379;
        TW[1] += 0 + 0x0100_0000_0000_0000;
        TW[1] += 255UL << 40;
        TW[2] = TW[0] ^ TW[1];

        CalcRound(state, TW, 9);
    }

    private void CalcRound(Record state, ulong* TW, ulong NumberOfRounds)
    {
        VinKekFish_Utils.Utils.MsgToFile($"round started {NumberOfRounds}", "KNe");
        VinKekFish_Utils.Utils.ArrayToFile((byte*)TW, 16, "KNe");

        // Осуществляем предварительное преобразование
        Transpose128(state);
        ThreeFish(state, TW, 0);
        Transpose128(state);

        // r - номер раунда. Расчёт номеров полураундов осуществляется в цикле
        for (ulong r = 0; r < NumberOfRounds; r++)
        {
            VinKekFish_Utils.Utils.MsgToFile($"semiround {r * 2 + 0}", "KNe");
            Keccak(state);
            Transpose200_8(state);
            ThreeFish(state, TW, r * 2);
            Transpose128(state);

            VinKekFish_Utils.Utils.MsgToFile($"semiround {r * 2 + 1}", "KNe");
            Keccak(state);
            Transpose200(state);
            ThreeFish(state, TW, r * 2 + 1);
            Transpose128(state);
        }

        VinKekFish_Utils.Utils.MsgToFile($"final", "KNe");

        Keccak(state);
        Transpose200(state);
        Keccak(state);
        Transpose200_8(state);
        Keccak(state);
        Transpose200(state);
        Keccak(state);
        Transpose200_8(state);

        Keccak(state);
        Transpose200(state);
        Keccak(state);
        Transpose200_8(state);
        Keccak(state);
        Transpose200(state);
        Keccak(state);
        Transpose200_8(state);
    }

    // Выполняет преобразование для перестановок, в частности, самое первое транспонирование перед ThreeFish предварительного преобразования
    private void Transpose128(Record state)
    {
        VinKekFish_Utils.Utils.ArrayToFile(state, 9600, "KNe");

        var buff = stackalloc byte[9600];

        // На всякий случай дублируем перестановки, чтобы точно знать, хотя бы что у нас в начале
        buff[0] = state[0];
        buff[1] = state[128];
        buff[2] = state[256];
        buff[3] = state[384];
        buff[4] = state[512];
        buff[5] = state[640];

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

    private void Transpose200(Record state)
    {
        VinKekFish_Utils.Utils.ArrayToFile(state, 9600, "KNe");

        var buff = stackalloc byte[9600];

        // На всякий случай дублируем перестановки, чтобы точно знать, хотя бы что у нас в начале
        buff[0] = state[0];
        buff[1] = state[0200];
        buff[2] = state[0400];
        buff[3] = state[0600];
        buff[4] = state[0800];
        buff[5] = state[1000];

        int j = 1200;
        for (int i = 6; i < 9600; i++)
        {
            buff[i] = state[j];
            j += 200;
            if (j >= 9600)
            {
                j -= 9600;
                j++;
            }
        }

        BytesBuilder.CopyTo(9600, 9600, buff, state);
        VinKekFish_Utils.Utils.ArrayToFile(state, 9600, "KNe");
    }

    private void Transpose200_8(Record state)
    {
        VinKekFish_Utils.Utils.ArrayToFile(state, 9600, "KNe");

        var buff = stackalloc byte[9600];

        int j = 0;
        j = Tr_200_8_h(state, buff, j, 000); if (j != 48) throw new Exception("transpose200_8: j != 48");
        j = Tr_200_8_h(state, buff, j, 008);
        j = Tr_200_8_h(state, buff, j, 016);
        j = Tr_200_8_h(state, buff, j, 024);
        j = Tr_200_8_h(state, buff, j, 032);
        j = Tr_200_8_h(state, buff, j, 040);
        j = Tr_200_8_h(state, buff, j, 048);
        j = Tr_200_8_h(state, buff, j, 056);
        j = Tr_200_8_h(state, buff, j, 064);
        j = Tr_200_8_h(state, buff, j, 072);
        j = Tr_200_8_h(state, buff, j, 080);
        j = Tr_200_8_h(state, buff, j, 088);
        j = Tr_200_8_h(state, buff, j, 096);
        j = Tr_200_8_h(state, buff, j, 104);
        j = Tr_200_8_h(state, buff, j, 112);
        j = Tr_200_8_h(state, buff, j, 120);
        j = Tr_200_8_h(state, buff, j, 128);
        j = Tr_200_8_h(state, buff, j, 136);
        j = Tr_200_8_h(state, buff, j, 144);
        j = Tr_200_8_h(state, buff, j, 152);
        j = Tr_200_8_h(state, buff, j, 160);
        j = Tr_200_8_h(state, buff, j, 168);
        j = Tr_200_8_h(state, buff, j, 176);
        j = Tr_200_8_h(state, buff, j, 184);
        j = Tr_200_8_h(state, buff, j, 192);

        for (int i = 1; i < 8; i++)
        {
            if (j != 48*25*i) throw new Exception("transpose200_8: j != 48*25*i)");

            j = Tr_200_8_h(state, buff, j, 000+i);
            j = Tr_200_8_h(state, buff, j, 008+i);
            j = Tr_200_8_h(state, buff, j, 016+i);
            j = Tr_200_8_h(state, buff, j, 024+i);
            j = Tr_200_8_h(state, buff, j, 032+i);
            j = Tr_200_8_h(state, buff, j, 040+i);
            j = Tr_200_8_h(state, buff, j, 048+i);
            j = Tr_200_8_h(state, buff, j, 056+i);
            j = Tr_200_8_h(state, buff, j, 064+i);
            j = Tr_200_8_h(state, buff, j, 072+i);
            j = Tr_200_8_h(state, buff, j, 080+i);
            j = Tr_200_8_h(state, buff, j, 088+i);
            j = Tr_200_8_h(state, buff, j, 096+i);
            j = Tr_200_8_h(state, buff, j, 104+i);
            j = Tr_200_8_h(state, buff, j, 112+i);
            j = Tr_200_8_h(state, buff, j, 120+i);
            j = Tr_200_8_h(state, buff, j, 128+i);
            j = Tr_200_8_h(state, buff, j, 136+i);
            j = Tr_200_8_h(state, buff, j, 144+i);
            j = Tr_200_8_h(state, buff, j, 152+i);
            j = Tr_200_8_h(state, buff, j, 160+i);
            j = Tr_200_8_h(state, buff, j, 168+i);
            j = Tr_200_8_h(state, buff, j, 176+i);
            j = Tr_200_8_h(state, buff, j, 184+i);
            j = Tr_200_8_h(state, buff, j, 192+i);
        }
        

        BytesBuilder.CopyTo(9600, 9600, buff, state);
        VinKekFish_Utils.Utils.ArrayToFile(state, 9600, "KNe");
    }

    private static int Tr_200_8_h(Record state, byte* buff, int j, int start)
    {
        for (int i = start; i < 9600; i += 200)
            buff[j++] = state[i];

        return j;
    }

    private void ThreeFish(Record state, ulong* TW, ulong numberOfSemiround)
    {
        var st2  = stackalloc byte[9600];
        var twi  = stackalloc ulong[3];

        twi[0]  = TW[0]; twi[1] = TW[1];
        twi[0] += numberOfSemiround << 32;  // 32 - старшее 4-хбайтовое слово; идёт сложение с номером полураунда

        BytesBuilder.CopyTo(9600, 9600, state, st2);

        for (ulong i = 0; i < 75; i++)
            ThreeFishBlock(state, st2, twi, i);
    }

    /// <summary>Преобразует один блок преобразованием ThreeFish</summary>
    /// <param name="state">Состояние для изменения</param>
    /// <param name="state2">Первоначальное состояние. Не подвергается изменению</param>
    /// <param name="TW">Твик для данного полураунда</param>
    /// <param name="i">i - номер блока ThreeFish во внутреннем состоянии</param>
    private static void ThreeFishBlock(Record state, byte * state2, ulong* TW, ulong i)
    {
        var ekey = stackalloc byte[128 + 8];
        var text = stackalloc byte[128 + 8];
        var twi  = stackalloc ulong[3];

        twi[0]   = TW[0]; twi[1] = TW[1];
        twi[0]  += i;
        twi[2]   = twi[0] ^ twi[1];

        var j = i + 37;
        if (j >= 75)
            j -= 75;
        var k = j+1;
        if (k == 75)
            k = 0;

        var ckey = state2       + 128 * j;
        var pt1  = state .array + 128 * i;
        var pt2  = state2       + 128 * i;
        var kkey = state2       + 128 * k;

        BytesBuilder.CopyTo(128, 128, pt2,  text);      // Копируем блок для шифрования в отдельный массив, чтобы случайно ничего не испортить внутри тестов
        BytesBuilder.CopyTo(128, 128, ckey, ekey);
        BytesBuilder.CopyTo(  8,   8, kkey, ekey + 128);

        Threefish_Static_Generated.Threefish1024_step((ulong*)ekey, twi, (ulong*)text);

        BytesBuilder.CopyTo(128, 128, text, pt1);
    }

    private void Keccak(Record state)
    {
        var c = stackalloc byte[200];   // Это имеет размер 040
        var b = stackalloc byte[200];

        for (ulong i = 0; i < 48; i++)
        {
            var a = state.array + 200*i;
            KeccakPrime.Keccackf(a: (ulong *) a, b: (ulong *) b, c: (ulong *) c);
        }
    }
}
