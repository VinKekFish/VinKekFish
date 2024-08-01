namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;
using VinKekFish_Utils.ProgramOptions;
using System.Runtime.CompilerServices;


[TestTagAttribute("inWork")]
[TestTagAttribute("service", duration: 100, singleThread: false)]
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
            Add
            (
                lst, "have no 'entropy'",
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
            Add
            (
                lst, "have no .input.Entropy.OS.*",
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
                    var options_service = new Options_Service(opt, true);

                    throw new Exception(options_service.root.warns.ToString());
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no .input.Entropy.OS.file.path",
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
                    fileString.Add("\t\t\tfile");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "incorrect .input.Entropy.OS.file.path",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t/dev/randomAAA");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "empty .input.Entropy.OS.file.path",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no .input.Entropy.OS.file.path.*",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t/dev/random");
                    fileString.Add("\t\t\t\t\t");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no .input.Entropy.OS.file.path.{min,max,EME}",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t/dev/random");
                    fileString.Add("\t\t\t\t\tinterval");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no .input.Entropy.OS.file.path.interval.min",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t/dev/random");
                    fileString.Add("\t\t\t\t\tmax");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tEME");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tinterval");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no .input.Entropy.OS.file.path.interval.max",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t/dev/random");
                    fileString.Add("\t\t\t\t\tmin");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tEME");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tinterval");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no .input.Entropy.OS.file.path.interval.EME",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t/dev/random");
                    fileString.Add("\t\t\t\t\tmin");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tmax");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tinterval");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no .input.Entropy.OS.file.path.interval.innerInterval",
                () =>
                {
                    add_Input_Entropy_OS_file(fileString);
                    fileString.Add("\t\t\t\t/dev/random");
                    fileString.Add("\t\t\t\t\tmin");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tmax");
                    fileString.Add("\t\t\t\t\t\t0");
                    fileString.Add("\t\t\t\t\tEME");
                    fileString.Add("\t\t\t\t\t\t0");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "incorrect time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t0xA");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "incorrect time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t1b");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "incorrect time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100,s");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "incorrect time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100d");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "correct time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;

                    throw new Exception($"{t1}");
                }
            );

            fileString.Clear();
            Add
            (
                lst, "correct time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t0");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;

                    throw new Exception($"{t1}");
                }
            );

            fileString.Clear();
            Add
            (
                lst, "correct time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100ms");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;

                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100   ms");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    opt = new Options(fileString);
                    options_service = new Options_Service(opt);

                    var t2 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;
                    throw new Exception($"{t1} {t2}");
                }
            );

            fileString.Clear();
            Add
            (
                lst, "correct time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100s");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;

                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100   s");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    opt = new Options(fileString);
                    options_service = new Options_Service(opt);

                    var t2 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;
                    throw new Exception($"{t1} {t2}");
                }
            );

            fileString.Clear();
            Add
            (
                lst, "correct time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100m");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;

                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100   m");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    opt = new Options(fileString);
                    options_service = new Options_Service(opt);

                    var t2 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;
                    throw new Exception($"{t1} {t2}");
                }
            );

            fileString.Clear();
            Add
            (
                lst, "correct time in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100h");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;

                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100   h");
                    add_Input_Entropy_OS_file_interval_LD(fileString);
                    opt = new Options(fileString);
                    options_service = new Options_Service(opt);

                    var t2 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Time;
                    throw new Exception($"{t1} {t2}");
                }
            );

            fileString.Clear();
            Add
            (
                lst, "length, date and difference in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval_full_32YesNo(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].flags!.date;
                    var t2 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Difference!.differenceValue;
                    var t3 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Length!.Length;

                    throw new Exception($"{t1} {t2} {t3}");
                }
            );

            fileString.Clear();
            Add
            (
                lst, "length, date and difference in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval_full_64NoYes(fileString);
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt);
                    var t1 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].flags!.date;
                    var t2 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Difference!.differenceValue;
                    var t3 = options_service.root!.input!.entropy!.os!.randoms![0].intervals!.Interval!.inner[0].Length!.Length;

                    throw new Exception($"{t1} {t2} {t3}");
                }
            );


            fileString.Clear();
            Add
            (
                lst, "have no a length element in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100h");
                    //fileString.Add("\t\t\t\t\t\t\tLength");
                    //fileString.Add("\t\t\t\t\t\t\t\t32");
                    fileString.Add("\t\t\t\t\t\t\tdate");
                    fileString.Add("\t\t\t\t\t\t\t\tyes");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt, true);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no a length value in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100h");
                    fileString.Add("\t\t\t\t\t\t\tLength");
                    //fileString.Add("\t\t\t\t\t\t\t\t32");
                    fileString.Add("\t\t\t\t\t\t\tdate");
                    fileString.Add("\t\t\t\t\t\t\t\tyes");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt, true);
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no a difference value in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100h");
                    fileString.Add("\t\t\t\t\t\t\tLength");
                    fileString.Add("\t\t\t\t\t\t\t\t32");
                    fileString.Add("\t\t\t\t\t\t\tdate");
                    fileString.Add("\t\t\t\t\t\t\t\tyes");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt, true);

                    throw new Exception(options_service.root.warns.ToString());
                }
            );

            fileString.Clear();
            Add
            (
                lst, "have no a date value in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100h");
                    fileString.Add("\t\t\t\t\t\t\tLength");
                    fileString.Add("\t\t\t\t\t\t\t\t32");
                    fileString.Add("\t\t\t\t\t\t\tdifference");
                    fileString.Add("\t\t\t\t\t\t\t\tyes");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt, true);

                    throw new Exception(options_service.root.warns.ToString());
                }
            );

            fileString.Clear();
            Add
            (
                lst, "incorrect a length value in .input.Entropy.OS.file.path.interval.*",
                () =>
                {
                    add_Input_Entropy_OS_file_interval(fileString);
                    fileString.Add("\t\t\t\t\t\t100h");
                    fileString.Add("\t\t\t\t\t\t\tLength");
                    fileString.Add("\t\t\t\t\t\t\t\tAAA");
                    fileString.Add("\t\t\t\t\t\t\tdifference");
                    fileString.Add("\t\t\t\t\t\t\t\tyes");
                    var opt = new Options(fileString);
                    var options_service = new Options_Service(opt, true);

                    throw new Exception(options_service.root.warns.ToString());
                }
            );



            return lst;

            static void add_Input_Entropy_OS_file(List<string> fileString)
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
                fileString.Add("\t\t\tfile");
            }

            static void add_Input_Entropy_OS_file_interval(List<string> fileString)
            {
                add_Input_Entropy_OS_file(fileString);
                fileString.Add("\t\t\t\t/dev/random");
                fileString.Add("\t\t\t\t\tmin");
                fileString.Add("\t\t\t\t\t\t0");
                fileString.Add("\t\t\t\t\tmax");
                fileString.Add("\t\t\t\t\t\t0");
                fileString.Add("\t\t\t\t\tEME");
                fileString.Add("\t\t\t\t\t\t0");
                fileString.Add("\t\t\t\t\tinterval");
            }

            static void add_Input_Entropy_OS_file_interval_LD(List<string> fileString)
            {
                fileString.Add("\t\t\t\t\t\t\tLength");
                fileString.Add("\t\t\t\t\t\t\t\t32");
                fileString.Add("\t\t\t\t\t\t\tdate");
                fileString.Add("\t\t\t\t\t\t\t\tyes");
                fileString.Add("\t\t\t\t\t\t\tDifference");
                fileString.Add("\t\t\t\t\t\t\t\tno");
            }

            static void add_Input_Entropy_OS_file_interval_full_32YesNo(List<string> fileString)
            {
                add_Input_Entropy_OS_file_interval(fileString);
                fileString.Add("\t\t\t\t\t\t100h");
                fileString.Add("\t\t\t\t\t\t\tLength");
                fileString.Add("\t\t\t\t\t\t\t\t32");
                fileString.Add("\t\t\t\t\t\t\tdate");
                fileString.Add("\t\t\t\t\t\t\t\tyes");
                fileString.Add("\t\t\t\t\t\t\tDifference");
                fileString.Add("\t\t\t\t\t\t\t\tno");
            }

            static void add_Input_Entropy_OS_file_interval_full_64NoYes(List<string> fileString)
            {
                add_Input_Entropy_OS_file_interval(fileString);
                fileString.Add("\t\t\t\t\t\t100h");
                fileString.Add("\t\t\t\t\t\t\tLength");
                fileString.Add("\t\t\t\t\t\t\t\t64");
                fileString.Add("\t\t\t\t\t\t\tdate");
                fileString.Add("\t\t\t\t\t\t\t\tno");
                fileString.Add("\t\t\t\t\t\t\tDifference");
                fileString.Add("\t\t\t\t\t\t\t\tyes");
            }
        }

        static void Add(List<string> lst, string msg, Func func)
        {
            try
            {
                func();
                throw new Exception($"ServiceAutoTestFile_Input: have no exception");
            }
            catch (Exception ex)
            {
                lst.Add(ex.Message + $" (for {msg})");
            }
        }
    }
}
