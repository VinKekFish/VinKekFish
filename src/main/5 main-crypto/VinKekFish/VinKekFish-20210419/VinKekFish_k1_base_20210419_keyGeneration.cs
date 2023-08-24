// TODO: tests
using cryptoprime.VinKekFish;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using vinkekfish.keccak_20200918;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    // Допустимые состояния см. ./Documentation/VinKekFish_k1_base_20210419_keyGeneration_состояния.md
    // Примитивы синхронизации и ввод-вывод в папке ./Documentation/VinKekFish_k1_base_20210419_keyGeneration-ввод-вывод.md

    /// <summary>
    /// Используйте вместо этого класса VinKekFishBase_KN_20210525 для всех применений, кроме тестирования.
    /// 
    /// При использовании класса обязательно вызвать Init1, затем Init2.
    /// После использования вызвать Dispose, или использовать конструкцию using
    /// 
    /// Класс предназначен для
    /// 1. Генерации ключей на основе основного ключа и открытого вектора инициализации. См. GetNewKey
    /// 2. Генерации любых последовательностей, в том числе, не повторяемых заново. См. GetNewKey, InputRandom, EnterToBackgroundCycle
    /// </summary>
    public unsafe class VinKekFish_k1_base_20210419_keyGeneration: VinKekFish_k1_base_20210419
    {
        public VinKekFish_k1_base_20210419_keyGeneration()
        {
            // Подумать насчёт 
            // new System.Security.Cryptography.RNGCryptoServiceProvider();
        }

        public override void Init1(int RoundsForTables, byte * additionalKeyForTables, long additionalKeyForTables_length, byte * OpenInitVectorForTables = null, long OpenInitVectorForTables_length = 0, int PreRoundsForTranspose = 8)
        {
            if (LightGenerator != null)
                ExitFromBackgroundCycle();

            base.Init1(RoundsForTables: RoundsForTables, additionalKeyForTables: additionalKeyForTables, additionalKeyForTables_length: additionalKeyForTables_length, OpenInitVectorForTables: OpenInitVectorForTables, OpenInitVectorForTables_length: OpenInitVectorForTables_length, PreRoundsForTranspose: PreRoundsForTranspose);
        }

        public override void Init2(byte * key, ulong key_length, byte[] OpenInitVector, int Rounds = NORMAL_ROUNDS, int RoundsForEnd = NORMAL_ROUNDS, int RoundsForExtendedKey = NORMAL_ROUNDS, bool IsEmptyKey = false)
        {
            base.Init2(key: key, key_length: key_length, OpenInitVector: OpenInitVector, Rounds: Rounds, RoundsForEnd: RoundsForEnd, RoundsForExtendedKey: RoundsForExtendedKey, IsEmptyKey);
        }

        public const int key_block_size = 64;
        public const int min_special_regime = 253;

        /// <summary>Функция генерирует ключи шифрования. После инициализации Init1 и Init2, функция готова к использованию без дополнительных вызовов</summary>
        /// <param name="len">Количество байтов, которые сгенерировать</param>
        /// <param name="blockLen">Длина блока генерации в байтах, не более BLOCK_SIZE. По умолчанию, длина уменьшена до 64 байтов (для надёжности)</param>
        /// <param name="allocator">Метод выделения памяти для ключа. Если null, то AllocHGlobal_AllocatorForUnsafeMemory</param>
        /// <param name="CountOfRounds">Количество раундов для генерации</param>
        /// <returns>Сгенерированный ключ</returns>
        public Record GetNewKey(long len = NORMAL_KEY, long blockLen = key_block_size, AllocatorForUnsafeMemoryInterface allocator = null, int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            if (isInited2 != true)
                throw new Exception("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: isInited2 != true (not initialized)");

            if (allocator == null)
                allocator = new AllocHGlobal_AllocatorForUnsafeMemory();

            if (_RTables < CountOfRounds)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: RTables < CountOfRounds");

            if (blockLen > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: blockLen > BLOCK_SIZE");

            if (CountOfRounds < MIN_ROUNDS)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: CountOfRounds < MIN_ROUNDS");

            if (blockLen <= 0 || len <= 0)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: blockLen <= 0 || len <= 0");


            var  result = allocator.AllocMemory(len);
            result.Clear();
            byte regime = 0;
            long start  = 0;
            do
            {
                // Режим 255 - это режим отбоя после ввода основного ключа. 254 - это EmptyStep и EmptyStepOverwrite. 253 - InputRandom
                if (regime >= min_special_regime)
                    regime = 0;

                NoInputData_ChangeTweak(_state, t0, regime++);
                DoStep(CountOfRounds);

                outputData(result, start, outputLen: len, countToOutput: len - start > blockLen ? blockLen : len - start);
                start += blockLen;
            }
            while (start < len);

            isHaveOutputData = false;

            return result;
        }

        /// <summary>Пропустить один шаг (для большей отбивки от других значений)</summary>
        /// <param name="CountOfRounds">Количество раундов для отбивки</param>
        public void EmptyStep(int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            if (CountOfRounds < MIN_ABSORPTION_ROUNDS)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: CountOfRounds < MIN_INNER_ROUNDS");

            NoInputData_ChangeTweak(_state, t0, 254);
            DoStep(CountOfRounds);
        }

        /// <summary>Пропустить один шаг с перезаписью в режиме OVERWRITE (для большей отбивки от других значений и реализации необратимости: после ввода ключа это и так делается)</summary>
        /// <param name="CountOfRounds">Количество раундов для отбивки</param>
        public void EmptyStepOverwrite(int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            if (CountOfRounds < MIN_ABSORPTION_ROUNDS)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: CountOfRounds < MIN_INNER_ROUNDS");

            InputData_Overwrite(null, _state, 0, t0, 254);
            DoStep(CountOfRounds);
        }

        /// <summary>Вводит рандомизирующие данные в губку (выполняет криптографические операции)</summary>
        /// <param name="data">Данные для ввода</param>
        /// <param name="data_length">Длина вводимых данных</param>
        /// <param name="CountOfRounds">Количество раундов</param>
        public void InputRandom(byte * data, long data_length, int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            long len = 0;
            while (data_length > 0)
            {
                if (data_length > BLOCK_SIZE)
                    len = BLOCK_SIZE;
                else
                    len = data_length;

                InputData_Xor(data, _state, len, t0, 253);
                data        += len;
                data_length -= len;

                DoStep(CountOfRounds);
            }
        }

        /// <summary>Эта блокировка используется для подачи сигнала и ожидания в WaitForBackgroundCycle.
        /// Устанавливается при очередном впитанном блоке случайных данных в режиме EnterToBackgroundCycle.</summary>
        /// <remarks>Вхождение сначала в backgroundSync, затем в this, если нужно взять обе блокировки</remarks>
        public    readonly Object               backgroundSync     = new object();
        protected volatile Thread               backgroundThread   = null;
        protected volatile LightRandomGenerator LightGenerator     = null;
        protected volatile LightRandomGenerator LightGeneratorDisk = null;
        protected volatile Keccak_PRNG_20201128 keccak_prng        = null;

        /// <summary>См. описание параметра EnterToBackgroundCycle</summary>
        public ushort BackgroundSleepTimeout = 0;       /// <summary>См. описание параметра EnterToBackgroundCycle</summary>
        public ushort BackgroundSleepCount   = 0;
        // /// <summary>См. описание поля BackgourndGenerated</summary>
        // public ushort BackgroundKeccakCount  = 8;

        /// <summary>Количество поглощённых блоков.
        /// Для упрощения, считается, что на один блок приходится 1 бит энтропии, то есть на BLOCK_SIZE=512 приходится 1 бит
        /// (рекомендуется ещё уменьшать в пару раз хотя бы).
        /// В любом случае, это не надёжный источник рандомизации, вместе с ним необходимо использовать и другие источники, если возможно.
        /// Запись в данную переменную производить с помощью lock (this) - допускается только обнуление данной переменной</summary>
        public long  BackgourndGenerated     = 0;                       /// <summary>Аналогично BackgourndGenerated, только блоки поглощаются от более медленного генератора энтропии</summary>
        public long  BackgourndGeneratedDisk = 0;

        /// <summary>Войти в цикл дополнительной инициализации псевдослучайными значениями. Выход из цикла - вызов ExitFromBackgroundCycle.
        /// <para>До вызова ExitFromBackgroundCycle пользователь не должен использовать других методов, кроме WaitForBackgroundCycle.
        /// Если есть желание использовать другие методы, то их нужно оборачивать lock (this).</para>
        /// </summary>
        /// <remarks>Хотя данные метод начинает генерировать энтропию, эта энтропия не слишком хорошего качества - её непредсказуемость под вопросом. Поэтому, рекомендуется использовать другие источники энтропии.</remarks>
        /// <param name="BackgroundSleepTimeout">Thread.Sleep(BackgroundSleepTimeout). Устанавливает величину интервала, который поток спит после генерации BackgroundSleepCount блоков.
        /// BackgroundSleepCount = 0 - самое быстрое, загрузка почти всего процессорного ядра, если оно не занято чем-то ещё (точнее, загружается 2  процессорных ядра).
        /// BackgroundSleepCount = 72 - это загрузка на уровне не выше пары процентов от одного ядра.
        /// При BackgroundSleepCount = 0 происходит генерация где-то > 256 блоков VinKekFish (BLOCK_SIZE байтов) за секунду на двух ядрах 2,8 ГГц (BackgourndGenerated приращается на 256 за секунду или быстрее)</param>
        /// <param name="BackgroundSleepCount">После таймаута идёт генерация блоков без ожидания. На один таймаут приходится BackgroundSleepCount блоков.</param>
        /// <param name="generator">Генератор нестойких псевдослучайных чисел, должен генерировать по BLOCK_SIZE (512) байта в блок.  В ExitFromBackgroundCycle автоматически удаляется. Может быть <see langword="null"/>, в таком случае, создаётся с помощью LightRandomGenerator.</param>
        /// <param name="doWaitR">Параметр инициализирует одноимённое поле generator.doWaitR, но только если generator = null</param>
        /// <param name="doWaitW">Параметр инициализирует одноимённое поле generator.doWaitW, но только если generator = null</param>
        /// <param name="generatorDisk">Более медленный генератор нестойких псевдослучайных чисел, должен генерировать по BLOCK_SIZE (512) байта в блок. В ExitFromBackgroundCycle автоматически удаляется. Может быть null, в таком случае создаётся с помощью LightRandomGenerator_DiskSlow</param>
        public void EnterToBackgroundCycle(ushort BackgroundSleepTimeout = 72, ushort BackgroundSleepCount = 8, bool doWaitR = true, bool doWaitW = true, LightRandomGenerator generator = null, LightRandomGenerator generatorDisk = null)
        {
            if (backgroundThread != null || LightGenerator != null)
                throw new Exception("VinKekFish_k1_base_20210419_keyGeneration.EnterToBackgroundCycle: backgroundThread != null. Call ExitFromBackgroundCycle");
            
            SimpleInit();

            if (generator == null)
            {
                generator = new LightRandomGenerator(/*Keccak_PRNG_20201128.InputSize*/ BLOCK_SIZE);
                generator.doWaitR = doWaitR;
                generator.doWaitW = doWaitW;
            }

            if (generatorDisk == null)
            {
                generatorDisk = new LightRandomGenerator_DiskSlow(BLOCK_SIZE);
            }

            this.BackgroundSleepTimeout = BackgroundSleepTimeout;
            this.BackgroundSleepCount = BackgroundSleepCount;
            keccak_prng = new Keccak_PRNG_20201128(outputSize: BLOCK_SIZE * 2);

            lock (backgroundSync)
            lock (this)
            {
                BackgourndGenerated     = 0;
                BackgourndGeneratedDisk = 0;
                LightGenerator          = generator;
                LightGeneratorDisk      = generatorDisk;
            }

            backgroundThread = new Thread
            (
                delegate ()
                {
                    // using Record data = AllocHGlobal_allocator.AllocMemory(BLOCK_SIZE);

                    // int keccakCnt = 0;
                    int cnt = 0;
                    do
                    {
                        Thread.Sleep(BackgroundSleepTimeout);
                        if (BackgroundSleepCount < 1)
                            BackgroundSleepCount = 1;

                        for (cnt = 0; cnt < BackgroundSleepCount; cnt++)
                        {
                            Thread.Sleep(0);
                            lock (backgroundSync)
                            {
                                if (LightGenerator == null)
                                    break;

                                LightGenerator.WaitForGenerator();
                                if (LightGenerator.ended)
                                    break;

                                lock (this)
                                {
                                    // Каждые BackgroundKeccakCount мы сохраняем результат. Остальные разы просто вводим данные без сохранения
                                    /*keccakCnt++;
                                    keccak_prng.InputBytes(LightGenerator.GeneratedBytes, LightGenerator.GeneratedBytes.len);
                                    keccak_prng.calcStep(SaveBytes: keccakCnt >= BackgroundKeccakCount, inputReadyCheck: true);

                                    if (keccakCnt >= BackgroundKeccakCount)
                                        keccakCnt = 0;
                                    if (keccak_prng.outputCount >= BLOCK_SIZE)
                                    {
                                        keccak_prng.output.getBytesAndRemoveIt(data);
                                        InputRandom(data, data.len, MIN_INNER_ROUNDS);
                                        data.Clear();
                                        */

                                    InputRandom(LightGenerator.GeneratedBytes, LightGenerator.GeneratedBytes.len, MIN_ABSORPTION_ROUNDS);

                                    Interlocked.Increment(ref BackgourndGenerated);
                                    Monitor.PulseAll(backgroundSync);
                                }

                                LightGenerator.ResetGeneratedBytes();

                                if (LightGeneratorDisk.GeneratedBytesCount >= BLOCK_SIZE)
                                {
                                    lock (this)
                                    {
                                        InputRandom(LightGeneratorDisk.GeneratedBytes, LightGeneratorDisk.GeneratedBytes.len, MIN_ROUNDS);

                                        Interlocked.Increment(ref BackgourndGeneratedDisk);
                                        Monitor.PulseAll(backgroundSync);
                                    }

                                    LightGeneratorDisk.ResetGeneratedBytes();
                                }

                                // Убрано, т.к. может негативно повлиять на производительность кода, исполняющегося одновременно в других местах програмы
                                // GC.Collect();       // Иначе бывает так, что программа занимает лишнюю системную память (хотя, обычно, нет)
                                // GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                    while (LightGenerator != null);

                    Monitor.PulseAll(backgroundSync);
                }
            );

            backgroundThread.Priority = ThreadPriority.Lowest;
            backgroundThread.Start();
        }

        /// <summary>Простая инициализация без ключей и открытых векторов инициализации (если инициализация будет от внешней энтропии)</summary>
        public void SimpleInit()
        {
            if (!isInited1)
            {
                Init1(NORMAL_ROUNDS, null, 0);
            }

            if (!isInited2)
            {
                Init2(null, 0, null, IsEmptyKey: true);
            }
        }

        /// <summary>Закончить цикл напитывания некриптостойкой случайной информацией</summary>
        /// <param name="Overwrite">Перезаписать нулями состояние для обеспечения его необратимости</param>
        public void ExitFromBackgroundCycle(bool Overwrite = true)
        {
            lock (backgroundSync)
            {
                try
                {
                    LightGenerator    ?.Dispose();
                    LightGeneratorDisk?.Dispose();
                    keccak_prng       ?.Dispose();
                }
                finally
                {
                    LightGenerator     = null;
                    LightGeneratorDisk = null;
                    keccak_prng        = null;
                    backgroundThread   = null;

                    Monitor.PulseAll(backgroundSync);
                }
            }

            if (Overwrite)
                InputData_Overwrite(null, _state, 0, t0, 253);
            else
                InputData_Xor(null, _state, 0, t0, 253);

            DoStep(NORMAL_ROUNDS);
        }

        public void WaitForBackgroundCycle(int generated = 64, int diskGenerated = 1)
        {
            lock (backgroundSync)
            {
                while (BackgourndGenerated < generated || BackgourndGeneratedDisk < diskGenerated)
                {
                    if (LightGenerator == null)
                        break;

                    Monitor.Wait(backgroundSync);
                }
            }
        }

        public override void Dispose(bool disposing)
        {
            if (LightGenerator != null)
                ExitFromBackgroundCycle();

            base.Dispose(disposing);
        }
    }
}
