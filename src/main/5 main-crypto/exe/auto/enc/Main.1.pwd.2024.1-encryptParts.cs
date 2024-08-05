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
            using var sponge = new CascadeSponge_mt_20230930(_wide: wide, _tall: tall);
            sponge.InitThreeFishByKey(Key0Csc_init);
            sponge.InitKeyAndOIV(Key0Csc, InitThreeFishByCascade_stepToKeyConst: 0);        // Не делаем встроенной инициализации ThreeFish, чтобы сделать её затем с другими параметрами
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
                  var sponge = new CascadeSponge_mt_20230930(_wide: wide, _tall: tall);
                  var vkf    = new VinKekFishBase_KN_20210525(vkfOpt.Rounds, vkfOpt.K);

            sponge.InitThreeFishByKey(Key1Csc_init);
            sponge.InitKeyAndOIV(Key1Csc, InitThreeFishByCascade_stepToKeyConst: 0);        // Не делаем встроенной инициализации ThreeFish, чтобы сделать её затем с другими параметрами
            sponge.InitThreeFishByCascade(stepToKeyConst: cscOpt.InitSteps, countOfSteps: cscOpt.ArmoringSteps, countOfStepsForSubstitutionTable: cscOpt.StepsForTable);

            vkf.Init1(vkfOpt.PreRounds, prngToInit: sponge);
            vkf.Init2(Key1Vkf, RoundsForFinal: vkfOpt.Rounds, RoundsForFirstKeyBlock: vkfOpt.Rounds, RoundsForTailsBlock: vkfOpt.Rounds);

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

        protected void EncryptStage3()
        {
            // Инициализация первой губки для гаммирования с обратной связью.
            using var sponge = new CascadeSponge_mt_20230930(_wide: wide, _tall: tall);
            sponge.InitThreeFishByKey(Key2Csc_init);
            sponge.InitKeyAndOIV(Key2Csc, InitThreeFishByCascade_stepToKeyConst: 0);        // Не делаем встроенной инициализации ThreeFish, чтобы сделать её затем с другими параметрами
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
    }
}
