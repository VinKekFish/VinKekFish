namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;
using maincrypto.keccak;
using vinkekfish;

// tests::docs:rQN6ZzeeepyOpOnTPKAT:

[TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public unsafe class CascadeSponge_BaseTest : TestTask
{
    public CascadeSponge_BaseTest(TestConstructor constructor) :
                                            base(nameof(CascadeSponge_BaseTest), constructor)
    {
        taskFunc = Test;
    }

    public void Test()
    {
        var cascade = new CascadeSponge_1t_20230905();

        try
        {
                  var dlen = cascade.maxDataLen;
            using var data = Keccak_abstract.allocator.AllocMemory(dlen);

            byte * a = data;
            // Инициализируем массивы данных - имитируем синхропосылку и ключ простыми значениями
            for (int i = 0; i < dlen; i++)
                a[i] = (byte) i;

            // Вводим данные и делаем шаг. Имитируем, что вводим синхропосылку и ключ
            for (int i = 0; i < cascade.countStepsForKeyGeneration; i++)
                cascade.step(1, 0, a, dlen);

            cascade.initKeyAndOIV(data, null, 2);

            var msg = VinKekFish_Utils.Utils.ArrayToHex(cascade.lastOutput, cascade.maxDataLen);
            Console.WriteLine(msg);
        }
        finally
        {
            cascade.Dispose();
        }
    }
}

