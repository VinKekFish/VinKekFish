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
using alien_SkeinFish;
using System.Collections;
using System.Runtime.InteropServices;

public partial class Regime_Service
{
                                                                                            /// <summary>Указывает папку, где содержатся данные, хранящиеся между запусками программы. В том числе, данные по рандомизации на старте</summary>
    public DirectoryInfo? RandomAtFolder;
    public DirectoryInfo? RandomAtFolder_Static;
    public RandomAtFolder_Current? randomAtFolder_Current;
    public const int RandomAtFolder_Current_countOfFiles = 4;

    public const int OutputStrenght = 11*1024;      // При изменении этого, поменять инициализацию VinKekFish
    public VinKekFishBase_KN_20210525 VinKekFish    = new(VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(11), 11, 1);   // 275 == inKekFish.EXTRA_ROUNDS_K, K = 11, ThreadCount = 1
    public CascadeSponge_mt_20230930  CascadeSponge = new(OutputStrenght, ThreadsCount: Environment.ProcessorCount - 1) { StepTypeForAbsorption = CascadeSponge_1t_20230905.TypeForShortStepForAbsorption.weak };

    public bool IsInitiated { get; protected set; } = false;
                                                                    /// <summary>Буферная запись, которая создаётся в StartEntropy и используется в InputEntropyFromSources. Осторожно, она хранит данные между запусками функций: её нельзя нигде использовать. Её размер MAX_RANDOM_AT_START_FILE_LENGTH</summary>
    protected Record? bufferRec = null;                             /// <summary>Текущее значение объёма данных, которые  хранятся в bufferRec</summary>
    protected nint    bufferRec_current = 0;

    /// <summary>Функция вызывается для инициализации всех губок, накапливающих энтропию</summary>
    protected unsafe virtual void StartEntropy()
    {
        lock (entropy_sync)
        {
            Record? rnd = null;
            try
            {
                if (VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0)
                    throw new Exception("Regime_Service.StartEntropy: Fatal algorithmic error: VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0");

                CreateFolders();

                ExecEntorpy_now = DateTime.Now.Ticks;
                bufferRec = allocator.AllocMemory(MAX_RANDOM_AT_START_FILE_LENGTH);

                var sb = new StringBuilder();
                var eVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
                foreach (DictionaryEntry entry in eVars)
                    sb.AppendLine((entry.Key as String) + ": " + (entry.Value as String));

                sb.AppendLine("OSVersion: "   + Environment.OSVersion);
                sb.AppendLine("CLR Version: " + Environment.Version);
                sb.AppendLine("User: "        + Environment.UserName);
                sb.AppendLine("Cur. dir.: "   + Environment.CurrentDirectory);
                sb.AppendLine("MTI: "         + Environment.CurrentManagedThreadId);
                sb.AppendLine("MN: "          + Environment.MachineName);
                sb.AppendLine("PID: "         + Environment.ProcessId);
                sb.AppendLine("PC: "          + Environment.ProcessorCount);

                // Console.WriteLine(sb.ToString().Length);
                // Console.WriteLine(sb.ToString());
                var bb = new UTF8Encoding().GetBytes(  sb.ToString()  );

                var simpleData = stackalloc byte[sizeof(long) + bb.Length];
                BytesBuilder.ULongToBytes((ulong)ExecEntorpy_now, simpleData, sizeof(long));

                if (bb.Length > 0)
                {
                    fixed (byte * bp = bb)
                        BytesBuilder.CopyTo(bb.Length, bb.Length, bp, simpleData + sizeof(long));
                }
                else
                {
                    Console.WriteLine("Regime_Service.StartEntropy: Warning: Environment.GetEnvironmentVariables return null");
                }

                CascadeSponge.InitEmptyThreeFish((ulong)ExecEntorpy_now);
                CascadeSponge.InitEmptySubstitutionTable((ushort) ExecEntorpy_now);
                CascadeSponge.Step(regime: 3, data: simpleData, dataLen: sizeof(long) + bb.Length);
                CascadeSponge.InitThreeFishByCascade(1, false, countOfSteps: 1);    // Упрощённая предварительная инициализация с пониженным количеством шагов

                nint realRandomLength = 0;
                nint rndCount         = 0;
                using (var rndbytes = new BytesBuilderForPointers())
                {
                    realRandomLength = GetRandomFromOSEntropy_Startup(bufferRec, rndbytes, realRandomLength);

                    if (realRandomLength < 16)
                        throw new Exception("Regime_Service.StartEntropy: realRandomLength < 16.\n" + L("Check the options file for the input.entropy.OS.file element. Interval element with 'once' or '--' keywords required"));

                    GetStartupEntropy(bufferRec, rndbytes);

                    realRandomLength = GetRandomFromStandardEntropy_Startup(bufferRec, rndbytes, realRandomLength);

                    rnd = rndbytes.GetBytes();
                    rndCount = rnd.len;
                }

                if (this.Terminated)
                    return;

                Console.WriteLine(L("Startup entropy absorption has begun: initialization continues"));

                CascadeSponge.Step(regime: 1, data: rnd, dataLen: rnd.len);
                CascadeSponge.Step(CascadeSponge.countStepsForKeyGeneration, regime: 255, inputRegime: CascadeSponge_1t_20230905.InputRegime.overwrite);
                CascadeSponge.InitThreeFishByCascade(1, false, CascadeSponge.maxDataLen >> 1);


                // Делаем первичную инициализацию временем при старте
                // Ограничиваем вывод, иначе получается слишком долго
                /*var sz = (int)(realRandomLength + rndCount);
                if (sz > OutputStrenght)
                    sz = OutputStrenght;
                    */
                var sz = Threefish_slowly.twLen - sizeof(long);

                var arr = stackalloc byte[sizeof(long) + sz];
                using var rec = new Record() { array = arr, len = sizeof(long) + sz };
                BytesBuilder.ULongToBytes((ulong)ExecEntorpy_now, arr, sizeof(long));

                nint curLen;
                for (nint pointer = 0; pointer < sz; pointer += curLen)
                {
                    curLen = sz - pointer;
                    if (curLen > CascadeSponge.maxDataLen)
                        curLen = CascadeSponge.maxDataLen;

                    CascadeSponge.Step(CascadeSponge.countStepsForKeyGeneration, regime: 7);
                    BytesBuilder.CopyTo(curLen, rec.len, s: CascadeSponge.lastOutput, t: arr, targetIndex: sizeof(long) + pointer);
                }

                if (Terminated)
                    return;

                Console.WriteLine(L("Deep initialization has begun: initialization continues"));
/*
                Parallel.Invoke
                (
                    () =>
                    {
                        VinKekFish.input = new BytesBuilderStatic(MAX_RANDOM_AT_START_FILE_LENGTH);

                        // rec является синхропосылкой, но т.к. ключа нет, то rec вводится как ключ
                        VinKekFish.Init1
                        (
                            keyForPermutations: rec,
                            PreRoundsForTranspose: VinKekFish.EXTRA_ROUNDS_K - VinKekFish.Calc_OptimalRandomPermutationCount(rec.len),
                            ThreeFishInitSteps: 1
                        );
                        VinKekFish.Init2(key: rnd, TweakInit: rec);
                    },

                    () =>
                    {
                        // Вводим здесь только время и снова переопределяем ключи шифрования ThreeFish
                        CascadeSponge.step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, regime: 3, data: rec.array, dataLen: sizeof(long), inputRegime: CascadeSponge_1t_20230905.InputRegime.xor);
                        CascadeSponge.InitThreeFishByCascade(1, false, CascadeSponge.maxDataLen >> 1);
                    }
                );*/

                VinKekFish.input = new BytesBuilderStatic(MAX_RANDOM_AT_START_FILE_LENGTH);

                VinKekFish.Init1
                (
                    PreRoundsForTranspose: VinKekFish.EXTRA_ROUNDS_K - 4, // VinKekFish.Calc_OptimalRandomPermutationCount(K),
                    prngToInit: CascadeSponge
                );
                Console.WriteLine(L("Deep initialization: VinKekFish.Init1 ended"));
                VinKekFish.Init2(key: rnd, TweakInit: rec);
                Console.WriteLine(L("Deep initialization: VinKekFish.Init2 ended"));

                if (this.Terminated)
                    return;

                // Вводим здесь только время и снова переопределяем ключи шифрования ThreeFish
                CascadeSponge.Step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, regime: 3, data: rec.array, dataLen: sizeof(long), inputRegime: CascadeSponge_1t_20230905.InputRegime.xor);
                CascadeSponge.InitThreeFishByCascade(1, false, CascadeSponge.maxDataLen >> 1);

                Console.WriteLine(L("Deep initialization: CascadeSponge.InitThreeFishByCascade ended"));

                SetCountOfBytesCounters_and_ClearBufferRec();
                IsInitiated = true;
            }
            catch
            {
                DoTerminate();
                throw;
            }
            finally
            {
                TryToDispose(rnd);
                Monitor.PulseAll(entropy_sync);
            }
        }
    }

    protected virtual unsafe void SetCountOfBytesCounters_and_ClearBufferRec()
    {
        bufferRec!.Clear();
        bufferRec_current = 0;

        CountOfBytesCounterTotal = countOfBytesCounterTotal_h.Clone();
        CountOfBytesCounterCorr  = countOfBytesCounterCorr_h .Clone();
    }

    protected unsafe void RemoveFromCountOfBytesCounters(nint outputStrenght)
    {
        countOfBytesCounterCorr_h.RemoveBytes(outputStrenght);
        CountOfBytesCounterCorr  .RemoveBytes(outputStrenght);
    }

    public unsafe nint GetRandomFromOSEntropy_Startup(Record bufferRec, BytesBuilderForPointers rndbytes, nint realRandomLength)
    {
        checked
        {
            try
            {
                var rndList      = options_service!.root!.input!.entropy!.os!.randoms;
                realRandomLength = GetRandomFromRndCommandList_Startup(bufferRec, rndbytes, realRandomLength, rndList);
            }
            catch (NullReferenceException)
            { }
        }

        return realRandomLength;
    }

    public unsafe nint GetRandomFromStandardEntropy_Startup(Record bufferRec, BytesBuilderForPointers rndbytes, nint realRandomLength)
    {
        checked
        {
            try
            {
                var rndList      = options_service!.root!.input!.entropy!.standard!.randoms;
                realRandomLength = GetRandomFromRndCommandList_Startup(bufferRec, rndbytes, realRandomLength, rndList);
            }
            catch (NullReferenceException)
            { }
        }

        return realRandomLength;
    }

    // ::cp:all:dhpOU4GDHUNYcaXq:2023.10.30
    public unsafe nint GetRandomFromRndCommandList_Startup(Record bufferRec, BytesBuilderForPointers rndbytes, nint realRandomLength, List<Options_Service.Input.Entropy.InputElement> rndList)
    {
        checked
        {
            var sb = new StringBuilder();
            foreach (var rnd in rndList)
            {
                var intervals = rnd.intervals!.Interval!.inner;
                foreach (var interval in intervals)
                {
                    if (interval.IntervalType == IntervalTypeEnum.once)
                    {
                        if (string.IsNullOrEmpty(rnd.PathString))
                            throw new Exception($"Regime_Service.StartEntropy: for the element '{rnd.GetFullElementName()} at line {rnd.thisBlock.startLine}': file name is empty. The random file name is required.");
                                                    #pragma warning disable IDE0066
                        switch (rnd)
                        {
                            case Options_Service.Input.Entropy.InputFileElement fileElement:
                                realRandomLength = GetRandomFromFile(bufferRec, rndbytes, realRandomLength, sb, rnd, fileElement.FileInfo!, interval);
                            break;

                            case Options_Service.Input.Entropy.InputCmdElement cmdElement:
                                realRandomLength = GetRandomFromCommand(bufferRec, rndbytes, realRandomLength, sb, cmdElement, interval);
                            break;
/*
                            case Options_Service.Input.Entropy.InputDirElement dirElement:

                                var files = dirElement.dirInfo!.GetFiles("*", SearchOption.AllDirectories);
                                foreach (var file in files)
                                    realRandomLength = getRandomFromFile(bufferRec, rndbytes, realRandomLength, sb, rnd, file, interval);
                            break;
*/
                            default:
                                throw new Exception($"Regime_Service.StartEntropy: for the element '{rnd.GetFullElementName()} at line {rnd.thisBlock.startLine}': unknown command type '{rnd.GetType().Name}'. Fatal error; this is error in the program code, not in the option file");
                        }
                    }
                }
            }
                                                    #pragma warning restore IDE0066
            if (sb.Length > 0)
            {
                Console.WriteLine(L("Initialization got random values from file or command"));
                Console.WriteLine(sb.ToString());
            }

            return realRandomLength;
        }
    }

    // ::cp:all:ZwUElzYfZkK4PfXzUrO7:20231104
    public unsafe nint GetRandomFromCommand(Record bufferRec, BytesBuilderForPointers rndbytes, nint realRandomLength, StringBuilder sb, Options_Service.Input.Entropy.InputCmdElement cmdElement, Options_Service.Input.Entropy.Interval.InnerIntervalElement interval)
    {
        checked
        {
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
                psi.WorkingDirectory ??= Directory.GetCurrentDirectory();
            }

            int timeout = 3*60_000;
            if (cmdElement.timeout > 0)
                timeout = cmdElement.timeout;

            using var ps = Process.Start(psi);
            if (!ps!.WaitForExit((int) timeout))
            {
                sb.AppendLine($"WARNING: Process is hung (with timeout ${timeout} ms): ${psi.FileName} + ${psi.Arguments}");
                return 0;
            }

            var readedLen = ps.StandardOutput.BaseStream.Read(bufferRec);
            var ignored   = false;
            // fixed (byte * bytes = ob)
            if (readedLen > 0)
            {
                if (interval.flags!.log == Flags.FlagValue.yes && readedLen > 0)
                    WriteToLog(bufferRec, readedLen);

                if (interval.flags!.ignored == Flags.FlagValue.yes)
                {
                    ignored = true;
                }
                else
                    rndbytes.AddWithCopy(bufferRec.array, readedLen, allocator: allocator);
            }

            if (!ignored)
            {
                sb.AppendLine($"len = {readedLen, 5}; name = {cmdElement.PathString} {cmdElement.parameters}");
                return realRandomLength + readedLen;
            }
            else
            {
                sb.AppendLine($"ignored name = {cmdElement.PathString} {cmdElement.parameters}");
                return realRandomLength;
            }
        }
    }

    public unsafe nint GetRandomFromFile(Record bufferRec, BytesBuilderForPointers rndbytes, nint realRandomLength, StringBuilder sb, Options_Service.Input.Entropy.InputElement rnd, FileInfo rndFileInfo, Options_Service.Input.Entropy.Interval.InnerIntervalElement interval)
    {
        checked
        {
            rndFileInfo!.Refresh();
            var len = (nint)rndFileInfo.Length;

            if (interval.Length!.Length > 0)
                len = (nint)interval.Length!.Length;

            if (len > bufferRec.len)
                throw new Options_Service_Exception($"Regime_Service.StartEntropy: for the element '{rnd.GetFullElementName()}' at line {rnd.thisBlock.startLine} ('{rndFileInfo.FullName}'): the file length too match. The length ({len}) of the random file must be lowest ${MAX_RANDOM_AT_START_FILE_LENGTH}.");
            if (len <= 0)
            {
                // throw new Options_Service_Exception($"Regime_Service.StartEntropy: for the element '{rnd.getFullElementName()}' at line {rnd.thisBlock.startLine} ('{rndFileInfo.FullName}'): the file length is zero. The length ({len}) of the random file must greater than zero. Please, ensure the file length is not zero and length for the read operation greater than zero. /proc/cpuinfo and etc. can readed by comman cat /proc/cpuinfo (see ls -l file_name where length == 0)");*/
                // Некоторые файлы не имеют размера, например, /dev/random или /proc/cpuinfo
                // Иногда бывает, что и файл может попасться пустой - программу надо устойчиво запустить всё равно                
                Console.Error.WriteLine($"Regime_Service.StartEntropy: for the element '{rnd.GetFullElementName()}' at line {rnd.thisBlock.startLine} ('{rndFileInfo.FullName}'): the file length is zero. The length ({len}) of the random file must greater than zero. Please, ensure the file length is not zero and length for the read operation greater than zero. /proc/cpuinfo and etc. can readed by comman cat /proc/cpuinfo (see ls -l file_name where length == 0)");
                sb.AppendLine($"EMPTY name = {rndFileInfo.FullName}");
                return realRandomLength;
            }

            var ignored    = false;
            var bufferSpan = new Span<byte>(bufferRec, len == 0 ? MAX_RANDOM_AT_START_FILE_LENGTH : (int)len);

            int readedLen = 0;
            using (var rs = rndFileInfo.OpenRead())
            {
                readedLen = rs.Read(bufferSpan);

                if (readedLen <= 0)
                {
                    // throw new Exception($"Regime_Service.StartEntropy ('{rndFileInfo.FullName}'): for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': factually readed the {readedLen} bytes. It is error. File must be greater than zero");
                    Console.Error.WriteLine($"Regime_Service.StartEntropy: for the element '{rnd.GetFullElementName()}' at line {rnd.thisBlock.startLine} ('{rndFileInfo.FullName}'): the file length is zero. The length ({len}) of the random file must greater than zero. Please, ensure the file length is not zero and length for the read operation greater than zero. /proc/cpuinfo and etc. can readed by comman cat /proc/cpuinfo (see ls -l file_name where length == 0)");
                    sb.AppendLine($"EMPTY (from read) name = {rndFileInfo.FullName}");
                    return realRandomLength;
                }
            }

            if (interval.flags!.ignored == Flags.FlagValue.yes)
            {
                ignored = true;
                if (interval.flags!.log == Flags.FlagValue.yes && readedLen > 0)
                    WriteToLog(bufferRec, readedLen);
            }
            else
                rndbytes.AddWithCopy(bufferRec << bufferRec.len - readedLen, allocator: allocator);

            if (!ignored)
            {
                realRandomLength += len;
                sb.AppendLine($"len = {len,5}; name = {rndFileInfo.FullName}");
            }
            else
            {
                sb.AppendLine($"ignored name = {rndFileInfo.FullName}");
            }

            return realRandomLength;
        }
    }

    public static readonly object WriteToLog_sync = new();
    public static unsafe void WriteToLog(Record bufferRec, int readedLen)
    {
        checked
        {
            var log = new FileInfo("log.log");
            using (var ws = log.OpenWrite())
            {
                lock (WriteToLog_sync)
                {
                    ws.Seek(0, SeekOrigin.End);
                    ws.Write(bufferRec << bufferRec.len - readedLen);
                }
            }
        }
    }

    /// <summary>Создаёт папки для сохранения энтропии между запусками</summary>
    protected virtual void CreateFolders()
    {
        RandomAtFolder = options_service!.root!.Path!.randomAtStartFolder!.dir!; RandomAtFolder.Refresh();

        if (!RandomAtFolder.Exists)
            RandomAtFolder.Create();

        RandomAtFolder_Static = new DirectoryInfo(Path.Combine(RandomAtFolder.FullName, "static")); RandomAtFolder.Refresh();
        if (!RandomAtFolder_Static.Exists)
            RandomAtFolder_Static.Create();

        Console.WriteLine($"RandomAtFolder = {RandomAtFolder.FullName}");

        randomAtFolder_Current = new RandomAtFolder_Current(this);
    }

    public const int MAX_RANDOM_AT_START_FILE_LENGTH = 256*1024;

    protected unsafe virtual void GetStartupEntropy(Record bufferRec, BytesBuilderForPointers rndbytes)
    {
        var sb    = new StringBuilder();
        var files = RandomAtFolder!.GetFiles("*", SearchOption.AllDirectories);

        if (files.Length > 0)
        {
            sb.AppendLine($"{L("Initialization got random from file")}");
            foreach (var file in files)
            {
                using (var readStream = file.OpenRead())
                {
                    InputFromFileName   (bufferRec, file, rndbytes);
                    InputFromFileContent(bufferRec, file, readStream, rndbytes);
                    InputFromFileAttr   (bufferRec, file, rndbytes);        // Это идёт последним, т.к. использует текущее время для доп. энтропии, а это время зависит от длины файла и задержек при работе с файлом
                }

                sb.AppendLine($"len = {file.Length, 5}; name = {file.FullName}");
            }

            Console.WriteLine(sb.ToString());
        }


        unsafe void InputFromFileContent(Record bufferRec, FileInfo file, FileStream readStream, BytesBuilderForPointers rndbytes)
        {
            int flen = (int) file.Length;
            if (file.Length > bufferRec.len)
                throw new ArgumentOutOfRangeException("InputFromFile: flen > MAX_RANDOM_AT_START_FILE_LENGTH");
            if (flen <= 0)
                //throw new ArgumentOutOfRangeException("InputFromFile: flen <= 0");
                return;

            var span = new Span<byte>(bufferRec, flen);
            readStream.Read(span);

            rndbytes.AddWithCopy(bufferRec, flen, allocator);
        }

        unsafe void InputFromFileAttr(Record bufferRec, FileInfo file, BytesBuilderForPointers rndbytes)
        {
            var esize = sizeof(long);
            var size  = esize*4;
            var bytes = stackalloc byte[size];

            // Получаем энтропию как из времени последней записи в файл,
            // так и из текущего времени, т.к. оно может зависеть от загрузки жётского диска и быть частично случайным
            BytesBuilder.ULongToBytes((ulong) file.LastWriteTimeUtc.Ticks, bytes, size, 0);
            BytesBuilder.ULongToBytes((ulong) file.LastAccessTime  .Ticks, bytes, size, esize);
            BytesBuilder.ULongToBytes((ulong) DateTime.Now.Ticks         , bytes, size, esize*2);
            BytesBuilder.ULongToBytes((ulong) file.Length                , bytes, size, esize*3);

            rndbytes.AddWithCopy(bytes, size, allocator);
        }

        unsafe void InputFromFileName(Record bufferRec, FileInfo file, BytesBuilderForPointers rndbytes)
        {
            var size  = file.Name.Length * sizeof(char);

            fixed (char * str = file.Name)
                BytesBuilder.CopyTo(size, bufferRec.len, (byte *) str, bufferRec);

            rndbytes.AddWithCopy(bufferRec, size, allocator);
        }
    }
}
