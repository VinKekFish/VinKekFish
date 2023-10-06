#define CAN_CREATEFILE_FOR_SERVICE

namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;
using VinKekFish_Utils.ProgramOptions;
using System.Runtime.CompilerServices;

// [TestTagAttribute("inWork")]
// [TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public class ServiceAutoTests: Keccak_test_parent
{
    protected ServiceAutoTests(TestConstructor constructor, SaverParent parentSaver):
                            base (  constructor: constructor, parentSaver: parentSaver  )
    {
        #if CAN_CREATEFILE_FOR_SERVICE
        this.parentSaver.canCreateFile = true;
        #warning CAN_CREATEFILE_FOR_SERVICE
        #endif
    }

    public override DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath("src/tests/src/main/main-crypto/exe/service");
    }
}

[TestTagAttribute("inWork")]
[TestTagAttribute("service", duration: 1e16, singleThread: false)]
public class ServiceAutoTestFile: ServiceAutoTests
{
    public ServiceAutoTestFile(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    delegate void Func();

    protected sealed unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            var lst = new List<string>();

            var fileString = new List<string>();

            add
            (
                lst, "empty",
                () =>
                {
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "no have 'input'",
                () =>
                {
                    fileString.Add("output");
                    fileString.Add("");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "no have 'output'",
                () =>
                {
                    fileString.Add("input");
                    fileString.Add("");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "no have 'output.random'",
                () =>
                {
                    fileString.Add("input");
                    fileString.Add("output");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "no have 'output.random.unix stream'",
                () =>
                {
                    fileString.Add("input");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "no have 'output.random.unix stream.path'",
                () =>
                {
                    fileString.Add("input");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tunix stream");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "no have 'output.random.unix stream.path.value'",
                () =>
                {
                    fileString.Add("input");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tunix stream");
                    fileString.Add("\t\t\tpath");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "error at 'output.random'",
                () =>
                {
                    fileString.Add("input");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tERROR");
                    fileString.Add("\t\t\tpath");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );



            return lst;
        }

        void add(List<string> lst, string msg, Func func)
        {
            try
            {
                func();
                throw new Exception($"ServiceAutoTestFile: have not exception");
            }
            catch (Exception ex)
            {
                lst.Add(ex.Message + $" (for {msg})");
            }
        }
    }
}