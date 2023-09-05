namespace cryptoprime_tests;

using cryptoprime;
using DriverForTestsLib;

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
            var dlen  = cascade.maxDataLen;
            var data  = new byte[dlen];
            fixed (byte * a = data)
            {
                // Инициализируем массивы данных
                for (int i = 0; i < dlen; i++)
                    a[i] = (byte) i;

                // Вводим данные и делаем шаг
                for (int i = 0; i < 27; i++)
                cascade.step(1, a, dlen);
                var msg = VinKekFish_Utils.Utils.ArrayToHex(cascade.lastOutput, cascade.maxDataLen);
                Console.WriteLine(msg);
            }
        }
        finally
        {
            cascade.Dispose();
        }
    }
}

