#define CAN_CREATEFILE_FOR_CascadeSponge_1t_tests
namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using maincrypto.keccak;
using vinkekfish;

using static CodeGenerated.Cryptoprimes.Threefish_Static_Generated;
using static VinKekFish_Utils.Utils;

using static cryptoprime.KeccakPrime;
using static cryptoprime.BytesBuilderForPointers;

[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 500, singleThread: false)]
public unsafe class CascadeSponge_20230905_BaseTest_noonce : TestTask
{
    public CascadeSponge_20230905_BaseTest_noonce(TestConstructor constructor) :
                                            base(nameof(CascadeSponge_20230905_BaseTest), constructor)
    {
        TaskFunc = Test;
    }

    public void Test()
    {
        // Проверка расчёта параметров каскадной губки
        nint _wide = 0;
        CascadeSponge_1t_20230905.CalcCascadeParameters(192, 404, _tall: out nint _tall, _wide: ref _wide);
        if (_tall != 42 || _wide != 42) throw new Exception($"CascadeSponge_1t_20230905.CalcCascadeParameters(192, 404, _tall: out nint _tall, _wide: out nint _wide): _tall != 42 || _wide != 42. {_tall} {_wide}");
        //Console.WriteLine(_tall);Console.WriteLine(_wide);
        _wide = 0;
        CascadeSponge_1t_20230905.CalcCascadeParameters(11*1024, 404, _tall: out _tall, _wide: ref _wide);
        if (_tall != 176 || _wide != 176) throw new Exception($"CascadeSponge_1t_20230905.CalcCascadeParameters(11*1024, 404, _tall: out nint _tall, _wide: out nint _wide): _tall != 176 || _wide != 176. {_tall} {_wide}");
        // Console.WriteLine(_tall);Console.WriteLine(_wide);


        var cascade = new CascadeSponge_1t_20230905() { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };
        // Console.WriteLine(cascade);
        try
        {
            var dlen = cascade.maxDataLen - 1;
            using var data = Keccak_abstract.allocator.AllocMemory(dlen);

            byte* a = data;
            // Инициализируем массивы данных - имитируем синхропосылку и ключ простыми значениями
            for (int i = 0; i < dlen; i++)
                a[i] = (byte)i;

            // Это тест
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
            if (21 != cascade.Wn || dlen != 21 * 4 - 1 || cascade.countOfThreeFish_RC != 2)
                throw new Exception($"CascadeSponge_20230905_BaseTest: 21 != cascade.Wn || dlen != 83 || cascade.countOfThreeFish != 2. {cascade.Wn} {dlen} {cascade}");

            // wide = 4
            var output = stackalloc byte[64 * 4];
            var revcon = stackalloc byte[64 * 4];
            var buff   = stackalloc byte[64 * 4];
            var Threef = stackalloc byte[256 * 4];    // Это твики и ключи ThreeFish обратной связи. По 256 на каждый ключ (128+8 байтов ключ + 16 байтов твик)

            // Инициализируем ключи и твики обратной связи
            BytesBuilder.ToNull(256 * 4, Threef);

            var TFl = (ulong*)Threef;      // 192/8=24
            TFl[24] = CascadeSponge_1t_20230905.TweakInitIncrement;    // 2743445726634853529
            TFl[25] = 0;
            TFl[26] = TFl[24] ^ TFl[25];
            TFl[32 + 24] = TFl[24] + CascadeSponge_1t_20230905.TweakInitIncrement;  // 2743445726634853529+2743445726634853529=5486891453269707058
            TFl[32 + 25] = 0;
            TFl[32 + 26] = TFl[32 + 24] ^ TFl[32 + 25];
            TFl[64 + 24] = TFl[32 + 24] + CascadeSponge_1t_20230905.TweakInitIncrement; // 5486891453269707058+2743445726634853529=8230337179904560587
            TFl[64 + 25] = 0;
            TFl[64 + 26] = TFl[64 + 24] ^ TFl[64 + 25];
            TFl[96 + 24] = TFl[64 + 24] + CascadeSponge_1t_20230905.TweakInitIncrement; // 8230337179904560587+2743445726634853529=10973782906539414116
            TFl[96 + 25] = 0;
            TFl[96 + 26] = TFl[96 + 24] ^ TFl[96 + 25];

            var key = CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[0] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[1] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[2] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[3] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[4] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[5] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[6] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[7] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[8] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[9] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[10] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[11] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[12] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[13] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[14] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[15] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[16] = Threefish_slowly.C240 ^ TFl[0] ^ TFl[1] ^ TFl[2] ^ TFl[3] ^ TFl[4] ^ TFl[5] ^ TFl[6] ^ TFl[7] ^ TFl[8] ^ TFl[9] ^ TFl[10] ^ TFl[11] ^ TFl[12] ^ TFl[13] ^ TFl[14] ^ TFl[15];

            TFl[32 + 0] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 1] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 2] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 3] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 4] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 5] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 6] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 7] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 8] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 9] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 10] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 11] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 12] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 13] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 14] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 15] = key; key += CascadeSponge_1t_20230905.KeyInitIncrement;
            TFl[32 + 16] = Threefish_slowly.C240 ^ TFl[32 + 0] ^ TFl[32 + 1] ^ TFl[32 + 2] ^ TFl[32 + 3] ^ TFl[32 + 4] ^ TFl[32 + 5] ^ TFl[32 + 6] ^ TFl[32 + 7] ^ TFl[32 + 8] ^ TFl[32 + 9] ^ TFl[32 + 10] ^ TFl[32 + 11] ^ TFl[32 + 12] ^ TFl[32 + 13] ^ TFl[32 + 14] ^ TFl[32 + 15];

            // Заполняем через циклы два оставшихся ключа для заключительного преобразования
            for (int i = 0; i < 16; i++)
            {
                TFl[64 + i] = key;
                key += CascadeSponge_1t_20230905.KeyInitIncrement;
            }
            for (int i = 0; i < 16; i++)
            {
                TFl[96 + i] = key;
                key += CascadeSponge_1t_20230905.KeyInitIncrement;
            }

            TFl[64 + 16] = Threefish_slowly.C240;
            for (int i = 0; i < 16; i++)
                TFl[64 + 16] ^= TFl[64 + i];

            TFl[96 + 16] = Threefish_slowly.C240;
            for (int i = 0; i < 16; i++)
                TFl[96 + 16] ^= TFl[96 + i];

            // Console.WriteLine("test: keys"); Console.WriteLine(ArrayToHex(Threef, cascade.countOfThreeFish*256));

            // Начинаем первый шаг шифрования

            // Вводим данные и делаем шаг (тут сразу двойной шаг). Имитируем, что вводим синхропосылку и ключ
            cascade.Step(data: data, dataLen: dlen, regime: 255);

            BytesBuilder.ToNull(256, revcon);
            // Ввводим 83 байта ввода в верхнюю губку
            for (int w = 0; w < 4; w++)
            {                
                BytesBuilder.CopyTo(21, 256, a + 0,  revcon + 0);
                BytesBuilder.CopyTo(21, 256, a + 21, revcon + 64);
                BytesBuilder.CopyTo(21, 256, a + 42, revcon + 128);
                BytesBuilder.CopyTo(20, 256, a + 63, revcon + 192);

                // Делаем первый шаг: это первая фаза двойного шага многократный ввод данных в губку)
                DoExpandedSmallStep(top0, top1, top2, top3, mid0, mid1, mid2, mid3, bot0, bot1, bot2, bot3, out0, out1, out2, out3, output, revcon, 255);

                // Console.WriteLine("test: before ThreeFish step1a"); Console.WriteLine(ArrayToHex(revcon, cascade.maxDataLen));

                BytesBuilder.CopyTo(256, 256, revcon, output);
                BytesBuilder.CopyTo(256, 256, revcon, buff);
                Threefish1024_step(TFl + 0,  TFl +  0 + 24, (ulong*)output);       // Обратная связь
                Threefish1024_step(TFl + 32, TFl + 32 + 24, (ulong*)(output + 128));
                //            Threefish1024_step(TFl + 64, TFl + 64 + 24, (ulong*) buff);         // Вывод - сейчас вывод не делается, ведь вывод только на последней фазе двойного шага
                //            Threefish1024_step(TFl + 96, TFl + 96 + 24, (ulong*)(buff + 128));

                // Делаем подстановку таблицей подстановок по-умолчанию (для обратной связи)
                SubstituteEmpty(output);

                // Console.WriteLine("test: after ThreeFish step1a without transpose"); Console.WriteLine(ArrayToHex(buff, cascade.ReserveConnectionLen));

                // Транспонируем вывод: по 128-мь байтов блок
                Transpose128_2(output, revcon);     // Обратная связь
                Transpose128_2(buff,   output);     // Выход
            }

            // Console.WriteLine("test:  rc after ThreeFish step1a with transpose"); Console.WriteLine(ArrayToHex(revcon, cascade.ReserveConnectionLen));
            // Console.WriteLine("test: out after ThreeFish step1a with transpose"); Console.WriteLine(ArrayToHex(output, cascade.ReserveConnectionLen));

            // Делаем из первого шага двойной (удваиваем первый шаг)
            DoExpandedSmallStep(top0, top1, top2, top3, mid0, mid1, mid2, mid3, bot0, bot1, bot2, bot3, out0, out1, out2, out3, output, revcon, 255);

            // Console.WriteLine("test: before ThreeFish; step1d"); Console.WriteLine(ArrayToHex(revcon, cascade.ReserveConnectionLen));

            TFl[00 + 24] += CascadeSponge_1t_20230905.CounterIncrement;
            TFl[00 + 25] += 0;
            TFl[00 + 26] = TFl[24] ^ TFl[25];

            TFl[32 + 24] += CascadeSponge_1t_20230905.CounterIncrement;
            TFl[32 + 25] += 0;  // Здесь всё ещё нет переполнения
            TFl[32 + 26] = TFl[32 + 24] ^ TFl[32 + 25];
            /*          Вывод не делался - твики остаются неизменными
                        TFl[64 + 24] += CascadeSponge_1t_20230905.CounterIncrement;
                        TFl[64 + 25] += 0;
                        TFl[64 + 26] = TFl[64 + 24] ^ TFl[64 + 25];

                        TFl[96 + 24] += CascadeSponge_1t_20230905.CounterIncrement; // 10973782906539414116 + 3148241843069173559 = 14122024749608587675
                        TFl[96 + 25] += 0;  // Здесь всё ещё нет переполнения
                        TFl[96 + 26] = TFl[96 + 24] ^ TFl[96 + 25];
            */
            BytesBuilder.CopyTo(256, 256, revcon, output);
            BytesBuilder.CopyTo(256, 256, revcon, buff);
            Threefish1024_step(TFl + 0,  TFl + 0 + 24,  (ulong*)output);       // Обратная связь
            Threefish1024_step(TFl + 32, TFl + 32 + 24, (ulong*)(output + 128));
            Threefish1024_step(TFl + 64, TFl + 64 + 24, (ulong*)buff);         // Вывод
            Threefish1024_step(TFl + 96, TFl + 96 + 24, (ulong*)(buff + 128));

            // Транспонируем вывод: по 128-мь байтов блок
            SubstituteEmpty(output);
            Transpose128_2(output, revcon);
            Transpose128_2(buff, output);

            // Console.WriteLine("test:  rc after ThreeFish step1d +t"); Console.WriteLine(ArrayToHex(revcon, cascade.ReserveConnectionLen));
            // Console.WriteLine("test: out after ThreeFish step1d +t"); Console.WriteLine(ArrayToHex(output, cascade.ReserveConnectionLen));

            if (!BytesBuilder.UnsecureCompare(cascade.maxDataLen, cascade.maxDataLen, cascade.lastOutput, output))
                throw new Exception("CascadeSponge_20230905_BaseTest: results not equals (step 1d)");

            cascade.Step(data: null, dataLen: 0, regime: 34);
            DoExpandedSmallStep(top0, top1, top2, top3, mid0, mid1, mid2, mid3, bot0, bot1, bot2, bot3, out0, out1, out2, out3, output, revcon, 34);

            // Console.WriteLine("test: before ThreeFish; step1d"); Console.WriteLine(ArrayToHex(revcon, cascade.ReserveConnectionLen));

            TFl[00 + 24] += CascadeSponge_1t_20230905.CounterIncrement;
            TFl[00 + 25] += 0;
            TFl[00 + 26] = TFl[24] ^ TFl[25];

            TFl[32 + 24] += CascadeSponge_1t_20230905.CounterIncrement;
            TFl[32 + 25] += 0;  // Здесь всё ещё нет переполнения
            TFl[32 + 26] = TFl[32 + 24] ^ TFl[32 + 25];

            TFl[64 + 24] += CascadeSponge_1t_20230905.CounterIncrement;
            TFl[64 + 25] += 0;
            TFl[64 + 26] = TFl[64 + 24] ^ TFl[64 + 25];

            TFl[96 + 24] += CascadeSponge_1t_20230905.CounterIncrement; // 14122024749608587675 + 3148241843069173559 = 14122024749608587675
            TFl[96 + 25] += 0;  // Здесь всё ещё нет переполнения
            TFl[96 + 26] = TFl[96 + 24] ^ TFl[96 + 25];

            BytesBuilder.CopyTo(256, 256, revcon, output);
            BytesBuilder.CopyTo(256, 256, revcon, buff);
            Threefish1024_step(TFl + 0, TFl + 0 + 24, (ulong*)output);       // Обратная связь
            Threefish1024_step(TFl + 32, TFl + 32 + 24, (ulong*)(output + 128));
            Threefish1024_step(TFl + 64, TFl + 64 + 24, (ulong*)buff);         // Вывод
            Threefish1024_step(TFl + 96, TFl + 96 + 24, (ulong*)(buff + 128));

            // Транспонируем вывод: по 128-мь байтов блок
            /*SubstituteEmpty(output);
            Transpose128_2(output, revcon);*/
            Transpose128_2(buff, output);

            // Console.WriteLine("test:  rc after ThreeFish step1d +t"); Console.WriteLine(ArrayToHex(revcon, cascade.ReserveConnectionLen));
            // Console.WriteLine("test: out after ThreeFish step1d +t"); Console.WriteLine(ArrayToHex(output, cascade.ReserveConnectionLen));

            if (!BytesBuilder.UnsecureCompare(cascade.maxDataLen, cascade.maxDataLen, cascade.lastOutput, output))
                throw new Exception("CascadeSponge_20230905_BaseTest: results not equals (step 2)");
        }
        finally
        {
            cascade.Dispose();
        }
    }

    private static void SubstituteEmpty(byte* revcon)
    {
        // 64 байта на ширину 4 = 256.
        // 256 байтов - это 128-мь значений ushort
        var rv = (ushort*) revcon;
        for (int i = 0; i < 128; i++)
            rv[i] ^= 44381;
    }

    /// <summary>Транспонирует массив output, длиной 256-ть байтов в массив revcon. Строки по 128-мь байтов</summary>
    /// <param name="output">Входной массив</param>
    /// <param name="revcon">Выходной массив (транспонированный output)</param>
    private static void Transpose128_2(byte* output, byte* revcon)
    {
        for (int i = 0, j = 0;   i < 256; i += 2, j++)
            revcon[i] = output[j];
        for (int i = 1, j = 128; i < 256; i += 2, j++)
            revcon[i] = output[j];
    }

    private static void DoExpandedSmallStep(Keccak_20200918 top0, Keccak_20200918 top1, Keccak_20200918 top2, Keccak_20200918 top3, Keccak_20200918 mid0, Keccak_20200918 mid1, Keccak_20200918 mid2, Keccak_20200918 mid3, Keccak_20200918 bot0, Keccak_20200918 bot1, Keccak_20200918 bot2, Keccak_20200918 bot3, Keccak_20200918 out0, Keccak_20200918 out1, Keccak_20200918 out2, Keccak_20200918 out3, byte* output, byte* revcon, byte regime)
    {
        Keccak_Input64_512(revcon +   0, 64, top0.S, regime);
        Keccak_Input64_512(revcon +  64, 64, top1.S, regime);
        Keccak_Input64_512(revcon + 128, 64, top2.S, regime);
        Keccak_Input64_512(revcon + 192, 64, top3.S, regime);
        top0.CalcStep();
        top1.CalcStep();
        top2.CalcStep();
        top3.CalcStep();
        Keccak_Output_512(output +   0, 64, top0.S);
        Keccak_Output_512(output +  64, 64, top1.S);
        Keccak_Output_512(output + 128, 64, top2.S);
        Keccak_Output_512(output + 192, 64, top3.S);

        // Транспонируем вывод
        for (int i = 0, j = 0; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 1, j = 64; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 2, j = 128; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 3, j = 192; i < 256; i += 4, j++)
            revcon[i] = output[j];

        Keccak_Input64_512(revcon +   0, 64, mid0.S, regime);
        Keccak_Input64_512(revcon +  64, 64, mid1.S, regime);
        Keccak_Input64_512(revcon + 128, 64, mid2.S, regime);
        Keccak_Input64_512(revcon + 192, 64, mid3.S, regime);
        mid0.CalcStep();
        mid1.CalcStep();
        mid2.CalcStep();
        mid3.CalcStep();
        Keccak_Output_512(output +   0, 64, mid0.S);
        Keccak_Output_512(output +  64, 64, mid1.S);
        Keccak_Output_512(output + 128, 64, mid2.S);
        Keccak_Output_512(output + 192, 64, mid3.S);

        // Транспонируем вывод
        for (int i = 0, j = 0; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 1, j = 64; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 2, j = 128; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 3, j = 192; i < 256; i += 4, j++)
            revcon[i] = output[j];

        Keccak_Input64_512(revcon +   0, 64, bot0.S, regime);
        Keccak_Input64_512(revcon +  64, 64, bot1.S, regime);
        Keccak_Input64_512(revcon + 128, 64, bot2.S, regime);
        Keccak_Input64_512(revcon + 192, 64, bot3.S, regime);
        bot0.CalcStep();
        bot1.CalcStep();
        bot2.CalcStep();
        bot3.CalcStep();
        Keccak_Output_512(output +   0, 64, bot0.S);
        Keccak_Output_512(output +  64, 64, bot1.S);
        Keccak_Output_512(output + 128, 64, bot2.S);
        Keccak_Output_512(output + 192, 64, bot3.S);

        // Транспонируем вывод
        for (int i = 0, j = 0; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 1, j = 64; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 2, j = 128; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 3, j = 192; i < 256; i += 4, j++)
            revcon[i] = output[j];

        Keccak_Input64_512(revcon +   0, 64, out0.S, regime);
        Keccak_Input64_512(revcon +  64, 64, out1.S, regime);
        Keccak_Input64_512(revcon + 128, 64, out2.S, regime);
        Keccak_Input64_512(revcon + 192, 64, out3.S, regime);
        out0.CalcStep();
        out1.CalcStep();
        out2.CalcStep();
        out3.CalcStep();
        Keccak_Output_512(output +   0, 64, out0.S);
        Keccak_Output_512(output +  64, 64, out1.S);
        Keccak_Output_512(output + 128, 64, out2.S);
        Keccak_Output_512(output + 192, 64, out3.S);

        // Транспонируем вывод
        for (int i = 0, j = 0;   i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 1, j = 64;  i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 2, j = 128; i < 256; i += 4, j++)
            revcon[i] = output[j];
        for (int i = 3, j = 192; i < 256; i += 4, j++)
            revcon[i] = output[j];
    }
}

