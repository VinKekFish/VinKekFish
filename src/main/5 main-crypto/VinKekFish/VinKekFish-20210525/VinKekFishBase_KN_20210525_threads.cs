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
        protected readonly Thread[]? threads = null;

        /// <summary>isEnded должен быть всегда false. Если true, то потоки завершают свою работу</summary>
        protected volatile bool   isEnded = false;
        /// <summary>Объект для синхронизации. Порядок двойных блокировок: сначала this, потом sync</summary>
        public    readonly object sync    = new object();
        /// <summary>Объект для передачи сигнала о завершении работы потоков. Вызывается однократно после выполнения задачи</summary>
        public    readonly object taskEndedSync = new object();
                                                                                    /// <summary>Функция, которая должна быть выполнена в этой задаче</summary>
        protected volatile ThreadStart?    ThreadsFunc_Current       = null;        /// <summary>Приращается каждый раз, когда ставится на исполнение новая задача</summary><remarks>Обязательно в lock (sync)</remarks>
        protected volatile int             ThreadsTask_CountOfTasks  = 0;           /// <summary>Количество потоков, ещё не исполнивших эту задачу</summary><remarks>Обязательно в lock (sync)</remarks>
        protected volatile int             ThreadsExecutedForTask    = 0;
                                                                                    /// <summary>Исключения, свершившиеся при исполнении задач</summary>
        protected readonly ConcurrentQueue <Exception> ThreadsTask_Errors      = new ConcurrentQueue<Exception>();      /// <summary>Количество исключений, свершившихся при выполнении задач</summary>
        protected volatile int                         ThreadsTask_ErrorsCount = 0;
                                                                                    /// <summary>Количество потоков, которое не находися в ожидании.</summary><remarks>Обязательно в lock (sync)</remarks>
        protected volatile int           ThreadsInFunc   = 0;                       /// <summary>Количество запущенных потоков</summary>
        protected volatile int           ThreadsExecuted = 0;

        /// <summary>Завершено ли использование объекта (сигнал потокам к завершению). Можно установить только в true</summary>
        /// <value><c>true</c> указывает на то, что потоки завершаются или завершены (объект к использованию не пригоден); <c>false</c> объект пригоден к использованию</value>
        public bool IsEnded
        {
            get => isEnded || ThreadsExecuted <= 0;
            set
            {
                if (value)
                lock (sync)
                {
                    isEnded = true;
                    Monitor.PulseAll(sync);
                }
                else
                    throw new ArgumentException("VinKekFishBase_KN_20210525: IsEnded = false");
            }
        }

        /// <summary>Эта функция выполняется каждым потоком</summary>
        /*
            Функция позволяет установить только одну задачу на выполнение одновременно.
            При постановке следующей задачи нужно дождаться полного завершения предыдущей, иначе будет ошибка.
            Полное завершение определяется параметром ThreadsInFunc == 0

            Какая сейчас задача выполняется устанавливается тремя переменными
            ThreadsFunc_Current - это функция, которую нужно выполнить
            ThreadsTask_CountOfTasks - глобальная переменная, показывающая текущий номер задачи
            Task_CountOfTasks - это локальная переменная, говорящая, сколько задач было выполнено именно в этом потоке
            Пока Task_CountOfTasks == ThreadsTask_CountOfTasks, идёт ожидание постановки новой задачи. То есть все задачи уже выполнены
            При постановке задачи в "doFunction", выполняется приращение ThreadsTask_CountOfTasks
            Таким образом идёт выход за пределы цикла ожидания

            После этого выполняется ThreadsFunc_Current

            Ожидание выполнения задачи идёт с помощью функции waitForDoFunction
        */
        protected virtual void ThreadsFunction()
        {
            try
            {
                Interlocked.Increment(ref ThreadsExecuted);
                int Task_CountOfTasks;

                while (!isEnded)
                {
                    Task_CountOfTasks = ThreadsTask_CountOfTasks;
                    try
                    {
                        ThreadsFunc_Current!();
                    }
                    catch (Exception ex)
                    {
                        ThreadsTask_Errors.Append(ex);
                        Interlocked.Increment(ref ThreadsTask_ErrorsCount);
                    }

                    lock (sync)
                    {
                        // Уменьшаем количество потоков вне функции ожидания
                        ThreadsInFunc--;                                            // Это обязательно в блокировке sync
                        // Уменьшаем количество потоков, которые ещё не выполнили текущую задачу
                        ThreadsExecutedForTask--;                                   // Это обязательно в блокировке sync

                        // Даём сигнал о том, что задача завершена
                        if (ThreadsExecutedForTask <= 0)
                        {
                            lock (taskEndedSync)
                                Monitor.PulseAll(taskEndedSync);

                            if (ThreadsExecutedForTask < 0)
                            {
                                IsEnded = true;
                                throw new Exception("VinKekFishBase_KN_20210525.ThreadsFunction: ThreadsExecutedForTask < 0");
                            }
                        }

                        // Ждём постановки задач
                        while (Task_CountOfTasks == ThreadsTask_CountOfTasks)       // Это обязательно в блокировке sync
                        {
                            if (isEnded)
                                goto EndThread;

                            Monitor.Wait(sync);
                        }

                        // Приращаем (для информации) количество потоков вне функции ожидания
                        ThreadsInFunc++;                                            // Это обязательно в блокировке sync
                    }
                }

                EndThread: ;
            }
            catch (Exception ex)
            {
                ThreadsTask_Errors.Append(ex);
                Interlocked.Increment(ref ThreadsTask_ErrorsCount);
            }
            finally
            {
                Interlocked.Decrement(ref ThreadsExecuted);

                lock (sync)
                {
                    Monitor.PulseAll(sync);
                }
            }
        }

        protected void ThreadFunction_empty()
        {}
                                                    /// <summary>Вызывается для ожидания выполнения всех потоков перед тем, как ставить новую задачу или брать результат<remarks>При добавлении функции для добавления любой задачи нужно, чтобы функция автоматически ждала waitForDoFunction перед тем, как добавить себя</remarks></summary>
        protected virtual void waitForDoFunction()
        {
            lock (taskEndedSync)
            {
                while (IsTaskExecuted)
                    Monitor.Wait(taskEndedSync);

                // На всякий случай устанавливаем пустую функцию для выполнения
                ThreadsFunc_Current = ThreadFunction_empty;
            }
        }
                                                                                                    /// <summary>Если <see langword="true"/>, значит, выполняется задача. Постановка другой задачи невозможна, пока эта не будет закончена</summary>
        public bool IsTaskExecuted => ThreadsExecutedForTask > 0 && !IsEnded;

                                                    /// <summary>Вызывается после waitForDoFunction для постановки новой задачи после инициализации её параметров. Пример, см. в функции doKeccak()</summary>
        protected virtual void doFunction(ThreadStart ThreadFunc)
        {
            lock (sync)
            {
                if (ThreadsExecutedForTask > 0 || ThreadsExecuted <= 0 || IsEnded)
                    throw new Exception("VinKekFishBase_KN_20210525.doFunction: ThreadsExecutedForTask > 0 || ThreadsExecuted <= 0 || IsEnded");

                ThreadsFunc_Current = ThreadFunc;

                ThreadsTask_CountOfTasks++;             // Это обязательно в блокировке sync
                ThreadsExecutedForTask = ThreadCount;   // Это обязательно в блокировке sync
                // ОШИБКА: В конструкторе нет this.ThreadsExecutedForTask = ThreadCount;

                Monitor.PulseAll(sync);
            }
        }

        /// <summary>Запускает многопоточную поблочную обработку алгоритмом keccak</summary>
        protected void doKeccak()
        {
            // Ждать такие вещи не очень эффективно, хоть и более безопасно. Но ждём только в одном потоке (который ставит задачу), так что это не так страшно
            waitForDoFunction();                // Ждём конца выполнения предыдущей задачи

            CurrentKeccakBlockNumber[0] = 0;
            CurrentKeccakBlockNumber[1] = 1;

            doFunction(ThreadFunction_Keccak);
            waitForDoFunction();
            State1Main ^= true;                 // Переключаем состояния (вспомогательный и основной массив состояний)
        }

                                                                                            /// <summary>Массив счётчика блоков для определения текущего блока для обработки keccak. [0] - чётные элементы, [1] - нечётные элементы</summary>
        protected volatile int[] CurrentKeccakBlockNumber = {0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};   // Нули ради того, чтобы больше в этой линии кеша ничего не было
        protected void ThreadFunction_Keccak()
        {
            // Во всех массивах keccak всегда чётное количество элементов, так что мы их можем обрабатывать, деля на чётные и нечётные
            // Сначала мы обрабатываем чётные, потом - нечётные. Это нужно, чтобы массивы не мешали друг другу, так как их размеры не кратны размеру линии кеша.
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

                    byte * mat = Matrix +  MatrixLen * index;
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
        }

        /// <summary>Запускает многопоточную поблочную обработку алгоритмом ThreeFish</summary>
        protected void doThreeFish()
        {
            waitForDoFunction();                // Ждём конца выполнения предыдущей задачи

            CurrentThreeFishBlockNumber = 0;
            BytesBuilder.CopyTo(Len, Len, st1, st2);

            doFunction(ThreadFunction_ThreeFish);
            waitForDoFunction();
            State1Main ^= true;
        }

        protected volatile int CurrentThreeFishBlockNumber = 0;
        protected void ThreadFunction_ThreeFish()
        {
            do
            {
                var index  = Interlocked.Increment(ref CurrentThreeFishBlockNumber) - 1;
                if (index >= LenInThreeFish)
                {
                    break;
                }

                var offsetC = ThreeFishBlockLen * index;
                var offsetK = ThreeFishBlockLen * NumbersOfThreeFishBlocks![index];
                var offC    = st2 + offsetC;
                var offK    = st1 + offsetK;


                // В элементе [0] массива содержится раундовый tweak. В элементе [1] - tweak, который изменяется в течении шага, [2] - tweak конкретного блока  ::an6c5JhGzyOO
                var tweaks  = (ulong *) (((byte *) Tweaks) + CountOfTweaks * CryptoTweakLen * index + CryptoTweakLen*2);
                tweaks[0]   = Tweaks[2+0] + (uint) index;     // Берём tweak из элемента [1]
                tweaks[1]   = Tweaks[2+1];
                tweaks[2]   = tweaks[0] ^ tweaks[1];
// TODO: Кажется, в обеих реаолизациях алгоритма есть проблема с верным вычислением tweak от шага к шагу
                // BytesBuilder.CopyTo(CryptoTweakLen, CryptoTweakLen, ((byte *) Tweaks) + CryptoTweakLen, (byte *) tweaks);

                Threefish_Static_Generated.Threefish1024_step(key: (ulong *) offK, tweak: tweaks, text: (ulong *) offC);
            }
            while (true);
        }

        /// <summary>Запускает многопоточную поблочную перестановку</summary>
        protected void doPermutation(ushort * CurrentPermutationTable)
        {
            waitForDoFunction();                // Ждём конца выполнения предыдущей задачи

            CurrentPermutationBlockNumber = 0;
            this.CurrentPermutationTable  = CurrentPermutationTable;

            // doFunction(ThreadFunction_Permutation);
            // Для повышения эффективности работы это делает один поток, а не пул (см. пояснения ThreadFunction_Permutation)
            ThreadFunction_Permutation();
            waitForDoFunction();
            State1Main ^= true;
        }

        protected volatile int      CurrentPermutationBlockNumber = 0;
        protected volatile ushort * CurrentPermutationTable       = null;
        protected void ThreadFunction_Permutation()
        {
            do
            {
                var index  = Interlocked.Increment(ref CurrentPermutationBlockNumber) - 1;
                if (index >= LenInThreadBlock)          // Len всегда кратно LenInThreadBlock, см. конструктор
                {
                    break;
                }

                var table  = CurrentPermutationTable;
                var offset = LenThreadBlock * index;

                // Должно быть жутко неэффективно при многопоточной реализации, т.к. линии кеша будут пересекаться
                var e = offset + LenThreadBlock;
                for (int i = offset; i < e; i++)
                {
                    st2[i] = st1[  table[i]  ];
                }

            }
            while (true);
        }
                                                                    /// <summary>Попусту теребит память: это простая защита от выгрузки памяти в файл подкачки</summary>
        protected void WaitFunction(object state)
        {
            if (IsTaskExecuted || !isInit1 || isDisposed || isEnded)
                return;

            lock (this)
            lock (sync)
            {
                if (!IsTaskExecuted)
                    BlankRead();
            }
        }
                                                                    /// <summary>Размер страницы оперативной памяти</summary>
        public const int PAGE_SIZE = 4096;
                                                                    /// <summary>Попусту теребит память: это простая защита от выгрузки памяти в файл подкачки. Это плохо, т.к. раз за разом ключ передаётся из памяти в процессор в неизменном виде, что повышает риски перехвата по ПЭМИН простыми средствами.</summary>
        protected void BlankRead()
        {
            for (int i = 0; i < States.len; i += PAGE_SIZE)
            {
                var b = State1[i];
            }

            var len = tablesForPermutations!.len;
            var a   = tablesForPermutations!.array;
            for (int i = 0; i < len; i += PAGE_SIZE)
            {
                var b = a[i];
            }
        }
    }
}

