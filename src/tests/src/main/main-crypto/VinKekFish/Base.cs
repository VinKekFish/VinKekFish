namespace cryptoprime_tests;

using cryptoprime_tests;
using DriverForTestsLib;

using cryptoprime;
using vinkekfish;
using cryptoprime.VinKekFish;
using static cryptoprime.BytesBuilderForPointers;

using static VinKekFish_Utils.Utils;


//[TestTagAttribute("inWork")]
//[TestTagAttribute("VinKekFish", duration: 2000, singleThread: false)]
public class VinKekFish_test_baseK: Keccak_test_parent
{
    public VinKekFish_test_baseK(TestConstructor constructor):
                            base (  constructor: constructor, parentSaver: new Saver()  )
    {}

    public override DirectoryInfo SetDirForFiles()
    {
        return GetDirectoryPath("src/tests/src/main/main-crypto/VinKekFish/");
    }

    protected unsafe class Saver: SaverParent
    {
        public override object ExecuteTest(AutoSaveTestTask task)
        {
            List<byte[]> lst = new();

            return lst;
        }
    }
}

// TODO: проверить VinKekFish на вводе только синхропосылки, а ключ вводить дальше вручную
// [TestTagAttribute("inWork")]
[TestTagAttribute("VinKekFish", duration: 15e3, singleThread: true)]
public unsafe class VinKekFish_test_base_compareToEtalon : TestTask
{
    public VinKekFish_test_base_compareToEtalon(TestConstructor constructor) :
                                            base(nameof(VinKekFish_test_base_compareToEtalon), constructor)
    {
        TaskFunc = Test;
    }

    public static void Test()
    {
        File.Delete("log-k1.log");
        File.Delete("log-KN.log");

        var keyLen = 128;
        ConstantRoundsTests(keyLen);
        keyLen = VinKekFishBase_etalonK1.CryptoStateLen + VinKekFishBase_etalonK1.BLOCK_SIZE * 2 + 2;  // Длина ключа должна быть чётной, т.к. мы дальше инициализируем ключ так, как будто его длина чётная
        ConstantRoundsTests(keyLen);

        TestByIncorrect();
    }

    private static void ConstantRoundsTests(int keyLen)
    {
        Test(VinKekFishBase_etalonK1.MIN_ROUNDS,     VinKekFishBase_etalonK1.MIN_ROUNDS,     VinKekFishBase_etalonK1.MIN_ABSORPTION_ROUNDS_D, VinKekFishBase_etalonK1.MIN_ABSORPTION_ROUNDS_D, keyLen);
        Test(VinKekFishBase_etalonK1.MIN_ROUNDS,     VinKekFishBase_etalonK1.MIN_ROUNDS,     1, 1, keyLen);
        Test(VinKekFishBase_etalonK1.REDUCED_ROUNDS, VinKekFishBase_etalonK1.REDUCED_ROUNDS, 1, 1, keyLen);
        Test(VinKekFishBase_etalonK1.NORMAL_ROUNDS,  VinKekFishBase_etalonK1.NORMAL_ROUNDS,  1, 1, keyLen);
        Test(VinKekFishBase_etalonK1.EXTRA_ROUNDS,   VinKekFishBase_etalonK1.EXTRA_ROUNDS,   1, 1, keyLen);
        Test(VinKekFishBase_etalonK1.MAX_ROUNDS,     VinKekFishBase_etalonK1.MAX_ROUNDS,     1, 1, keyLen);

        Test(  5,   5, 1, 1, keyLen);
        Test(  8,   8, 1, 1, keyLen);
        Test( 64,  64, 1, 1, keyLen);
        Test(128, 128, 1, 1, keyLen);
    }

    public static void Test(int roundsCnt, int RoundsForFinal, int RoundsForFirstKeyBlock, int RoundsForTailsBlock, int keyLen)
    {
        using var k1e   = new VinKekFish_k1_base_20210419();
        using var k1t1  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: 1);
        using var k1t4  = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: Environment.ProcessorCount);
        using var k1t16 = new VinKekFishBase_KN_20210525(CountOfRounds: roundsCnt, ThreadCount: Environment.ProcessorCount*4);

        k1t1 .output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);
        k1t4 .output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);
        k1t16.output  = new BytesBuilderStatic(VinKekFishBase_etalonK1.BLOCK_SIZE);

              var allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
        using var key       = allocator.AllocMemory(keyLen);
        using var out1e     = allocator.AllocMemory(VinKekFishBase_etalonK1.BLOCK_SIZE);

        // BytesBuilder.FillByBytes(1, key, key.len);
        for (int i = 0; i < key.len-1; i += 2)
        {
            key[i+0] = (byte) (i*3);
            key[i+1] = (byte) (i*5);
        }

        k1e  .Init1(roundsCnt, PreRoundsForTranspose: roundsCnt);
        k1e  .Init2(key, key.len, RoundsForEnd: RoundsForFinal, RoundsForExtendedKey: RoundsForTailsBlock, Rounds: RoundsForFirstKeyBlock);
        k1t1 .Init1(PreRoundsForTranspose: roundsCnt);
        k1t1 .Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);
        k1t4 .Init1(roundsCnt);
        k1t4 .Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);
        k1t16.Init1(roundsCnt);
        k1t16.Init2(key, RoundsForTailsBlock: RoundsForTailsBlock, RoundsForFinal: RoundsForFinal, RoundsForFirstKeyBlock: RoundsForFirstKeyBlock);


        k1e  .InputData_Xor(null, 0, 0);
        k1e  .DoStep(roundsCnt);
        k1t1 .DoStepAndIO(roundsCnt);
        k1t4 .DoStepAndIO(roundsCnt);
        k1t16.DoStepAndIO(roundsCnt);

        k1e .OutputData(out1e , 0, out1e .len, VinKekFishBase_etalonK1.BLOCK_SIZE);
        var sp = new ReadOnlySpan<byte>(out1e, (int) out1e.len);

        using var out1t1 = k1t1 .output.GetBytes();
        sp = new ReadOnlySpan<byte>(out1t1, (int) out1t1.len);

        using var out1t4 = k1t4 .output.GetBytes();
        sp = new ReadOnlySpan<byte>(out1t4, (int) out1t4.len);

        using var out1t16 = k1t16.output.GetBytes();
        sp = new ReadOnlySpan<byte>(out1t16, (int) out1t16.len);

        if (!out1t1.UnsecureCompare(out1e))
        {
            throw new Exception($"!out1t1.UnsecureCompare(out1e) {roundsCnt}, {keyLen}");
        }
        if (!out1t4.UnsecureCompare(out1e))
        {
            throw new Exception($"!out1t4.UnsecureCompare(out1e) {roundsCnt}, {keyLen}");
        }
        if (!out1t16.UnsecureCompare(out1e))
        {
            throw new Exception($"!out1t16.UnsecureCompare(out1e) {roundsCnt}, {keyLen}");
        }
    }

    public static void TestByIncorrect()
    {
        int incorrect = 0;
        try
        {
            Test(VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MIN_ABSORPTION_ROUNDS_D, VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS, 128);
            incorrect += 1;
            // this.error.Add();
        }
        catch
        {}

        try
        {
            Test(VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS, 0, VinKekFishBase_etalonK1.MAX_ROUNDS, 128);
            incorrect += 2;
        }
        catch
        {}

        try
        {
            Test(VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS, VinKekFishBase_etalonK1.MAX_ROUNDS, 0, 128);
            incorrect += 4;
        }
        catch
        {}

        if (incorrect != 0)
            throw new Exception($"testByIncorrect ({incorrect})");
    }
}
