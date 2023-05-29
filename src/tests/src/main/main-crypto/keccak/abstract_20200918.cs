namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;


[TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public class Keccak_test_abstract_20200918: Keccak_test_parent
{
    public Keccak_test_abstract_20200918(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}


    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<string> lst = new List<string>();

            using var k = new Keccak_20200918();
            
            using var debugRecord = new Record() { array = k.S, len = KeccakPrime.b_size };
            Console.WriteLine(debugRecord);

            isNull(k, "1.1");

            k.CalcStep();

            var cnt = 0;
            for (int i = 0; i < KeccakPrime.b_size << 3; i++)
                cnt += BitToBytes.getBit(k.S, i) ? 1 : 0;

            var etalonCnt = KeccakPrime.b_size << 2;
            var deviation = etalonCnt - cnt;
            if (deviation < 0)
                deviation *= -1;

            // Вероятность, примерно, одна миллионная - это 2^-20
            // Если установленных битов больше, чем 20, значит у нас точно плохие параметры криптографии
            if (deviation >= 20)
            {
                throw new Exception("1.2");
            }
            lst.Add(debugRecord.ToString());

            return lst;
        }

        private static void isNull(Keccak_20200918 k, string msg)
        {
            for (int i = 0; i < KeccakPrime.b_size; i++)
                if (k.S[i] != 0)
                    throw new Exception(msg);
        }
    }
}
