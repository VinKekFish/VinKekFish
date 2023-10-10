// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.ComponentModel.DataAnnotations;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{
    /// <summary>Создаёт папки для сохранения энтропии между запусками</summary>
    protected virtual void CreateFolders()
    {
        RandomAtFolder = options_service!.root!.Path!.randomAtStartFolder!.dir!; RandomAtFolder.Refresh();

        if (!RandomAtFolder.Exists)
            RandomAtFolder.Create();

        RandomAtFolder_Static = new DirectoryInfo(Path.Combine(RandomAtFolder.FullName, "static")); RandomAtFolder.Refresh();
        if (!RandomAtFolder_Static.Exists)
            RandomAtFolder_Static.Create();
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
                InputFromFileContent(file, readStream);
                InputFromFileAttr   (file);
                InputFromFileName   (file);
            }
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
