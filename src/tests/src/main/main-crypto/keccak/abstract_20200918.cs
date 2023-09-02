namespace cryptoprime_tests;

// ::test:N9vgPiXnOHBJrgMjXM6o:

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;


// [TestTagAttribute("inWork")]
[TestTagAttribute("keccak")]
public class Keccak_test_erfc_S : TestTask
{
    public Keccak_test_erfc_S(TestConstructor constructor) :
                                            base(nameof(Keccak_test_erfc_S), constructor)
    {
        taskFunc = () => 
        {
            // Console.WriteLine(Keccak_test_parent.sqrt(0.25m));
            // Console.WriteLine(Keccak_test_parent.sqrt(25m));
            var etalon = new Dictionary<double, double>()
            {
                {-1.0, 1.8427007929497148},
                { 0.0, 1.0},
                { 1.0, 0.15729920705028522},
                { 1.5, 0.033894853524688906},
                { 2.0, 0.004677734981047177}
            };
            foreach (var eval in etalon)
            {
                var val = Keccak_test_parent.erfc((decimal) eval.Key);
                if (Math.Abs(((double) val - eval.Value)/eval.Value) > 1e-12)
                {
                    throw new Exception($"{val:E17} == {eval.Value:E17}; for {eval.Key:E3}");
                }
            }
        };
    }
}


// [TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 550, singleThread: false)]
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

            using var k           = new Keccak_20200918();
            using var debugRecord = new Record() { array = k.S, len = KeccakPrime.b_size };

            isNull(k, "1.1");
            k.CalcStep();
            int deviation = Keccak_test_parent.GetDeviationOfBits(k.S, KeccakPrime.b_size << 3);

            if (deviation >= 20)
            {
                throw new Exception("1.2");
            }
            lst.Add(debugRecord.ToString());
            lst.Add("Deviation: " + deviation.ToString("D2"));

            using var k2 = k.Clone();
            if (!BytesBuilder.UnsecureCompare(KeccakPrime.b_size, KeccakPrime.b_size, k.S, k2.S))
                throw new Exception("1.3.1");
            k2.CalcStep();
            if (BytesBuilder.UnsecureCompare(KeccakPrime.b_size, KeccakPrime.b_size, k.S, k2.S))
                throw new Exception("1.3.2");
            k .CalcStep();
            if (!BytesBuilder.UnsecureCompare(KeccakPrime.b_size, KeccakPrime.b_size, k.S, k2.S))
                throw new Exception("1.3.3");

                deviation = Keccak_test_parent.GetDeviationOfBits(k.S, KeccakPrime.b_size << 3);
            lst.Add(debugRecord.ToString());
            lst.Add("Deviation: " + deviation.ToString("D2"));

            k2.clearOnly_C_and_B();
            isNullBC(k2, "k2.b || k2.c != null");
            k2.ClearStateWithoutStateField();
            k2.CalcStep();
            if (BytesBuilder.UnsecureCompare(KeccakPrime.b_size, KeccakPrime.b_size, k.S, k2.S))
                throw new Exception("1.4.1");
            k .CalcStep();
            if (!BytesBuilder.UnsecureCompare(KeccakPrime.b_size, KeccakPrime.b_size, k.S, k2.S))
                throw new Exception("1.4.2");


            var N = 1024;
            for (int i = 0; i < N; i++)
            {
                k.CalcStep();
                // deviation = GetDeviationOfBits(k);
                deviation = Keccak_test_parent.GetDeviationOfBits(k.S, KeccakPrime.b_size << 3);
                var P  = Keccak_test_parent.erfc(deviation / Keccak_test_parent.sqrt(KeccakPrime.b_size << 3) / Keccak_test_parent.sqrt2);
                var P2 = Keccak_test_parent.erfc_2N(deviation, KeccakPrime.b_size << 3);
                if (Math.Abs(P - P2) > 1e-22m)
                    throw new Exception("Math.Abs(P - P2) > 1e-22m");

                if ((double) P < 1/Math.Sqrt(N) || P < 0.01m)
                    throw new Exception($"for i={i}: deviation {deviation}\t{P}");
            }

            lst.Add(debugRecord.ToString());

            return lst;
        }

        private static void isNull(Keccak_abstract k, string msg)
        {
            for (int i = 0; i < KeccakPrime.b_size; i++)
                if (k.S[i] != 0)
                    throw new Exception(msg);
        }

        private static void isNullBC(Keccak_abstract k, string msg)
        {
            for (int i = 0; i < KeccakPrime.b_size; i++)
                if (k.B[i] != 0)
                    throw new Exception(msg);
            for (int i = 0; i < KeccakPrime.c_size; i++)
                if (k.C[i] != 0)
                    throw new Exception(msg);
        }
    }
}
