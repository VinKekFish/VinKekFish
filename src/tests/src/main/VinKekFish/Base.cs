using cryptoprime_tests;
using DriverForTestsLib;

[TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public class VinKekFish_test_baseK: Keccak_test_parent
{
    public VinKekFish_test_baseK(TestConstructor constructor):
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

[TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public class VinKekFish_test_base_compareToEtalon : TestTask
{
    public VinKekFish_test_base_compareToEtalon(TestConstructor constructor) :
                                            base(nameof(Keccak_test_empty), constructor)
    {
        taskFunc = () => {};
    }
}
