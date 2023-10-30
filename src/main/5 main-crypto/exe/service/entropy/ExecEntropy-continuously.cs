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
                                StartContinuouslyGetter(rnd, fileElement);
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

    protected unsafe void StartContinuouslyGetter(Options_Service.Input.Entropy.InputElement rnd, Options_Service.Input.Entropy.InputFileElement fileElement)
    {
        fileElement.fileInfo!.Refresh();
        if (!fileElement.fileInfo.Exists)
        {
            Console.Error.WriteLine($"Regime_Service.StartContinuouslyGetter: file not found: {fileElement.fileInfo.FullName}");
            return;
        }
// TODO: Добавить сюда учёт флага date
// TODO: Добавить сюда подсчёт количества энтропии, которое было введено
        var t = new Thread
        (
            () =>
            {
                // KeccakPrime
                var keccak = new Keccak_20200918();

                var input   = stackalloc byte[KeccakPrime.BlockLen];    // Массив, из которого будет вводиться энтропия в губку keccak
                int pos     = 0;
                int dateLen = sizeof(long);

                int len  = 24;
                var buff = stackalloc byte[len];
                var span = new Span<byte>(buff, len);
                using (var rs = fileElement.fileInfo.OpenRead())
                {
                    while (true)
                    {
                        var bytesReaded = rs.Read(span);
                        if (bytesReaded <= 0)
                            return;

                        if (KeccakPrime.BlockLen - pos < bytesReaded + dateLen)
                        {
                            KeccakPrime.Keccak_Input64_512(input, (byte) pos, keccak.S);
                            keccak.CalcStep();
                            pos = 0;
                        }

                        if (dateLen > 0)
                        {
                            var ticks = DateTime.Now.Ticks;
                            BytesBuilder.ULongToBytes((ulong) ticks, input, KeccakPrime.BlockLen, pos);
                            pos += dateLen;
                        }

                        BytesBuilder.CopyTo(len, KeccakPrime.BlockLen, buff, input, pos, bytesReaded);
                        pos += bytesReaded;

                        BytesBuilder.ToNull(len, buff);
                    }
                }
            }
        );
    }
}
