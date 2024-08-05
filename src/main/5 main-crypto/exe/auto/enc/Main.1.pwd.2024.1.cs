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
        protected Record   Key1Csc_init;
        protected Record   Key2Csc;
        protected Record   Key2Csc_init;


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
            Key2Csc      = getDataByAdd.GetBytes(tall*wide*KeccakPrime.BlockLen*2,                251);
            Key2Csc_init = getDataByAdd.GetBytes(wide*Threefish_slowly.keyLen,                      8);
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
                BytesBuilder.ReverseBytes(PrimaryStream.len, PrimaryStream);
                EncryptStage3();

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
            TryToDispose(Key2Csc);
            TryToDispose(Key2Csc_init);

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
