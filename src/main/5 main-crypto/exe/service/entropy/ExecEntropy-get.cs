// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

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
        FileInfo RandomAtFolder_Current = GetOldestCurrentFile();

        using (var ws = RandomAtFolder_Current!.OpenWrite())
        {
            using (Record output = GetEntropyForOut(OutputStrenght, ignoreTerminated: true))
            {
                WriteRecordToFileStream(ws, output);
            }
        }

        LastCurrentFile = ExecEntorpy_now;
        Console.WriteLine(L("Entropy saved for the file") + ": " + RandomAtFolder_Current.FullName);
    }

    private unsafe FileInfo GetOldestCurrentFile()
    {
        randomAtFolder_Current!.Refresh();

        try
        {
            var file = randomAtFolder_Current.GetFirstNotExists();
            if (file != null)
                return file;
        }
        catch (Exception ex)
        {
            DoFormatException(ex);
        }

        return randomAtFolder_Current.GetOldestFile();
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

    /// <summary>Минимальный размер блока. Именно этот размер возвращает программа, когда выдаёт энтропию стронним приложениям.</summary>
    public const int MinBlockSize = 404;

    /// <summary>Возвращает минимальный размер блока. Именно этот размер возвращает программа, когда выдаёт энтропию стронним приложениям.</summary>
    public static unsafe nint GetMinBlockSize()
    {
        return MinBlockSize;
        // return Math.Min(VinKekFish.BLOCK_SIZE_KEY_K, CascadeSponge.maxDataLen >> 1);
    }

    /// <summary>Возвращает максимальный размер блока. Именно этот размер возвращает программа, когда выдаёт энтропию стронним приложениям.</summary>
    public unsafe nint GetMaxBlockSize()
    {
        return Math.Max(VinKekFish.BLOCK_SIZE_K, CascadeSponge.maxDataLen);
    }

    /// <summary>Получает случайный вывод, предназначенный для пользователя. Для безопасности при многопоточности, входит в блокировку entropy_sync.</summary>
    /// <param name="outputStrenght">Количество байтов случайного вывода, которое необходимо получить. Не менее чем sizeof(long) байтов</param>
    /// <param name="ignoreTerminated">Всегда false. true только для вызовов для записи файлов current при завершении</param>
    /// <returns>Запрошенный случайный вывод</returns>
    public unsafe Record GetEntropyForOut(nint outputStrenght, bool ignoreTerminated = false)
    {
        ConditionalInputEntropyToMainSponges(nint.MaxValue);

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
                        byte * dt = stackalloc byte[sizeof(long)];

                        nint outLen = 0;
                        do
                        {
                            BytesBuilder.ULongToBytes((ulong)DateTime.Now.Ticks, dt, sizeof(long));

                            CascadeSponge.Step
                            (
                                ArmoringSteps: CascadeSponge.countStepsForKeyGeneration - 1,
                                data:          dt,
                                dataLen:       sizeof(long),
                                regime:        11
                            );

                            outLen += BytesBuilder.CopyTo
                            (
                                sourceLength: CascadeSponge.maxDataLen >> 1,
                                targetLength: rec.len,
                                s:            CascadeSponge.lastOutput,
                                t:            rec,
                                targetIndex:  outLen
                            );
                        }
                        while (outLen < outputStrenght);
                    },

                    () =>
                    {
                        byte * dt = stackalloc byte[sizeof(long)];

                        var vkfData = recvk.array;
                        var outLen = outputStrenght;
                        do
                        {
                            BytesBuilder.ULongToBytes((ulong)DateTime.Now.Ticks, dt, sizeof(long));
                            VinKekFish.input!.Add(dt, sizeof(long));

                            VinKekFish.DoStepAndIO
                            (
                                countOfRounds: VinKekFish.EXTRA_ROUNDS_K,
                                outputLen: 0,
                                regime: 11
                            );

                            var len = outLen;
                            if (len > VinKekFish.BLOCK_SIZE_KEY_K)
                                len = VinKekFish.BLOCK_SIZE_KEY_K;

                            VinKekFish.DoOutput(vkfData, len);
                            vkfData += len;
                            outLen  -= len;
                        }
                        while (outLen > 0);
                    }
                );
            }

            // Арифметически складываем байты вывода каскадной губки и ВинКекФиша
            BytesBuilder.ArithmeticAddBytes(rec.len, rec.array, recvk.array);

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
}
