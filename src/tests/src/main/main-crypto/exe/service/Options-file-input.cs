#define CAN_CREATEFILE_FOR_SERVICE

namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;
using VinKekFish_Utils.ProgramOptions;
using System.Runtime.CompilerServices;


[TestTagAttribute("inWork")]
[TestTagAttribute("service", duration: 1e16, singleThread: false)]
public class ServiceAutoTestFile_Input: ServiceAutoTests
{
    public ServiceAutoTestFile_Input(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    delegate void Func();

    protected sealed unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            var lst = new List<string>();

            var fileString = new List<string>();

            fileString.Clear();
            add
            (
                lst, "no have 'entropy'",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("\t\trandom_at_start_folder");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tunix stream");
                    fileString.Add("\t\t\tpath");
                    fileString.Add("\t\t\t\tvalue");
                    fileString.Add("input");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            add
            (
                lst, "empty .input.Entropy.OS",
                () =>
                {
                    fileString.Add("path");
                    fileString.Add("\trandom at start folder");
                    fileString.Add("\t\trandom_at_start_folder");
                    fileString.Add("output");
                    fileString.Add("\trandom");
                    fileString.Add("\t\tunix stream");
                    fileString.Add("\t\t\tpath");
                    fileString.Add("\t\t\t\tvalue");
                    fileString.Add("input");
                    fileString.Add("\tEntropy");
                    fileString.Add("\t\tOS");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);

                    throw new Exception(options_service.root.warns.ToString());
                }
            );

            return lst;
        }

        void add(List<string> lst, string msg, Func func)
        {
            try
            {
                func();
                throw new Exception($"ServiceAutoTestFile_Input: have not exception");
            }
            catch (Exception ex)
            {
                lst.Add(ex.Message + $" (for {msg})");
            }
        }
    }
}
