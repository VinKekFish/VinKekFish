#define CAN_CREATEFILE_FOR_keccak

namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;

[TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public class Keccak_test_PRNG_20201128 : TestTask
{
    public Keccak_test_PRNG_20201128(TestConstructor constructor) :
                                            base(nameof(Keccak_test_PRNG_20201128), constructor)
    {
        taskFunc = () =>
        {
            
        };
    }
}
