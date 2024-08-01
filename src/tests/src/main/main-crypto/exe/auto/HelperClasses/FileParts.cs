// #define CAN_CREATEFILE_FOR_HelperClasses
namespace cryptoprime_tests;

// TODO: Здесь очень слабый тест
// entry::test:6mN7tkWO7uSf70KW9M3I:


using cryptoprime;
using VinKekFish_EXE;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;
using VinKekFish_Utils.ProgramOptions;
using System.Runtime.CompilerServices;
using System.Text;

public class FilePartsAutoTests: Keccak_test_parent
{
    protected FilePartsAutoTests(TestConstructor constructor, SaverParent parentSaver):
                            base (  constructor: constructor, parentSaver: parentSaver  )
    {
        #if CAN_CREATEFILE_FOR_HelperClasses
        this.parentSaver.canCreateFile = true;
        #warning CAN_CREATEFILE_FOR_HelperClasses
        #endif
    }

    public override DirectoryInfo SetDirForFiles()
    {
        return GetDirectoryPath("src/tests/src/main/main-crypto/exe/auto/HelperClasses");
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("FileParts", duration: 100, singleThread: false)]
public class FilePartsTests : FilePartsAutoTests
{
    public FilePartsTests(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    delegate void Func();

    protected sealed unsafe class Saver: SaverParent
    {
        public override byte[][] ExecuteTest(AutoSaveTestTask task)
        {
            var result = new byte[2][];

            using var root = new FileParts(Name: "root");

            var uft8 = new UTF8Encoding();

            root.AddFilePart("part 1", uft8.GetBytes(".part 1."));
            root.AddFilePart("part 2", uft8.GetBytes(".part 2."));

            var part1 = root.FindFirstPart("part 1");
            var part11_content = Record.GetRecordFromBytesArray(uft8.GetBytes(".part 1.1."));
            part1.FoundFilePart?.AddFilePart("part 1.1", part11_content);

            // root.WriteToFile(new FileInfo("/inRamA/1"), FileMode.OpenOrCreate);

            using var a = new MemoryStream(1024*1024);
            root.WriteToFile(a);

            a.Seek(0, SeekOrigin.Begin);
            result[0] = new byte[a.Length];
            a.Read(result[0]);

            nint current = 0;
            using var rec = root.WriteToRecord(ref current);
            result[1] = rec.CloneToSafeBytes();

            if (!BytesBuilder.UnsecureCompare(result[0], result[1]))
                throw new Exception("!BytesBuilder.UnsecureCompare(result[0], result[1])");

            return result;
        }
    }
}
