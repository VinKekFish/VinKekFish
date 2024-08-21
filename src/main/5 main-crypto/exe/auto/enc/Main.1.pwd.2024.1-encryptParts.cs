// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using static AutoCrypt;
using static cryptoprime.BytesBuilderForPointers;
using maincrypto.keccak;
using cryptoprime;
using System.Reflection.Metadata.Ecma335;
using cryptoprime.VinKekFish;
using vinkekfish;
using VinKekFish_Utils;
using System.Drawing;

public unsafe partial class Main_1_PWD_2024_1
{
    public partial class CryptDataClass: IDisposable
    {
        /// <summary>Первый проход шифрования: гаммирование с обратной связью каскадной губкой с ключами Key0Csc и Key0Csc_init.</summary>
        protected void EncryptStage1()
        {
            // Инициализация первой губки для гаммирования с обратной связью.
            using var sponge = new CascadeSponge_mt_20230930(_wide: wide, _tall: tall) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };
            sponge.InitThreeFishByKey(Key1Csc_init);
            sponge.InitKeyAndOIV(Key1Csc, InitThreeFishByCascade_stepToKeyConst: 0);        // Не делаем встроенной инициализации ThreeFish, чтобы сделать её затем с другими параметрами
            sponge.InitThreeFishByCascade(stepToKeyConst: cscOpt.InitSteps, countOfSteps: cscOpt.ArmoringSteps, countOfStepsForSubstitutionTable: cscOpt.StepsForTable);

            // Выделяем память на шифротекст
            SecondaryStream = Keccak_abstract.allocator.AllocMemory(ResultFileLen - file!.FullLen.max - VkfHashLen);
            SecondaryStream.Clear();
            BytesBuilder.CopyTo(PrimaryStream!.len, SecondaryStream.len, PrimaryStream, SecondaryStream);

            var MaxBlockLen = sponge.maxDataLen >> 1;

            // Начинаем шифровать
            nint cur = 0, curPrimary = 0;
            sponge.Step(0, cscOpt.ArmoringSteps, regime: 1);
            var curLen = PrimaryStream.len;
            if (curLen > MaxBlockLen)
                curLen = MaxBlockLen;

            BytesBuilder.Xor(curLen, SecondaryStream.array + cur, sponge.lastOutput);
            cur += curLen;

            while (cur < PrimaryStream!.len)
            {
                curLen = PrimaryStream.len - cur;
                if (curLen > MaxBlockLen)
                    curLen = MaxBlockLen;

                curPrimary += sponge.Step(1, cscOpt.ArmoringSteps, data: PrimaryStream.array + curPrimary, dataLen: curLen, regime: 0);
                BytesBuilder.Xor(curLen, SecondaryStream.array + cur, sponge.lastOutput);
                cur += curLen;
            }

            // Делаем сброс инициализации перед вычислением хеша.
            sponge.InitThreeFishByCascade(1, false);

            // Вычисляем хеш
            while (cur < SecondaryStream.len)
            {
                sponge.Step(1, cscOpt.ArmoringSteps, regime: 3);
                cur += BytesBuilder.CopyTo(sponge.lastOutput.len, SecondaryStream.len, sponge.lastOutput, SecondaryStream, cur);
            }

            TryToDispose(PrimaryStream);

            this.PrimaryStream   = SecondaryStream;
            this.SecondaryStream = null;
        }

        /// <summary>Второй проход шифрования: простое гаммирование.</summary>
        protected void EncryptStage2()
        {
            // При освобождении gen автоматически освободятся и губки, входящие в него
            using var gen    = new GetDataByAdd();
                  var sponge = new CascadeSponge_mt_20230930(_wide: wide, _tall: tall) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };
                  var vkf    = new VinKekFishBase_KN_20210525(vkfOpt.Rounds, vkfOpt.K);

            sponge.InitThreeFishByKey(Key2Csc_init);
            sponge.InitKeyAndOIV(Key2Csc, InitThreeFishByCascade_stepToKeyConst: 0);        // Не делаем встроенной инициализации ThreeFish, чтобы сделать её затем с другими параметрами
            sponge.InitThreeFishByCascade(stepToKeyConst: cscOpt.InitSteps, countOfSteps: cscOpt.ArmoringSteps, countOfStepsForSubstitutionTable: cscOpt.StepsForTable);

            vkf.Init1(vkfOpt.PreRounds, prngToInit: sponge);
            vkf.Init2(Key2Vkf, RoundsForFinal: vkfOpt.Rounds, RoundsForFirstKeyBlock: vkfOpt.Rounds, RoundsForTailsBlock: vkfOpt.Rounds);

            gen.AddSponge(new GetDataFromCascadeSponge(sponge, setBlockLen: sponge.lastOutput.len, setArmoringSteps: cscOpt.ArmoringSteps));
            gen.AddSponge(new GetDataFromVinKekFishSponge(vkf, setBlockLen: vkf.BLOCK_SIZE_K, setArmoringSteps: vkfOpt.Rounds));

            this.SecondaryStream = gen.GetBytes(this.PrimaryStream!.len, 11);
            try
            {
                BytesBuilder.Xor(PrimaryStream.len, PrimaryStream, SecondaryStream);
            }
            finally
            {
                TryToDispose(SecondaryStream);
                this.SecondaryStream = null;
            }
        }

        protected void EncryptStage4()
        {
            // Инициализация второй губки для гаммирования с обратной связью.
            using var sponge = new CascadeSponge_mt_20230930(_wide: wide, _tall: tall) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };
            sponge.InitThreeFishByKey(Key4Csc_init);
            sponge.InitKeyAndOIV(Key4Csc, InitThreeFishByCascade_stepToKeyConst: 0);        // Не делаем встроенной инициализации ThreeFish, чтобы сделать её затем с другими параметрами
            sponge.InitThreeFishByCascade(stepToKeyConst: cscOpt.InitSteps, countOfSteps: cscOpt.ArmoringSteps, countOfStepsForSubstitutionTable: cscOpt.StepsForTable);

            // Выделяем память на шифротекст
            SecondaryStream = Keccak_abstract.allocator.AllocMemory(PrimaryStream!.len);
            SecondaryStream.Clear();
            BytesBuilder.CopyTo(PrimaryStream!.len, SecondaryStream.len, PrimaryStream, SecondaryStream);

            var MaxBlockLen = sponge.maxDataLen >> 1;

            // Начинаем шифровать
            nint cur = 0, curPrimary = 0;
            sponge.Step(0, cscOpt.ArmoringSteps, regime: 1);
            var curLen = PrimaryStream.len;
            if (curLen > MaxBlockLen)
                curLen = MaxBlockLen;

            BytesBuilder.Xor(curLen, SecondaryStream.array + cur, sponge.lastOutput);
            cur += curLen;

            while (cur < PrimaryStream!.len)
            {
                curLen = PrimaryStream.len - cur;
                if (curLen > MaxBlockLen)
                    curLen = MaxBlockLen;

                curPrimary += sponge.Step(1, cscOpt.ArmoringSteps, data: PrimaryStream.array + curPrimary, dataLen: curLen, regime: 0);
                BytesBuilder.Xor(curLen, SecondaryStream.array + cur, sponge.lastOutput);
                cur += curLen;
            }

            TryToDispose(PrimaryStream);

            this.PrimaryStream   = SecondaryStream;
            this.SecondaryStream = null;
        }

        protected void EncryptStagePermutation(Record key, Record key_init, byte regime, out CascadeSponge_mt_20230930 sponge)
        {
            // Инициализация губки для вычисления перестановок.
            sponge = new CascadeSponge_mt_20230930(_wide: wide, _tall: tall) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.effective };
            sponge.InitThreeFishByKey(key_init);
            sponge.InitKeyAndOIV(key, InitThreeFishByCascade_stepToKeyConst: 0);        // Не делаем встроенной инициализации ThreeFish, чтобы сделать её затем с другими параметрами
            sponge.InitThreeFishByCascade(stepToKeyConst: cscOpt.InitSteps, countOfSteps: cscOpt.ArmoringSteps, countOfStepsForSubstitutionTable: cscOpt.StepsForTable);

            sponge.DoRandomPermutationForBytes(PrimaryStream!.len, PrimaryStream, cscOpt.StepsForTable, regime: regime);
        }

        protected void EncryptStage6()
        {
            var vkf = new VinKekFishBase_KN_20210525(vkfOpt.Rounds, vkfOpt.K);

            var MaxBlockLen = vkf.BLOCK_SIZE_KEY_K;

            vkf.input  = new(MaxBlockLen);
            vkf.output = new(MaxBlockLen);
            vkf.Init1(vkfOpt.PreRounds, prngToInit: sponge5);
            vkf.Init2(Key6Vkf, RoundsForFinal: vkfOpt.Rounds, RoundsForFirstKeyBlock: vkfOpt.Rounds, RoundsForTailsBlock: vkfOpt.Rounds);

            using var getData = new GetDataFromVinKekFishSponge(vkf, setBlockLen: MaxBlockLen, setArmoringSteps: vkfOpt.Rounds); getData.NameForRecord = "EncryptStage6.GetDataFromVinKekFishSponge.getData";

            // Выделяем память на шифротекст
            SecondaryStream = Keccak_abstract.allocator.AllocMemory(PrimaryStream!.len + VkfHashLen*vkfOpt.K);
            SecondaryStream.Clear();
            BytesBuilder.CopyTo(PrimaryStream!.len, SecondaryStream.len, PrimaryStream, SecondaryStream);

            using var buffer = Keccak_abstract.allocator.AllocMemory(MaxBlockLen, nameof(EncryptStage6));

            // Начинаем шифровать
            nint cur = 0, curPrimary = 0;
            getData.GetBytes(buffer, MaxBlockLen, regime: 47);
            var curLen = PrimaryStream.len;
            if (curLen > MaxBlockLen)
                curLen = MaxBlockLen;

            BytesBuilder.Xor(curLen, SecondaryStream.array + cur, buffer);
            cur += curLen;

            while (cur < PrimaryStream!.len)
            {
                curLen = PrimaryStream.len - cur;
                if (curLen > MaxBlockLen)
                    curLen = MaxBlockLen;

                vkf.input.Add(PrimaryStream.array + curPrimary, curLen);
                curPrimary += curLen;

                getData.GetBytes(buffer, MaxBlockLen, regime: 74, false);
                BytesBuilder.Xor(curLen, SecondaryStream.array + cur, buffer);
                cur += curLen;
            }

            // Вычисляем хеш
            while (cur < SecondaryStream.len)
            {
                getData.GetBytes(buffer, MaxBlockLen, regime: 38, false);
                cur += BytesBuilder.CopyTo(buffer.len, SecondaryStream.len, buffer, SecondaryStream, cur);
            }

            TryToDispose(PrimaryStream);

            this.PrimaryStream   = SecondaryStream;
            this.SecondaryStream = null;
        }

        protected void EncryptStage8()
        {
            var vkf = new VinKekFishBase_KN_20210525(vkfOpt.Rounds, vkfOpt.K);

            var MaxBlockLen = vkf.BLOCK_SIZE_KEY_K;

            vkf.input  = new(MaxBlockLen);
            vkf.output = new(MaxBlockLen);
            vkf.Init1(vkfOpt.PreRounds, prngToInit: sponge5);
            vkf.Init2(Key8Vkf, RoundsForFinal: vkfOpt.Rounds, RoundsForFirstKeyBlock: vkfOpt.Rounds, RoundsForTailsBlock: vkfOpt.Rounds);

            using var getData = new GetDataFromVinKekFishSponge(vkf, setBlockLen: MaxBlockLen, setArmoringSteps: vkfOpt.Rounds); getData.NameForRecord = "EncryptStage8.GetDataFromVinKekFishSponge.getData";

            // Выделяем память на шифротекст
            SecondaryStream = Keccak_abstract.allocator.AllocMemory(PrimaryStream!.len);
            SecondaryStream.Clear();
            BytesBuilder.CopyTo(PrimaryStream!.len, SecondaryStream.len, PrimaryStream, SecondaryStream);

            using var buffer = Keccak_abstract.allocator.AllocMemory(MaxBlockLen, nameof(EncryptStage8));

            // Начинаем шифровать
            nint cur = 0, curPrimary = 0;
            getData.GetBytes(buffer, MaxBlockLen, regime: 47);
            var curLen = PrimaryStream.len;
            if (curLen > MaxBlockLen)
                curLen = MaxBlockLen;

            BytesBuilder.Xor(curLen, SecondaryStream.array + cur, buffer);
            cur += curLen;

            while (cur < PrimaryStream!.len)
            {
                curLen = PrimaryStream.len - cur;
                if (curLen > MaxBlockLen)
                    curLen = MaxBlockLen;

                vkf.input.Add(PrimaryStream.array + curPrimary, curLen);
                curPrimary += curLen;

                getData.GetBytes(buffer, MaxBlockLen, regime: 74, false);
                BytesBuilder.Xor(curLen, SecondaryStream.array + cur, buffer);
                cur += curLen;
            }

            TryToDispose(PrimaryStream);

            this.PrimaryStream   = SecondaryStream;
            this.SecondaryStream = null;
        }
    }
}
