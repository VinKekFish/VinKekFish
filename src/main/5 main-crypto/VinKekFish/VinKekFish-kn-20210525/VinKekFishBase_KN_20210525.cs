// TODO: tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using cryptoprime;
using cryptoprime.VinKekFish;
using static VinKekFish_Utils.Utils;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    // code::docs:m0vbJGmf34Sx5nKnLnpz:
    /// <summary>Основная реализация VinKekFish - необходимо использовать именно эту реализацию. Многопоточный вариант для любых разрешённых K. Рекомендуется использовать на одном потоке, т.к. производительность растёт очень слабо с увеличением потоков.
    /// Описание реализации искать по шаблону docs::docs:m0vbJGmf34Sx5nKnLnpz:</summary>
    /// <remarks>IsDisposed == true означает, что объект более не пригоден для использования.</remarks>
    /// <remarks>Обязательная инициализация вызовом Init1 и Init2</remarks>
    /// <remarks>При работе в разных потоках с одним экземпляром объекта использовать для синхронизации отдельно созданный объект либо lock (this). В некоторых случаях, сигналы можно получать через sync</remarks>
    /// <remarks>См.также GetDataFromVinKekFishSponge</remarks>
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        /// <summary>Здесь содержатся таблицы перестановок, длина CountOfRounds*4*Len*ushort</summary>
        protected volatile Record?  tablesForPermutations = null;

        /// <summary>Аллокатор для выделения памяти внутри объекта</summary>
        public readonly BytesBuilderForPointers.IAllocatorForUnsafeMemoryInterface allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();

        /// <summary>Эти значения содержат записи для State1, State2, Tweaks</summary>
        protected Record  rState1, rState2, rTweaks;
                                                                            /// <summary>Криптографическое состояние 1. Всегда в начале общего массива. Может быть неактуальным. Для получения состояния нужно использовать st1</summary>
        protected byte *  State1 = null;                                    /// <summary>Криптографическое состояние 2</summary>
        protected byte *  State2 = null;
                                                                            /// <summary>Массив tweak: тут только пара значений</summary>
        protected ulong * Tweaks = null;
                                                                            /// <summary>Длина массива Tweaks в байтах</summary>
        public readonly int TweaksArrayLen = 0;                             /// <summary>Количество tweak на один блок ThreeFish</summary>
        public const    int CountOfTweaks  = 8;                             /// <summary>Длина одного блока массива Matrix в байтах (для выделения в стеке для keccak)</summary>
        public const    int MatrixLen      = 256;

                                                                            /// <summary>Максимальное количество раундов, для которого инициализированны таблицы перестановок</summary>
        public readonly int CountOfRounds  = 0;                             /// <summary>Коэффициент размера K</summary>
        public readonly int K              = 1;                             /// <summary>Количество заключительных пар перестановок в завершающем преобразовании (2 => 4*keccak, 3 => 6*keccak)</summary>
        public readonly int CountOfFinal   = Int32.MaxValue;
                                                                            /// <summary>Размер одного криптографического состояния в байтах, включая продление для расширения ключа ThreeFish для последнего блока</summary>
        public readonly int FullLen        = 0;                             /// <summary>Размер одного криптографического состояния в байтах (логический, без продления состояния для расширения ключа ThreeFish для последнего блока)</summary>
        public readonly int Len            = 0;                             /// <summary>Размер криптографического состояния в блоках ThreeFish</summary>
        public readonly int LenInThreeFish = 0;                             /// <summary>Размер криптографического состояния в блока Keccak</summary>
        public readonly int LenInKeccak    = 0;
                                                                            /// <summary>Размер криптографического состояния, поделенного между потоками, в байтах. Последний блок может быть большей длины</summary>
        public readonly int LenThreadBlock   = 0;                           /// <summary>Количество блоков перестановки для потоков (размер в блоках длиной LenThreadBlock)</summary>
        public readonly int LenInThreadBlock = 0;
                                                                            /// <summary>Максимальная длина ОВИ (открытого вектора инициализации)</summary>
        public readonly int MAX_OIV_K;                                      /// <summary>Максимальная длина первого блока ключа (это максимально рекомендуемая длина, но можно вводить больше)</summary>
        public readonly int MAX_SINGLE_KEY_K;                               /// <summary>Длина блока ввода/вывода</summary>
        public readonly int BLOCK_SIZE_K;                                   /// <summary>Длина блока ввода/вывода при генерации ключевой информации (сниженная длина)</summary>
        public readonly int BLOCK_SIZE_KEY_K;
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
        public bool IsState1Main
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
        protected readonly int[] NumbersOfThreeFishBlocks;                      
        //                                                                               /// <summary>Таймер чтения вхолостую. Может быть <see langword="null"/>.</summary>
        // protected readonly Timer? Timer                    = null; // Таймер нужно подвергнуть Dispose

        int ThreadCount;
        /// <summary>Создаёт и первично инициализирует объект VinKekFish (инициализация ключём и ОВИ должна быть отдельно). Создаёт Environment.ProcessorCount потоков для объекта. После конструктора необходимо вызвать init1 и init2</summary>
        /// <param name="CountOfRounds">Максимальное количество раундов шифрования, которое будет использовано, не менее VinKekFishBase_etalonK1.MIN_ROUNDS</param>
        /// <param name="K">Коэффициент размера K. Только нечётное число. Подробности смотреть в VinKekFish.md</param>
        /// <param name="ThreadCount">Количество потоков. Может быть 0 (Environment.ProcessorCount). Рекомендуется значение 1, т.к. при большем количестве потоков рост производительности незначительный</param>
        public VinKekFishBase_KN_20210525(int CountOfRounds = -1, int K = 1, int ThreadCount = 0)
        {
            cryptoprime.BytesBuilderForPointers.Record.DoRegisterDestructor(this);

            BLOCK_SIZE_K     = CalcBlockSize      (K);
            BLOCK_SIZE_KEY_K = CalcBlockSizeForKey(K);
            MAX_OIV_K        = K * MAX_OIV;
            MAX_SINGLE_KEY_K = K * MAX_SINGLE_KEY;

            static int ce(double x) => (int) Math.Ceiling(x);

            // var kr = (K - 1) >> 1;
            // Рассчитываем константы для рекомендуемого количества раундов
            MIN_ABSORPTION_ROUNDS_D_K = ce(Math.Log2(K + 1));
            MIN_ABSORPTION_ROUNDS_K   = ce(K * 1.337 - 0.328);
            MIN_ROUNDS_K              = Calc_MIN_ROUNDS_K    (K);
            REDUCED_ROUNDS_K          = Calc_REDUCED_ROUNDS_K(K);
            NORMAL_ROUNDS_K           = Calc_NORMAL_ROUNDS_K (K);
            EXTRA_ROUNDS_K            = Calc_EXTRA_ROUNDS_K  (K);
            MAX_ROUNDS_K              = Calc_MAX_ROUNDS_K    (K);

            if (ThreadCount == 0)
            {
                ThreadCount = Environment.ProcessorCount;
                if (ThreadCount > K)
                    ThreadCount = K;
            }

            this.ThreadCount = ThreadCount;

            if (CountOfRounds < 0)
                CountOfRounds = MAX_ROUNDS_K;

            if (CountOfRounds < MIN_ROUNDS_K)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: CountOfRounds < MIN_ROUNDS_K");
            if (K < 1 || K > 19)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: K < 1 || K > 19. Read VinKekFish.md");
            if ((K & 1) == 0)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: (K & 1) == 0. Read VinKekFish.md");

            this.CountOfRounds = CountOfRounds;
            this.K         = K;
            FullLen        = K * CryptoStateLen + CryptoStateLenExtension;
            FullLen        = (int)CalcAlignment(FullLen);
            Len            = K * CryptoStateLen;            // Этот размер всегда выравнен на значение, кратное 128-ми, и никогда - на значение, кратное 256-ти
            LenInThreeFish = Len / ThreeFishBlockLen;
            LenInKeccak    = Len / KeccakBlockLen;

            if ((Len & 127) > 0 || (Len & 255) == 0)
            {
                throw new Exception("VinKekFishBase_KN_20210525: Fatal algorithmic error. (Len & 127) > 0 || (Len & 255) == 0");
            }

            // Нам нужно 5 элементов, но мы делаем так, чтобы было кратно линии кеша
            TweaksArrayLen = CryptoTweakLen * 2; //CountOfTweaks * CryptoTweakLen * LenInThreeFish;
            TweaksArrayLen = (int)CalcAlignment(TweaksArrayLen);
            /*MatrixArrayLen = MatrixLen * LenInKeccak;
            MatrixArrayLen = calcAlignment(MatrixArrayLen);*/
            CountOfFinal = MIN_ABSORPTION_ROUNDS_D_K * 2;

            // Делаем перестановку в один поток, т.к. всё равно он сильно зависит от шины памяти и обращается к общей памяти. Хотя, в целом, это может быть и не так уж и оправдано
            LenInThreadBlock = 1;
            LenThreadBlock = Len;

            rState1 = allocator.AllocMemory(FullLen);
            rState2 = allocator.AllocMemory(FullLen);
            rTweaks = allocator.AllocMemory(TweaksArrayLen);
            State1 = rState1;
            State2 = rState2;
            Tweaks = rTweaks;
            ClearState();

            // ThreadsFunc_Current = ThreadFunction_empty; // Это уже сделано в ClearState
            /*            threads = new Thread[ThreadCount];

                        for (int i = 0; i < threads.Length; i++)
                            threads[i] = new Thread(ThreadsFunction);
            */

            NumbersOfThreeFishBlocks = new int[LenInThreeFish];
            var j = LenInThreeFish / 2;
            for (int i = 0; i < LenInThreeFish; i++)
            {
                NumbersOfThreeFishBlocks[i] = j++;
                if (j >= LenInThreeFish)
                    j = 0;
            }

            CheckNumbersOfThreeFishBlocks();
            /*
                        if (TimerIntervalMs > 0)
                        {
                            Timer = new Timer(WaitFunction!, period: TimerIntervalMs, dueTime: TimerIntervalMs, state: this);
                        }
            */
            IsState1Main = true;
        }

        public static int CalcBlockSize(int K)
        {
            return K * BLOCK_SIZE;
        }

        public static int CalcBlockSizeForKey(int K)
        {
            return (int) Math.Floor(CalcBlockSize(K) / (2.0 * Math.Log2(8 * K) + 1.0));
        }

        public static int Calc_MIN_ROUNDS_K(int K)
        {
            static int ce(double x) => (int) Math.Ceiling(x);

            var MIN_ROUNDS_K = ce(K * 2.674);
            if (MIN_ROUNDS_K < ce(4.0 * Math.Log2(K + 1)))
                MIN_ROUNDS_K = ce(4.0 * Math.Log2(K + 1));

            return MIN_ROUNDS_K;
        }

        public static int Calc_REDUCED_ROUNDS_K(int K)
        {
            static int ce (double x) => (int) Math.Ceiling(x);

            return ce(K * 6.168);
        }

        public static int Calc_NORMAL_ROUNDS_K(int K)
        {
            static int ce (double x) => (int) Math.Ceiling(x);

            return ce(K * 6.168 * 1.5);
        }

        public static int Calc_EXTRA_ROUNDS_K(int K)
        {
            static int ce (double x) => (int)Math.Ceiling(x);

            return ce(K * 25.0);
        }

        public static int Calc_MAX_ROUNDS_K(int K)
        {
            static int ce (double x) => (int)Math.Ceiling(x);

            return ce(K * 25.0 * (2 * Math.Log2(K + 1) + 2));
        }

        /// <summary>Рассчитывает оптимальное количество случайных таблиц перестановок для VinKekFish</summary>
        /// <param name="key_length">Длина ключа для инициализации перестановок в байтах</param>
        public int Calc_OptimalRandomPermutationCount(nint key_length)
        {
            return Calc_OptimalRandomPermutationCount_static(K, key_length);
        }

        /// <summary>Рассчитывает оптимальное количество случайных таблиц перестановок для VinKekFish</summary>
        /// <param name="K">Параметр K (множитель стойкости и размера шифра)</param>
        /// <param name="key_length">Длина ключа для инициализации перестановок в байтах</param>
        public static int Calc_OptimalRandomPermutationCount_static(int K, nint key_length)
        {
            var k   = Calc_OptimalRandomPermutationCountK  (K);
            var key = Calc_OptimalRandomPermutationCountKey(key_length);

            if (k < key)
                return K;

            return key;
        }

        // В целом, приблизительно мы считаем, что в одной таблице перестановок может быть зашито примерно столько битов, сколько у нас длина ключа, или более
        /// <summary>Рассчитывает оптимальное количество случайных таблиц перестановок для VinKekFish, исходя из значения K (можно больше, но инициализация может затянуться).<para>Количество раундов со случайными перестановками рассчитывается исходя из минимального значения Calc_OptimalRandomPermutationCountK и Calc_OptimalRandomPermutationCountKey</para></summary>
        /// <param name="K">Параметр K (множитель стойкости и размера шифра)</param>
        public static int Calc_OptimalRandomPermutationCountK(int K)
        {
            return K*1024*8*2/(1344+1720);
        }

        /// <summary>Рассчитывает оптимальное количество случайных таблиц перестановок для VinKekFish, исходя из длины вводимого для перестановок ключа (можно больше, но инициализация может затянуться)</summary>
        /// <param name="key_length">Длина ключа для инициализации перестановок в байтах</param>
        public static int Calc_OptimalRandomPermutationCountKey(nint key_length)
        {
            return (int) Math.Ceiling(  key_length*8.0*2.0/(1344.0+1720.0)  );
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

        /// <summary>Очистить всё состояние (кроме таблиц перестановок) (сброс до состояния после Init1)</summary>
        public virtual void ClearState()
        {
            lock (this)
            lock (sync)
            {
                isInit2 = false;

                rState1?.Clear();
                rState2?.Clear();
                rTweaks?.Clear();

                output?.Clear();
                isHaveOutputData = false;
                isDataInputed    = false;

                entireCountOfRoundHasBeen = 0;
            }
        }

        /// <summary>Очистить вспомогательные массивы, включая второе состояние. Первичное состояние не очищается: объект остаётся инициализированным</summary>
        /// <remarks>Имеет смысл вызывать только после завершения блока вычислений, если новый будет не скоро, чтобы побыстрее очистить вспомогательные данные и они бы никуда не попали для криптоанализа.</remarks>
        public virtual void ClearSecondaryStates()
        {// TODO: В тестах проверить, что два шага алгоритма подряд без очистки равны двум шагам с очисткой между ними
            BytesBuilder.ToNull(targetLength: FullLen, st2);
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
        protected virtual void Dispose(bool fromDispose = true)
        {
            var id = isDisposed;
            if (isDisposed)
            {
                var msg = "VinKekFishBase_KN_20210525: Dispose executed twiced";
                if (fromDispose)
                {
                    Record.ErrorsInDispose = true;

                    if (Record.doExceptionOnDisposeTwiced)
                    {
                        throw new Exception(msg);
                    }
                    else
                    {
                        Console.Error.WriteLine(msg);
                    }
                }

                return;
            }

            lock (this)
            {
                Clear();    // tablesForPermutations очищаются здесь

                if (input is not null && input.Count > 0)
                {
                    Record.ErrorsInDispose = true;
                    var iemsg = "VinKekFishBase_KN_20210525.Dispose: input.Count > 0 in Dispose (data to input has not been processed)";

                    if (Record.doExceptionOnDisposeInDestructor)
                        throw new Exception(iemsg);
                    else
                        Console.Error.WriteLine(iemsg);
                }

                TryToDispose(output);
                TryToDispose(input);
                TryToDispose(inputRecord);
                TryToDispose(rState1);
                TryToDispose(rState2);
                TryToDispose(rTweaks);

                output      = null;
                input       = null;
                inputRecord = null;

                isDisposed = true;
            }

            if (!id)
            if (!fromDispose)
            {
                Record.ErrorsInDispose = true;

                var emsg = "VinKekFishBase_KN_20210525.Dispose: you must call Dispose() after use";
                if (Record.doExceptionOnDisposeInDestructor)
                    throw new Exception(emsg);
                else
                    Console.Error.WriteLine(emsg);
            }
        }
                                                                                            /// <summary></summary>
        ~VinKekFishBase_KN_20210525()
        {
            Dispose(false);
        }

        /// <summary>Функция для отладки. Выполняет сравнение криптографического состояния с заданным с помощью VinKekFish_Utils.Utils.SecureCompareFast</summary>
        /// <param name="state">Массив для сравнения</param>
        /// <returns>true, если состояния равны</returns>
        public bool Debug_compareState(Record state)
        {
            return VinKekFish_Utils.Utils.SecureCompareFast(Len, state.len, st1, state);
        }
    }
}
