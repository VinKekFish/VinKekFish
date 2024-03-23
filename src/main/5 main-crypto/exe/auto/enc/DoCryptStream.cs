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

public unsafe partial class Main_PWD_2024_1
{
    public partial class DoCryptDataStream: IDisposable
    {                                                                       /// <summary>Поток данных, выравненный по длине. Автоматически создаётся и удаляется.</summary>
        public Record       AlignedStream;                                  /// <summary>Исходный поток данных, переданный в конструктор. Автоматически удаляется.</summary>
        public Record       PrimaryStream;

        protected Record   Key0NoiseVkf;
        protected Record   Key0NoiseCsc;
        protected Record   Key1Vkf;
        protected Record   Key2PCsc;
        protected Record   Key3Csc;
        protected Record   Key4Csc;
        protected Record   Key4Vkf;
        protected Record   Key5Vkf;

        /// <summary>Создаёт логический поток шифрования. Выравнивает его, дополняет шумами, генерирует ключи шифрования.</summary>
        /// <param name="dataStream">Исходные данные для шифрования. Будут автоматически очищены при очистке этого объекта.</param>
        /// <param name="getDataByAdd">Генератор ключей, который уже должен быть проинициализирован заранее. Используется только в конструкторе, далее может быть использован в других потоках и должен быть удалён вызывающим методом. Первый режим работы: 255.</param>
        /// <param name="vkfOpt">Опции создания губки VinKekFish.</param>
        /// <param name="cscOpt">Опции создания каскадной губки.</param>
        public DoCryptDataStream(Record dataStream, GetDataByAdd getDataByAdd, VinKekFishOptions vkfOpt, CascadeOptions cscOpt)
        {
            GC.ReRegisterForFinalize(this);

            byte[]? length_array = null;
            BytesBuilder.VariableULongToBytes((ulong) dataStream.len, ref length_array);
            var fLen = dataStream.len + length_array!.Length;
            var aLen = Align(fLen + 16); // Как минимум 16 байтов на шумы - они всегда должны быть

            PrimaryStream = dataStream;
            AlignedStream = Keccak_abstract.allocator.AllocMemory(aLen, "Main_PWD_2024_1.DoCryptDataStream");

            // Копируем в выравненный поток длину открытого текста и сам открытый текст
            fixed (byte * s = length_array)
            BytesBuilder.CopyTo(length_array.Length, AlignedStream.len, s, AlignedStream);

            BytesBuilder.CopyTo(PrimaryStream.len, AlignedStream.len, PrimaryStream, AlignedStream, targetIndex: length_array.Length);

            var cur  = fLen;
            var nLen = aLen - fLen;

            Key0NoiseVkf = getDataByAdd.getBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*1, 255);
            Key0NoiseCsc = getDataByAdd.getBytes(cscOpt.StrengthInBytes, 254);
            Key1Vkf      = getDataByAdd.getBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*2, 255);
            Key2PCsc     = getDataByAdd.getBytes(cscOpt.StrengthInBytes*2, 254);
            Key3Csc      = getDataByAdd.getBytes(cscOpt.StrengthInBytes*2, 255);
            Key4Csc      = getDataByAdd.getBytes(cscOpt.StrengthInBytes*2, 254);
            Key4Vkf      = getDataByAdd.getBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*2, 255);
            Key5Vkf      = getDataByAdd.getBytes(vkfOpt.K * VinKekFishBase_etalonK1.BLOCK_SIZE*2, 254);
        }

        ~DoCryptDataStream()       => Dispose(true);
        void IDisposable.Dispose() => Dispose();

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
                Record.errorsInDispose = true;
                emsg = L("DoCryptDataStream.Dispose: Dispose twiced");
                Console.Error.WriteLine(emsg);
                return;
            }

            TryToDispose(PrimaryStream);
            TryToDispose(AlignedStream);

            if (emsg is not null)
            {
                Record.errorsInDispose = true;
                Console.Error.WriteLine(emsg);
                return;
            }
        }
    }
}