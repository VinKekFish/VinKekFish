// TODO: tests
// Сделать тесты на то, что вспомогательный блок действительно работает
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CodeGenerated.Cryptoprimes;

namespace cryptoprime.VinKekFish
{
    /// <summary>Базовая однопоточная реализация VinKekFish для K = 1. Использование для тестирования. См. также descr.md</summary>
    public static unsafe class VinKekFishBase_etalonK1
    {                                                                                   /// <summary>Размер криптографического состояния в байтах (3200). Размер криптографического состояния для выделения памяти другой: см. CryptoStateLenWithExtension</summary>
        public const int  CryptoStateLen          = 3200;                               /// <summary>Размер криптографического состояния в блоках keccak (16)</summary>
        public const int  CryptoStateLenKeccak    = CryptoStateLen / KeccakBlockLen;    /// <summary>Размер криптографического состояния в блоках ThreeFish (25)</summary>
        public const int  CryptoStateLenThreeFish = CryptoStateLen / ThreeFishBlockLen;
                                                                                        /// <summary>Размер блока ThreeFish</summary>
        public const int  ThreeFishBlockLen       = 128;                                /// <summary>Размер блока Keccak</summary>
        public const int     KeccakBlockLen       = 200;
                                                                                        /// <summary>Размер tweak (16 байтов, 2*ulong)</summary>
        public const int  CryptoTweakLen          = 8*2;
                                                                                        /// <summary>Размер блока ввода-вывода (512 байтов = 4096 битов), для K = 1</summary>
        public const int  BLOCK_SIZE              = 512;                                /// <summary>Размер максимального блока для ввода начала ключа: 2048 байтов (16384 бита), для K = 1</summary>
        public const int  MAX_SINGLE_KEY          = 2048;                               /// <summary>Максимально допустимая длина ОВИ (открытого вектора инициализации): 1148 байтов = 9184 битов, для K = 1</summary>
        public const int  MAX_OIV                 = 1148;
                                                                                        /// <summary>Минимально допустимое количество раундов на поглощение, для K = 1; это нестойкое поглощение, обеспечивающее минимальную диффузию</summary>
        public const int  MIN_ABSORPTION_ROUNDS_D = 1;                                  /// <summary>Минимально допустимое количество раундов на поглощение, для K = 1</summary>
        public const int  MIN_ABSORPTION_ROUNDS   = 2;                                  /// <summary>Минимально допустимое количество раундов (для любых операций), для K = 1</summary>
        public const int  MIN_ROUNDS              = 4;                                  /// <summary>Нормальное количество раундов, для K = 1</summary>
        public const int  NORMAL_ROUNDS           = 10;                                 /// <summary>Уменьшенное количество раундов, для K = 1</summary>
        public const int  REDUCED_ROUNDS          = 7;
        public const int  EXTRA_ROUNDS            = 25;
        public const int  MAX_ROUNDS              = 50;
                                                                                        /// <summary>Нормальная длина ключа в байтах (1024 байта = 8192 бита), для K = 1</summary>
        public const int  NORMAL_KEY              = 1024;                               /// <summary>Рекомендованная длина ключа в байтах (2048 байтов = 16384 бита), для K = 1</summary>
        public const int  RECOMMENDED_KEY         = 2048;                               /// <summary>Уменьшенная длина ключа в байтах (512 байтов = 4096 битов) - соответствует номинальной стойкости шифра, для K = 1</summary>
        public const int  REDUCED_KEY             = 512;
                                                                                        /// <summary>Это внутренняя константа алгоритма. Не нужна пользователю</summary>
        public const long TWEAK_STEP_NUMBER       = 1253539379;
                                                                                        /// <summary>Размер удлиннения криптографического состояния - это для расширения ключа, которое здесь просто берётся из следующего блока. Эта часть состояния носит технологический характер: в ней не хранится действующее состояние: это лишь временная копия нулевого блока нулевого подключа ThreeFish (нулевой блок состояния)</summary>
        public const int  CryptoStateLenExtension     = 8;                              /// <summary>Размер основного криптографического состояния в байтах включая технологическое удлиннение. Используется для выделения блоков состояния. Это размер CryptoStateLen + CryptoStateLenExtension, но выровненный до нечётного количества 64-байтных линий кеша</summary>
        public const int  CryptoStateLenWithExtension = CryptoStateLen + 64;


        /// <summary>Поглощение ключа губкой. Полное поглощение, включая криптографию. Пользователю не нужно, т.к. нужно использовать более специфические классы, например, VinKekFish_k1_base_20210419_keyGeneration</summary>
        /// <param name="key">Ключ</param>
        /// <param name="key_length">Длина ключа</param>
        /// <param name="OIV">Открытый вектор инициализации. Может быть null</param>
        /// <param name="OIV_length">Длина открытого вектора инициализации</param>
        /// <param name="state">Криптографическое состояние</param>
        /// <param name="state2">Вспомогательный массив криптографического состояния</param>
        /// <param name="b">Вспомогательный массив для функции keccak-f, размер b_size (25*8)</param>
        /// <param name="c">Вспомогательный массив для функции keccak-f, размер b_size (05*8)</param>
        /// <param name="tweak">Tweak (длина CryptoTweakLen = 16)</param>
        /// <param name="tweakTmp">Вспомогательный массив для хранения tweak (длина CryptoTweakLen)</param>
        /// <param name="tweakTmp2">Второй вспомогательный массив для хранения tweak (длина CryptoTweakLen)</param>
        /// <param name="Initiated"> При пользовательском вызове всегда false. Если <see langword="false"/>, то state инициализированно, но никакие данные не вводились. Если true, то в state уже вводились данные: например, другой ключ. Если false - идёт перезапись. Если <see langword="true"/> - поглощение через xor</param>
        /// <param name="ExtendedKey">При пользовательском вызове всегда false. Вторичный отрезок ключа: при рекурсивном вызове этот параметр равен true, означая, что идёт поглощение следующих за первым отрезков ключей</param>
        /// <param name="R">Количество раундов для первого поглощения. Таблицы перестановок должны быть проинициализированны для нужного количества раундов</param>
        /// <param name="RE">Количество раундов для отбоя после поглощения всего ключа (не рекомендуется делать низким). Таблицы перестановок должны быть проинициализированны для нужного количества раундов</param>
        /// <param name="RM">Количество раундов для поглощения дополнительных участков ключа (можно сделать низким, например, REDUCED_ROUNDS). Таблицы перестановок должны быть проинициализированны для нужного количества раундов</param>
        /// <param name="tablesForPermutations">Таблицы перестановок для всех раундов</param>
        public static void InputKey(byte * key, nint key_length, byte * OIV, nint OIV_length, byte * state, byte * state2, byte * b, byte *c, ulong * tweak, ulong * tweakTmp, ulong * tweakTmp2, bool Initiated, bool ExtendedKey, int R, int RE, int RM, ushort * tablesForPermutations)
        {
            if (ExtendedKey && OIV != null)
                throw new ArgumentException("SecondKey && OIV", "VinKekFishBase_etalonK1.InputKey: SecondKey && OIV != null");

            if (ExtendedKey && RE != 0)
                throw new ArgumentOutOfRangeException("SecondKey && RE", "VinKekFishBase_etalonK1.InputKey: SecondKey && RE != 0");

            if (ExtendedKey != Initiated)
                throw new ArgumentOutOfRangeException("SecondKey", "VinKekFishBase_etalonK1.InputKey: SecondKey != Initiated");

            if (OIV == null && OIV_length != 0)
                throw new ArgumentOutOfRangeException("OIV", "VinKekFishBase_etalonK1.InputKey: OIV == null && OIV_length != 0");

            if (OIV != null && OIV_length > MAX_OIV)
                throw new ArgumentOutOfRangeException("OIV_length", "VinKekFishBase_etalonK1.InputKey: OIV_length > MAX_OIV");

            if (key == null)
                throw new ArgumentNullException("key", "VinKekFishBase_etalonK1.InputKey: key == null");

            if (key_length <= 0)
                throw new ArgumentNullException("key_length", "VinKekFishBase_etalonK1.InputKey: key_length <= 0");

            if (R < MIN_ABSORPTION_ROUNDS_D)
                throw new ArgumentOutOfRangeException("R", "VinKekFishBase_etalonK1.InputKey: R < MIN_ABSORPTION_ROUNDS_D");
            if (RE < MIN_ROUNDS && !ExtendedKey)
                throw new ArgumentOutOfRangeException("RE", "VinKekFishBase_etalonK1.InputKey: RE < MIN_ROUNDS && !SecondKey");


            var dataLen = key_length;
            var data    = key;
            if (ExtendedKey)
            {
                if (dataLen > BLOCK_SIZE)
                    dataLen = BLOCK_SIZE;

                InputData_Xor(data, state, dataLen, tweak, regime: 0);
                data += dataLen;
            }
            else
            {
                if (dataLen > MAX_SINGLE_KEY)
                    dataLen = MAX_SINGLE_KEY;

                for (nint i = 0; i < dataLen; i++, data++)
                {
                    state[i+2] = *data;
                }

                byte len1 = (byte) dataLen;
                byte len2 = (byte) (dataLen >> 8);

                state[0] ^= len1;
                state[1] ^= len2;
                tweak[0] += TWEAK_STEP_NUMBER;
                tweak[1] += (ulong) dataLen;

                if (OIV != null && OIV_length > 0)
                {
                    len1 = (byte) OIV_length;
                    len2 = (byte) (OIV_length >> 8);

                    state[2050] ^= len1;
                    state[2051] ^= len2;

                    for (nint i = 0; i < OIV_length; i++, OIV++)
                    {
                        state[i+2052] = *OIV;
                    }
                }
            }

            // TODO: указатели на таблицы перестановок
            step
            (
                countOfRounds: R, tablesForPermutations: tablesForPermutations,
                tweak: tweak, tweakTmp: tweakTmp, state: state, state2: state2, b: b, c: c
            );

            if (key_length > dataLen)
            {
                InputKey
                (
                    key:        data,
                    key_length: key_length - dataLen,

                    ExtendedKey:  true,
                    Initiated:    true,

                    OIV:        null,
                    OIV_length: 0,

                    R:          RM,             // Повторный ввод ключа осуществляется под RM раундов
                    RM:         RM,
                    RE:         0,

                    state: state, state2: state2, tweak: tweak, tweakTmp: tweakTmp, tweakTmp2: tweakTmp2, b: b, c: c, tablesForPermutations: tablesForPermutations
                );
            }

            // Завершаем ввод ключа отбоем. Т.к. вызов функции рекурсивный, отбой происходит только в самой верхней функции - SecondKey = false
            if (!ExtendedKey)
            {
                InputData_Overwrite(data: null, state: state, dataLen: 0, tweak: tweak, regime: 255);
                step
                (
                    countOfRounds: RE, tablesForPermutations: tablesForPermutations,
                    tweak: tweak, tweakTmp: tweakTmp, state: state, state2: state2, b: b, c: c
                );
            }
        }

        /// <summary>Сырой ввод данных. Вводит данные в состояние путём перезатирания (режим OVERWRITE), изменяет tweak. Не вызывает криптографические функции</summary>
        /// <param name="data">Указатель на вводимые данные, может быть null, если dataLen == 0</param>
        /// <param name="state">Указатель на криптографическое состояние</param>
        /// <param name="dataLen">Длина вводимых данных, не более BLOCK_SIZE</param>
        /// <param name="tweak">Указатель на tweak (для соответствующего изменения tweak)</param>
        /// <param name="regime">Счётчик режима ввода</param>
        /// <param name="nullPaddding">Если <see langword="true"/>, то если данных меньше, чем BLOCK_SIZE, оставшиеся байты будут перезатёрты нулями, обеспечивая необратимость. Иначе остальные байты останутся неизменными</param>
        public static void InputData_Overwrite(byte * data, byte * state, ulong dataLen, ulong * tweak, byte regime, bool nullPaddding = true)
        {
            if (dataLen > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException("dataLen", "InputData_Overwrite: dataLen > BLOCK_SIZE");

            ulong i = 0;
            for (; i < dataLen; i++, data++)
            {
                state[i+3] = *data;
            }

            if (nullPaddding)
            for (; i < BLOCK_SIZE; i++)
            {
                state[i+3] = 0;
            }

            byte len1 = (byte) dataLen;
            byte len2 = (byte) (dataLen >> 8);

            len2 |= 0x80;       // Старший бит количества вводимых байтов устанавливается в 1, если используется режим Overwrite
            if (!nullPaddding)   // Второй (начиная с 1) по старшинству бит устанавливаем, если не перезатирали значения
            {
                if ((len2 & 0x40) > 0)
                    throw new Exception("InputData_Overwrite: fatal algorithmic error: (len2 & 0x40) > 0");

                len2 |= 0x40;
            }

            state[0] ^= len1;
            state[1] ^= len2;
            state[2] ^= regime;

            InputData_ChangeTweak(state: state, tweak: tweak, dataLen: (long) dataLen, Overwrite: true, regime: regime);
        }

        /// <summary>Сырой ввод данных. Вводит данные в состояние через xor (режим ввода sponge), изменяет tweak. Не вызывает криптографические функции</summary>
        public static void InputData_Xor(byte * data, byte * state, long dataLen, ulong * tweak, byte regime)
        {
            if (dataLen > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException();

            for (long i = 0; i < dataLen; i++, data++)
            {
                state[i+3] ^= *data;
            }

            byte len1 = (byte) dataLen;
            byte len2 = (byte) (dataLen >> 8);

            state[0] ^= len1;
            state[1] ^= len2;
            state[2] ^= regime;

            InputData_ChangeTweak(state: state, tweak: tweak, dataLen: dataLen, Overwrite: false, regime: regime);
        }

        /// <summary>Этот метод вызывать не надо, изменяет tweak. Он автоматически вызывается при вызове InputData_*</summary>
        public static void InputData_ChangeTweak(byte * state, ulong * tweak, long dataLen, bool Overwrite, byte regime)
        {
            // Приращение tweak перед вводом данных
            tweak[0] += TWEAK_STEP_NUMBER;

            tweak[1] += (ulong) dataLen;
            if (Overwrite)
                tweak[1] += 0x0100_0000_0000_0000;

            var reg = ((ulong) regime) << 40; // 8*5 - третий по старшинству байт, нумерация с 1
            tweak[1] += reg;
            state[2] ^= regime;
        }

        /// <summary>Если никаких данных не введено в режиме Sponge (xor), изменяет tweak. В режиме OVERWRITE нужно использовать InputData_Overwrite с dataLen=0</summary>
        public static void NoInputData_ChangeTweak(byte * state, ulong * tweak, byte regime)
        {
            // Приращение tweak перед вводом данных
            tweak[0] += TWEAK_STEP_NUMBER;

            // tweak[1] += dataLen;
            state[2] ^= regime;

            var reg = ((ulong) regime) << 40; // 8*5 - третий по старшинству байт, нумерация с 1
            tweak[1] += reg;
        }

        /// <summary>Шаг алгоритма ПОСЛЕ ввода данных. Перед step необходимо вызывать NoInputData_ChangeTweak или InputData_*</summary>
        /// <param name="countOfRounds">Количество раундов</param>
        /// <param name="tweak">Tweak после ввода данных, 16 байтов (все массивы могут быть в одном, если это удобно). Не изменяется в функции.</param>
        /// <param name="tweakTmp">Дополнительный массив для временного tweak, 16 байтов. Изменяется в функции.</param>
        /// <param name="state">Криптографическое состояние (размер в байтах CryptoStateLenWithExtension)</param>
        /// <param name="state2">Вспомогательный массив для криптографического состояния (размер в байтах CryptoStateLenWithExtension)</param>
        /// <param name="tablesForPermutations">Массив таблиц перестановок на каждый раунд. Длина должна быть countOfRounds*4 таблиц (CryptoStateLen*ushort на каждую таблицу)</param>
        /// <param name="b">Вспомогательный массив b для keccak.Keccackf</param>
        /// <param name="c">Вспомогательный массив c для keccak.Keccackf</param>
        public static void step(int countOfRounds, ulong * tweak, ulong * tweakTmp, byte * state, byte * state2, ushort * tablesForPermutations, byte* b, byte* c)
        {
            tweakTmp[0] = tweak[0];
            tweakTmp[1] = tweak[1];

            #if SUPER_CHECK_PERMUTATIONS
            vinkekfish.VinKekFish_k1_base_20210419.CheckAllPermutationTables(tablesForPermutations, countOfRounds, CryptoStateLen, "before step");
            #endif

            VinKekFish_Utils.Utils.MsgToFile($"round started {countOfRounds}", "k1");   // TODO: !!!
            VinKekFish_Utils.Utils.ArrayToFile((byte *) tweakTmp, 16, "k1");   // TODO: !!!

            // Распределение впитывания (Предварительное преобразование)
            DoPermutation(state, state2, CryptoStateLen, transpose128_3200);
            DoThreefishForAllBlocks(state2, state, tweakTmp);
            DoPermutation(state, state2, CryptoStateLen, transpose128_3200);
            BytesBuilder.CopyTo(CryptoStateLen, CryptoStateLen, state2, state);

            // Основной шаг алгоритма: раунды
            for (int round = 0; round < countOfRounds; round++)
            {
                VinKekFish_Utils.Utils.MsgToFile($"semiround {round*2}", "k1");   // TODO: !!!

                DoKeccakForAllBlocks(state, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state, state2, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                DoThreefishForAllBlocks(state2, state, tweakTmp);
                DoPermutation(state, state2, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                // Довычисление tweakVal для второго преобразования VinKekFish
                tweakTmp[0] += 0x1_0000_0000U;

                VinKekFish_Utils.Utils.MsgToFile($"semiround {round*2+1}", "k1");   // TODO: !!!

                DoKeccakForAllBlocks(state2, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state2, state, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                DoThreefishForAllBlocks(state, state2, tweakTmp);
                DoPermutation(state2, state, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                // Вычисляем tweak для данного раунда (работаем со старшим 4-хбайтным словом младшего 8-мибайтного слова tweak)
                // Каждый раунд берёт +2 к старшему 4-хбайтовому слову; +1 - после первой половины, и +1 - после второй половины
                tweakTmp[0] += 0x1_0000_0000U;
            }

            VinKekFish_Utils.Utils.MsgToFile($"final", "k1");   // TODO: !!!

            // После последнего раунда производится заключительная рандомизация поблочной функцией keccak-f
            for (int i = 0; i < 2; i++)
            {
                DoKeccakForAllBlocks(state,  CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state, state2, CryptoStateLen, transpose200_3200);
                DoKeccakForAllBlocks(state2, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state2, state, CryptoStateLen, transpose200_3200_8);
            }
        }
        /*
        /// <summary>Выравнивает целое число i на интервал [0; ringModulo)</summary>
        /// <param name="i">Выравниваемое число</param>
        /// <param name="ringModulo">[0; ringModulo)</param>
        /// <returns>Выровненное число</returns>
        public static int getNumberFromRing(int i, int ringModulo)
        {
            while (i < 0)
                i += ringModulo;

            while (i >= ringModulo)
                i -= ringModulo;

            return i;
        }
        */

        /// <summary>Применяет ThreeFish поблочно ко всему состоянию алгоритма</summary>
        /// <param name="beginCryptoState">Начальное криптографическое состояние (инициализированное) (размер CryptoStateLenWithExtension байтов)</param>
        /// <param name="finalCryptoState">Финальное криптографическое состояние (для результата, будет перезатёрто)</param>
        /// <param name="tweak">Базовый tweak для раунда. Не изменяется</param>
        public static unsafe void DoThreefishForAllBlocks(byte* beginCryptoState, byte * finalCryptoState, ulong * tweak)
        {
            int len = CryptoStateLenThreeFish;  // len здесь точно рассчитана на K = 1, никак иначе; len = 25
            /*
            if ((len & 1) == 0)
                throw new ArgumentException("'len' must be odd", "len");
            */
            byte* cur = finalCryptoState;
            byte* key = beginCryptoState;

            BytesBuilder.CopyTo(CryptoStateLen, CryptoStateLen, beginCryptoState, finalCryptoState);
            // Копируем вспомогательный блок для расширения ключа
            BytesBuilder.CopyTo(CryptoStateLenWithExtension, CryptoStateLenWithExtension, beginCryptoState, beginCryptoState,
                                targetIndex: CryptoStateLen, count: CryptoStateLenExtension, index: 0);

            var tweakTmp = stackalloc ulong[3];
            tweakTmp[0]  = tweak[0];
            tweakTmp[1]  = tweak[1];
            tweakTmp[2]  = tweak[0] ^ tweak[1];

            // getNumberFromRing не вызывается, вместо этого используется самостоятельный расчёт, он должен быть более быстрым
            int j   = len >> 1;
            int add = 0;

            // cur - это финальное состояние, которое изменяется
            // key всегда вычисляется заново, т.к. он переходит через нуль - это массив ключевой информации для ThreeFish
            for (int i = 0; i < len; i++, j++, cur += ThreeFishBlockLen)
            {
                if (j >= len)
                    j = 0;

                add = j * ThreeFishBlockLen;
                key = beginCryptoState + add;

                Threefish_Static_Generated.Threefish1024_step(key: (ulong *) key, tweak: (ulong *) tweakTmp, text: (ulong *) cur);

                tweakTmp[0] += 1;
                tweakTmp[2]  = tweakTmp[0] ^ tweakTmp[1];
            }
        }

        /// <summary>Применяет к криптографическому состоянию CryptoState поблочное преобразование keccak</summary>
        /// <param name="CryptoState">Криптографическое состояние</param>
        /// <param name="len">Длина криптографического состояния в блоках keccak (длина по 200 байтов; KeccakBlockLen)</param>
        public static unsafe void DoKeccakForAllBlocks(byte* CryptoState, int len, ulong * b, ulong * c)
        {
            byte* cur = CryptoState;

            for (int i = 0; i < len; i++, cur += KeccakBlockLen)
            {
                KeccakPrime.Keccackf(a: (ulong *) cur, c: c, b: b);
            }
        }

        /// <summary>Осуществляет перестановки байтов для обеспечения диффузии</summary>
        /// <param name="source">Исходный массив: из него берутся значения</param>
        /// <param name="target">Целевой массив: в него записываются значения</param>
        /// <param name="len">Длины обоих массивов в байтах</param>
        /// <param name="permutationTable">Таблица перестановок</param>
        public static void DoPermutation(byte* source, byte* target, int len, ushort* permutationTable)
        {
            /*
             * Перестановка:
             * Теперь байт с позиции source[permutationTable[i]] мы переставляем на позицию target[i]
             * 
             * Например, transpose200 должна быть [0, 200, 400, 600, 800 ...]
             * 
             * */
            #if SUPER_CHECK_PERMUTATIONS
            // vinkekfish.VinKekFish_k1_base_20210419.CheckPermutationTable(permutationTable, len, "DoPermutation.k1 function");
            #endif

            VinKekFish_Utils.Utils.ArrayToFile(source, len, "k1");  // TODO: !!!

            for (int i = 0; i < len; i++)
            {
                target[i] = source[  permutationTable[i]  ];
            }

            // TODO: !!!
            VinKekFish_Utils.Utils.ArrayToFile((byte *) permutationTable, len*2, "k1");
            VinKekFish_Utils.Utils.ArrayToFile(target, len, "k1");
        }

        public static ushort* transpose128_3200    = null;
        public static ushort* transpose200_3200    = null;
        public static ushort* transpose200_3200_8  = null;
        // public static ushort* transpose400_3200_16 = null;

        public static readonly object sync = new object();

        /// <summary>Эту процедуру нужно вызвать для инициализации таблиц перестановок перед любым вызовом методов класса. Допускается многопоточный вызов без синхронизации. Вызов производится один раз на всю программу (на весь процесс)</summary>
        public static void GenTables()
        {
            lock (sync)
            {
                if (transpose128_3200 != null)
                    return;
                if (CryptoStateLen != 3200 || ThreeFishBlockLen != 128 || KeccakBlockLen != 200)
                    throw new Exception();

                transpose128_3200    = GenTransposeTable(3200, 128);
                transpose200_3200    = GenTransposeTable(3200, 200);
                transpose200_3200_8  = GenTransposeTable(3200, 200,  stepInEndOfBlocks: 8);
                // transpose400_3200_16 = GenTransposeTable(3200, 400,  stepInEndOfBlocks: 16);
            }

            if (transpose128_3200[1] != 128)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[1] != 128");
            if (transpose128_3200[8] != 1024)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[8] != 1024");
            if (transpose200_3200[1] != 200)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_3200[1] != 200");
            if (transpose200_3200[8] != 1600)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_3200[8] != 1600");
            if (transpose200_3200[400] != 25)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_3200[400] != 25");
            if (transpose200_3200_8[2800] != 07)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_3200_8[2800] != 07");
        }

        public static ushort* GenTransposeTable(ushort blockSize, ushort step, int numberOfRetries = 1, ushort stepInEndOfBlocks = 1, ushort stepInEndOfStep = 1)
        {
            var newTable = new ushort[blockSize];
            var buffer   = new ushort[blockSize];
            for (ushort i = 0; i < newTable.Length; i++)
            {
                newTable[i] = i;
                buffer  [i] = i;
            }

            for (int z = 0; z < numberOfRetries; z++)
            {
                int j = 0, k = 0;
                for (ushort i = 0; i < blockSize; i++)
                {
                    buffer[j++] = newTable[k];

                    k += step;
                    if (k >= blockSize)
                    {
                        k -= blockSize;
                        k += stepInEndOfBlocks;
                        if (k >= step)
                        {
                            k -= step;
                            k += stepInEndOfStep;
                        }
                    }
                }

                fixed (ushort* nt = newTable, buff = buffer)
                {
                    BytesBuilder.CopyTo(buffer.Length << 1, buffer.Length << 1, (byte*)buff, (byte*)nt);
                }
            }
/*
            // Тестирование таблицы
            // Каждое значение должно быть представлено хотя бы один раз (и только один раз)
            for (ushort i = 0; i < newTable.Length; i++)
            {
                if (!newTable.Contains(i))
                {
                    throw new Exception("VinKekFish: fatal algotirhmic error 1: GenTransposeTable");
                }
            }
*/
            int rLen   = newTable.Length * sizeof(ushort);
            var result = (ushort *) Marshal.AllocHGlobal(rLen).ToPointer();

            fixed (ushort * newTablePointer = newTable)
            {
                BytesBuilder.CopyTo(rLen, rLen, (byte *) newTablePointer, (byte *) result);
            }

            return result;
        }
    }
}
