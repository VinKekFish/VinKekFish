// TODO: сделать тесты на игнорирование слишком маленького ключа
// TODO: сделать тесты на игнорирование синхропосылки (тесты на то, что синхропосылка используется и используется на всю длину вне зависимости от того, маленькая она или большая)
// TODO: незаконченные тексты
namespace cryptoprime_tests;

// entry::test:bGx3blJD6yexv1d8VgC7:

using cryptoprime;
using DriverForTestsLib;
using VinKekFish_Utils;
using maincrypto.keccak;
using Record = cryptoprime.BytesBuilderForPointers.Record;
using System.Text;

// [TestTagAttribute("inWork")]
[TestTagAttribute("keccak", duration: 1e16, singleThread: false)]
public unsafe class Keccak_test_PRNG_20201128_1 : TestTask
{
    SortedDictionary<nint, Keccak_PRNG_20201128>? keccaks;
    Keccak_PRNG_20201128? osKeccak;

    ~Keccak_test_PRNG_20201128_1()
    {
        osKeccak?.Dispose();
        DisposeKeccaks(keccaks);

        osKeccak = null;
        keccaks  = null;
    }

    private void DisposeKeccaks(SortedDictionary<nint, Keccak_PRNG_20201128>? keccaks)
    {
        if (keccaks is not null)
        {
            foreach (var (_, keccak) in keccaks)
            {
                keccak?.Dispose();
            }
        }
    }

    public Keccak_test_PRNG_20201128_1(TestConstructor constructor) :
                                            base(nameof(Keccak_test_PRNG_20201128_1), constructor)
    {
        base.taskFunc = () =>
        {
            keccaks = new SortedDictionary<nint, Keccak_PRNG_20201128>();
            try
            {
                try
                {
                    using var tmp = new Keccak_PRNG_20201128(outputSize: Keccak_PRNG_20201128.InputSize - 1);
                    throw new Exception("outputSize < InputSize");
                }
                catch (ArgumentOutOfRangeException)
                { }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    throw;
                }

                if (Keccak_PRNG_20201128.InputSize != 64)
                    throw new Exception("Keccak_PRNG_20201128.InputSize != 64: the test destined for InputSize == 64");

                osKeccak = new Keccak_PRNG_20201128();
                addNewToKeccaks(64);
                addNewToKeccaks(65);
                addNewToKeccaks(66);
                addNewToKeccaks(67);
                addNewToKeccaks(71);
                addNewToKeccaks(72);
                addNewToKeccaks(73);
                addNewToKeccaks(74);
                addNewToKeccaks(77);
                addNewToKeccaks(121);
                addNewToKeccaks(127);
                addNewToKeccaks(128);
                addNewToKeccaks(129);
                addNewToKeccaks(142);
                addNewToKeccaks(143);
                addNewToKeccaks(144);
                addNewToKeccaks(145);
                addNewToKeccaks(255);
                addNewToKeccaks(256);
                addNewToKeccaks(257);
                addNewToKeccaks(osKeccak.output!.size-1);
                addNewToKeccaks(osKeccak.output .size+0);
                addNewToKeccaks(osKeccak.output .size+1);
                addNewToKeccaks(osKeccak.output .size*16-1);
                addNewToKeccaks(osKeccak.output .size*16+0);
                addNewToKeccaks(osKeccak.output .size*16+1);

                var bytes  = Encoding.UTF8.GetBytes("Я0123456789012345678901234567890123456789");
                var bytes2 = Encoding.UTF8.GetBytes("9876543210");
                var bytes3 = Encoding.UTF8.GetBytes("абвгдеёжзиклмн");

                using var bt2 = Record.getRecordFromBytesArray(bytes2);
                using var bt3 = Record.getRecordFromBytesArray(bytes3);
                exec(  (k) => k.InputKeyAndStep(bt2, bt2.len, bt3, bt3.len)  );

                // Ещё вводим значения для инициализации
                exec(  (k) => k.InputBytes(bytes)  );

                for (int i = 0; i < 16; i++)
                {
                    bytes[2]++;
                    exec
                    (
                        (k) =>
                        {
                            k.InputBytes(bytes);
                        }
                    );
                }

                // Рассчитываем блоки
                exec
                (
                    (k) =>
                    {
                        k.InputBytesImmediately();
                        while (k.isInputReady)
                        {
                            k.calcStepAndSaveBytes(true, 1);
                        }

                        k.calcStepAndSaveBytes(false, 32);
                    }
                );

                using var eRec = osKeccak.output.getBytes();
                exec
                (
                    (k) =>
                    {
                        if (k.outputCount != eRec.len)
                        lock (this)
                        {
                            var testError = new TestError()
                            {
                                Message = "k.outputCount != osKeccak.outputCount"
                            };
                            this.error.Add(testError);
                        }

                        using var rec = k.curAllocator.AllocMemory(eRec.len);
                        k.output!.getBytesAndRemoveIt(rec);
                        if (!rec.UnsecureCompare(eRec))
                        lock (this)
                        {
                            var testError = new TestError()
                            {
                                Message = "!rec.UnsecureCompare(eRec)"
                            };
                            this.error.Add(testError);
                        }
                    },
                    doOsKeccak: false
                );
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
                throw;
            }
            finally
            {
                osKeccak?.Dispose();
                DisposeKeccaks(keccaks);

                osKeccak = null;
                keccaks  = null;
            }
        };  // base.taskFunc end
    }

    private void addNewToKeccaks(nint outputSize, SortedDictionary<nint, Keccak_PRNG_20201128>? keccaks2 = null)
    {
        var keccak = new Keccak_PRNG_20201128(outputSize: outputSize);
        keccaks!.Add(outputSize, keccak);

        if (keccaks2 is not null)
            keccaks2!.Add(outputSize, keccak);
    }

    public delegate void Test_fn(Keccak_PRNG_20201128 keccak);
    void exec(Test_fn func, bool doOsKeccak = true)
    {
        if (doOsKeccak)
            func(osKeccak!);

        //foreach (var (os, keccak) in keccaks!)
        Parallel.ForEach
        (
            keccaks!,
            (keccak, state, index) =>
            {
                func(keccak.Value);
            }
        );
    }
}
