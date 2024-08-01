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

    private static void DisposeKeccaks(SortedDictionary<nint, Keccak_PRNG_20201128>? keccaks)
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
        base.TaskFunc = () =>
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
                AddNewToKeccaks(64);
                AddNewToKeccaks(65);
                AddNewToKeccaks(66);
                AddNewToKeccaks(67);
                AddNewToKeccaks(71);
                AddNewToKeccaks(72);
                AddNewToKeccaks(73);
                AddNewToKeccaks(74);
                AddNewToKeccaks(77);
                AddNewToKeccaks(121);
                AddNewToKeccaks(127);
                AddNewToKeccaks(128);
                AddNewToKeccaks(129);
                AddNewToKeccaks(142);
                AddNewToKeccaks(143);
                AddNewToKeccaks(144);
                AddNewToKeccaks(145);
                AddNewToKeccaks(255);
                AddNewToKeccaks(256);
                AddNewToKeccaks(257);
                AddNewToKeccaks(osKeccak.output!.size-1);
                AddNewToKeccaks(osKeccak.output .size+0);
                AddNewToKeccaks(osKeccak.output .size+1);
                AddNewToKeccaks(osKeccak.output .size*16-1);
                AddNewToKeccaks(osKeccak.output .size*16+0);
                AddNewToKeccaks(osKeccak.output .size*16+1);

                var bytes  = Encoding.UTF8.GetBytes("Я0123456789012345678901234567890123456789");
                var bytes2 = Encoding.UTF8.GetBytes("9876543210");
                var bytes3 = Encoding.UTF8.GetBytes("абвгдеёжзиклмн");

                using var bt2 = Record.GetRecordFromBytesArray(bytes2);
                using var bt3 = Record.GetRecordFromBytesArray(bytes3);
                Exec(  (k) => k.InputKeyAndStep(bt2, bt2.len, bt3, bt3.len)  );

                // Ещё вводим значения для инициализации
                Exec(  (k) => k.InputBytes(bytes)  );

                for (int i = 0; i < 16; i++)
                {
                    bytes[2]++;
                    Exec
                    (
                        (k) =>
                        {
                            k.InputBytes(bytes);
                        }
                    );
                }

                // Рассчитываем блоки
                Exec
                (
                    (k) =>
                    {
                        k.InputBytesImmediately();
                        while (k.IsInputReady)
                        {
                            k.CalcStepAndSaveBytes(true, 1);
                        }

                        k.CalcStepAndSaveBytes(false, 32);
                    }
                );

                using var eRec = osKeccak.output.GetBytes();
                Exec
                (
                    (k) =>
                    {
                        if (k.OutputCount != eRec.len)
                        lock (this)
                        {
                            var testError = new TestError()
                            {
                                Message = "k.outputCount != osKeccak.outputCount"
                            };
                            this.error.Add(testError);
                        }

                        using var rec = k.curAllocator.AllocMemory(eRec.len);
                        k.output!.GetBytesAndRemoveIt(rec);
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

    private void AddNewToKeccaks(nint outputSize, SortedDictionary<nint, Keccak_PRNG_20201128>? keccaks2 = null)
    {
        var keccak = new Keccak_PRNG_20201128(outputSize: outputSize);
        keccaks!.Add(outputSize, keccak);

        if (keccaks2 is not null)
            keccaks2!.Add(outputSize, keccak);
    }

    public delegate void Test_fn(Keccak_PRNG_20201128 keccak);
    void Exec(Test_fn func, bool doOsKeccak = true)
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
