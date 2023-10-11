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
        // Здесь алгоритмы частично скопипащены
        // ::cp:alg:1LTt5CRnTQjLt3M4JX6Z:2023.09.04
        /// <summary>Генерирует стандартную таблицу перестановок</summary>
        /// <param name="Rounds">Количество раундов, для которых идёт генерация. Для каждого раунда по 4-ре таблицы</param>
        /// <param name="key">Это вспомогательный ключ для генерации таблиц перестановок. Основной ключ вводить нельзя! Этот ключ не может быть ключом, вводимым в VinKekFish, см. описание VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов, где таблицы перестановок не генерируются от ключа, а идут стандартно transpose128_3200 и transpose200_3200</param>
        public Record GenStandardPermutationTables(int Rounds, AllocatorForUnsafeMemoryInterface? allocator = null, byte * key = null, nint key_length = 0, byte * OpenInitVector = null, nint OpenInitVector_length = 0, int PreRoundsForTranspose = 0, int ThreeFishInitSteps = 2)
        {
            this.GenTables();

            if (PreRoundsForTranspose > Rounds)
                PreRoundsForTranspose = Rounds;

            if (PreRoundsForTranspose < 1)
                // throw new ArgumentOutOfRangeException("VinKekFish_KN_base_20210525.GenStandardPermutationTables: PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds");
                PreRoundsForTranspose = this.MIN_ABSORPTION_ROUNDS_D_K;

            if (allocator == null)
                allocator = VinKekFish_k1_base_20210419.AllocHGlobal_allocator;

            //using var prng = new Keccak_PRNG_20201128();
            // this.K*1024 - реальная стойкость VinKekFish в байтах именно такая. Поэтому, создаём губку именно такой стойкости
            nint gpKeyLen   = this.K*1024;
            if (gpKeyLen > key_length)
                gpKeyLen = key_length;

            using var prng = PreRoundsForTranspose >= Rounds  ? null
                             : new CascadeSponge_mt_20230930(gpKeyLen);

            if (key != null || OpenInitVector != null)
            if (prng == null)
                throw new ArgumentException("GenStandardPermutationTables: key != null || OpenInitVector != null but prng == null");

            if (key != null && key_length > 0)
            {
                if (OpenInitVector == null)
                {
                    prng!.initKeyAndOIV(key, key_length, null, 0, ThreeFishInitSteps);
                }
                else
                {
                    prng!.initKeyAndOIV(key, key_length, OpenInitVector, OpenInitVector_length, ThreeFishInitSteps);
                }
            }
            else
            if (OpenInitVector != null)
                throw new ArgumentException("GenStandardPermutationTables: key == null && OpenInitVector != null. Set OpenInitVector as key");

            nint len1  = Len;
            nint len2  = len1 * sizeof(ushort);

            // Каждый раунд расходует по 4 таблицы. Всего раундов не более Rounds.
            // Длина таблицы - len1 (Len) * размер двухбайтового целого
            // CountOfFinal таблиц заключительного преобразования и 2 таблицы предварительного здесь не учитываются вообще
            var rCheck = 4 * Rounds;
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
                byte   * r = result;

                for (; PreRoundsForTranspose > 0 && Rounds > 0; Rounds--, PreRoundsForTranspose--)
                {
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200_8, r); CheckPermutationTable_fast((ushort*)r, len1, "transpose200_8"); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128  , r); CheckPermutationTable_fast((ushort*)r, len1, "transpose128.1"); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200  , r); CheckPermutationTable_fast((ushort*)r, len1, "transpose200.1"); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128  , r); CheckPermutationTable_fast((ushort*)r, len1, "transpose128.2"); r += len2;
                }

                for (; Rounds > 0; Rounds--)
                {
                    prng!.doRandomPermutationForUShorts(len1, Table1);
                    prng!.doRandomPermutationForUShorts(len1, Table2);

                    // Если необходимо, раскомментировать отладочный код: здесь проверяется, что перестановки были корректны (что они перестановки, а не какие-то ошибки)
                    /*CheckPermutationTable(Table1, table1.Length);
                    CheckPermutationTable(Table2, table2.Length);*/

                    BytesBuilder.CopyTo(len2, len2, (byte*)Table1,       r); CheckPermutationTable_fast((ushort*)r, len1, "Table1"); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte*)Table2,       r); CheckPermutationTable_fast((ushort*)r, len1, "Table2"); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose200, r); CheckPermutationTable_fast((ushort*)r, len1, "transpose200.3"); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose128, r); CheckPermutationTable_fast((ushort*)r, len1, "transpose128.3"); r += len2;
                }

                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table1);
                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table2);
            }

            CheckAllPermutationTables (result, rCheck, len1, "after VinKekFishBase_KN_20210525.GenStandardPermutationTables");

            return result;
        }

        // ::cp:alg:S7x1R5lmSapGlHsjoDoZ:2023.09.04
        public static void CheckPermutationTable(ushort* table, nint Length, string message = "")
        {
            bool found;
            for (int i = 0; i < Length; i++)
            {
                found = false;
                for (int j = 0; j < Length; j++)
                {
                    if (table[j] == i)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new Exception($"DEBUG: GenStandardPermutationTables incorrect: value {i} not found. {message}");
            }
        }

        // ::cp:alg:MTQCE0xzWZWA74kQcmuk:2023.09.04
        public static void CheckPermutationTable_fast(ushort* table, nint Length, string message = "")
        {
            var byteLen = 1 + Length >> 3;
            var check   = stackalloc byte[(int) byteLen];

            for (var i = 0; i < byteLen; i++)
                check[i] = 0;

            for (int i = 0; i < Length; i++)
            {
                var val = table[i];
                if (val >= Length)
                    throw new Exception($"Fatal algorithmic error: CheckPermutationTable_fast.kn incorrect: value {val} is incorrect (too big, Length={Length}). {message}");
                if (BitToBytes.getBit(check, val))
                    throw new Exception($"Fatal algorithmic error: CheckPermutationTable_fast.kn incorrect: value {val} found twice. {message}");

                BitToBytes.setBit(check, val);
            }

            for (int i = 0; i < Length; i++)
            {
                if (!BitToBytes.getBit(check, i))
                    throw new Exception($"Fatal algorithmic error: CheckPermutationTable_fast.kn incorrect: value {i} not found. {message}");
            }
        }

        // ::cp:alg:35OvLQRA0EzDC2CAJx7U:2023.09.04
        public static void CheckAllPermutationTables(ushort* table, nint countOfTables, nint Length, string message = "")
        {
            for (int i = 0; i < countOfTables; i++, table += Length)
            {
                CheckPermutationTable_fast(table, Length, $"(table number: {i}). " + message);
                #if SUPER_CHECK_PERMUTATIONS
                // CheckPermutationTable(table, Length, "! Attention for error in DEBUG code ! FATAL DEBUG ERROR: CheckAllPermutationTables triggered, but must been CheckPermutationTable_fast. " +  $"(table number: {i}). " + message);
                #endif
            }
        }

        // Эти таблицы теперь не статические, т.к. для каждого K эти таблицы разные
        public ushort* transpose128   = null;
        public ushort* transpose200   = null;
        public ushort* transpose200_8 = null;

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
/*            if (transpose200[LenInKeccak*LenInThreeFish] != LenInThreeFish)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200[LenInKeccak*LenInThreeFish] != LenInThreeFish");
            if (transpose200_8[200*(LenInKeccak - K*2)] != 07)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_8[200*(LenInKeccak - K*2)] != 07");*/
        }
    }
}
