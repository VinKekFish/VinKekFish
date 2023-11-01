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
            var sb = new StringBuilder();
            foreach (var rnd in rndList)
            {
                var intervals = rnd.intervals!.interval!.inner;
                foreach (var interval in intervals)
                {
                    if (interval.time == 0)
                    {
                        if (string.IsNullOrEmpty(rnd.PathString))
                            throw new Exception($"Regime_Service.ContinuouslyEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': file name is empty. The random file name is required.");

                        switch (rnd)
                        {
                            case Options_Service.Input.Entropy.InputFileElement fileElement:
                                StartContinuouslyGetter(rnd, interval, fileElement);
                            break;

                            case Options_Service.Input.Entropy.InputCmdElement cmdElement:
                                
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

            if (sb.Length > 0)
            {
                Console.WriteLine(L("Continuously getter started:"));
                Console.WriteLine(sb.ToString());
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
                    isInited = false;
                    countOfBytesFromLastOutput = 0;

                    countOfBytesToUser += len;
                }
            }
        }

        /// <summary>Проверяет, готово ли количество данных len для вывода.</summary>
        /// <param name="len">Количество байтов энтропии, которое хочет получить пользователь.</param>
        /// <returns>false, если данные не готовы. true, если данные готовы. Если false, то из объекта ещё нельзя извлекать данные с помощью функции getBytes.</returns>
        public bool isDataReady(nint len)
        {
            checked
            {
                if (!isInited)
                    return false;

                var val = GetCountOfReadyBytes();
                return len > val;
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

                disposed = true;
            }
        }
    }

    protected unsafe void StartContinuouslyGetter(Options_Service.Input.Entropy.InputElement rnd, Options_Service.Input.Entropy.Interval.InnerIntervalElement interval, Options_Service.Input.Entropy.InputFileElement fileElement)
    {
        fileElement.fileInfo!.Refresh();
        if (!fileElement.fileInfo.Exists)
        {
            Console.Error.WriteLine($"Regime_Service.StartContinuouslyGetter: file not found: {fileElement.fileInfo.FullName}");
            return;
        }

        var t = new Thread
        (
            () =>
            {
                checked
                {
                    int len     = 1024;                    // Значение должно быть строго больше KeccakPrime.BlockLen + dateLen
                    var input   = stackalloc byte[len*2];  // Массив, из которого будет вводиться энтропия в губку keccak
                    int pos     = 0;
                    int dateLen = interval.flags!.date == Flags.FlagValue.no ? 0 : sizeof(long);

                    long lastTimeInLog  = DateTime.Now.Ticks;
                    nint lastBytesInLog = 0;

                    var buff = stackalloc byte[len];
                    var span = new Span<byte>(buff, len - dateLen);

                    while (!this.Terminated)
                    {
                        try
                        {
                            getEntropyFromFile_h(rnd, interval, fileElement, len, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(VinKekFish_Utils.Memory.formatException(ex));
                            Thread.Sleep(5000);
                        }
                    }
                } // checked
            } // end thread function
        );

        t.IsBackground = true;
        t.Start();
    }

    protected unsafe void getEntropyFromFile_h(Options_Service.Input.Entropy.InputElement rnd, Options_Service.Input.Entropy.Interval.InnerIntervalElement interval, Options_Service.Input.Entropy.InputFileElement fileElement, int len, byte* input, ref int pos, int dateLen, ref long lastTimeInLog, ref nint lastBytesInLog, byte* buff, Span<byte> span)
    {
        checked
        {
            using (var rs = fileElement.fileInfo!.OpenRead())
            {
                var cgr = new ContinuouslyGetterRecord(Thread.CurrentThread, rnd);
                lock (continuouslyGetters)
                    continuouslyGetters.Add(cgr);

                try
                {
                    nint totalBytes = 0;
                    getEntropyFromFile_h(interval, fileElement, len, input, ref pos, dateLen, ref lastTimeInLog, ref lastBytesInLog, buff, span, rs, cgr, ref totalBytes);

                    if (interval.flags!.watchInLog == Flags.FlagValue.yes)
                        SendDebugMsgToConsole(fileElement, cgr);
                }
                catch (ThreadInterruptedException)
                { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(VinKekFish_Utils.Memory.formatException(ex));
                    Thread.Sleep(5000);
                }
                finally
                {
                    lock (continuouslyGetters)
                        continuouslyGetters.Remove(cgr);

                    cgr.Dispose();
                }
            } // using
        }
    }

    protected unsafe void getEntropyFromFile_h(Options_Service.Input.Entropy.Interval.InnerIntervalElement interval, Options_Service.Input.Entropy.InputFileElement fileElement, int len, byte* input, ref int pos, int dateLen, ref long lastTimeInLog, ref nint lastBytesInLog, byte* buff, Span<byte> span, FileStream rs, ContinuouslyGetterRecord cgr, ref nint totalBytes)
    {
        checked
        {
            while (true)
            {
                var bytesReaded = rs.Read(span);
                if (bytesReaded <= 0 || Terminated)
                    break;

                if (pos >= KeccakPrime.BlockLen)
                {
                    cgr.addBytes(totalBytes, pos, input);
                    pos = 0;
                    totalBytes = 0;

                    if (interval.flags!.watchInLog == Flags.FlagValue.yes)
                    {
                        var ticks = DateTime.Now.Ticks;

                        if (ticks - lastTimeInLog >= ticksPerHour)
                            if (lastBytesInLog != cgr.countOfBytesToUser)
                            {
                                SendDebugMsgToConsole(fileElement, cgr);

                                lastTimeInLog  = ticks;
                                lastBytesInLog = cgr.countOfBytesToUser;
                            }
                    }
                }

                if (dateLen > 0)
                {
                    var ticks = DateTime.Now.Ticks;
                    BytesBuilder.ULongToBytes((ulong)ticks, input, KeccakPrime.BlockLen, pos);
                    pos += dateLen;
                }

                BytesBuilder.CopyTo(len, KeccakPrime.BlockLen, buff, input, pos, bytesReaded);
                pos        += bytesReaded;
                totalBytes += bytesReaded;

                BytesBuilder.ToNull(len, buff);
            }
        }
    }

    public unsafe void SendDebugMsgToConsole(Options_Service.Input.Entropy.InputFileElement fileElement, ContinuouslyGetterRecord cgr)
    {
        checked
        {
            lock (entropy_sync)
            {
                Console.WriteLine($"{cgr.countOfBytes} bytes got from '{fileElement.fileInfo!.FullName}'; {cgr.countOfBytesToUser} sended to the main sponges.");
            }
        }
    }
}
