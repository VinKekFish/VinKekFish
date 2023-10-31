// #define CAN_CREATEFILE_FOR_SERVICE

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

// [TestTagAttribute("inWork")]
[TestTagAttribute("service", duration: 100, singleThread: false)]
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
                lst, "have no 'input'",
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
                lst, "have no 'output'",
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
                lst, "have no 'path'",
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
                lst, "have no 'path.random at start folder'",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("input");
                    fileString.Add("output");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "have no 'path.random at start folder.value'",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("input");
                    fileString.Add("output");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "have no 'output.random'",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("\t\trandom_at_start_folder");
                    fileString.Add("output");
                    fileString.Add("input");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "have no 'output.random.unix stream'",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("\t\trandom_at_start_folder");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("input");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "have no 'output.random.unix stream.path'",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("\t\trandom_at_start_folder");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tunix stream");
                    fileString.Add("input");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "have no 'output.random.unix stream.path.value'",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("\t\trandom_at_start_folder");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tunix stream");
                    fileString.Add("\t\t\tpath");
                    fileString.Add("input");
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
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("\t\trandom_at_start_folder");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tERROR");
                    fileString.Add("\t\t\tpath");
                    fileString.Add("input");
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
