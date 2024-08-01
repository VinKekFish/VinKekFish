using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static cryptoprime.BytesBuilderForPointers;

// ::test:O0s1QcshQ7zCGVMMKZtf:

using static cryptoprime.Threefish_slowly;

/*
 * Реализация Trheefish 1024 бита. Реализовано только шифрование
 * */
namespace cryptoprime
{
    // Threefish 1024 bits realization with optimization. Helper class for Threefish_Static_Generated
    // Реализация Threefish 1024 бита с оптимизацией - вспомогательный класс для Threefish_Static_Generated
    public unsafe class Threefish1024: IDisposable
    {
        public readonly static AllocHGlobal_AllocatorForUnsafeMemory allocator = new();

        protected readonly Record memory = allocator.AllocMemory(3*sizeof(ulong) + (Threefish_slowly.Nw + 1)*sizeof(ulong));
        public    readonly ulong * tweak;
        public    readonly ulong * key;
        /// <summary>Создаёт вспомогательные массивы с расширенным ключом и твик для использования в Threefish_Static_Generated</summary>
        /// <param name="Key">Ключ</param><param name="kLen">Длина ключа (keyLen=128)</param>
        /// <param name="Tweak">tweak</param><param name="tLen">Длина твика (twLen=16)</param>
        public Threefish1024(byte* Key, nint kLen, byte* Tweak, nint tLen)
        {
            cryptoprime.BytesBuilderForPointers.Record.DoRegisterDestructor(this);

            if (Key == null || Tweak == null) throw new ArgumentNullException("cryptoprime.Threefish1024.Threefish1024: Key == null || Tweak == null");

            tweak = (ulong*)memory.array;
            key = (ulong*)(memory.array + 3 * sizeof(ulong));

            if (kLen < keyLen) throw new ArgumentException("cryptoprime.Threefish1024.Threefish1024: kLen < keyLen");
            if (tLen < twLen) throw new ArgumentException("cryptoprime.Threefish1024.Threefish1024: tLen <  twLen");

            ulong* tk = this.key, tt = this.tweak;

            // На случай, если передадут массивы большей длины, мы берём ровно столько, сколько надо
            BytesBuilder.CopyTo(keyLen, keyLen, Key, (byte*)tk);
            BytesBuilder.CopyTo(twLen, twLen, Tweak, (byte*)tt);

            // Вычисление расширения ключа и tweak; это 17-ый элемент ключа
            GenExpandedKey(tk);

            tt[2] = tt[0] ^ tt[1];
        }

        /// <summary>Сгенерировать расширение ключа</summary>
        /// <param name="tk">Ключ с дополнительным 8-мибайтовым словом для расширения (слово расширения в конце)</param>
        public static void GenExpandedKey(ulong* tk)
        {
            tk[16] = Threefish_slowly.C240;
            for (int i = 0; i < Threefish_slowly.Nw; i++)
                tk[16] ^= tk[i];
        }

        public void Dispose()
        {
            if (memory.array != null)
                memory.Free();
            
            GC.SuppressFinalize(this);
        }

        ~Threefish1024()
        {
            Dispose();
        }
    }
}
