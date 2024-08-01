// TODO: tests
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CodeGenerated.Cryptoprimes;

using cryptoprime;
using cryptoprime.VinKekFish;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    public unsafe partial class VinKekFishBase_KN_20210525
    {
        /// <summary>Запускает многопоточную поблочную обработку алгоритмом keccak</summary>
        protected void DoKeccak()
        {
            CurrentKeccakBlockNumber[0] = 0;
            CurrentKeccakBlockNumber[1] = 1;

            if (ThreadCount > 1)
                Parallel.For(0, ThreadCount, (i, state) => ThreadFunction_Keccak());
            else
                ThreadFunction_Keccak();

            IsState1Main ^= true;                 // Переключаем состояния (вспомогательный и основной массив состояний)
        }

                                                                                            /// <summary>Массив счётчика блоков для определения текущего блока для обработки keccak. [0] - чётные элементы, [1] - нечётные элементы</summary>
        protected volatile int[] CurrentKeccakBlockNumber = {0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};   // Нули ради того, чтобы больше в этой линии кеша ничего не было
        protected void ThreadFunction_Keccak()
        {
            byte * mat = stackalloc byte[MatrixLen];

            for (int i = 0; i <= 1; i++)
            {
                do
                {
                    // Interlocked.Add возвращает результат сложения. А нам нужно значение до результата. Поэтому вычитаем назад 2, чтобы получить индекс нужного нам блока
                    var index  = Interlocked.Add(ref CurrentKeccakBlockNumber[i], 2) - 2;
                    if (index >= LenInKeccak)
                    {
                        break;
                    }

                    var offset = KeccakBlockLen * index;
                    var off1   = st1 + offset;
                    var off2   = st2 + offset;

                    byte * off = off1;

                    if (i == 0)
                    {
                        BytesBuilder.CopyTo(KeccakBlockLen, KeccakBlockLen, off1, off2);
                        off = off2;
                    }

                    KeccakPrime.Keccackf(a: (ulong *) off, c: (ulong *) (mat + KeccakPrime.b_size), b: (ulong *) mat);

                    if (i != 0)
                    {
                        BytesBuilder.CopyTo(KeccakBlockLen, KeccakBlockLen, off1, off2);
                    }
                }
                while (true);
            }

            BytesBuilder.ToNull(MatrixLen, mat);
        }

        protected void DoThreeFish()
        {
            CurrentThreeFishBlockNumber = 0;
            BytesBuilder.CopyTo(Len, Len, st1, st2);        // Копируем старое состояние в новое, чтобы можно было его шифровать на новом месте
            // Копируем расширение ключа для последнего блока - это самые первые 8-мь байтов нулевого блока
            BytesBuilder.CopyTo(FullLen, FullLen, st1, st1, targetIndex: Len, count: CryptoStateLenExtension, index: 0);

            if (ThreadCount > 1)
                Parallel.For(0, ThreadCount, (i, state) => ThreadFunction_ThreeFish());
            else
                ThreadFunction_ThreeFish();

            IsState1Main ^= true;
        }

        protected volatile int CurrentThreeFishBlockNumber = 0;
        protected void ThreadFunction_ThreeFish()
        {
            // В элементах [0;1] массива содержится tweak текущего шага шифрования. В элементах [2;3] - tweak, который изменяется в течении шага (tweak полураунда)   ::an6c5JhGzyOO
            // var tweaks  = (ulong *) (((byte *) Tweaks) + CountOfTweaks * CryptoTweakLen * index + CryptoTweakLen*2);
            // Выделяем место для tweak текущего блока
            var tweak  = stackalloc ulong[3];

            do
            {
                var index  = Interlocked.Increment(ref CurrentThreeFishBlockNumber) - 1;
                if (index >= LenInThreeFish)
                {
                    break;
                }

                var offsetC = ThreeFishBlockLen * index;
                var offsetK = ThreeFishBlockLen * NumbersOfThreeFishBlocks[index];
                var offC    = st2 + offsetC;
                var offK    = st1 + offsetK;

                tweak[0]   = Tweaks[2+0] + (uint) index;     // Берём tweak из элемента [2]
                tweak[1]   = Tweaks[2+1];
                tweak[2]   = tweak[0] ^ tweak[1];

                Threefish_Static_Generated.Threefish1024_step(key: (ulong *) offK, tweak: tweak, text: (ulong *) offC);
            }
            while (true);

            BytesBuilder.ToNull(3*sizeof(ulong), (byte*) tweak);
        }

        protected void DoPermutation(ushort * CurrentPermutationTable)
        {
            CurrentPermutationBlockNumber = 0;
            this.CurrentPermutationTable  = CurrentPermutationTable;

            #if DEBUG_OUTPUT
            VinKekFish_Utils.Utils.ArrayToFile(st1, this.Len, "KN");
            #endif

            ThreadFunction_Permutation();
            IsState1Main ^= true;

            #if DEBUG_OUTPUT
            // VinKekFish_Utils.Utils.ArrayToFile((byte *) CurrentPermutationTable, this.Len*2, "KN");
            VinKekFish_Utils.Utils.ArrayToFile(st1, this.Len, "KN");
            #endif
        }

        protected volatile int      CurrentPermutationBlockNumber = 0;
        protected volatile ushort * CurrentPermutationTable       = null;
        protected void ThreadFunction_Permutation()
        {
            #if SUPER_CHECK_PERMUTATIONS
            CheckPermutationTable_fast(CurrentPermutationTable, Len, "ThreadFunction_Permutation.kn function");
            #endif

            for (int i = 0; i < Len; i++)
            {
                st2[i] = st1[  CurrentPermutationTable[i]  ];
            }
        }
    }
}

