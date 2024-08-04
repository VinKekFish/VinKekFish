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

/// <summary>
/// Класс реализует режим шифроывания "main.1.pwd.2024.1" (VinKekFish/Docs/Dev/Crypto/VinKekFish/Description/Regimes/main.1.pwd.2024.1)
/// </summary>
public unsafe partial class Main_1_PWD_2024_1
{
    public partial class EncryptDataClass: IDisposable
    {                                                                       /// <summary>Поток данных, выравненный по длине. Автоматически создаётся и удаляется.</summary>
        public Record       AlignedStream;                                  /// <summary>Исходный поток данных, переданный в конструктор. Автоматически удаляется.</summary>
        public Record       PrimaryStream;
                                                        /// <summary>Ключ для первого шифрования и хеширования каскадной губкой.</summary>
        protected Record   Key0Csc;                     /// <summary>Ключ для VinKekFish для гаммирования суммой губок.</summary>
        protected Record   Key1Vkf;                     /// <summary>Ключ для каскадной губки для гаммирования суммой губок.</summary>
        protected Record   Key1Csc;                     /// <summary>Ключ для VinKekFish с обратной связью и хешированием.</summary>
        protected Record   Key2Vkf;                     /// <summary>Ключ для перемешивания и инициализации таблиц перестановок VinKekFish.</summary>
        protected Record   Key3PCsc;                    /// <summary>Ключ для гаммирования с обратной связью без хеша.</summary>
        protected Record   Key4Csc;                     /// <summary>Ключ для гаммирования с обратной связью без хеша.</summary>
        protected Record   Key5Vkf;

        public readonly VinKekFishOptions vkfOpt;
        public readonly CascadeOptions    cscOpt;
        public readonly nint              tall, wide;

        public static nint Align(nint size)
        {
            var a = AlignUtils.Align(size, 65536, 65536);
            return a;
        }

        /// <summary>Создаёт логический поток шифрования. Выравнивает его, дополняет шумами, генерирует ключи шифрования. Вызывать в using.</summary>
        /// <param name="dataForEncrypt">Исходные данные для шифрования. Будут автоматически очищены при очистке этого объекта.</param>
        /// <param name="file">Описатель файла, который содержит секцию "Encrypted", в которую будет вставлены зашифрованные данные из dataForEncrypt.</param>
        /// <param name="getDataByAdd">Генератор ключей, который уже должен быть проинициализирован заранее. Используется только в конструкторе, далее может быть использован в других потоках и должен быть удалён вызывающим методом. Первый режим работы: 255.</param>
        /// <param name="vkfOpt">Опции создания губки VinKekFish.</param>
        /// <param name="cscOpt">Опции создания каскадной губки.</param>
        public EncryptDataClass(Record dataForEncrypt, FileParts file, GetDataByAdd getDataByAdd, VinKekFishOptions vkfOpt, CascadeOptions cscOpt)
        {
            GC.ReRegisterForFinalize(this);
            this.vkfOpt = vkfOpt;
            this.cscOpt = cscOpt;
            CascadeSponge_1t_20230905.CalcCascadeParameters(cscOpt.StrengthInBytes, 0, out tall, ref wide);

            byte[]? length_array = null;
            BytesBuilder.VariableULongToBytes((ulong) dataForEncrypt.len, ref length_array);
            var fLen = dataForEncrypt.len + length_array!.Length;
            var aLen = Align(fLen + 16); // Как минимум 16 байтов на шумы - они всегда должны быть
// aLen рассчитан НЕВЕРНО! Нужно добавить туда минимальные длины хешей и т.п.
            this.PrimaryStream = dataForEncrypt;
            this.AlignedStream = Keccak_abstract.allocator.AllocMemory(aLen, "Main_PWD_2024_1.EncryptDataClass");

            // Копируем в выравненный поток длину открытого текста и сам открытый текст
            fixed (byte * s = length_array)
            BytesBuilder.CopyTo(length_array.Length, AlignedStream.len, s, AlignedStream);

            BytesBuilder.CopyTo(PrimaryStream.len, AlignedStream.len, PrimaryStream, AlignedStream, targetIndex: length_array.Length);

            var cur  = fLen;
            var nLen = aLen - fLen;

            Key0Csc  = getDataByAdd.GetBytes(tall*wide*KeccakPrime.BlockLen*2+16, 251);
            Key1Vkf  = getDataByAdd.GetBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*4, 254);
            Key1Csc  = getDataByAdd.GetBytes(tall*KeccakPrime.BlockLen*2+16, 252);
            Key2Vkf  = getDataByAdd.GetBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*4, 251);
            Key3PCsc = getDataByAdd.GetBytes(tall*wide*KeccakPrime.BlockLen*2+16, 252);
            Key4Csc  = getDataByAdd.GetBytes(tall*KeccakPrime.BlockLen*2+16, 255);
            Key5Vkf  = getDataByAdd.GetBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*4, 254);

            var enPart = file.FindFirstPart("Encrypted");
        }

        ~EncryptDataClass()        => Dispose(true);
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
            TryToDispose(AlignedStream);

            TryToDispose(Key0Csc);
            TryToDispose(Key1Vkf);
            TryToDispose(Key1Csc);
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
