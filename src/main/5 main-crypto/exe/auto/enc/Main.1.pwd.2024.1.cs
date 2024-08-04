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

/// <summary>
/// Класс реализует режим шифроывания "main.1.pwd.2024.1" (VinKekFish/Docs/Dev/Crypto/VinKekFish/Description/Regimes/main.1.pwd.2024.1)
/// </summary>
public unsafe partial class Main_1_PWD_2024_1
{
    public partial class CryptDataClass: IDisposable
    {                                                                       /// <summary>Вторичный поток данных, используемый как вспомогательный буфер. Автоматически создаётся и удаляется.</summary>
        public Record?      SecondaryStream;                                /// <summary>Исходный поток данных для нового шага шифрования. Автоматически удаляется.</summary>
        public Record?      PrimaryStream;
                                                        /// <summary>Ключ для первого шифрования и хеширования каскадной губкой. tall*wide*KeccakPrime.BlockLen*2</summary>
        protected Record   Key0Csc;                     /// <summary>Ключ для инициализации ThreeFish в первом шифровании. Этот ключ логически является составной часть первого ключа. wide*Threefish_slowly.keyLen</summary>
        protected Record   Key0Csc_init;                /// <summary>Ключ для VinKekFish для гаммирования суммой губок.</summary>
        protected Record   Key1Vkf;                     /// <summary>Ключ для каскадной губки для гаммирования суммой губок.</summary>
        protected Record   Key1Csc;                     /// <summary>Ключ для инициализации ThreeFish во втором шифровании (гаммировании). Этот ключ логически является составной частью второго ключа. wide*Threefish_slowly.keyLen</summary>
        protected Record   Key1Csc_init;                /// <summary>Ключ для VinKekFish с обратной связью и хешированием.</summary>
        protected Record   Key2Vkf;                     /// <summary>Ключ для перемешивания и инициализации таблиц перестановок VinKekFish.</summary>
        protected Record   Key3PCsc;                    /// <summary>Ключ для гаммирования с обратной связью без хеша.</summary>
        protected Record   Key4Csc;                     /// <summary>Ключ для гаммирования с обратной связью без хеша.</summary>
        protected Record   Key5Vkf;

        protected FileParts? file;

        public readonly VinKekFishOptions vkfOpt;
        public readonly CascadeOptions    cscOpt;
        public readonly nint              tall, wide;
                                                                        /// <summary>Длина хеша vkf.</summary>
        public readonly nint VkfHashLen = -1;                           /// <summary>Минмальная длина хеша csc.</summary>
        public readonly nint CscHashLen = -1;                           /// <summary>Результирующая длина файла. (Она рассчитывается заранее так, что полная длина зашифрованного файла кратна 2^16). См. функцию Align.</summary>
        public          nint ResultFileLen  = -1;

        public static nint Align(nint size, nint mod = 65536, nint minSize = 65536)
        {
            var a = AlignUtils.Align(size, mod, minSize);
            return a;
        }

        /// <summary>Создаёт логический поток шифрования. Выравнивает его, дополняет шумами, генерирует ключи шифрования. Вызывать в using.</summary>
        /// <param name="file">Описатель файла, который содержит секцию "Encrypted", в которую будет вставлены зашифрованные данные из dataForEncrypt.</param>
        /// <param name="getDataByAdd">Генератор ключей, который уже должен быть проинициализирован заранее. Используется только в конструкторе, далее может быть использован в других потоках и должен быть удалён вызывающим методом. Первый режим работы: 255.</param>
        /// <param name="vkfOpt">Опции создания губки VinKekFish.</param>
        /// <param name="cscOpt">Опции создания каскадной губки.</param>
        public CryptDataClass(GetDataByAdd getDataByAdd, VinKekFishOptions vkfOpt, CascadeOptions cscOpt)
        {
            GC.ReRegisterForFinalize(this);
            this.vkfOpt = vkfOpt;
            this.cscOpt = cscOpt;
            CascadeSponge_1t_20230905.CalcCascadeParameters(cscOpt.StrengthInBytes, 0, out tall, ref wide);

            this.VkfHashLen = vkfOpt.K * 4 * 512;   // 4-х кратная стойкость хеша по сравнению с номиналом.
            this.CscHashLen = tall*wide*KeccakPrime.BlockLen*2;       // Двойной вывод всех губок

            this.PrimaryStream   = null;
            this.SecondaryStream = null; // Keccak_abstract.allocator.AllocMemory(aLen, "Main_PWD_2024_1.EncryptDataClass");

            Key0Csc      = getDataByAdd.GetBytes(tall*wide*KeccakPrime.BlockLen*2,                251);
            Key0Csc_init = getDataByAdd.GetBytes(wide*Threefish_slowly.keyLen,                      8);
            Key1Vkf      = getDataByAdd.GetBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*4, 254);
            Key1Csc      = getDataByAdd.GetBytes(tall*KeccakPrime.BlockLen*2,                     252);
            Key1Csc_init = getDataByAdd.GetBytes(wide*Threefish_slowly.keyLen,                      8);
            Key2Vkf      = getDataByAdd.GetBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*4, 251);
            Key3PCsc     = getDataByAdd.GetBytes(tall*wide*KeccakPrime.BlockLen*2+16,             252);
            Key4Csc      = getDataByAdd.GetBytes(tall*KeccakPrime.BlockLen*2+16,                  255);
            Key5Vkf      = getDataByAdd.GetBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*4, 254);
        }

        /// <summary>Зашифровать данные. Вызывается только один раз за всё время жизни объекта. new CryptDataClass + DoEncrypt + Dispose.</summary>
        /// <param name="dataForEncrypt">Исходные данные для шифрования. Будут автоматически очищены при очистке этого объекта.</param>
        public void DoEncrypt(Record dataForEncrypt, FileParts file)
        {
            try
            {
                this.file  = file;

                byte[]? dataForEncryptLen = null;
                BytesBuilder.ULongToBytes((ulong) dataForEncrypt.len, ref dataForEncryptLen);

                using var bbp = new BytesBuilderForPointers();
                bbp.Add(Record.GetRecordFromBytesArray(dataForEncryptLen!));
                bbp.Add(dataForEncrypt);

                this.PrimaryStream = bbp.GetBytes();
                this.ResultFileLen = Align(file.FullLen.max + dataForEncrypt.len + VkfHashLen + CscHashLen);

                EncryptStage1();
                EncryptStage2();

                file.AddFilePart("Encrypted", PrimaryStream);
                this.PrimaryStream = null;
            }
            finally
            {
                // dataForEncrypt уже был обнулён при уничтожении bbp, если только не произошло исключение
                if (!dataForEncrypt.isDisposed)
                    dataForEncrypt.Dispose();
            }
        }

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

            // Начинаем шифровать
            nint cur = 0, curPrimary = 0;
            sponge.Step(0, cscOpt.ArmoringSteps, regime: 1);
            var curLen = PrimaryStream.len;
            if (curLen > sponge.maxDataLen)
                curLen = sponge.maxDataLen;

            BytesBuilder.Xor(curLen, SecondaryStream.array + cur, sponge.lastOutput);
            cur += curLen;

            while (cur < PrimaryStream!.len)
            {
                curLen = PrimaryStream.len - cur;
                if (curLen > sponge.maxDataLen)
                    curLen = sponge.maxDataLen;

                curPrimary += sponge.Step(1, cscOpt.ArmoringSteps, data: PrimaryStream.array + curPrimary, dataLen: curLen, regime: 0);
                BytesBuilder.Xor(curLen, SecondaryStream.array + cur, sponge.lastOutput);
                cur += curLen;
            }

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

            gen.AddSponge(new GetDataFromCascadeSponge(sponge));
            gen.AddSponge(new GetDataFromVinKekFishSponge(vkf));

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

        ~CryptDataClass() => Dispose(true);
        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public bool isDisposed = false;
        public virtual void Dispose(bool fromDestructor = false)
        {
            string? emsg = null;
            if (fromDestructor)
            {
                if (!isDisposed)
                    emsg = L("DoCryptDataStream.Dispose: ~DoCryptDataStream not disposed");
                else
                    return;
            }

            if (isDisposed)
            {
                Record.ErrorsInDispose = true;
                emsg = L("DoCryptDataStream.Dispose: Dispose twiced");
                Console.Error.WriteLine(emsg);
                return;
            }

            TryToDispose(PrimaryStream);
            TryToDispose(SecondaryStream);

            TryToDispose(Key0Csc);
            TryToDispose(Key0Csc_init);
            TryToDispose(Key1Vkf);
            TryToDispose(Key1Csc);
            TryToDispose(Key1Csc_init);
            TryToDispose(Key2Vkf);
            TryToDispose(Key3PCsc);
            TryToDispose(Key4Csc);
            TryToDispose(Key5Vkf);

            isDisposed = true;

            if (emsg is not null)
            {
                Record.ErrorsInDispose = true;
                Console.Error.WriteLine(emsg);
                return;
            }
        }
    }
}
