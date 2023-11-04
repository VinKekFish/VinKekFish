// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.ProgramOptions.Options_Service.Input.Entropy.Interval;
using Options_Service_Exception = VinKekFish_Utils.ProgramOptions.Options_Service.Options_Service_Exception;
using Flags = VinKekFish_Utils.ProgramOptions.Options_Service.Input.Entropy.Interval.Flags;
using maincrypto.keccak;

public partial class Regime_Service
{
    /// <summary>Зарегистрировать постоянные сборщики энтропии</summary>
    protected unsafe virtual void StartContinuouslyEntropy()
    {
        lock (entropy_sync)
        {
            try
            {
                var rndList = options_service!.root!.input!.entropy!.standard!.randoms;
                getRandomFromRndCommandList_Continuously(rndList);
            }
            catch (NullReferenceException)
            {}

            try
            {
                var rndList = options_service!.root!.input!.entropy!.os!.randoms;
                getRandomFromRndCommandList_Continuously(rndList);
            }
            catch (NullReferenceException)
            {}

            Monitor.PulseAll(entropy_sync);
        }
    }

    // ::cp:all:dhpOU4GDHUNYcaXq:2023.10.30
    public unsafe void getRandomFromRndCommandList_Continuously(List<Options_Service.Input.Entropy.InputElement> rndList)
    {
        checked
        {
            foreach (var rnd in rndList)
            {
                var intervals = rnd.intervals!.interval!.inner;
                foreach (var interval in intervals)
                {
                    if (
                        interval.IntervalType == IntervalTypeEnum.continuously ||
                        interval.IntervalType == IntervalTypeEnum.fast ||
                        interval.IntervalType == IntervalTypeEnum.time ||
                        interval.IntervalType == IntervalTypeEnum.waitAndOnce
                        )
                    {
                        if (string.IsNullOrEmpty(rnd.PathString))
                            throw new Exception($"Regime_Service.ContinuouslyEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': file name is empty. The random file name is required.");

                        switch (rnd)
                        {
                            case Options_Service.Input.Entropy.InputFileElement fileElement:
                                StartContinuouslyGetter(rnd, interval, fileElement);
                            break;

                            case Options_Service.Input.Entropy.InputCmdElement cmdElement:
                                getRandomFromCommand_continuously(rnd, interval, cmdElement);
                            break;

                            case Options_Service.Input.Entropy.InputDirElement dirElement:

                                var files = dirElement.dirInfo!.GetFiles("*", SearchOption.AllDirectories);
                                foreach (var file in files)
                                    ;
                            break;

                            default:
                                throw new Exception($"Regime_Service.ContinuouslyEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': unknown command type '{rnd.GetType().Name}'. Fatal error; this is error in the program code, not in the option file");
                        }
                    }
                }
            }
        }
    }

    // При доступе синхронизация lock (continuouslyGetters)
    public List<ContinuouslyGetterRecord> continuouslyGetters = new List<ContinuouslyGetterRecord>();

    /// <summary>Инкапсулирует в себя промежуточную губку и предоставляет методы для записи в неё байтов из источника энтропии и получения из неё байтов энтропии</summary>
    public unsafe class ContinuouslyGetterRecord: IDisposable
    {
        public    readonly Thread          thread;
        protected readonly Keccak_20200918 keccak;
                                                                                            /// <summary>true, если объект уже удалён</summary>
        public    bool     disposed                   {get; protected set;} = false;        /// <summary>Количество байтов, полученное из этого источника (это количество сырых байтов, действительно полученных из источника, без учёта настроек {min,max,avg,EME})</summary>
        public    nint     countOfBytes               {get; protected set;} = 0;            /// <summary>Аналогично countOfBits. Количество байтов, которое было выведено для пользователя функцией getBytes</summary>
        public    nint     countOfBytesToUser         {get; protected set;} = 0;            /// <summary>Аналогично countOfBits. Количество байтов, которое было получено из источника энтропии после последнего изъятия битов из губки (необходимо для того, чтобы рассчитать, можно ли сейчас из этой губки что-то брать)</summary>
        public    nint     countOfBytesFromLastOutput {get; protected set;} = 0;            /// <summary>true, если объект не проинициализирован (сбрасывается после вызова getBytes). Это значение не должно быть нужно пользователю, используйте метод isDataReady. Устанавливается при вызове метода addBytes (addBytes вызывает поток, который собирает энтропию).</summary>
        public    bool     isInited                   {get; protected set;} = false;
                                                                                            /// <summary>Элемент из настроек, описывающий параметры данного источника энтропии</summary>
        public readonly Options_Service.Input.Entropy.InputElement inputElement;

        public ContinuouslyGetterRecord(Thread t, Options_Service.Input.Entropy.InputElement inputElement)
        {
            this.thread      = t;
            this.keccak = new Keccak_20200918();

            this.inputElement = inputElement;
        }

        /// <summary>Получает байты из промежуточной губки. <para>Пользователь должен проверить, что isInited установлен перед тем, как использовать этот метод. Если isInited == false, то губка ещё не готова к получению из неё информации: её надо просто пропустить и взять значения из других источников.</para><para>Код безопасен с точки зрения многопоточности.</para><para>countOfBytesFromLastOutput сбрасывается в ноль вне зависимости от количества получаемых данных.</para></summary>
        /// <param name="data">Массив данных, принимающий накопленные байты энтропии из промежуточной губки</param>
        /// <param name="len">Количество байтов энтропии для чтения. Не более чем KeccakPrime.BlockLen (64 байта)</param>
        public void getBytes(byte * data, nint len)
        {
            checked
            {
                lock (this)
                {
                    if (disposed)
                        throw new ObjectDisposedException("ContinuouslyGetterRecord.getBytes: disposed (you must check the 'disposed' field and skip the object, if disposed)");
                    if (len > KeccakPrime.BlockLen)
                        throw new ArgumentOutOfRangeException("maxLen", $"ContinuouslyGetterRecord.getBytes: maxLen > KeccakPrime.BlockLen ({len} > {KeccakPrime.BlockLen})");
                    if (!isInited)
                        throw new InvalidOperationException("ContinuouslyGetterRecord.getBytes: !isInited. You must check the 'isDataReady()' function and skip the object, if the return value is false");
                    if (!isDataReady(len))
                        throw new InvalidOperationException("ContinuouslyGetterRecord.getBytes: !isDataReady. You must check the 'isDataReady()' function and skip the object, if the return value is false");

                    KeccakPrime.Keccak_Output_512(data, (byte)len, keccak.S);
                    isInited     = false;
                    MandatoryUse = false;
                    countOfBytesFromLastOutput = 0;

                    countOfBytesToUser += len;
                }
            }
        }

        /// <summary>Проверяет, готово ли количество данных len для вывода.</summary>
        /// <param name="askedBytes">Количество байтов энтропии, которое хочет получить пользователь.</param>
        /// <returns>false, если данные не готовы. true, если данные готовы. Если false, то из объекта ещё нельзя извлекать данные с помощью функции getBytes.</returns>
        public bool isDataReady(nint askedBytes)
        {
            checked
            {
                if (!isInited)
                    return false;

                if (MandatoryUse)
                    return true;

                var ReadyBytes = GetCountOfReadyBytes();
                return askedBytes < ReadyBytes;
            }
        }

        /// <summary>Возвращает верхнюю оценку количества байтов энтропии, которое уже собрано промежуточной губкой</summary>
        public long GetCountOfReadyBytes()
        {
            checked
            {
                // Если min не установлен (равен нулю), то считаем, что на один байт выхода приходится 8-мь байтов входа
                if (inputElement.intervals!.entropy.min <= 0)
                    return countOfBytesFromLastOutput >> 3;

                return countOfBytesFromLastOutput / inputElement.intervals!.entropy.min;
            }
        }



        /// <summary>Добавить в губку дополнительные байты из источника энтропии</summary>
        /// <param name="bytes">Количество байтов из источника энтропии; для расчёта общего количества байтов</param>
        /// <param name="len">Количество добавляемых байтов из input. Может отличаться от bytes в связи с тем, что к input могло быть добавлено время или какие-то другие дополнительные параметры. len >= bytes. len > 0 и может быть больше блока шифрования.</param>
        /// <param name="input">Массив, содержащий добавляемые байты.</param>
        public void addBytes(nint bytes, nint len, byte * input)
        {
            checked
            {
                lock (this)
                {
                    if (len <= 0)
                        throw new ArgumentOutOfRangeException("len", "len <= 0");
                    if (bytes > len)
                        throw new ArgumentOutOfRangeException("bytes", "bytes > len");

                    do
                    {
                        nint curLen = len;
                        if (curLen > KeccakPrime.BlockLen)
                            curLen = KeccakPrime.BlockLen;

                        KeccakPrime.Keccak_Input64_512(input, (byte) curLen, keccak.S);
                        keccak.CalcStep();
                        len   -= curLen;
                        input += curLen;
                    }
                    while (len > 0);

                    isInited                    = true;
                    countOfBytes               += bytes;
                    countOfBytesFromLastOutput += bytes;
                }
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (disposed)
                    throw new Exception("ContinuouslyGetterRecord.Dispose executed twice");

                keccak.Dispose();
                disposed = true;
            }
        }

        protected bool MandatoryUse = false;
        public void CorrectEntropyForMandatoryUse()
        {
            MandatoryUse = true;
        }
    }

    protected unsafe void StartContinuouslyGetter
    (
        Options_Service.Input.Entropy.InputElement rnd,
        Options_Service.Input.Entropy.Interval.InnerIntervalElement interval,
        Options_Service.Input.Entropy.InputFileElement fileElement
    )
    {
        fileElement.fileInfo!.Refresh();/*
        if (!fileElement.fileInfo.Exists)
        {
            Console.Error.WriteLine($"Regime_Service.StartContinuouslyGetter: file not found: {fileElement.fileInfo.FullName}");
            return;
        }*/

        var t = new Thread
        (
            () =>
            {
                checked
                {
                    int len     = 1024;                    // Значение должно быть строго больше KeccakPrime.BlockLen + dateLen
                    int ilen    = len * 2;
                    int dateLen = interval.flags!.date == Flags.FlagValue.no ? 0 : sizeof(long);    // Длина массива, выделенная для данных
                    if (fileElement.fileInfo!.Length > len)
                    {
                        len = (int) fileElement.fileInfo!.Length;
                        if (len*2 > ilen)
                            ilen = len * 2; // ilen не должен быть меньше 127-ми байтов
                    }
                    if (interval.Length!.Length > 0)
                    {
                        len = (int) (interval.Length!.Length + dateLen);
                        if (len*2 > ilen)
                            ilen = len * 2; // ilen не должен быть меньше 127-ми байтов
                    }

                    var input   = stackalloc byte[ilen];  // Массив, из которого будет вводиться энтропия в губку keccak
                    int pos     = 0;                    

                    long lastTimeInLog  = DateTime.Now.Ticks;
                    nint lastBytesInLog = 0;

                    var buff = stackalloc byte[len];
                    var span = new Span<byte>(buff, len - dateLen); // Ровно столько, сколько запросил пользователь, если он вообще что-то запросил

                    nint totalBytes = 0;
                    var cgr = new ContinuouslyGetterRecord(Thread.CurrentThread, rnd);

                    int sleepTime = interval.time > 0 ? (int) interval.time : 1049;

                    try
                    {
                        lock (continuouslyGetters)
                            continuouslyGetters.Add(cgr);

                        while (!this.Terminated)
                        {
                            try
                            {
                                WaitForFileExists(fileElement.fileInfo, L("File not found. Wait for creation:") + $" '{fileElement.fileInfo.FullName}'.", L("File found successfully:") + $" '{fileElement.fileInfo.FullName}'.");
                                if (this.Terminated)
                                    break;

                                // Если мы "быстро" считываем файл,
                                // то мы будем делать это не так и задержка в цикле будет другой
                                if (interval.IntervalType == IntervalTypeEnum.fast)
                                {
                                    getEntropyFromFile_h(cgr, interval, fileElement, ilen, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span, ref totalBytes, sleepTime);
                                    if (!this.Terminated)
                                        Thread.Sleep(97);
                                }
                                else
                                {
                                    // Здесь мы считываем файл постоянно.
                                    // Задержка нужна только на случай какой-либо ошибки файлового ввода-вывода
                                    var isSleeped = getEntropyFromFile_h(cgr, interval, fileElement, ilen, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span, ref totalBytes, sleepTime);

                                    if (!isSleeped)
                                    if (!this.Terminated)
                                        Thread.Sleep(sleepTime);
                                }

                                if (interval.IntervalType == IntervalTypeEnum.waitAndOnce)
                                {
                                    if (interval.flags!.ignored != Flags.FlagValue.yes)
                                    {
                                        cgr.addBytes(totalBytes, pos, input);
                                        cgr.CorrectEntropyForMandatoryUse();
                                        pos = 0;
                                        totalBytes = 0;
                                    }

                                    SendGetterDebugMsgToConsole(fileElement, cgr);

                                    // Ожидание завершеня работы через Thread.Interrupt
                                    while (!Terminated)
                                        Thread.Sleep(1000*3600);

                                    break;
                                }
                            }
                            catch (ThreadInterruptedException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(VinKekFish_Utils.Memory.formatException(ex));
                                Thread.Sleep(3557);
                            }
                        }

                        if (interval.flags!.watchInLog == Flags.FlagValue.yes)
                        if (interval.IntervalType != IntervalTypeEnum.waitAndOnce)
                            SendGetterDebugMsgToConsole(fileElement, cgr);
                    }
                    finally
                    {
                        lock (continuouslyGetters)
                            continuouslyGetters.Remove(cgr);

                        cgr.Dispose();
                    }
                } // checked
            } // end thread function
        );

        t.IsBackground = true;
        t.Start();
    }

    /// <summary>Ожидает появления файла в файловой системе, если его ещё нет</summary>
    /// <param name="fileElement">Описатель файла</param>
    public unsafe void WaitForFileExists(FileInfo fi, string? MessageForConsole = null, string? MessageBySuccessfullForConsole = null)
    {
        fi.Refresh();

        bool messaged = false;
        while (!fi.Exists && !this.Terminated)
        {
            if (!messaged)
            {
                Console.WriteLine(MessageForConsole);
                messaged = true;
            }

            Thread.Sleep(1049);
            fi.Refresh();
        }

        if (messaged)
        {
            Console.WriteLine(MessageBySuccessfullForConsole);
        }
    }

    protected unsafe bool getEntropyFromFile_h(ContinuouslyGetterRecord cgr, Options_Service.Input.Entropy.Interval.InnerIntervalElement interval, Options_Service.Input.Entropy.InputFileElement fileElement, int ilen, byte* input, ref int pos, int dateLen, ref long lastTimeInLog, ref nint lastBytesInLog, byte* buff, Span<byte> span, ref nint totalBytes, int sleepTime)
    {
        checked
        {
            using (var rs = fileElement.fileInfo!.OpenRead())
            {
                try
                {
                    return getEntropyFromFile_h(interval, fileElement, ilen, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span, rs, cgr, ref totalBytes, sleepTime);
                }
                catch (ThreadInterruptedException)
                {}
                catch (Exception ex)
                {
                    Console.Error.WriteLine(VinKekFish_Utils.Memory.formatException(ex));
                    Thread.Sleep(3557);
                }
                finally
                {}
            } // using

            return true;        // Задержка в вызывающей функции уже не нужна: либо выполнена, либо произошло исключение, которое не подразумевает дополнительной задержки
        }
    }

    protected unsafe bool getEntropyFromFile_h
    (
        Options_Service.Input.Entropy.Interval.InnerIntervalElement interval,
        Options_Service.Input.Entropy.InputFileElement fileElement,
        int ilen, byte* input, ref int pos, int dateLen, ref long lastTimeInLog, ref nint lastBytesInLog,
        byte* buff, Span<byte> span,
        FileStream rs, ContinuouslyGetterRecord cgr, ref nint totalBytes, int sleepTime
    )
    {
        checked
        {
            bool isSleeped = false;
            long lastDate  = default;
            bool ignored   = interval.flags!.ignored == Flags.FlagValue.yes;
            bool doLog     = interval.flags!.log == Flags.FlagValue.yes;
            while (true)
            {
                // Количество байтов, получаемое за раз, регулируется вызывающей функцией
                var bytesReaded = rs.Read(span);
                if (bytesReaded <= 0 || Terminated)
                    break;

// TODO: Добавить сравнение с предыдущим считанным результатом
                // Если считываем только время, то пропускаем шаг цикла, если время совпадает
                if (interval.flags!.date == Flags.FlagValue.dateOnly)
                {
                    var now = DateTime.Now.Ticks;
                    if (now == lastDate)
                        continue;

                    lastDate = now;
                }

                // Вводим данные в промежуточную губку, если их накопилось на блок
                if (pos >= KeccakPrime.BlockLen)
                {
                    if (doLog)
                    {
                        using (var tmpRecord = new Record())
                        {
                            tmpRecord.array = input;
                            tmpRecord.len   = pos;
                            WriteToLog(tmpRecord, pos);

                            // Иначе будет обнуление буфера
                            tmpRecord.array = null;
                        }
                    }

                    if (!ignored)
                        cgr.addBytes(totalBytes, pos, input);

                    pos = 0;
                    totalBytes = 0;

                    if (!ignored)
                    {
                        if (interval.flags!.watchInLog == Flags.FlagValue.yes)
                        {
                            var ticks = DateTime.Now.Ticks;

                            if (ticks - lastTimeInLog >= ticksPerHour)
                                if (lastBytesInLog != cgr.countOfBytesToUser)
                                {
                                    SendGetterDebugMsgToConsole(fileElement, cgr);

                                    lastTimeInLog  = ticks;
                                    lastBytesInLog = cgr.countOfBytesToUser;
                                }
                        }
                    }
                }

                if (dateLen > 0)
                {
                    var ticks = DateTime.Now.Ticks;
                    BytesBuilder.ULongToBytes((ulong)ticks, input, ilen, pos);
                    pos += dateLen;

                    if (interval.flags!.date == Flags.FlagValue.dateOnly)
                        totalBytes += dateLen;
                }

                // Считываем данные только если указано, что источник энтропии можно считывать
                if (interval.flags!.date != Flags.FlagValue.dateOnly)
                {
                    BytesBuilder.CopyTo(span.Length, ilen, buff, input, pos, bytesReaded);
                    pos        += bytesReaded;
                    totalBytes += bytesReaded;
                }

                BytesBuilder.ToNull(span.Length, buff);

                // Если заявлена задержка, значит мы прочитали всё, что хотели,
                // т.к. при continuously задержка ставится 0,
                // а при fast задержка вообще не ставится (тоже 0).
                // Значит, эта задержка от периодического считывания
                if (sleepTime > 0)
                {
                    break;
                }
            }

            return isSleeped;
        }
    }

    public unsafe void getRandomFromCommand_continuously
    (
        Options_Service.Input.Entropy.InputElement rnd,
        Options_Service.Input.Entropy.Interval.InnerIntervalElement interval,
        Options_Service.Input.Entropy.InputCmdElement cmdElement
    )
    {
        var t = new Thread
        (
            () =>
            {
                long lastLogDate = default;

                int Ex_cnt    = 0;
                int sleepTime = interval.time > 0 ? (int) interval.time : 1049;
                if (interval.IntervalType == IntervalTypeEnum.fast)
                    sleepTime = 347;

                var cgr = new ContinuouslyGetterRecord(Thread.CurrentThread, rnd);
                lock (continuouslyGetters)
                    continuouslyGetters.Add(cgr);

                try
                {
                    while (!Terminated)
                    {
                        try
                        {
                            getRandomFromCommand_continuously_h(rnd, interval, cmdElement, cgr);
                            Thread.Sleep(sleepTime);

                            lastLogDate = SendGetterDebugMsgToConsole(interval, cmdElement, lastLogDate, cgr);

                            if (interval.IntervalType == IntervalTypeEnum.waitAndOnce)
                            {
                                // Ожидание завершеня работы через Thread.Interrupt
                                while (!Terminated)
                                    Thread.Sleep(1000*3600);

                                break;
                            }
                        }
                        catch (ThreadInterruptedException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            Ex_cnt++;
                            Console.Error.WriteLine(L("Error for command") + " " + cmdElement.PathString + " "  + cmdElement.parameters);
                            Console.Error.WriteLine(VinKekFish_Utils.Memory.formatException(ex));

                            if (Ex_cnt > 5)
                            {
                                Console.Error.WriteLine(L("Continuously reading has been ended by repeated errors for command") + " " + cmdElement.PathString + " "  + cmdElement.parameters);
                                return;
                            }

                            Thread.Sleep(3557);
                        }
                        finally
                        {}
                    }
                }
                finally
                {
                    lock (continuouslyGetters)
                        continuouslyGetters.Remove(cgr);

                    cgr.Dispose();
                }
            }
        );

        t.IsBackground = true;
        t.Start();
    }

    public unsafe long SendGetterDebugMsgToConsole(InnerIntervalElement interval, Options_Service.Input.Entropy.InputCmdElement cmdElement, long lastLogDate, ContinuouslyGetterRecord cgr)
    {
        var ticks = DateTime.Now.Ticks;
        if (interval.flags!.watchInLog == Flags.FlagValue.yes)
            if (ticks - lastLogDate > ticksPerHour)
                lock (entropy_sync)
                {
                    lastLogDate = ticks;
                    Console.WriteLine($"{cgr.countOfBytes} bytes got from '{cmdElement.PathString} {cmdElement.parameters}'; {cgr.countOfBytesToUser} sended to the main sponges (for entire the time of work).");
                }

        return lastLogDate;
    }

    // ::cp:all:ZwUElzYfZkK4PfXzUrO7:20231104
    public unsafe void getRandomFromCommand_continuously_h
    (
        Options_Service.Input.Entropy.InputElement rnd,
        Options_Service.Input.Entropy.Interval.InnerIntervalElement interval,
        Options_Service.Input.Entropy.InputCmdElement cmdElement,
        ContinuouslyGetterRecord cgr
    )
    {
        checked
        {
            bool ignored   = interval.flags!.ignored == Flags.FlagValue.yes;
            bool doLog     = interval.flags!.log == Flags.FlagValue.yes;

            var psi = new ProcessStartInfo(cmdElement.PathString!)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = new ASCIIEncoding()
            };
            if (cmdElement.workingDir is not null)
                psi.WorkingDirectory = cmdElement.workingDir;
            if (!string.IsNullOrEmpty(cmdElement.parameters))
                psi.Arguments = cmdElement.parameters;
            if (cmdElement.userName is not null)
            {
                psi.UserName = cmdElement.userName;
                if (psi.WorkingDirectory is null)
                    psi.WorkingDirectory = Directory.GetCurrentDirectory();
            }

            var ps = Process.Start(psi);
            ps!.WaitForExit();

            int len = 256*1024;
            if (interval.Length!.Length > 0)
            {
                len = (int) interval.Length!.Length;
            }
            var buffer  = stackalloc byte[len];
            var buffRec = new Record() {array = buffer, len = len};

            var readedLen = ps.StandardOutput.BaseStream.Read(buffRec);
            if (readedLen > 0)
            {
                if (doLog && readedLen > 0)
                    WriteToLog(buffRec, readedLen);

                if (!ignored)
                    cgr.addBytes(readedLen, readedLen, buffRec);
            }

            buffRec.array = null;
            buffRec.Dispose();
        }
    }

    public unsafe void SendGetterDebugMsgToConsole(Options_Service.Input.Entropy.InputFileElement fileElement, ContinuouslyGetterRecord cgr)
    {
        checked
        {
            lock (entropy_sync)
            {
                Console.WriteLine($"{cgr.countOfBytes} bytes got from '{fileElement.fileInfo!.FullName}'; {cgr.countOfBytesToUser} sended to the main sponges (for entire the time of work).");
            }
        }
    }
}
