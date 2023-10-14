// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.ComponentModel.DataAnnotations;
using System.Text;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{
                                                                                            /// <summary>Указывает папку, где содержатся данные, хранящиеся между запусками программы. В том числе, данные по рандомизации на старте</summary>
    public DirectoryInfo? RandomAtFolder;
    public DirectoryInfo? RandomAtFolder_Static;
    public FileInfo?      RandomAtFolder_Current0;
    public FileInfo?      RandomAtFolder_Current1;

    public const int OutputStrenght = 11*1024;      // При изменении этого, поменять инициализацию VinKekFish
    public VinKekFishBase_KN_20210525 VinKekFish    = new VinKekFishBase_KN_20210525(VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(11), 11, 1);   // 275 == inKekFish.EXTRA_ROUNDS_K, K = 11
    public CascadeSponge_mt_20230930  CascadeSponge = new CascadeSponge_mt_20230930(OutputStrenght, ThreadsCount: Environment.ProcessorCount - 1);

    public bool isInitiated { get; protected set; } = false;

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

                ExecEntorpy_now     = DateTime.Now.Ticks;
                using var bufferRec = allocator.AllocMemory(MAX_RANDOM_AT_START_FILE_LENGTH);

                CascadeSponge.InitEmptyThreeFish((ulong)ExecEntorpy_now);
                CascadeSponge.InitThreeFishByCascade(1, false);

                nint realRandomLength = 0;

                
                nint    rndCount = 0;
                using (var rndbytes = new BytesBuilderForPointers())
                {
                    realRandomLength = getRandomFromOSEntropy_Startup(bufferRec, rndbytes, realRandomLength);

                    if (realRandomLength < 16)
                        throw new Exception("Regime_Service.StartEntropy: realRandomLength < 16.\n" + L("Check the options file for the input.entropy.OS.file element. Interval element with 'once' or '--' keywords required"));

                    GetStartupEntropy(bufferRec, rndbytes);
                    rnd      = rndbytes.getBytes();
                    rndCount = rnd.len;
                }

                CascadeSponge.step(regime: 1, data: rnd, dataLen: rnd.len);
                CascadeSponge.step(CascadeSponge.countStepsForKeyGeneration, regime: 255, inputRegime: CascadeSponge_1t_20230905.InputRegime.overwrite);
                CascadeSponge.InitThreeFishByCascade(1, false, CascadeSponge.maxDataLen >> 1);


                // Делаем первичную инициализацию временем при старте
                var sz  = (int)(realRandomLength + rndCount);
                var arr = stackalloc byte[sizeof(long) + sz];
                using var rec = new Record() { array = arr, len = sizeof(long) + sz };
                BytesBuilder.ULongToBytes((ulong)ExecEntorpy_now, arr, sizeof(long));

                nint curLen;
                for (nint pointer = 0; pointer < sz; pointer += curLen)
                {
                    curLen = sz - pointer;
                    if (curLen > CascadeSponge.maxDataLen)
                        curLen = CascadeSponge.maxDataLen;

                    CascadeSponge.step(CascadeSponge.countStepsForKeyGeneration, regime: 7);
                    BytesBuilder.CopyTo(curLen, rec.len, CascadeSponge.lastOutput, arr, sizeof(long) + pointer);
                }

                if (Terminated)
                    return;

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
                );

                isInitiated = true;
            }
            catch
            {
                doTerminate();
                throw;
            }
            finally
            {
                rnd?.Dispose();
                Monitor.PulseAll(entropy_sync);
            }
        }
    }

    public unsafe nint getRandomFromOSEntropy_Startup(Record bufferRec, BytesBuilderForPointers rndbytes, nint realRandomLength)
    {
        checked
        {
            try
            {
                var rndList      = options_service!.root!.input!.entropy!.os!.random;
                realRandomLength = getRandomFromRndCommandList_Startup(bufferRec, rndbytes, realRandomLength, rndList);
            }
            catch (NullReferenceException)
            { }
        }

        return realRandomLength;
    }

    public unsafe nint getRandomFromRndCommandList_Startup(Record bufferRec, BytesBuilderForPointers rndbytes, nint realRandomLength, List<Options_Service.Input.Entropy.InputFileElement> rndList)
    {
        checked
        {
            var sb = new StringBuilder();
            foreach (var rnd in rndList)
            {
                var intervals = rnd.intervals!.interval!.inner;
                foreach (var interval in intervals)
                {
                    if (interval.time == -1 || interval.time == 0)
                    {
                        if (string.IsNullOrEmpty(rnd.PathString))
                            throw new Exception($"Regime_Service.StartEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': file name is empty. The random file name is required.");

                        var rndFileInfo = new FileInfo(rnd.PathString); rndFileInfo.Refresh();
                        var len = (nint)rndFileInfo.Length;

                        if (interval.Length!.Length > 0)
                            len = (nint)interval.Length!.Length;

                        if (len > MAX_RANDOM_AT_START_FILE_LENGTH)
                            throw new Exception($"Regime_Service.StartEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': the file length too match. The length ({len}) of the random file must be lowest ${MAX_RANDOM_AT_START_FILE_LENGTH}.");

                        var bufferSpan = new Span<byte>(bufferRec, (int)len);
                        using (var rs = rndFileInfo.OpenRead())
                        {
                            rs.Read(bufferSpan);
                            rndbytes.addWithCopy(bufferRec << bufferRec.len - len, allocator: allocator);
                        }

                        // CascadeSponge.step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, data: bufferRec, dataLen: len, regime: 2);

                        realRandomLength += len;
                        sb.AppendLine($"len = {len, 5}; name = {rndFileInfo.FullName}");
                    }
                }
            }

            if (sb.Length > 0)
            {
                Console.WriteLine(L("Initialization got random from file"));
                Console.WriteLine(sb.ToString());
            }

            return realRandomLength;
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

        RandomAtFolder_Current0 = new FileInfo(  Path.Combine(RandomAtFolder_Static.FullName, "current.0")  );
        RandomAtFolder_Current1 = new FileInfo(  Path.Combine(RandomAtFolder_Static.FullName, "current.1")  );
    }

    public const int MAX_RANDOM_AT_START_FILE_LENGTH = 256*1024;

    protected unsafe virtual void GetStartupEntropy(Record bufferRec, BytesBuilderForPointers rndbytes)
    {
        var sb    = new StringBuilder();
        var files = RandomAtFolder_Static!.GetFiles("*", SearchOption.AllDirectories);

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
            if (file.Length > MAX_RANDOM_AT_START_FILE_LENGTH)
                throw new ArgumentOutOfRangeException("InputFromFile: flen > MAX_RANDOM_AT_START_FILE_LENGTH");
            if (flen <= 0)
                //throw new ArgumentOutOfRangeException("InputFromFile: flen <= 0");
                return;

            var span = new Span<byte>(bufferRec, flen);
            readStream.Read(span);
/*
            VinKekFish.input!.add(bytes, flen);
            do
            {
                VinKekFish.doStepAndIO(VinKekFish.NORMAL_ROUNDS_K, regime: 1);
            }
            while (VinKekFish.input!.Count > 0);
            VinKekFish.step(VinKekFish.MIN_ABSORPTION_ROUNDS_D_K);
*/

            rndbytes.addWithCopy(bufferRec, flen, allocator);
            // CascadeSponge.step(data: bytes, dataLen: (nint) file.Length, regime: 1);
        }

        unsafe void InputFromFileAttr(Record bufferRec, FileInfo file, BytesBuilderForPointers rndbytes)
        {
            var size  = sizeof(long)*4;
            var bytes = stackalloc byte[size];

            // Получаем энтропию как из времени последней записи в файл,
            // так и из текущего времени, т.к. оно может зависеть от загрузки жётского диска и быть частично случайным
            BytesBuilder.ULongToBytes((ulong) file.LastWriteTimeUtc.Ticks, bytes       , size);
            BytesBuilder.ULongToBytes((ulong) file.LastAccessTime  .Ticks, bytes+size  , size);
            BytesBuilder.ULongToBytes((ulong) DateTime.Now.Ticks         , bytes+size*2, size);
            BytesBuilder.ULongToBytes((ulong) file.Length                , bytes+size*3, size);
            /*
            VinKekFish.input!.add(bytes, size);

            while (VinKekFish.input!.Count > 0)
                VinKekFish.doStepAndIO(VinKekFish.NORMAL_ROUNDS_K, regime: 2);
*/
            rndbytes.addWithCopy(bytes, size, allocator);
            // CascadeSponge.step(data: bytes, dataLen: size, regime: 2);
        }

        unsafe void InputFromFileName(Record bufferRec, FileInfo file, BytesBuilderForPointers rndbytes)
        {
            var size  = file.Name.Length * sizeof(char);

            fixed (char * str = file.Name)
                BytesBuilder.CopyTo(size, bufferRec.len, (byte *) str, bufferRec);
/*
            VinKekFish.input!.add(bytes, size);

            while (VinKekFish.input!.Count > 0)
                VinKekFish.doStepAndIO(VinKekFish.NORMAL_ROUNDS_K, regime: 3);

            CascadeSponge.step(data: bytes, dataLen: size, regime: 3);*/
            rndbytes.addWithCopy(bufferRec, size, allocator);
        }
    }
}
