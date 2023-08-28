// TODO: tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using cryptoprime;
using cryptoprime.VinKekFish;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    /// <summary>Основная реализация VinKekFish. Создаёт потоки внутри объекта для многопоточной обработки</summary>
    /// <remarks>IsDisposed == true означает, что объект более не пригоден для использования.</remarks>
    /// <remarks>Обязательная инициализация вызовом Init1 и Init2</remarks>
    /// <remarks>При работе в разных потоках с одним экземпляром объекта использовать для синхронизации отдельно созданный объект либо lock (this). В некоторых случаях, сигналы можно получать через sync</remarks>
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        /// <summary>Здесь содержится два состояния, 4 твика на каждый блок TreeFish, матрицы c и b на каждый блок keccak. Матрицы c и b выровнены на 64 байта</summary>
        protected readonly Record   States;
        /// <summary>Здесь содержатся таблицы перестановок, длина CountOfRounds*4*Len*ushort</summary>
        protected volatile Record?  tablesForPermutations = null;

        /// <summary>Аллокатор для выделения памяти внутри объекта</summary>
        public readonly BytesBuilderForPointers.AllocatorForUnsafeMemoryInterface allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();

                                                                            /// <summary>Криптографическое состояние 1. Всегда в начале общего массива</summary>
        protected byte *  State1 => States!;                                /// <summary>Криптографическое состояние 2</summary>
        protected byte *  State2 => State1 + Len;
                                                                            /// <summary>Массив матриц b и c на каждый блок Keccak</summary>
        protected byte *  Matrix => State2 + Len;                           /// <summary>Массив tweak - по 4 tweak на каждый блок ThreeFish</summary>
        protected ulong * Tweaks => (ulong *) (Matrix + MatrixArrayLen);
                                                                            /// <summary>Длина массива Tweaks в байтах</summary>
        public readonly int TweaksArrayLen = 0;                             /// <summary>Количество tweak на один блок ThreeFish</summary>
        public const    int CountOfTweaks  = 8;                             /// <summary>Длина массива Matrix в байтах</summary>
        public readonly int MatrixArrayLen = 0;                             /// <summary>Длина одного блока массива Matrix в байтах</summary>
        public const    int MatrixLen      = 256;

                                                                            /// <summary>Максимальное количество раундов</summary>
        public readonly int CountOfRounds  = 0;                             /// <summary>Коэффициент размера K</summary>
        public readonly int K              = 1;                             /// <summary>Количество заключительных пар перестановок в завершающем преобразовании (2 => 4*keccak, 3 => 6*keccak)</summary>
        public readonly int CountOfFinal   = Int32.MaxValue;
                                                                            /// <summary>Размер одного криптографического состояния в байтах</summary>
        public readonly int Len            = 0;                             /// <summary>Размер криптографического состояния в блоках ThreeFish</summary>
        public readonly int LenInThreeFish = 0;                             /// <summary>Размер криптографического состояния в блока Keccak</summary>
        public readonly int LenInKeccak    = 0;
                                                                            /// <summary>Размер криптографического состояния, поделенного между потоками, в байтах. Последний блок может быть большей длины</summary>
        public readonly int LenThreadBlock   = 0;                           /// <summary>Количество блоков перестановки для потоков (размер в блоках длиной LenThreadBlock)</summary>
        public readonly int LenInThreadBlock = 0;                           /// <summary>Количество потоков</summary>
        public readonly int ThreadCount      = 0;
                                                                            /// <summary>Максимальная длина ОВИ (открытого вектора инициализации)</summary>
        public readonly int MAX_OIV_K;                                      /// <summary>Максимальная длина первого блока ключа (это максимально рекомендуемая длина, но можно вводить больше)</summary>
        public readonly int MAX_SINGLE_KEY_K;                               /// <summary>Длина блока ввода/вывода</summary>
        public readonly int BLOCK_SIZE_K;
                                                                            /// <summary>Минимальное количество раундов для поглощения без выдачи выходных данных, для установленного K. Нестойкое значение: обеспечивается диффузия, но криптостойкость может быть недостаточной</summary>
        public readonly int MIN_ABSORPTION_ROUNDS_D_K;                                                                    /// <summary>Минимальное количество раундов для поглощения без выдачи выходных данных, для установленного K</summary>
        public readonly int MIN_ABSORPTION_ROUNDS_K;                        /// <summary>Минимальное количество раундов с выдачей выходных данных, для установленного K</summary>
        public readonly int MIN_ROUNDS_K;                                   /// <summary>Нормальное количество раундов, для установленного K</summary>
        public readonly int NORMAL_ROUNDS_K;                                /// <summary>Уменьшенное количество раундов, для установленного K</summary>
        public readonly int REDUCED_ROUNDS_K;                               /// <summary>Усиленное количество раундов</summary>
        public readonly int EXTRA_ROUNDS_K;                                 /// <summary>Максимально рекомендуемое количество раундов (выше почти бессмысленно)</summary>
        public readonly int MAX_ROUNDS_K;

        /// <summary>Вспомогательные переменные, показывающие, какие состояния сейчас являются целевыми. Изменяются в алгоритме (st2 - вспомогательное/дополнительное; st1 - основное состояние, содержащее актуальную криптографическую информацию)</summary>
        protected volatile byte * st1 = null, st2 = null, st3 = null;
        /// <summary>Устанавливает st1 и st2 на нужные состояния. Если true, то st1 = State1, иначе st1 = State2. State1Main ^= true - переключение состояний между основным и вспомогательным</summary>
        public bool State1Main
        {
            get => st1 == State1;
            set
            {
                if (value)
                {
                    st1 = State1;
                    st2 = State2;
                }
                else
                {
                    st1 = State2;
                    st2 = State1;
                }
            }
        }

        /// <summary>Массив, устанавливающий номера ключевых блоков TreeFish для каждого трансформируемого блока</summary>
        protected readonly int[]? NumbersOfThreeFishBlocks = null;                           /// <summary>Таймер чтения вхолостую. Может быть <see langword="null"/>.</summary>
        protected readonly Timer? Timer                    = null;

        /// <summary>Создаёт и первично инициализирует объект VinKekFish (инициализация ключём и ОВИ должна быть отдельно). Создаёт Environment.ProcessorCount потоков для объекта</summary>
        /// <param name="CountOfRounds">Максимальное количество раундов шифрования, которое будет использовано, не менее VinKekFishBase_etalonK1.MIN_ROUNDS</param>
        /// <param name="K">Коэффициент размера K. Только нечётное число. Подробности смотреть в VinKekFish.md</param>
        /// <param name="ThreadCount">Количество создаваемых потоков. Рекомендуется использовать значение по-умолчанию: 0 (0 == Environment.ProcessorCount)</param>
        /// <param name="TimerIntervalMs">Интервал таймера холостого чтения. Если нет желания использовать таймер, поставьте Timeout.Infinite или любое отрицательное число</param>
        public VinKekFishBase_KN_20210525(int CountOfRounds = -1, int K = 1, int ThreadCount = 0, int TimerIntervalMs = 500)
        {
            BLOCK_SIZE_K     = K * BLOCK_SIZE;
            MAX_OIV_K        = K * MAX_OIV;
            MAX_SINGLE_KEY_K = K * MAX_SINGLE_KEY;

            var ce = (double x) => (int) Math.Ceiling( x );

            // var kr = (K - 1) >> 1;
            // Рассчитываем константы для рекомендуемого количества раундов
            MIN_ABSORPTION_ROUNDS_D_K = ce( Math.Log2(K+1) );
            MIN_ABSORPTION_ROUNDS_K   = ce( K*1.337 - 0.328 );
            MIN_ROUNDS_K              = ce( K*2.674 );
            if (MIN_ROUNDS_K < ce(  4.0*Math.Log2(K+1)  ))
                MIN_ROUNDS_K = ce(  4.0*Math.Log2(K+1)  );

            REDUCED_ROUNDS_K          = ce( K*6.168 );
            NORMAL_ROUNDS_K           = ce( K*6.168*1.5 );
            EXTRA_ROUNDS_K            = ce( K*25.0 );
            MAX_ROUNDS_K              = ce( K*25.0*(2*Math.Log2(K+1)+2) );


            if (CountOfRounds < 0)
                CountOfRounds = NORMAL_ROUNDS_K;

            if (CountOfRounds < MIN_ROUNDS_K)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: CountOfRounds < MIN_ROUNDS_K");
            if (K < 1 || K > 19)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: K < 1 || K > 19. Read VinKekFish.md");
            if ((K & 1) == 0)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: (K & 1) == 0. Read VinKekFish.md");

            if (ThreadCount <= 0)
                ThreadCount = Environment.ProcessorCount;

            this.ThreadCount            = ThreadCount;
            this.ThreadsInFunc          = ThreadCount;
            this.ThreadsExecutedForTask = ThreadCount;

            this.CountOfRounds = CountOfRounds;
            this.K             = K;
            Len                = K * CryptoStateLen;
            LenInThreeFish     = Len / ThreeFishBlockLen;
            LenInKeccak        = Len / KeccakBlockLen;

            // Нам нужно 5 элементов, но мы делаем так, чтобы было кратно линии кеша
            TweaksArrayLen = CountOfTweaks * CryptoTweakLen * LenInThreeFish;
            MatrixArrayLen = MatrixLen * LenInKeccak;
            CountOfFinal = K <= 11 ? 2 : 3;

            // Вообще говоря, больше 2-х потоков на перестановке может быть не оправдано, однако там всё сложно
            LenInThreadBlock = ThreadCount;
            LenThreadBlock   = Len / LenInThreadBlock;
            if (LenThreadBlock < 512 || Len % LenInThreadBlock > 0)
            {
                if (LenThreadBlock < 512)
                {
                    LenThreadBlock   = 512;
                    LenInThreadBlock = Len / LenThreadBlock;
                }

                // Пытаемся увеличить количество блоков так, чтобы минимальный блок был хотя бы 256, и размер был всегда кратный линии кеша и кратный длине состояния
                while (Len % LenInThreadBlock > 0 || LenThreadBlock % 64 > 0)
                {
                    if (LenThreadBlock <= 256)
                        break;

                    LenInThreadBlock++;
                    LenThreadBlock = Len / LenInThreadBlock;
                }

                if (Len % LenInThreadBlock > 0 || LenThreadBlock % 64 > 0)
                    throw new Exception("VinKekFishBase_KN_20210525: Fatal algorithmic error");
            }

            //                            Состояния      Твики            b и c
            States = allocator.AllocMemory(Len * 2 + TweaksArrayLen + MatrixArrayLen);
            ClearState();

            // ThreadsFunc_Current = ThreadFunction_empty; // Это уже сделано в ClearState
            threads = new Thread[ThreadCount];

            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(ThreadsFunction);


            NumbersOfThreeFishBlocks = new int[LenInThreeFish];
            var j = LenInThreeFish / 2;
            for (int i = 0; i < LenInThreeFish; i++)
            {
                NumbersOfThreeFishBlocks[i] = j++;
                if (j >= LenInThreeFish)
                    j = 0;
            }

            CheckNumbersOfThreeFishBlocks();

            if (TimerIntervalMs > 0)
            {
                Timer = new Timer(WaitFunction!, period: TimerIntervalMs, dueTime: TimerIntervalMs, state: this);
            }

            State1Main = true;
        }

        /// <summary>Проверка верности заполнения NumbersOfThreeFishBlocks</summary>
        protected void CheckNumbersOfThreeFishBlocks()
        {
            var nums = new int[LenInThreeFish];
            for (int i = 0; i < LenInThreeFish; i++)
                nums[i] = -1;

            int j = 0;
            for (int i = 0; i < LenInThreeFish; i++)
            {
                var k = NumbersOfThreeFishBlocks![j];
                if (nums[j] >= 0)
                    throw new Exception("VinKekFishBase_KN_20210525.CheckNumbersOfThreeFishBlocks: Fatal algorithmic error");

                nums[j] = k;
                j = k;
            }
        }

        /// <summary>Очистить всё состояние (кроме таблиц перестановок)</summary>
        public virtual void ClearState()
        {
            lock (this)
            lock (sync)
            {
                isInit2 = false;
                ThreadsFunc_Current = ThreadFunction_empty;
                BytesBuilder.ToNull(States!.len, States);
            }
        }

        /// <summary>Очистить вспомогательные массивы, включая второе состояние. Первичное состояние не очищается: объект остаётся инициализированным</summary>
        /// <remarks>Рекомендуется вызывать после завершения блока вычислений, если новый будет не скоро.</remarks>
        public virtual void ClearSecondaryStates()
        {// TODO: В тестах проверить, что два шага без очистки равны двум шагам с очисткой между ними
            BytesBuilder.ToNull(targetLength: Len,                             st2);
            BytesBuilder.ToNull(targetLength: MatrixArrayLen,                  Matrix);
            BytesBuilder.ToNull(targetLength: TweaksArrayLen - CryptoTweakLen, ((byte *) Tweaks) + CryptoTweakLen);
        }

        /// <summary>Очищает таблицы перестановок</summary>
        public virtual void ClearPermutationTables()
        {
            lock (this)
            {
                isInit1 = false;
                tablesForPermutations?.Dispose();
                tablesForPermutations = null;
            }
        }

        /// <summary>Полная очистка объекта без его освобождения. Допустимо повторное использование после инициализации</summary>
        public virtual void Clear()
        {
            ClearState();
            ClearPermutationTables();
        }

        /// <summary>Очищает объект и освобождает все выделенные под него ресурсы</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
                                                                            /// <summary>См. IsDisposed</summary>
        protected bool isDisposed =  false;                                 /// <summary>Если true, объект уничтожен и не пригоден к дальнейшему использованию</summary>
        public    bool IsDisposed => isDisposed;                            /// <summary>Очищает объект и освобождает все выделенные под него ресурсы</summary>
        protected virtual void Dispose(bool dispose = true)
        {
            IsEnded = true;
            // lock (sync) Monitor.PulseAll(sync);
            // Свойство IsEnded уже вызывает PulseAll

            if (isDisposed)
                return;

            waitForDoFunction();

            lock (this)
            {
                Clear();
                try     {  output?.Dispose(); input?.Dispose(); inputRecord?.Dispose(); Timer?.Dispose(); }
                finally {  States .Dispose();  }

                output      = null;
                input       = null;
                inputRecord = null;

                isDisposed = true;
            }

            if (!dispose)
                throw new Exception("VinKekFishBase_KN_20210525.Dispose: you must call Dispose() after use");
        }
                                                                                            /// <summary></summary>
        ~VinKekFishBase_KN_20210525()
        {
            Dispose(false);
        }
    }
}
