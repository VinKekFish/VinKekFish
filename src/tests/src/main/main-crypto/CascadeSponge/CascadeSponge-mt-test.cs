// #define CAN_CREATEFILE_FOR_CascadeSponge_1t_tests
namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using maincrypto.keccak;
using vinkekfish;

using static CodeGenerated.Cryptoprimes.Threefish_Static_Generated;
using static VinKekFish_Utils.Utils;

using static cryptoprime.KeccakPrime;
using static cryptoprime.BytesBuilderForPointers;
using System.Data.Common;
using System.Data;

// example::docs:rQN6ZzeeepyOpOnTPKAT:

// Ниже можно посмотреть на простейший способ создания каскада


[TestTagAttribute("inWork")]
[TestTagAttribute("CascadeSponge", duration: 500, singleThread: true)]
public unsafe class CascadeSponge_mt_20230930_BaseTest : TestTask
{
    public CascadeSponge_mt_20230930_BaseTest(TestConstructor constructor) :
                                            base(nameof(CascadeSponge_mt_20230930_BaseTest), constructor)
    {
        taskFunc = Test;
    }

    public void Test()
    {
        var cnt       = 5;
        var wide      = 128;
        var cascade1t = new CascadeSponge_1t_20230905(_tall: wide, _wide: wide);
        var cascademt = new CascadeSponge_mt_20230930(_tall: wide, _wide: wide);

        var dlen = cascade1t.maxDataLen*5 - 1;
        var data = Keccak_abstract.allocator.AllocMemory(dlen);
        try
        {
            byte* a = data;
            // Инициализируем массивы данных - имитируем синхропосылку и ключ простыми значениями
            for (int i = 0; i < dlen; i++)
                a[i] = (byte) (i*7);

            var st1 = new DriverForTestsLib.SimpleTimeMeter();
            using (st1)
                cascade1t.step(data: data, dataLen: dlen, countOfSteps: cnt);

            var stm = new DriverForTestsLib.SimpleTimeMeter();
            using (stm)
                cascademt.step(data: data, dataLen: dlen, countOfSteps: cnt);

            if (!BytesBuilder.UnsecureCompare(cascade1t.maxDataLen, cascademt.maxDataLen, cascade1t.lastOutput, cascademt.lastOutput))
                throw new Exception("cascade1t != cascademt");

            Console.WriteLine(st1.TotalMilliseconds);
            Console.WriteLine(stm.TotalMilliseconds);
        }
        finally
        {
            cascade1t.Dispose();
            cascademt.Dispose();
            data     .Dispose();
        }
    }
}

