// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Runtime.Serialization.Json;
using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;

public partial class Regime_Service
{
    protected unsafe void MayBeWriteCurrentFile()
    {
        var lcf = ExecEntorpy_now - LastCurrentFile;
        if (lcf > 3600 * ticksPerSecond)
        {
            MandatoryWriteCurrentFile();
        }
    }

    /// <summary>Записывает файлы current.0 и current.1</summary>
    protected unsafe void MandatoryWriteCurrentFile()
    {
        FileInfo RandomAtFolder_Current = getOldestCurrentFile();

        using (var ws = RandomAtFolder_Current!.OpenWrite())
        {
            using (Record output = getEntropyForOut(OutputStrenght, ignoreTerminated: true))
            {
                WriteRecordToFileStream(ws, output);
            }
        }
        LastCurrentFile = ExecEntorpy_now;
    }

    private unsafe FileInfo getOldestCurrentFile()
    {
        RandomAtFolder_Current0!.Refresh(); RandomAtFolder_Current1!.Refresh();

        var RandomAtFolder_Current = RandomAtFolder_Current0;
        try
        {
            if (!RandomAtFolder_Current0.Exists)
                return RandomAtFolder_Current0;
            if (!RandomAtFolder_Current1.Exists)
                return RandomAtFolder_Current1;

            if (RandomAtFolder_Current0.Length < OutputStrenght)
                return RandomAtFolder_Current0;
            if (RandomAtFolder_Current1.Length < OutputStrenght)
                return RandomAtFolder_Current1;

            if (RandomAtFolder_Current.LastWriteTime > RandomAtFolder_Current1.LastWriteTime)
                RandomAtFolder_Current = RandomAtFolder_Current1;
        }
        catch
        {}

        return RandomAtFolder_Current;
    }

    public static unsafe void WriteRecordToFileStream(FileStream ws, Record output, int size = 0)
    {
        checked
        {
            if (size == 0)
                size = (int) output.len;

            var span = new Span<byte>(output, size);
            ws.Write(span);
        }
    }

    /// <summary>Получает случайный вывод, предназначенный для пользователя. Для безопасности при многопоточности, входит в блокировку entropy_sync.</summary>
    /// <param name="outputStrenght">Количество байтов случайного вывода, которое необходимо получить. Не менее чем sizeof(long) байтов</param>
    /// <param name="ignoreTerminated">Всегда false. true только для вызовов для записи файлов current при завершении</param>
    /// <returns>Запрошенный случайный вывод</returns>
    public unsafe Record getEntropyForOut(nint outputStrenght, bool ignoreTerminated = false)
    {
        if (outputStrenght < sizeof(long))
            outputStrenght = sizeof(long);

        var rec   = allocator.AllocMemory(outputStrenght, "getEntropyForOut.rec");
        var recvk = allocator.AllocMemory(outputStrenght, "getEntropyForOut.recvkf");

        try
        {
            lock (entropy_sync)
            {
                if (Terminated)
                    if (!ignoreTerminated)
                    {
                        throw new Exception("Regime_Service.getEntropyForOut: termitaned");
                    }

                Parallel.Invoke
                (
                    () =>
                    {
                        nint outLen = 0;
                        do
                        {
                            BytesBuilder.ULongToBytes((ulong)DateTime.Now.Ticks, rec.array, sizeof(long));

                            CascadeSponge.step
                            (
                                ArmoringSteps: CascadeSponge.countStepsForKeyGeneration - 1,
                                data: rec,
                                dataLen: sizeof(long),
                                regime: 11
                            );

                            outLen += BytesBuilder.CopyTo(CascadeSponge.maxDataLen >> 1, rec.len, CascadeSponge.lastOutput, rec, outLen);
                        }
                        while (outLen < outputStrenght);
                    },

                    () =>
                    {
                        var vkfData = recvk.array;
                        var outLen = outputStrenght;
                        do
                        {
                            BytesBuilder.ULongToBytes((ulong)DateTime.Now.Ticks, recvk.array, sizeof(long));
                            VinKekFish.input!.add(recvk, sizeof(long));

                            VinKekFish.doStepAndIO
                            (
                                countOfRounds: VinKekFish.EXTRA_ROUNDS_K,
                                outputLen: 0,
                                regime: 11
                            );

                            var len = outLen;
                            if (len > VinKekFish.BLOCK_SIZE_KEY_K)
                                len = VinKekFish.BLOCK_SIZE_KEY_K;

                            VinKekFish.doOutput(vkfData, len);
                            vkfData += len;
                            outLen -= len;
                        }
                        while (outLen > 0);
                    }
                );
            }

            // Арифметически складываем байты вывода каскадной губки и ВинКекФиша
            AddBytes(rec.len, rec.array, recvk.array);

            if (!ignoreTerminated)  // Это выполняется не для файлов current
                RemoveFromCountOfBytesCounters(outputStrenght);
        }
        catch
        {
            rec.Dispose();
            throw;
        }
        finally
        {
            recvk.Dispose();
        }

        return rec;
    }

    /// <summary>Арифметически складывает два числа одного и того же размера, записанные в массивах rc1 и rc2: rc1 = rc1 + rc2</summary>
    /// <param name="len">Длина чисел (в байтах)</param>
    /// <param name="rc1">Первое число. Результат сложения будет записан в этот же массив</param>
    /// <param name="rc2">Второе число.</param>
    public static unsafe void AddBytes(nint len, byte* rc1, byte* rc2)
    {
        ushort cf = 0;
        for (nint i = 0; i < len; i++)
            checked
            {
                ushort a = rc1[i];
                ushort b = rc2[i];
                ushort c = (ushort)(a + b + cf);

                rc1[i] = unchecked((byte)c);
                cf = (byte)(c >> 8);
            }
    }
}
