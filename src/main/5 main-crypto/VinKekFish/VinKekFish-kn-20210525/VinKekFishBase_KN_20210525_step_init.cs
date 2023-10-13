// TODO: tests

// #define DEBUG_OUTPUT

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
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {                                                           /// <summary>Устанавливается после выполнения шага криптографического преобразования. Означает, что есть данные в выходной части криптографического состояния</summary>
        protected bool isHaveOutputData = false;                /// <summary>Если true, то произведена первичная инициализация (сгенерированны таблицы перестановок)</summary>
        protected bool isInit1 = false;                         /// <summary>Если true, то произведена полная инициализация (введён ключ и ОВИ)</summary>
        protected bool isInit2 = false;

                                                                /// <summary>Если <see langword="true"/>, то выполнена инициализация 1 (сгенерированы таблицы перестановок)</summary>
        public bool IsInit1 => isInit1;                         /// <summary>Если <see langword="true"/>, то выполнена инициализация 2 (полная инициализация состояния)</summary>
        public bool IsInit2 => isInit2;
                                                                /// <summary>Перед шагом необходимо изменить tweak и state, даже если ничего не вводилось. Если false - при попытке выполнить шаг будет сгенерированно исключение. Устанавливается в функциях, изменяющих tweak от шага к шагу (InputData_Xor и InputData_Overwrite; InputData_ChangeTweakAndState).</summary>
        protected bool isDataInputed = false;


        /// <summary>Осуществляет непосредственный шаг алгоритма без ввода данных и изменения tweak</summary><remarks>Вызывайте эту функцию, если хотите переопределить поведение VinKekFish. В большинстве случаев стоит использовать doStepAndIO после Init2.</remarks>
        /// <param name="askedCountOfRounds">Количество раундов.</param>
        public void step(int askedCountOfRounds = -1)
        {
            if (!isDataInputed)
                throw new ArgumentException("VinKekFishBase_KN_20210525.step: !isDataInputed. Before step you must call InputData_Overwrite or InputData_Xor", "isDataInputed");

            isDataInputed = false;

            if (askedCountOfRounds > CountOfRounds)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.step: askedCountOfRounds > this.CountOfRounds");

            if (!isInit1)
                throw new Exception("VinKekFishBase_KN_20210525.step: you must call Init1 (and Init2 too?) before doing this");

            if (askedCountOfRounds < 0)
                askedCountOfRounds = this.CountOfRounds;

            var TB = (ushort *) tablesForPermutations!.array;
            if (!isState1Main)
                throw new Exception("VinKekFishBase_KN_20210525.step: Fatal algorithmic error: !State1Main (at start)");
            // State1Main = true;

            Tweaks[2+0] = Tweaks[0+0];
            Tweaks[2+1] = Tweaks[0+1];

            #if DEBUG_OUTPUT
            VinKekFish_Utils.Utils.MsgToFile($"round started {askedCountOfRounds}", "KN");
            VinKekFish_Utils.Utils.ArrayToFile((byte *) Tweaks, 16, "KN");
            #endif

            // Предварительное преобразование
            doPermutation(transpose128);
            doThreeFish();
            doPermutation(transpose128);

            BytesBuilder.CopyTo(Len, Len, State2, State1); isState1Main = true;

            // Основной шаг алгоритма: раунды
            // Каждая итерация цикла - это полураунд
            askedCountOfRounds <<= 1;
            for (int round = 0; round < askedCountOfRounds; round++)
            {
                #if DEBUG_OUTPUT
                VinKekFish_Utils.Utils.MsgToFile($"semiround {round}", "KN");
                #endif

                doKeccak();
                doPermutation(TB); TB += Len;

                doThreeFish();
                doPermutation(TB); TB += Len;

                // Довычисление tweakVal для второго преобразования VinKekFish
                // Вычисляем tweak для данного раунда (работаем со старшим 4-хбайтным словом младшего 8-мибайтного слова tweak)
                // Каждый раунд берёт +2 к старшему 4-хбайтовому слову: +1 - после первого полураунда, и +1 - после второго полураунда
                Tweaks[2+0] += 0x1_0000_0000U;  // Берём элемент [1], расположение tweak см. по метке :an6c5JhGzyOO
            }

            #if DEBUG_OUTPUT
            VinKekFish_Utils.Utils.MsgToFile($"final", "KN");
            #endif

            // После последнего раунда производится заключительное преобразование (заключительная рандомизация) поблочной функцией keccak-f
            for (int i = 0; i < CountOfFinal; i++)
            {
                doKeccak();
                doPermutation(transpose200);
                doKeccak();
                doPermutation(transpose200_8);
            }

            if (!isState1Main)
                throw new Exception("VinKekFishBase_KN_20210525.step: Fatal algorithmic error: !State1Main");

            isHaveOutputData = CountOfRounds >= MIN_ROUNDS_K;
        }

        /// <summary>Предварительная инициализация объекта. Осуществляет установку таблиц перестановок.</summary>
        /// <param name="PreRoundsForTranspose">Количество раундов, которое будет происходить со стандартными таблицами (не зависящими от ключа)</param>
        /// <param name="keyForPermutations">Дополнительный ключ: ключ для определения таблиц перестановок. Не должен зависеть от основного ключа<para>Пользователь должен обеспечить, чтобы при разглашении дополнительного ключа основной оставался бы неизвестным. Такой ключ можно добавить при инициализации к основному ключу (вводимому в саму губку) после основного ключа, но этот ключ, считается менее защищённым, чем основной</para><para>В зависимости от длины ключа вычисляется и стойкость генератора таблиц перестановок (но не более, чем удвоенная номинальная стойкость VinKekFish). Желательно, чтобы этот ключ каждую сессию шифрования был разный.</para></param>
        /// <param name="OpenInitVectorForPermutations">Дополнительный вектор инициализации</param>
        /// <param name="ThreeFishInitSteps">Количество раундов инициализации ключей ThreeFish для каскадной губки, инициализирующей случайные таблицы перестановок</param>
        public virtual void Init1(int PreRoundsForTranspose = 0, Record? keyForPermutations = null, Record? OpenInitVectorForPermutations = null, int ThreeFishInitSteps = 2)
        {
            if (!isState1Main)
                throw new Exception("VinKekFishBase_KN_20210525.Init1: Fatal algorithmic error: !State1Main");

            // Устанавливаем PreRoundsForTranspose по-умолчанию
            if (PreRoundsForTranspose <= 0)
                PreRoundsForTranspose = this.MIN_ABSORPTION_ROUNDS_D_K;

            lock (this)
            {
                Clear();
                tablesForPermutations = GenStandardPermutationTables(CountOfRounds, allocator, key: keyForPermutations, key_length: keyForPermutations == null ? 0 : keyForPermutations.len, OpenInitVector: OpenInitVectorForPermutations, OpenInitVector_length: OpenInitVectorForPermutations == null ? 0 : OpenInitVectorForPermutations.len, PreRoundsForTranspose: PreRoundsForTranspose, ThreeFishInitSteps: ThreeFishInitSteps);
                isInit1    = true;
            }

            // Console.WriteLine(VinKekFish_Utils.Utils.ArrayToHex(tablesForPermutations, Math.Min(tablesForPermutations.len, 256)));
        }
// TODO: посмотреть в тестах, что исходный tweak не изменяется
        /// <summary>Вторая инициализация (полная инициализация): инициализация внутреннего состояния ключём. Вызывается после Init1</summary>
        /// <param name="key">Основной ключ алгоритма</param>
        /// <param name="OpenInitializationVector">Основной ОВИ (открытый вектор инициализации), не более чем MAX_OIV_K байтов</param>
        /// <param name="TweakInit">Инициализатор Tweak (дополнительная синхропосылка), может быть null или инициализирован нулями</param>
        /// <param name="RoundsForFinal">Количество раундов отбоя после ввода ключи и ОВИ. Имеет смысл делать это количество раундов самым большим.</param>
        /// <param name="RoundsForFirstKeyBlock">Количество раундов преобразования первого блока ключа и ОВИ</param>
        /// <param name="RoundsForTailsBlock">Количество раундов преобразования иных блоков ключа, кроме первого блока</param>
        /// <param name="FinalOverwrite">Если true, то заключительный шаг впитывания ключа происходит с перезаписыванием нулями входного блока (есть дополнительная необратимость - это рекомендуется и это по умолчанию)</param>
        /// <param name="noInputKey">Если true, то инициализация ключом и синхропосылкой должна быть осуществлена пользователем вручную. key, TweakInit и OpenInitializationVector должны быть null</param>
        public virtual void Init2(Record? key = null, Record? OpenInitializationVector = null, Record? TweakInit = null, int RoundsForFinal = -1, int RoundsForFirstKeyBlock = -1, int RoundsForTailsBlock = -1, bool FinalOverwrite = true, bool noInputKey = false)
        {
            if (RoundsForFinal < 0)
            {
                RoundsForFinal = NORMAL_ROUNDS_K;
                if (RoundsForFinal > CountOfRounds)
                    RoundsForFinal = CountOfRounds;
            }

            if (RoundsForFirstKeyBlock < 0)
            {
                RoundsForFirstKeyBlock = NORMAL_ROUNDS_K;
                if (RoundsForFirstKeyBlock > CountOfRounds)
                    RoundsForFirstKeyBlock = CountOfRounds;
            }

            if (RoundsForTailsBlock < 0)
            {
                RoundsForTailsBlock = REDUCED_ROUNDS_K;
                if (RoundsForTailsBlock > CountOfRounds)
                    RoundsForTailsBlock = CountOfRounds;
            }

            if (!isState1Main)
                throw new Exception("VinKekFishBase_KN_20210525.Init2: Fatal algorithmic error: !State1Main");


            lock (this)
            {
                ClearState();       // Здесь должны быть обнулены и tweak
                StartThreads();

                if (noInputKey)
                {
                    if (key != null || TweakInit != null || OpenInitializationVector != null)
                        throw new ArgumentException("VinKekFishBase_KN_20210525.Init2: noInputKey is true but key != null || TweakInit != null || OpenInitializationVector != null");
                }
                else
                    InputKey(key: key, OpenInitializationVector: OpenInitializationVector, TweakInit: TweakInit, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock, RoundsForTailBlocks: RoundsForTailsBlock, FinalOverwrite: FinalOverwrite);

                output?.Clear();
                isInit2 = true;
            }
        }
                                                            /// <summary>Запускает все потоки</summary>
        protected virtual void StartThreads()
        {/*
            foreach (var t in threads!)
            {
                if (t.ThreadState != ThreadState.Running && t.ThreadState != ThreadState.WaitSleepJoin)
                    t.Start();
            }

            // Ожидаем запуска потоков
            lock (sync)
            while (ThreadsExecuted < ThreadCount && !isEnded)
            {
                Monitor.Wait(sync);
            }*/
        }

        /// <summary>Простая инициализация объекта без ключа для принятия энтропии в дальнейшем</summary>
        /// <param name="RandomInit">Если True, то tweak инициализируется текущим временем и GC.GetTotalAllocatedBytes и вычисляется его состояние. Если false, то состояние инициализируется нулями</param>
        public virtual void SimpleInit(bool RandomInit = true)
        {
            if (RandomInit)
            {
                using var tweak = allocator.AllocMemory(CryptoTweakLen);

                tweak.Clear();
                var tw = (long*) tweak.array;
                tw[0] = DateTime.Now.Ticks;
                tw[1] = GC.GetTotalAllocatedBytes();
                var r = tw[0] & tw[1] & 3;
                r++;

                Init1();
                Init2(TweakInit: tweak, RoundsForFirstKeyBlock: 0, RoundsForFinal: (int) r, FinalOverwrite: false);
            }
            else
            {
                Init1();
                Init2(RoundsForFirstKeyBlock: 0, RoundsForFinal: 0, FinalOverwrite: false);
            }
        }

        /// <summary>Функция ввода ключа. Используйте Init2 вместо этой функции. Эта функция вызывается в начале использования для инициализации. После начала использования она испортит губку.</summary>
        /// <param name="key">Основной ключ алгоритма</param>
        /// <param name="OpenInitializationVector">Основной ОВИ (открытый вектор инициализации), не более чем MAX_OIV_K байтов</param>
        /// <param name="TweakInit">Инициализатор Tweak (дополнительная синхропосылка), может быть null или инициализирован нулями</param>
        /// <param name="RoundsForFinal">Количество раундов отбоя после ввода ключи и ОВИ. Имеет смысл делать это количество раундов самым большим.</param>
        /// <param name="RoundsForFirstKeyBlock">Количество раундов преобразования первого блока ключа и ОВИ</param>
        /// <param name="RoundsForTailBlocks">Количество раундов преобразования иных блоков ключа, кроме первого блока</param>
        /// <param name="FinalOverwrite">Если true, то заключительный шаг впитывания ключа происходит с перезаписыванием нулями входного блока (есть дополнительная необратимость)</param>
        protected virtual void InputKey(Record? key = null, Record? OpenInitializationVector = null, Record? TweakInit = null, int RoundsForFinal = NORMAL_ROUNDS, int RoundsForFirstKeyBlock = NORMAL_ROUNDS, int RoundsForTailBlocks = MIN_ROUNDS, bool FinalOverwrite = true)
        {
            if (!isState1Main)
                throw new Exception("VinKekFishBase_KN_20210525.InputKey: Fatal algorithmic error: !State1Main");

            if (TweakInit != null && TweakInit.len < CryptoTweakLen)
                throw new ArgumentOutOfRangeException("TweakInit.len", $"VinKekFishBase_KN_20210525.InputKey: TweakInit.len < CryptoTweakLen ({TweakInit.len} < {CryptoTweakLen})");
            if (RoundsForFinal < MIN_ROUNDS_K)
                throw new ArgumentOutOfRangeException("RoundsForFinal", $"VinKekFishBase_KN_20210525.InputKey: RoundsForFinal < MIN_ROUNDS_K ({RoundsForFinal} < {MIN_ROUNDS_K})");
            if (RoundsForFirstKeyBlock < MIN_ABSORPTION_ROUNDS_D_K)
                throw new ArgumentOutOfRangeException("RoundsForFirstKeyBlock", $"VinKekFishBase_KN_20210525.InputKey: RoundsForFirstKeyBlock < MIN_ABSORPTION_ROUNDS_D_K ({RoundsForFirstKeyBlock} < {MIN_ABSORPTION_ROUNDS_D_K})");
            if (RoundsForTailBlocks < MIN_ABSORPTION_ROUNDS_D_K)
                throw new ArgumentOutOfRangeException("RoundsForTailsBlock", $"VinKekFishBase_KN_20210525.InputKey: RoundsForTailsBlock < MIN_ABSORPTION_ROUNDS_D_K ({RoundsForTailBlocks} < {MIN_ABSORPTION_ROUNDS_D_K})");

// TODO: проверить в тестах, что все инициализаторы действительно используются
            if (TweakInit != null)
            {
                // Tweaks уже очищено в init2, так что инициализируем только если нужно
                var T     = (ulong *) TweakInit;
                Tweaks[0] = T[0];
                Tweaks[1] = T[1];
            }

            Tweaks[0] += TWEAK_STEP_NUMBER;

            if (OpenInitializationVector != null)
            {
                if (OpenInitializationVector.len > MAX_OIV_K)
                    throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.InputKey: OpenInitializationVector > MAX_OIV");

                byte len1 = (byte)  OpenInitializationVector.len;
                byte len2 = (byte) (OpenInitializationVector.len >> 8);

                BytesBuilder.CopyTo(OpenInitializationVector.len, MAX_OIV_K, OpenInitializationVector, State1 + MAX_SINGLE_KEY_K + 2 + 2);
                State1[MAX_SINGLE_KEY_K + 2 + 0] ^= len1;
                State1[MAX_SINGLE_KEY_K + 2 + 1] ^= len2;
            }

            nint keyLen = key == null ? 0 : key.len;
            var dt      = keyLen;
            if (key != null)
            {
                if (dt > MAX_SINGLE_KEY_K)
                    dt = MAX_SINGLE_KEY_K;

                byte len1 = (byte) dt;
                byte len2 = (byte) (dt >> 8);
                State1[0] ^= len1;
                State1[1] ^= len2;
                Tweaks[1] += (ulong) keyLen;

                BytesBuilder.CopyTo(dt, MAX_SINGLE_KEY_K, key, State1 + 2);
                keyLen -= dt;
            }

            isDataInputed = true;
            step(RoundsForFirstKeyBlock);

            byte * TailOfKey = null;         // key + dt - это будет неверно! , см. перегрузку оператора "+" в Record
            if (key != null)
                TailOfKey = key.array + dt;

            while (keyLen > 0)
            {
                dt = keyLen;
                if (dt > BLOCK_SIZE_K)
                    dt = BLOCK_SIZE_K;

                InputData_Overwrite(TailOfKey, dt, regime: 0, nullPadding: false);

                keyLen    -= dt;
                TailOfKey += dt;

                step(RoundsForTailBlocks);
            }

            // После инициализации обнуляем часть данных для обеспечения необратимости
            if (FinalOverwrite)
            {
                InputData_Overwrite(null, 0, regime: 255, nullPadding: true);
            }
            else
                InputData_Xor(null, 0, regime: 255);

            step(RoundsForFinal);
        }

        /// <summary>Производит наложение на массив t массива s с помощью операции xor</summary>
        /// <param name="len">Длина массива s</param>
        /// <param name="s">Налагаемый массив (не изменяется)</param>
        /// <param name="t">Изменяемый массив</param>
        public static void XorWithBytes(long len, byte * s, byte * t)
        {
            var len8 = len >> 3;
                len -= len8 << 3;
            var s8   = (ulong*) s;
            var t8   = (ulong*) t;

            while (len8 > 0)
            {
                *t8 ^= *s8;
                t8++; s8++; len8--;
            }

            s = (byte *) s8;
            t = (byte *) t8;
            while (len > 0)
            {
                *t ^= *s;
                t++; s++; len--;
            }
        }

        /// <summary>Вводит данные путём перезаписывания внешней части криптографического состояния (вместо xor). Это режим работы OVERWRITE</summary>
        /// <param name="data">Данные для ввода, не более BLOCK_SIZE_K байтов</param>
        /// <param name="dataLen">Длина вводимых данных</param>
        /// <param name="regime">Режим шифрования (это определяемое пользователем байтовое поле, вводимое во внешную часть криптографического состояния)</param>
        /// <param name="nullPadding">Если true, то вся внешняя часть криптографического состояния будет перезаписана нулями, даже если данных не хватит для перезаписи всего внешнего состояния</param>
        public void InputData_Overwrite(byte * data, nint dataLen, byte regime, bool nullPadding = true)
        {
            if (dataLen > BLOCK_SIZE_K)
                throw new ArgumentOutOfRangeException("dataLen", "VinKekFishBase_KN_20210525.InputData_Overwrite: dataLen > BLOCK_SIZE_K");
            if (!isState1Main)
                throw new Exception("VinKekFishBase_KN_20210525.InputData_Overwrite: Fatal algorithmic error: !State1Main");

            if (nullPadding)
            {
                var paddingLen = BLOCK_SIZE_K - dataLen;
                if (paddingLen > 0)
                    BytesBuilder.ToNull(paddingLen, State1 + 3 + dataLen);
            }

            if (dataLen > 0)
            BytesBuilder.CopyTo(dataLen, Len, data, State1 + 3);

            byte len1 = (byte) dataLen;
            byte len2 = (byte) (dataLen >> 8);

            len2 |= 0x80;       // Старший бит количества вводимых байтов устанавливается в 1, если используется режим Overwrite
            if (!nullPadding)   // Второй (начиная с 1) по старшинству бит устанавливаем, если не перезатирали значения нулями
            {
                if ((len2 & 0x40) > 0)
                    throw new Exception("InputData_Overwrite: fatal algorithmic error: (len2 & 0x40) > 0");

                len2 |= 0x40;
            }

            State1[0] ^= len1;
            State1[1] ^= len2;
            // State1[2] ^= regime;

            InputData_ChangeTweakAndState(dataLen: dataLen, Overwrite: true, regime: regime);
        }

        /// <summary>Ввод данных во внешнюю часть криптографического состояния через xor</summary>
        /// <param name="data">Вводимые данные, не более BLOCK_SIZE_K байтов</param>
        /// <param name="dataLen">Длина вводимых данных</param>
        /// <param name="regime">Режим шифрования (это определяемое пользователем байтовое поле, вводимое во внешную часть криптографического состояния)</param>
        public void InputData_Xor(byte * data, long dataLen, byte regime)
        {
            if (dataLen > BLOCK_SIZE_K)
                throw new ArgumentOutOfRangeException("dataLen", "VinKekFishBase_KN_20210525.InputData_Xor: dataLen > BLOCK_SIZE_K");
            if (!isState1Main)
                throw new Exception("VinKekFishBase_KN_20210525.InputData_Xor: Fatal algorithmic error: !State1Main");

            if (dataLen > 0)
            {
                XorWithBytes(dataLen, data, State1 + 3);

                byte len1 = (byte) dataLen;
                byte len2 = (byte) (dataLen >> 8);

                State1[0] ^= len1;
                State1[1] ^= len2;
            }
            // State1[2] ^= regime;

            InputData_ChangeTweakAndState(dataLen: dataLen, Overwrite: false, regime: regime);
        }

        /// <summary>Изменяет tweak. Этот метод вызывать не надо. Он автоматически вызывается при вызове InputData_*</summary>
        /// <param name="dataLen">Длина введённых данных</param>
        /// <param name="Overwrite">Режим ввода. true - если overwrite</param>
        /// <param name="regime">Номер режима схемы шифрования</param>
        protected void InputData_ChangeTweakAndState(long dataLen, bool Overwrite, byte regime)
        {
            // Приращение tweak перед вводом данных
            Tweaks[0] += TWEAK_STEP_NUMBER;

            Tweaks[1] += (ulong) dataLen;
            if (Overwrite)
                Tweaks[1] += 0x0100_0000_0000_0000;

            var reg = ((ulong) regime) << 40; // 8*5 - третий по старшинству байт, нумерация с 1
            Tweaks[1] += reg;
            State1[2] ^= regime;

            isDataInputed = true;
        }

        /// <summary>Если никаких данных не введено в режиме Sponge (xor), изменяет tweak. Вместо этого можно вызвать InputData_Xor(null, 0, regime)</summary>
        /// <param name="regime">Номер режима схемы шифрования</param>
        public void NoInputData_ChangeTweakAndState(byte regime)
        {
            // Приращение tweak перед вводом данных
            Tweaks[0] += TWEAK_STEP_NUMBER;

            var reg = ((ulong) regime) << 40; // 8*5 - третий по старшинству байт, нумерация с 1
            Tweaks[1] += reg;
            State1[2] ^= regime;

            isDataInputed = true;
        }
    }
}
