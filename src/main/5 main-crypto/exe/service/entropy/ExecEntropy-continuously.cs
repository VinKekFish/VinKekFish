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
using static VinKekFish_Utils.Utils;
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
                GetRandomFromRndCommandList_Continuously(rndList);
            }
            catch (NullReferenceException)
            {}

            try
            {
                var rndList = options_service!.root!.input!.entropy!.os!.randoms;
                GetRandomFromRndCommandList_Continuously(rndList);
            }
            catch (NullReferenceException)
            {}

            Monitor.PulseAll(entropy_sync);
        }
    }

    // ::cp:all:dhpOU4GDHUNYcaXq:2023.10.30
    public unsafe void GetRandomFromRndCommandList_Continuously(List<Options_Service.Input.Entropy.InputElement> rndList)
    {
        checked
        {
            foreach (var rnd in rndList)
            {
                var intervals = rnd.intervals!.Interval!.inner;
                foreach (var interval in intervals)
                {
                    if (Terminated)
                        return;

                    if (
                        interval.IntervalType == IntervalTypeEnum.continuously ||
                        interval.IntervalType == IntervalTypeEnum.fast ||
                        interval.IntervalType == IntervalTypeEnum.time ||
                        interval.IntervalType == IntervalTypeEnum.waitAndOnce
                        )
                    {
                        if (string.IsNullOrEmpty(rnd.PathString))
                            throw new Exception($"Regime_Service.ContinuouslyEntropy: for the element '{rnd.GetFullElementName()} at line {rnd.thisBlock.startLine}': file name is empty. The random file name is required.");

                        switch (rnd)
                        {
                            case Options_Service.Input.Entropy.InputFileElement fileElement:
                                StartContinuouslyGetter(rnd, interval, fileElement);
                            break;

                            case Options_Service.Input.Entropy.InputCmdElement cmdElement:
                                GetRandomFromCommand_continuously(rnd, interval, cmdElement);
                            break;
/*
                            case Options_Service.Input.Entropy.InputDirElement dirElement:

                                var files = dirElement.dirInfo!.GetFiles("*", SearchOption.AllDirectories);
                                foreach (var file in files)
                                    ;
                            break;
*/
                            default:
                                throw new Exception($"Regime_Service.ContinuouslyEntropy: for the element '{rnd.GetFullElementName()} at line {rnd.thisBlock.startLine}': unknown command type '{rnd.GetType().Name}'. Fatal error; this is error in the program code, not in the option file");
                        }
                    }
                }
            }
        }
    }

    // При доступе синхронизация lock (continuouslyGetters)
    public List<ContinuouslyGetterRecord> continuouslyGetters = new();

    /// <summary>Инкапсулирует в себя промежуточную губку и предоставляет методы для записи в неё байтов из источника энтропии и получения из неё байтов энтропии</summary>
    public unsafe class ContinuouslyGetterRecord: IDisposable
    {
        public    readonly Thread                   thread;
        protected readonly Keccak_20200918?         keccak;
        protected readonly BytesBuilderForPointers? bb;
                                                                                            /// <summary>Это служебное поле для того, чтобы открытый поток можно было завершить вручную, если необходимо закончить поток, ожидающий ввода-вывода.</summary>
        public volatile FileStream? StreamForClose = null;
                                                                                            /// <summary>true, если объект уже удалён</summary>
        public    bool     Disposed                   {get; protected set;} = false;        /// <summary>Количество байтов, полученное из этого источника (это количество сырых байтов, действительно полученных из источника, без учёта настроек {min,max,avg,EME})</summary>
        public    nint     CountOfBytes               {get; protected set;} = 0;            /// <summary>Аналогично countOfBits. Количество байтов, которое было выведено для пользователя функцией getBytes</summary>
        public    nint     CountOfBytesToUser         {get; protected set;} = 0;            /// <summary>Аналогично countOfBits. Количество байтов, которое было получено из источника энтропии после последнего изъятия битов из губки (необходимо для того, чтобы рассчитать, можно ли сейчас из этой губки что-то брать)</summary>
        public    nint     CountOfBytesFromLastOutput {get; protected set;} = 0;            /// <summary>true, если объект не проинициализирован (сбрасывается после вызова getBytes). Это значение не должно быть нужно пользователю, используйте метод isDataReady. Устанавливается при вызове метода addBytes (addBytes вызывает поток, который собирает энтропию).</summary>
        public    bool     IsInited                   {get; protected set;} = false;
                                                                                            /// <summary>Элемент из настроек, описывающий параметры данного источника энтропии</summary>
        public readonly Options_Service.Input.Entropy.InputElement inputElement;

        /// <summary></summary>
        /// <param name="t">Созданный для данного объекта поток</param>
        /// <param name="inputElement">Объект-описатель</param>
        /// <param name="directInput">Если true, то промежуточная губка не используется</param>
        public ContinuouslyGetterRecord(Thread t, Options_Service.Input.Entropy.InputElement inputElement, bool directInput = false)
        {
            cryptoprime.BytesBuilderForPointers.Record.DoRegisterDestructor(this);

            this.thread = t;
            if (directInput)
                this.bb = new BytesBuilderForPointers() { debugNameForRecords = "ContinuouslyGetterRecord" };
            else
                this.keccak = new Keccak_20200918();

            this.inputElement = inputElement;
        }

        /// <summary>Получает байты из промежуточной губки. <para>Пользователь должен проверить, что isInited установлен перед тем, как использовать этот метод. Если isInited == false, то губка ещё не готова к получению из неё информации: её надо просто пропустить и взять значения из других источников.</para><para>Код безопасен с точки зрения многопоточности.</para><para>countOfBytesFromLastOutput сбрасывается в ноль вне зависимости от количества получаемых данных.</para></summary>
        /// <param name="data">Массив данных, принимающий накопленные байты энтропии из промежуточной губки</param>
        /// <param name="len">Количество байтов энтропии для чтения. Не более чем KeccakPrime.BlockLen (64 байта)</param>
        /// <param name="allowSmallData">Если true, то допускает, что готовой энтропии меньше, чем len</param>
        public nint GetBytes(byte * data, nint len, bool allowSmallData = false)
        {
            checked
            {
                lock (this)
                {
                    if (Disposed)
                        throw new ObjectDisposedException("ContinuouslyGetterRecord.getBytes: disposed (you must check the 'disposed' field and skip the object, if disposed)");
                    if (len > KeccakPrime.BlockLen)
                        throw new ArgumentOutOfRangeException("maxLen", $"ContinuouslyGetterRecord.getBytes: maxLen > KeccakPrime.BlockLen ({len} > {KeccakPrime.BlockLen})");
                    if (!IsInited)
                        throw new InvalidOperationException("ContinuouslyGetterRecord.getBytes: !isInited. You must check the 'isDataReady()' function and skip the object, if the return value is false");
                    if (!IsDataReady(  allowSmallData ? 0 : len  ))
                        throw new InvalidOperationException("ContinuouslyGetterRecord.getBytes: !isDataReady. You must check the 'isDataReady()' function and skip the object, if the return value is false");

                    if (keccak is not null)
                    {
                        KeccakPrime.Keccak_Output_512(data, (byte)len, keccak.S);

                        IsInited     = false;
                        MandatoryUse = false;
                        CountOfBytesFromLastOutput = 0;
                    }
                    else
                    {
                        if (bb!.Count < len)
                            len = bb.Count;

                        using var buffer = Keccak_abstract.allocator.AllocMemory(len, "ContinuouslyGetterRecord.getBytes");
                        bb!.GetBytesAndRemoveIt(buffer);
                        BytesBuilder.CopyTo(len, len, buffer, data);

                        CountOfBytesFromLastOutput -= len;
                        if (bb.Count <= 0)
                        {
                            IsInited     = false;
                            MandatoryUse = false;       // Сообщаем потоку-читателю, что можно завершаться
                        }
                    }

                    CountOfBytesToUser += len;
                    return len;
                }
            }
        }

        /// <summary>Проверяет, готово ли количество данных len для вывода.</summary>
        /// <param name="askedBytes">Количество байтов энтропии, которое хочет получить пользователь.</param>
        /// <returns>false, если данные не готовы. true, если данные готовы. Если false, то из объекта ещё нельзя извлекать данные с помощью функции getBytes.</returns>
        public bool IsDataReady(nint askedBytes)
        {
            checked
            {
                if (!IsInited)
                    return false;

                if (MandatoryUse && CountOfBytesFromLastOutput > 0)
                    return true;

                if (askedBytes == 0)
                    return CountOfBytesFromLastOutput > 0;

                var ReadyBytes = GetCountOfReadyBytes();
                return askedBytes <= ReadyBytes;
            }
        }

        /// <summary>Возвращает верхнюю оценку количества байтов энтропии, которое уже собрано промежуточной губкой</summary>
        public long GetCountOfReadyBytes()
        {
            checked
            {
                // Если min не установлен (равен нулю), то считаем, что на один байт выхода приходится 8-мь байтов входа
                if (inputElement.intervals!.entropy.min <= 0)
                    return CountOfBytesFromLastOutput >> 3;

                return CountOfBytesFromLastOutput / inputElement.intervals!.entropy.min;
            }
        }



        /// <summary>Добавить в губку дополнительные байты из источника энтропии</summary>
        /// <param name="bytes">Количество байтов из источника энтропии; для расчёта общего количества байтов</param>
        /// <param name="len">Количество добавляемых байтов из input. Может отличаться от bytes в связи с тем, что к input могло быть добавлено время или какие-то другие дополнительные параметры. len >= bytes. len > 0 и может быть больше блока шифрования.</param>
        /// <param name="input">Массив, содержащий добавляемые байты.</param>
        public void AddBytes(nint bytes, nint len, byte * input)
        {
            checked
            {
                if (Disposed)
                    throw new ObjectDisposedException("ContinuouslyGetterRecord.addBytes: disposed (you must check the 'disposed' field and skip the object, if disposed)");

                lock (this)
                {
                    if (len <= 0)
                        throw new ArgumentOutOfRangeException(nameof(len), "len <= 0");
                    if (bytes > len)
                        throw new ArgumentOutOfRangeException(nameof(bytes), "bytes > len");

                    if (keccak is not null)
                    {
                        do
                        {
                            nint curLen = len;
                            if (curLen > KeccakPrime.BlockLen)
                                curLen = KeccakPrime.BlockLen;

                            KeccakPrime.Keccak_Input64_512(input, (byte) curLen, keccak.S);
                            keccak.spongeState = Keccak_abstract.SpongeState.DataInputed;
                            keccak.CalcStep();
                            len   -= curLen;
                            input += curLen;
                        }
                        while (len > 0);
                    }
                    else
                    {
                        bb!.AddWithCopy(input, len, Keccak_abstract.allocator);
                    }

                    IsInited                    = true;
                    CountOfBytes               += bytes;
                    CountOfBytesFromLastOutput += bytes;
                }
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (Disposed)
                    throw new Exception("ContinuouslyGetterRecord.Dispose executed twice");

                TryToDispose(keccak);
                TryToDispose(bb);

                Disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        protected volatile bool MandatoryUse = false;
        public    bool MandatoryUseGet => MandatoryUse;
        public void CorrectEntropyForMandatoryUse()
        {
            MandatoryUse = true;
        }
    }

    public void Sleep(int milliseconds, int maxWait = 10000)
    {
        if (milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds), $"milliseconds < 0 ({milliseconds})");

        var cnt = milliseconds;
        int cur;
        while (cnt > 0 && !Terminated)
        {
            cur = cnt;
            if (cur > maxWait)
                cur = maxWait;

            Thread.Sleep(cur);
            cnt -= cur;
        }
    }

    protected unsafe void StartContinuouslyGetter
    (
        Options_Service.Input.Entropy.InputElement rnd,
        Options_Service.Input.Entropy.Interval.InnerIntervalElement interval,
        Options_Service.Input.Entropy.InputFileElement fileElement
    )
    {
        fileElement.FileInfo!.Refresh();

        var t = new Thread
        (
            () =>
            {
                checked
                {
                    WaitForFileExists(fileElement.FileInfo, L("File not found. Wait for creation:") + $" '{fileElement.FileInfo.FullName}'.", L("File found successfully:") + $" '{fileElement.FileInfo.FullName}'.");

                    int len     = 1024;                    // Значение должно быть строго больше KeccakPrime.BlockLen + dateLen
                    int ilen    = len * 2;
                    int dateLen = interval.flags!.date == Flags.FlagValue.no ? 0 : sizeof(long);    // Длина массива, выделенная для данных
                    if (fileElement.FileInfo!.Length > len)
                    {
                        len = (int) fileElement.FileInfo!.Length;
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
                    var cgr = new ContinuouslyGetterRecord(Thread.CurrentThread, rnd, interval.IntervalType == IntervalTypeEnum.waitAndOnce);

                    int sleepTime = interval.Time > 0 ? (int) interval.Time : 1049;

                    try
                    {
                        lock (continuouslyGetters)
                            continuouslyGetters.Add(cgr);

                        while (!this.Terminated)
                        {
                            try
                            {
                                WaitForFileExists(fileElement.FileInfo, L("File not found. Wait for creation:") + $" '{fileElement.FileInfo.FullName}'.", L("File found successfully:") + $" '{fileElement.FileInfo.FullName}'.");
                                if (this.Terminated)
                                    break;

                                // Если мы "быстро" считываем файл,
                                // то мы будем делать это не так и задержка в цикле будет другой
                                if (interval.IntervalType == IntervalTypeEnum.fast)
                                {
                                    GetEntropyFromFile_h(cgr, interval, fileElement, ilen, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span, ref totalBytes, sleepTime);
                                    if (!this.Terminated)
                                        Thread.Sleep(97);
                                }
                                else
                                {
                                    // Здесь мы считываем файл постоянно.
                                    // Задержка нужна только на случай какой-либо ошибки файлового ввода-вывода
                                    var isSleeped = GetEntropyFromFile_h(cgr, interval, fileElement, ilen, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span, ref totalBytes, sleepTime);

                                    if (!isSleeped)
                                        Sleep(sleepTime);
                                }

                                if (interval.IntervalType == IntervalTypeEnum.waitAndOnce)
                                {
                                    if (interval.flags!.ignored != Flags.FlagValue.yes)
                                    {
                                        if (pos <= 0)
                                        {
                                            Console.WriteLine(L("From the file got zero bytes") + $": {fileElement.FileInfo.FullName}");
                                        }
                                        lock (cgr)
                                        {
                                            cgr.AddBytes(totalBytes, pos, input);
                                            cgr.CorrectEntropyForMandatoryUse();
                                        }
                                        pos = 0;
                                        totalBytes = 0;
                                    }

                                    // Ожидание завершеня работы через Thread.Interrupt
                                    // Теперь Thread.Interrupt отсутствует
                                    while (!Terminated && cgr.MandatoryUseGet)
                                        Thread.Sleep(1000);

                                    // Логирование идёт именно сейчас, когда флаг MandatoryUseGet сброшен.
                                    // Это означает, что данные действительно были введены в основную губку.
                                    // Т.к. ввод идёт не сразу весь, то это может занять какое-то время.
                                    SendGetterDebugMsgToConsole(fileElement, cgr);

                                    break;
                                }
                            }
                            catch (ThreadInterruptedException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                DoFormatException(ex);
                                Sleep(3557);
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

    protected unsafe bool GetEntropyFromFile_h(ContinuouslyGetterRecord cgr, Options_Service.Input.Entropy.Interval.InnerIntervalElement interval, Options_Service.Input.Entropy.InputFileElement fileElement, int ilen, byte* input, ref int pos, int dateLen, ref long lastTimeInLog, ref nint lastBytesInLog, byte* buff, Span<byte> span, ref nint totalBytes, int sleepTime)
    {
        checked
        {
            try
            {
                var rs = fileElement.FileInfo!.OpenRead();

                lock (cgr)
                cgr.StreamForClose = rs;

                try
                {
                    return GetEntropyFromFile_h(interval, fileElement, ilen, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span, rs, cgr, ref totalBytes, sleepTime);
                }
                catch (ThreadInterruptedException)
                {}
                catch (Exception ex)
                {
                    DoFormatException(ex);
                    Sleep(3557);
                }
                finally
                {
                    // Закрываем поток именно так, т.к. он может быть закрыт ранее внешним потоком
                    lock (cgr)
                    {
                        cgr.StreamForClose?.Close();
                        cgr.StreamForClose = null;
                    }
                }
            }
            catch (Exception ex)
            {
                DoFormatException(ex);
                Sleep(3557);
            }

            return true;        // Задержка в вызывающей функции уже не нужна: либо выполнена, либо произошло исключение, которое не подразумевает дополнительной задержки
        }
    }

    protected unsafe bool GetEntropyFromFile_h
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
            while (!Terminated)
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
                        cgr.AddBytes(totalBytes, pos, input);

                    pos = 0;
                    totalBytes = 0;

                    if (!ignored)
                    {
                        if (interval.flags!.watchInLog == Flags.FlagValue.yes)
                        {
                            var ticks = DateTime.Now.Ticks;

                            if (ticks - lastTimeInLog >= ticksPerHour)
                                if (lastBytesInLog != cgr.CountOfBytesToUser)
                                {
                                    SendGetterDebugMsgToConsole(fileElement, cgr);

                                    lastTimeInLog  = ticks;
                                    lastBytesInLog = cgr.CountOfBytesToUser;
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

                if (interval.IntervalType == IntervalTypeEnum.time)
                {
                    /*if (interval.flags.log == Flags.FlagValue.yes)
                        Console.WriteLine("breaked: " + fileElement.fileInfo!.FullName);*/
                    break;
                }
            }

            return isSleeped;
        }
    }

    public unsafe void GetRandomFromCommand_continuously
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
                int sleepTime = interval.Time > 0 ? (int) interval.Time : 1049;
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
                            GetRandomFromCommand_continuously_h(rnd, interval, cmdElement, cgr);
                            if (Terminated)
                                break;

                            Sleep(sleepTime);

                            lastLogDate = SendGetterDebugMsgToConsole(interval, cmdElement, lastLogDate, cgr);

                            if (interval.IntervalType == IntervalTypeEnum.waitAndOnce)
                            {
                                // Ожидание завершеня работы через Thread.Interrupt
                                // Теперь Thread.Interrupt отсутствует (присутствует только аварийно)
                                while (!Terminated && cgr.MandatoryUseGet)
                                    Thread.Sleep(1000);

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
                            Console.Error.WriteLine(DoFormatException(ex, false));

                            if (Ex_cnt > 5)
                            {
                                Console.Error.WriteLine(L("Continuously reading has been ended by repeated errors for command") + " " + cmdElement.PathString + " "  + cmdElement.parameters);
                                return;
                            }

                            Sleep(3557);
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
        try
        {
            var ticks = DateTime.Now.Ticks;
            if (interval.flags!.watchInLog == Flags.FlagValue.yes)
                if (ticks - lastLogDate > ticksPerHour)
                    lock (cgr)
                    {
                        lastLogDate = ticks;
                        Console.WriteLine($"{cgr.CountOfBytes} {L("bytes got from")} '{cmdElement.PathString} {cmdElement.parameters}'; {cgr.CountOfBytesToUser} {L("sended to the main sponges (for the entire time of work)")}. [" + L("Awaiting processing in the queue") + $" {bufferRec_current} " + L("bytes from all sources") + "]");
                    }
        }
        catch (ThreadInterruptedException)
        {}

        return lastLogDate;
    }

    // ::cp:all:ZwUElzYfZkK4PfXzUrO7:20231104
    public unsafe void GetRandomFromCommand_continuously_h
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
                RedirectStandardError  = true,
                StandardOutputEncoding = new ASCIIEncoding()
            };
            if (cmdElement.workingDir is not null)
                psi.WorkingDirectory = cmdElement.workingDir;
            if (!string.IsNullOrEmpty(cmdElement.parameters))
                psi.Arguments = cmdElement.parameters;
            if (cmdElement.userName is not null)
            {
                psi.UserName = cmdElement.userName;
                psi.WorkingDirectory ??= Directory.GetCurrentDirectory();
            }

            using var ps = Process.Start(psi);
            while (!ps!.WaitForExit(1000) && !Terminated)
            {}

            // Если мы завершаем работу, но процесс не завершился, то мы не работаем с процессом, т.к. он может подвесить нас на ожидании конца потоков ввода-вывода
            if (!ps.HasExited && Terminated)
                return;

            int len = 256*1024;
            if (interval.Length!.Length > 0)
            {
                len = (int) interval.Length!.Length;
            }
            var buffer  = stackalloc byte[len];
            var buffRec = new Record() {array = buffer, len = len};

            var errorStr = ps.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(errorStr))
            {
                Console.WriteLine(L("Error occured with command") + $": {cmdElement.PathString} {cmdElement.parameters ?? ""}");
                Console.WriteLine(errorStr);
            }

            var readedLen = ps.StandardOutput.BaseStream.Read(buffRec);
            if (readedLen > 0)
            {
                if (doLog && readedLen > 0)
                    WriteToLog(buffRec, readedLen);

                if (!ignored)
                {
                    lock (cgr)
                    {
                        cgr.AddBytes(readedLen, readedLen, buffRec);
                        if (interval.IntervalType == IntervalTypeEnum.waitAndOnce)
                            cgr.CorrectEntropyForMandatoryUse();
                    }
                }
            }

            buffRec.array = null;
            buffRec.Dispose();
        }
    }

    public unsafe void SendGetterDebugMsgToConsole(Options_Service.Input.Entropy.InputFileElement fileElement, ContinuouslyGetterRecord cgr)
    {
        checked
        {
            try
            {
                lock (cgr)
                {
                    Console.WriteLine($"{cgr.CountOfBytes} {L("bytes got from")} '{fileElement.FileInfo!.FullName}'; {cgr.CountOfBytesToUser} {L("sended to the main sponges (for the entire time of work)")}. [" + L("Awaiting processing in the queue") + $" {bufferRec_current} " + L("bytes from all sources") + "]");
                }
            }
            catch (ThreadInterruptedException)
            {}
        }
    }
}
