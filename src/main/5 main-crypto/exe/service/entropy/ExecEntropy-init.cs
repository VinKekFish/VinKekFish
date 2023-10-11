// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.ComponentModel.DataAnnotations;
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

    public VinKekFishBase_KN_20210525 VinKekFish    = new VinKekFishBase_KN_20210525(VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(11), 11, 1);   // 275 == inKekFish.EXTRA_ROUNDS_K, K = 11
    public CascadeSponge_mt_20230930  CascadeSponge = new CascadeSponge_mt_20230930(10*1024);

    public bool isInitiated { get; protected set; } = false;

    /// <summary>Функция вызывается для инициализации всех губок, накапливающих энтропию</summary>
    protected unsafe virtual void StartEntropy()
    {
        lock (entropy_sync)
        {
            try
            {
                if (VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0)
                    throw new Exception("Regime_Service.StartEntropy: Fatal algorithmic error: VinKekFish.CountOfRounds - VinKekFish.EXTRA_ROUNDS_K < 0");

                CreateFolders();

                ExecEntorpy_now  = DateTime.Now.Ticks;
                VinKekFish.input = new BytesBuilderStatic(MAX_RANDOM_AT_START_FILE_LENGTH);

                using var bufferRec = allocator.AllocMemory(MAX_RANDOM_AT_START_FILE_LENGTH);
                // using var rndbytes  = new BytesBuilderForPointers();

                CascadeSponge.InitEmptyThreeFish((ulong) ExecEntorpy_now);
                CascadeSponge.step(regime: 255);
                CascadeSponge.InitThreeFishByCascade(1, false);

                nint realRandomLength = 0;

                checked
                {
                    try
                    {
                        var rndList  = options_service!.root!.input!.entropy!.os!.random;
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
                                    var len         = (nint) rndFileInfo.Length;

                                    if (interval.Length!.Length > 0)
                                        len = (nint) interval.Length!.Length;

                                    if (len > MAX_RANDOM_AT_START_FILE_LENGTH)
                                        throw new Exception($"Regime_Service.StartEntropy: for the element '{rnd.getFullElementName()} at line {rnd.thisBlock.startLine}': the file length too match. The length ({len}) of the random file must be lowest ${MAX_RANDOM_AT_START_FILE_LENGTH}.");

                                    var bufferSpan = new Span<byte>(bufferRec, (int) len);
                                    using (var rs = rndFileInfo.OpenRead())
                                    {
                                        rs.Read(bufferSpan);
                                        // rndbytes.addWithCopy(bufferRec << bufferRec.len - len, allocator: allocator);
                                    }

                                    CascadeSponge.step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, data: bufferRec, dataLen: len, regime: 2);

                                    realRandomLength += len;
                                    Console.WriteLine($"{L("Initialization got random from file")}; len = {len}; name = {rndFileInfo.FullName}");
                                }
                            }
                        }
                    }
                    catch (NullReferenceException)
                    {}
                }

                if (realRandomLength < 16)
                    throw new Exception("Regime_Service.StartEntropy: realRandomLength < 16.\n" + L("Check the options file for the input.entropy.OS.file element. Interval element with 'once' or '--' keywords required"));

                // Делаем первичную инициализацию временем при старте
                    var sz  = (int) realRandomLength;       // Стойкость перестановок - 4 килобита
                    var arr = stackalloc byte[sizeof(long) + sz];
                using var rec = new Record() {array = arr, len = sizeof(long) + sz};
                BytesBuilder.ULongToBytes((ulong) ExecEntorpy_now, arr, sizeof(long));

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
                    {   // rec является синхропосылкой, но т.к. ключа нет, то rec вводится как ключ
                        VinKekFish.Init1
                        (
                            keyForPermutations: rec,
                            PreRoundsForTranspose: VinKekFish.EXTRA_ROUNDS_K - VinKekFish.Calc_OptimalRandomPermutationCount(rec.len),
                            ThreeFishInitSteps:    1
                        );
                        VinKekFish.Init2(key: rec, TweakInit: rec >> sizeof(long));
                    },

                    () =>
                    {
                        CascadeSponge.step(ArmoringSteps: CascadeSponge.countStepsForKeyGeneration, regime: 3, data: rec.array, dataLen: sizeof(long), inputRegime: CascadeSponge_1t_20230905.InputRegime.overwrite);
                        CascadeSponge.InitThreeFishByCascade(1, false);
                    }
                );
    
                GetStartupEntropy();
                CascadeSponge.InitThreeFishByCascade(1, false);

                isInitiated = true;
            }
            finally
            {
                Monitor.PulseAll(entropy_sync);
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
    }

    public const int MAX_RANDOM_AT_START_FILE_LENGTH = 256*1024;

    protected unsafe virtual void GetStartupEntropy()
    {
        // var bb    = new BytesBuilderStatic(1024*1024);
        var files = RandomAtFolder_Static!.GetFiles("*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            using (var readStream = file.OpenRead())
            {
                InputFromFileAttr   (file);
                InputFromFileName   (file);

                CascadeSponge.InitThreeFishByCascade();
                InputFromFileContent(file, readStream);
                CascadeSponge.InitThreeFishByCascade();
            }

            Console.WriteLine($"{L("Initialization got random from file")}; len = {file.Length}; name = {file.FullName}");
        }

        unsafe void InputFromFileContent(FileInfo file, FileStream readStream)
        {
            int flen = (int) file.Length;
            if (file.Length > MAX_RANDOM_AT_START_FILE_LENGTH)
                throw new ArgumentOutOfRangeException("InputFromFile: flen > MAX_RANDOM_AT_START_FILE_LENGTH");
            if (flen <= 0)
                throw new ArgumentOutOfRangeException("InputFromFile: flen <= 0");

            var bytes = stackalloc byte[flen];

            var span = new Span<byte>(bytes, flen);
            readStream.Read(span);

            VinKekFish.input!.add(bytes, flen);
            while (VinKekFish.input!.Count > 0)
                VinKekFish.doStepAndIO(VinKekFish.NORMAL_ROUNDS_K, regime: 1);

            CascadeSponge.step(data: bytes, dataLen: (nint) file.Length, regime: 1);
        }

        unsafe void InputFromFileAttr(FileInfo file)
        {
            var size  = sizeof(long);
            var bytes = stackalloc byte[size];

            BytesBuilder.ULongToBytes((ulong) file.LastWriteTimeUtc.Ticks, bytes, size);
            VinKekFish.input!.add(bytes, size);

            while (VinKekFish.input!.Count > 0)
                VinKekFish.doStepAndIO(VinKekFish.NORMAL_ROUNDS_K, regime: 2);

            CascadeSponge.step(data: bytes, dataLen: size, regime: 2);
        }

        unsafe void InputFromFileName(FileInfo file)
        {
            var size  = file.Name.Length * sizeof(char);
            var bytes = stackalloc byte[size];

            fixed (char * str = file.Name)
                BytesBuilder.CopyTo(size, size, (byte *) str, bytes);

            VinKekFish.input!.add(bytes, size);

            while (VinKekFish.input!.Count > 0)
                VinKekFish.doStepAndIO(VinKekFish.NORMAL_ROUNDS_K, regime: 3);

            CascadeSponge.step(data: bytes, dataLen: size, regime: 3);
        }
    }
}
