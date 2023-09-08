namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using maincrypto.keccak;
using vinkekfish;

using static CodeGenerated.Cryptoprimes.Threefish_Static_Generated;

using static cryptoprime.KeccakPrime;

// tests::docs:rQN6ZzeeepyOpOnTPKAT:

[TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public unsafe class CascadeSponge_20230905_BaseTest : TestTask
{
    public CascadeSponge_20230905_BaseTest(TestConstructor constructor) :
                                            base(nameof(CascadeSponge_20230905_BaseTest), constructor)
    {
        taskFunc = Test;
    }

    public void Test()
    {
        BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor = false;
        BytesBuilderForPointers.Record.doExceptionOnDisposeTwiced       = false;

        // Проверка расчёта параметров каскадной губки
        nint _wide = 0;
        CascadeSponge_1t_20230905.CalcCascadeParameters(192, 404, _tall: out nint _tall, _wide: ref _wide);
        if (_tall != 42 || _wide != 42) throw new Exception($"CascadeSponge_1t_20230905.CalcCascadeParameters(192, 404, _tall: out nint _tall, _wide: out nint _wide): _tall != 47 || _wide != 46. {_tall} {_wide}");
        //Console.WriteLine(_tall);Console.WriteLine(_wide);
        _wide = 0;
        CascadeSponge_1t_20230905.CalcCascadeParameters(11*1024, 404, _tall: out _tall, _wide: ref _wide);
        if (_tall != 176 || _wide != 58) throw new Exception($"CascadeSponge_1t_20230905.CalcCascadeParameters(11*1024, 404, _tall: out nint _tall, _wide: out nint _wide): _tall != 176 || _wide != 58. {_tall} {_wide}");
        // Console.WriteLine(_tall);Console.WriteLine(_wide);


        var cascade = new CascadeSponge_1t_20230905();
        // Console.WriteLine(cascade);
        try
        {
                  var dlen = cascade.maxDataLen-1;
            using var data = Keccak_abstract.allocator.AllocMemory(dlen);

            byte * a = data;
            // Инициализируем массивы данных - имитируем синхропосылку и ключ простыми значениями
            for (int i = 0; i < dlen; i++)
                a[i] = (byte) i;

            // Вводим данные и делаем шаг. Имитируем, что вводим синхропосылку и ключ
            //cascade.initKeyAndOIV(data, null, 0);
            cascade.step(data: data, dataLen: dlen, regime: 255);

            //var msg = VinKekFish_Utils.Utils.ArrayToHex(cascade.lastOutput, cascade.maxDataLen);
            //Console.WriteLine(msg);
            //Console.WriteLine();


            // Вычисляем все преобразования вручную
            using var top0 = new Keccak_20200918();
            using var top1 = new Keccak_20200918();
            using var top2 = new Keccak_20200918();
            using var top3 = new Keccak_20200918();
            using var mid0 = new Keccak_20200918();
            using var mid1 = new Keccak_20200918();
            using var mid2 = new Keccak_20200918();
            using var mid3 = new Keccak_20200918();
            using var bot0 = new Keccak_20200918();
            using var bot1 = new Keccak_20200918();
            using var bot2 = new Keccak_20200918();
            using var bot3 = new Keccak_20200918();
            using var out0 = new Keccak_20200918();
            using var out1 = new Keccak_20200918();
            using var out2 = new Keccak_20200918();
            using var out3 = new Keccak_20200918();

             // var W      = 3.0; // = Math.Log2(4)+1;
            // var Wn     = 21;  // Wn = 64 / W
            if (21 != cascade.Wn || dlen != 21*4-1 || cascade.countOfThreeFish != 2)
                throw new Exception($"CascadeSponge_20230905_BaseTest: 21 != cascade.Wn || dlen != 83 || cascade.countOfThreeFish != 2. {cascade.Wn} {dlen} {cascade}");

            // var mdl    = 72;  // maxDataLen  24*3
            // wide = 3
            var output = stackalloc byte[64*4];
            var revcon = stackalloc byte[64*4];
            var Threef = stackalloc byte[256*2];    // Это твики и ключи ThreeFish обратной связи

            // Инициализируем ключи и твики обратной связи
            BytesBuilder.ToNull(256*2, Threef);

            var TFl    = (ulong *) Threef;      // 192/8=24
            TFl[24]    = CascadeSponge_1t_20230905.TweakInitIncrement;
            TFl[25]    = 0;
            TFl[26]    = TFl[24] ^ TFl[25];
            TFl[32+24] = TFl[24] + CascadeSponge_1t_20230905.TweakInitIncrement;
            TFl[32+25] = 0;
            TFl[32+26] = TFl[32+24] ^ TFl[32+25];

            var key    = CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[0]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[1]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[2]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[3]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[4]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[5]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[6]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[7]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[8]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[9]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[10]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[11]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[12]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[13]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[14]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[15]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[16]    = threefish_slowly.C240 ^ TFl[0] ^ TFl[1] ^ TFl[2] ^ TFl[3] ^ TFl[4] ^ TFl[5] ^ TFl[6] ^ TFl[7] ^ TFl[8] ^ TFl[9] ^ TFl[10] ^ TFl[11] ^ TFl[12] ^ TFl[13] ^ TFl[14] ^ TFl[15];

            TFl[32+0]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+1]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+2]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+3]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+4]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+5]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+6]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+7]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+8]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+9]     = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+10]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+11]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+12]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+13]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+14]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+15]    = key; key       += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32+16]    = threefish_slowly.C240 ^ TFl[32+0] ^ TFl[32+1] ^ TFl[32+2] ^ TFl[32+3] ^ TFl[32+4] ^ TFl[32+5] ^ TFl[32+6] ^ TFl[32+7] ^ TFl[32+8] ^ TFl[32+9] ^ TFl[32+10] ^ TFl[32+11] ^ TFl[32+12] ^ TFl[32+13] ^ TFl[32+14] ^ TFl[32+15];

            BytesBuilder.ToNull(256, revcon);
            BytesBuilder.CopyTo(21, 256, a +  0,  revcon + 0);
            BytesBuilder.CopyTo(21, 256, a + 21,  revcon + 64);
            BytesBuilder.CopyTo(21, 256, a + 42,  revcon + 128);
            BytesBuilder.CopyTo(20, 256, a + 63,  revcon + 192);

            // Ввводим 47-мь байтов ввода в верхнюю губку
            Keccak_Input64_512(revcon +  0, 64, top0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, top1.S, 255);
            Keccak_Input64_512(revcon +128, 64, top2.S, 255);
            Keccak_Input64_512(revcon +192, 64, top3.S, 255);
            top0.CalcStep();
            top1.CalcStep();
            top2.CalcStep();
            top3.CalcStep();
            Keccak_Output_512(output +  0, 64, top0.S);
            Keccak_Output_512(output + 64, 64, top1.S);
            Keccak_Output_512(output +128, 64, top2.S);
            Keccak_Output_512(output +192, 64, top3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            Keccak_Input64_512(revcon +  0, 64, mid0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, mid1.S, 255);
            Keccak_Input64_512(revcon +128, 64, mid2.S, 255);
            Keccak_Input64_512(revcon +192, 64, mid3.S, 255);
            mid0.CalcStep();
            mid1.CalcStep();
            mid2.CalcStep();
            mid3.CalcStep();
            Keccak_Output_512(output +  0, 64, mid0.S);
            Keccak_Output_512(output + 64, 64, mid1.S);
            Keccak_Output_512(output +128, 64, mid2.S);
            Keccak_Output_512(output +192, 64, mid3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            Keccak_Input64_512(revcon +  0, 64, bot0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, bot1.S, 255);
            Keccak_Input64_512(revcon +128, 64, bot2.S, 255);
            Keccak_Input64_512(revcon +192, 64, bot3.S, 255);
            bot0.CalcStep();
            bot1.CalcStep();
            bot2.CalcStep();
            bot3.CalcStep();
            Keccak_Output_512(output +  0, 64, bot0.S);
            Keccak_Output_512(output + 64, 64, bot1.S);
            Keccak_Output_512(output +128, 64, bot2.S);
            Keccak_Output_512(output +192, 64, bot3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            Keccak_Input64_512(revcon +  0, 64, out0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, out1.S, 255);
            Keccak_Input64_512(revcon +128, 64, out2.S, 255);
            Keccak_Input64_512(revcon +192, 64, out3.S, 255);
            out0.CalcStep();
            out1.CalcStep();
            out2.CalcStep();
            out3.CalcStep();
            Keccak_Output_512(output +  0, 64, out0.S);
            Keccak_Output_512(output + 64, 64, out1.S);
            Keccak_Output_512(output +128, 64, out2.S);
            Keccak_Output_512(output +192, 64, out3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            Threefish1024_step(TFl + 0,  TFl +  0+24, (ulong *)  revcon);
            Threefish1024_step(TFl + 32, TFl + 32+24, (ulong *) (revcon + 128));
            BytesBuilder.CopyTo(256, 256, revcon, output);

            // Транспонируем вывод: по 128-мь байтов блок
            for (int i = 0, j = 0;    i < 256; i += 2, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 128;  i < 256; i += 2, j++)
                revcon[i] = output[j];

            Keccak_Input64_512(revcon +  0, 64, top0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, top1.S, 255);
            Keccak_Input64_512(revcon +128, 64, top2.S, 255);
            Keccak_Input64_512(revcon +192, 64, top3.S, 255);
            top0.CalcStep(); Keccak_Output_512(output +  0, 64, top0.S);
            top1.CalcStep(); Keccak_Output_512(output + 64, 64, top1.S);
            top2.CalcStep(); Keccak_Output_512(output +128, 64, top2.S);
            top3.CalcStep(); Keccak_Output_512(output +192, 64, top3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            Keccak_Input64_512(revcon +  0, 64, mid0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, mid1.S, 255);
            Keccak_Input64_512(revcon +128, 64, mid2.S, 255);
            Keccak_Input64_512(revcon +192, 64, mid3.S, 255);
            mid0.CalcStep(); Keccak_Output_512(output +  0, 64, mid0.S);
            mid1.CalcStep(); Keccak_Output_512(output + 64, 64, mid1.S);
            mid2.CalcStep(); Keccak_Output_512(output +128, 64, mid2.S);
            mid3.CalcStep(); Keccak_Output_512(output +192, 64, mid3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            Keccak_Input64_512(revcon +  0, 64, bot0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, bot1.S, 255);
            Keccak_Input64_512(revcon +128, 64, bot2.S, 255);
            Keccak_Input64_512(revcon +192, 64, bot3.S, 255);
            bot0.CalcStep(); Keccak_Output_512(output +  0, 64, bot0.S);
            bot1.CalcStep(); Keccak_Output_512(output + 64, 64, bot1.S);
            bot2.CalcStep(); Keccak_Output_512(output +128, 64, bot2.S);
            bot3.CalcStep(); Keccak_Output_512(output +192, 64, bot3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            Keccak_Input64_512(revcon +  0, 64, out0.S, 255);
            Keccak_Input64_512(revcon + 64, 64, out1.S, 255);
            Keccak_Input64_512(revcon +128, 64, out2.S, 255);
            Keccak_Input64_512(revcon +192, 64, out3.S, 255);
            out0.CalcStep();
            out1.CalcStep();
            out2.CalcStep();
            out3.CalcStep();
            Keccak_Output_512(output +  0, 64, out0.S);
            Keccak_Output_512(output + 64, 64, out1.S);
            Keccak_Output_512(output +128, 64, out2.S);
            Keccak_Output_512(output +192, 64, out3.S);

            // Транспонируем вывод
            for (int i = 0, j = 0;   i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 1, j = 64;  i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 2, j = 128; i < 256; i += 4, j++)
                revcon[i] = output[j];
            for (int i = 3, j = 192; i < 256; i += 4, j++)
                revcon[i] = output[j];

            if (!BytesBuilder.UnsecureCompare(cascade.maxDataLen, cascade.maxDataLen, cascade.lastOutput, revcon))
                throw new Exception("CascadeSponge_20230905_BaseTest: results not equals");
        }
        finally
        {
            cascade.Dispose();
        }
    }
}

