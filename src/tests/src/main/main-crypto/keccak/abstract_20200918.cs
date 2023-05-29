namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;


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
            List<byte[]> lst = new List<byte[]>();

            

            return lst;
        }
    }
}
