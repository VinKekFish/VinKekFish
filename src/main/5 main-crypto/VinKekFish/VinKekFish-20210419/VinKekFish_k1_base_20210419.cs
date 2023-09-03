// TODO: tests
// Здесь нужно сделать тест на диффузию и равномерность распределения TreeFish в этом модифицированном варианте
using cryptoprime.VinKekFish;
using cryptoprime;
// using maincrypto.keccak;

using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using static cryptoprime.BytesBuilderForPointers;

// #nullable disable

using maincrypto.keccak;

namespace vinkekfish
{
    // Описание состояний в файле ./Documentation/VinKekFish_k1_base_20210419_состояния.md
    // Там же см. "Рекомендуемый порядок вызовов"
    // Файл не осуществляет ввода-вывода
    // Не имеет примитивов синхронизации
    // Не читает/записывает данные во внешние глобальные переменные

    /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/VinKekFish_k1_base_20210419/*' />
    public unsafe class VinKekFish_k1_base_20210419: IDisposable
    {
        protected       int    _RTables = 0;                            /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/RTables/*' />
        public          int    RTables => _RTables;

                                                                        /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/_state/*' />
        protected       Record? _state = null, _state2 = null, t0 = null, t1 = null, t2 = null, _transpose200_3200 = null, _transpose200_3200_8 = null, _b = null, _c = null; /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/stateHandle/*' />
        protected       Record? stateHandle   = null;                    /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/pTablesHandle/*' />
        protected       Record? pTablesHandle = null;

        protected bool isInited1 = false;
        protected bool isInited2 = false;

        protected int  _InitedPTRounds = 0;
        public    int   InitedPTRounds => _InitedPTRounds;

                                            /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/IsInited1/*' />
        public bool IsInited1 => isInited1; /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/IsInited2/*' />
        public bool IsInited2 => isInited2;
                                            /// <include file='Documentation/VinKekFish_k1_base_20210419.xml' path='docs/members[@name="VinKekFish_k1_base_20210419"]/AllocHGlobal_allocator/*' />
        public static readonly AllocatorForUnsafeMemoryInterface AllocHGlobal_allocator = new AllocHGlobal_AllocatorForUnsafeMemory();


        protected volatile bool isHaveOutputData = false;
        public             bool IsHaveOutputData => isHaveOutputData;

        public VinKekFish_k1_base_20210419()
        {
        }


        /// <summary>Первичная инициализация: генерация таблиц перестановок (перед началом вызывает Clear)</summary>
        /// <param name="RoundsForTables">Количество раундов, под которое генерируются таблицы перестановок</param>
        /// <param name="additionalKeyForTables">Дополнительный ключ: это ключ для таблиц перестановок</param>
        /// <param name="OpenInitVectorForTables">Дополнительный вектор инициализации для перестановок (используется совместно с ключом)</param>
        /// <param name="PreRoundsForTranspose">Количество раундов со стандартными таблицами transpose &lt; (не менее 1)</param>
        public virtual void Init1(int RoundsForTables, byte * additionalKeyForTables = null, nint additionalKeyForTables_length = 0, byte * OpenInitVectorForTables = null, nint OpenInitVectorForTables_length = 0, int PreRoundsForTranspose = 8)
        {
            Clear();
            GC.Collect();

            // Место на
            // Криптографическое состояние
            // Копию криптографического состояния
            // 4 tweak (основной и запасные)
            // new byte[CryptoStateLenWithExtension * 2 + CryptoTweakLen * 4];
            // место для вспомогательных матриц c и b
            stateHandle = AllocHGlobal_allocator.AllocMemory(CryptoStateLenWithExtension * 2 + CryptoTweakLen * 4 + cryptoprime.KeccakPrime.b_size + cryptoprime.KeccakPrime.c_size);
            stateHandle.Clear();

            // При изменении не забыть обнулить указатели в ClearState()
            _state  = stateHandle.NoCopyClone(CryptoStateLenWithExtension);
            _state2 = _state  & CryptoStateLenWithExtension; // Это перегруженная операция, _state2 идёт за массивом _state и имеет длину CryptoStateLenWithExtension
            t0      = _state2 & CryptoTweakLen;
            t1      = t0      & CryptoTweakLen;
            t2      = t1      & CryptoTweakLen;
            var tmp = t2      & CryptoTweakLen;     // Это поле не используется; оно для выравнивания на 64-х байтную линию кеша
            _b      = tmp     & cryptoprime.KeccakPrime.b_size;
            _c      = _b      & cryptoprime.KeccakPrime.c_size;

            // Проверяем, что мы верно заполнили массив:
            // конец всех массивов-вхождений должен совпадать с концом массива-контейнера
            // Т.к. массив выравнен до кратного 64-рём размера, то массив может не совпадать на величину до 64-х байтов
            var ctrl  = _c & 0;
            var ctrll = ctrl.array - stateHandle.array - stateHandle.len;
            if (ctrll != 0)
                throw new Exception($"VinKekFish_k1_base_20210419.Init1: ctrll != 0 ({ctrll})");

            // Проверяем, что _b выравнен по линии кеша
            nint tmpb = (nint) _b.array;
            tmpb &= 63;
            if (tmpb != 0)
                throw new Exception($"VinKekFish_k1_base_20210419.Init1: fatal error: tmpb != 0 ({tmpb}, {((nint)stateHandle.array&63)})");


            _RTables        = RoundsForTables;
            pTablesHandle   = GenStandardPermutationTables(Rounds: _RTables, key: additionalKeyForTables, key_length: additionalKeyForTables_length, OpenInitVector: OpenInitVectorForTables, OpenInitVector_length: OpenInitVectorForTables_length, PreRoundsForTranspose: PreRoundsForTranspose);
            _InitedPTRounds = RoundsForTables;

            GC.Collect();
            GC.WaitForPendingFinalizers();  // Это чтобы сразу получить все проблемные вызовы, связанные с утечками памяти
            isInited1 = true;

            #if SUPER_CHECK_PERMUTATIONS
            vinkekfish.VinKekFish_k1_base_20210419.CheckAllPermutationTables(pTablesHandle, RoundsForTables, CryptoStateLen, "after init1");
            #endif
        }

        /// <summary>Вторая инициализация: ввод ключа и ОВИ, обнуление состояния и т.п.</summary>
        /// <param name="key">Основной ключ. Если null, то должен быть установлен флаг IsEmptyKey</param>
        /// <param name="OpenInitVector">Основной вектор инициализации, может быть null</param>
        /// <param name="Rounds">Количество раундов при шифровании первого блока ключа (рекомендуется 16-64)</param>
        /// <param name="RoundsForEnd">Количество раундов при широфвании последующих блоков ключа (допустимо 4)</param>
        /// <param name="RoundsForExtendedKey">Количество раундов отбоя ключа (рекомендуется NORMAL_ROUNDS = 64)</param>
        /// <param name="IsEmptyKey">Если key == null, то флаг должен быть установлен. Криптографическое преобразование тогда выполняться не будет</param>
        public virtual void Init2(byte * key, nint key_length, byte[]? OpenInitVector = null, int Rounds = NORMAL_ROUNDS, int RoundsForEnd = NORMAL_ROUNDS, int RoundsForExtendedKey = REDUCED_ROUNDS, bool IsEmptyKey = false)
        {
            if (!isInited1)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419: Init1 must be executed before Init2");

            // В этой и вызываемых функциях требуется проверка на наличие ошибок в неверных параметрах
            ClearState();
            if (pTablesHandle == null)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419: Init1 must be executed before Init2 (pTables == null)");

            if (!IsEmptyKey || key != null)
            {
                if (Rounds > InitedPTRounds)
                    throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.Init2: Rounds > InitedPTRounds");
                if (RoundsForEnd > InitedPTRounds)
                    throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.Init2: RoundsForEnd > InitedPTRounds");
                if (RoundsForEnd > InitedPTRounds && key_length > MAX_SINGLE_KEY)
                    throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.Init2: RoundsForEnd > RoundsForExtendedKey && key_length > MAX_SINGLE_KEY");

                fixed (byte * oiv = OpenInitVector)
                {
                    InputKey
                    (
                        key: key, key_length: key_length, OIV: oiv, OpenInitVector == null ? 0 : (nint) OpenInitVector.LongLength,
                        state: _state!, state2: _state2!, b: _b!, c: _c!,
                        tweak: t0!, tweakTmp: t1!, tweakTmp2: t2!,
                        Initiated: false, SecondKey: false,
                        R: Rounds, RE: RoundsForEnd, RM: RoundsForExtendedKey, tablesForPermutations: pTablesHandle, transpose200_3200: _transpose200_3200!, transpose200_3200_8: _transpose200_3200_8!
                    );
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();  // Это чтобы сразу получить все проблемные вызовы, связанные с утечками памяти
            isInited2        = true;
            isHaveOutputData = true;

            #if SUPER_CHECK_PERMUTATIONS
            vinkekfish.VinKekFish_k1_base_20210419.CheckAllPermutationTables(pTablesHandle, RTables, CryptoStateLen, "after init2");
            #endif
        }

        /// <summary>Очистка всех данных, включая таблицы перестановок. Использовать после окончания использования объекта (либо использовать Dispose)</summary>
        public void Clear()
        {
            isInited1       = false;
            _InitedPTRounds = 0;

            ClearState();

            pTablesHandle        ?.Dispose();
            _transpose200_3200   ?.Dispose();
            _transpose200_3200_8 ?.Dispose();

            stateHandle?.Dispose();
            stateHandle = null;
            _state      = null;
            _state2     = null;
            t0          = null;
            t1          = null;
            t2          = null;
            _b          = null;
            _c          = null;

            _RTables             = 0;
            pTablesHandle        = null;
            _transpose200_3200   = null;
            _transpose200_3200_8 = null;

            GC.Collect();
        }

        /// <summary>Обнуляет состояние без перезаписи таблиц перестановок. Использовать после окончания шифрования, если нужно использовать объект повторно с другим ключом</summary>
        public void ClearState()
        {
            isInited2        = false;
            isHaveOutputData = false;

            // Здесь обнуление состояния
            stateHandle?.Clear();
        }

        
        // Эти вещи дублируются в VinKekFish-20210525/VinKekFishBase_KN_20210525_get_pt.cs

        /// <summary>Генерирует стандартную таблицу перестановок</summary>
        /// <param name="Rounds">Количество раундов, для которых идёт генерация. Для каждого раунда по 4-ре таблицы</param>
        /// <param name="key">Это вспомогательный ключ для генерации таблиц перестановок. Основной ключ вводить нельзя! Этот ключ не может быть ключом, вводимым в VinKekFish, см. описание VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов, где таблицы перестановок не генерируются от ключа, а идут стандартно transpose128_3200 и transpose200_3200</param>
        public static Record GenStandardPermutationTables(int Rounds, AllocatorForUnsafeMemoryInterface? allocator = null, byte * key = null, nint key_length = 0, byte * OpenInitVector = null, nint OpenInitVector_length = 0, int PreRoundsForTranspose = 8)
        {
            GenTables();

            if (PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.GenStandardPermutationTables: PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds");

            if (allocator == null)
                allocator = AllocHGlobal_allocator;

            using var prng = new Keccak_PRNG_20201128();

            if (key != null && key_length > 0)
            {
                if (OpenInitVector == null)
                    prng.InputKeyAndStep(key, key_length, null, 0);
                else
                {
                    prng.InputKeyAndStep(key, key_length, OpenInitVector, OpenInitVector_length);
                }
            }
            else
            if (OpenInitVector != null)
                throw new ArgumentException("key == null && OpenInitVector != null. Set OpenInitVector as key");

            const nint len1  = VinKekFishBase_etalonK1.CryptoStateLen;
            const nint len2  = len1 * sizeof(ushort);

            // На каждый раунд приходится по 4-ре таблицы
            var roundsCheck = 4 * Rounds;
            var result = allocator.AllocMemory(4 * Rounds * len2);
            var table1 = new ushort[len1];
            var table2 = new ushort[len1];

            for (int i = 0; i < table1.Length; i++)
            {
                table1[i] = (ushort) i;
                table2[i] = (ushort) (table1.Length - i - 1);
            }

            fixed (ushort * Table1 = table1, Table2 = table2)
            {
                ushort * R = result;
                byte   * r = result;

                for (; PreRoundsForTranspose > 0 && Rounds > 0; Rounds--, PreRoundsForTranspose--)
                {
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200_3200_8, r); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128_3200  , r); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200_3200  , r); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128_3200  , r); r += len2;
                }
// TODO: Сколько можно ввести дополнительной рандомизирующей информации, чтобы она вводилась при перестановках от раунда к раунду
                for (; Rounds > 0; Rounds--)
                {
                    prng.doRandomPermutationForUShorts(table1);
                    prng.doRandomPermutationForUShorts(table2);

                    BytesBuilder.CopyTo(len2, len2, (byte*)Table1,              r); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte*)Table2,              r); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose200_3200  , r); r += len2;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose128_3200  , r); r += len2;
                }

                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table1);
                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table2);
            }

            #if SUPER_CHECK_PERMUTATIONS
            vinkekfish.VinKekFish_k1_base_20210419.CheckAllPermutationTables(result, roundsCheck, CryptoStateLen, "GenStandardPermutationTables");
            #endif

            return result;
        }

//#if DEBUG
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

        public static void CheckAllPermutationTables(ushort* table, nint countOfTables, nint Length, string message = "")
        {
            for (int i = 0; i < countOfTables; i++, table += Length)
            {
                CheckPermutationTable(table, Length, $"(table number: {i}). " + message);
            }
        }
//#endif

        /// <summary>Выполняет один шаг криптографического преобразования. Это сокращённый вызов step без подготовки tweak. Не использовать напрямую</summary>
        /// <param name="CountOfRounds">Количество раундов</param>
        public void DoStep(int CountOfRounds)
        {
            if (!isInited1 || !isInited2)
                throw new Exception("VinKekFish_k1_base_20210419.DoStep: !isInited1 || !isInited2");

            if (CountOfRounds > _InitedPTRounds)
                throw new Exception("VinKekFish_k1_base_20210419.DoStep: CountOfRounds > _InitedPTRounds");

            step
            (
                countOfRounds: CountOfRounds, tablesForPermutations: pTablesHandle!,
                tweak: t0!, tweakTmp: t1!, tweakTmp2: t2!, state: _state!, state2: _state!, b: _b!, c: _c!
            );

            isHaveOutputData = true;
        }

        /// <summary>Получает из криптографического состояния вывод</summary>
        /// <param name="output">Массив для получения вывода</param>
        /// <param name="start">Индекс в массиве output, с которого надо начинать запись</param>
        /// <param name="outputLen">Длина массива output</param>
        /// <param name="countToOutput">Количество байтов, которое нужно изъять из массива</param>
        public virtual void outputData(byte * output, nint start, nint outputLen, nint countToOutput)
        {
            if (!isHaveOutputData)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.outputData: !isHaveOutputData");

            if (countToOutput > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.outputData: lenToOutput > BLOCK_SIZE");

            if (start + countToOutput > outputLen)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.outputData: start + lenToOutput > len");

            BytesBuilder.CopyTo(countToOutput, outputLen, _state!, output, start);
            isHaveOutputData = false;
        }

        /// <summary>Уничтожает объект: реализация IDisposable</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Очищает объект</summary>
        /// <param name="disposing"><see langword="true"/> при всех вызовах, исключая деструктор</param>
        public virtual void Dispose(bool disposing)
        {
            Clear();

            if (!disposing)
                throw new Exception("VinKekFish_k1_base_20210419.Dispose: ~VinKekFish_k1_base_20210419 executed");
        }

        ~VinKekFish_k1_base_20210419()
        {
            Dispose(false);
        }
    }
}
