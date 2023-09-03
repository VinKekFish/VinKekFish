// TODO: tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using cryptoprime;
using cryptoprime.VinKekFish;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        
        /// <summary>Генерирует стандартную таблицу перестановок</summary>
        /// <param name="Rounds">Количество раундов, для которых идёт генерация. Для каждого раунда по 4-ре таблицы</param>
        /// <param name="key">Это вспомогательный ключ для генерации таблиц перестановок. Основной ключ вводить нельзя! Этот ключ не может быть ключом, вводимым в VinKekFish, см. описание VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов, где таблицы перестановок не генерируются от ключа, а идут стандартно transpose128_3200 и transpose200_3200</param>
        public Record GenStandardPermutationTables(int Rounds, AllocatorForUnsafeMemoryInterface? allocator = null, byte * key = null, nint key_length = 0, byte * OpenInitVector = null, nint OpenInitVector_length = 0, int PreRoundsForTranspose = 8)
        {
            this.GenTables();

            if (PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds)
                throw new ArgumentOutOfRangeException("VinKekFish_base_20210419.GenStandardPermutationTables: PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds");

            if (allocator == null)
                allocator = VinKekFish_k1_base_20210419.AllocHGlobal_allocator;

            using var prng = new Keccak_PRNG_20201128();

            if (key != null && key_length > 0)
            {
                if (OpenInitVector == null)
                {
                    prng.InputKeyAndStep((byte*)key, key_length, null, 0);
                }
                else
                {
                    prng.InputKeyAndStep((byte*)key, key_length, (byte*)OpenInitVector, OpenInitVector_length);
                }
            }
            else
            if (OpenInitVector != null)
                throw new ArgumentException("key == null && OpenInitVector != null. Set OpenInitVector as key");

            nint len1  = Len;
            nint len2  = Len * sizeof(ushort);

            // Каждый раунд расходует по 4 таблицы. Всего раундов не более Rounds.
            // Длина таблицы - len1 (Len) * размер двухбайтового целого
            // CountOfFinal таблиц заключительного преобразования и 2 таблицы предварительного здесь не учитываются вообще
            var result = allocator.AllocMemory(Rounds * 4 * len2);
            var table1 = new ushort[len1];
            var table2 = new ushort[len1];

            for (ushort i = 0; i < table1.Length; i++)
            {
                table1[i] = i;
                table2[i] = (ushort) (table1.Length - i - 1);
            }

            fixed (ushort * Table1 = table1, Table2 = table2)
            {
                ushort * R = result;
                ushort * r = R;

                for (; PreRoundsForTranspose > 0 && Rounds > 0; Rounds--, PreRoundsForTranspose--)
                {
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200_8, (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128  , (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200  , (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128  , (byte *) r); r += len1;
                }
// TODO: Сколько можно ввести дополнительной рандомизирующей информации, чтобы она вводилась при перестановках от раунда к раунду?
                for (; Rounds > 0; Rounds--)
                {
                    prng.doRandomPermutationForUShorts(table1);
                    prng.doRandomPermutationForUShorts(table2);
/*  // Если необходимо, раскомментировать отладочный код: здесь проверяется, что перестановки были корректны (что они перестановки, а не какие-то ошибки)
#if DEBUG
                    CheckPermutationTable(table1);
                    CheckPermutationTable(table2);
#endif
*/
                    BytesBuilder.CopyTo(len2, len2, (byte*)Table1,       (byte*)r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte*)Table2,       (byte*)r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose200, (byte*)r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose128, (byte*)r); r += len1;
                }

                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table1);
                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table2);
            }

            return result;
        }

#if DEBUG
        private static void CheckPermutationTable(ushort[] table1)
        {
            bool found;
            for (int i = 0; i < table1.Length; i++)
            {
                found = false;
                for (int j = 0; j < table1.Length; j++)
                {
                    if (table1[j] == i)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new Exception($"DEBUG: doRandomPermutationForUShorts incorrect: value {i} not found");
            }
        }
#endif

        public static ushort* transpose128   = null;
        public static ushort* transpose200   = null;
        public static ushort* transpose200_8 = null;

        public void GenTables()
        {
            lock (sync)
            {
                if (transpose128 != null)
                    return;

                transpose128   = GenTransposeTable((ushort) Len, 128);
                transpose200   = GenTransposeTable((ushort) Len, 200);
                transpose200_8 = GenTransposeTable((ushort) Len, 200,  stepInEndOfBlocks: 8);
            }

            if (transpose128[1] != 128)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128[1] != 128");
            if (transpose128[8] != 1024)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128[8] != 1024");
            if (transpose128[LenInThreeFish] != 1)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128[LenInThreeFish] != 1");
            if (transpose200[1] != 200)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200[1] != 200");
            if (transpose200[8] != 1600)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200[8] != 1600");
            if (transpose200[LenInKeccak] != 1)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200[LenInKeccak] != 1");
            if (transpose200[400] != 25)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200[400] != 25");
            if (transpose200_8[2800] != 07)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_8[2800] != 07");
        }
    }
}
