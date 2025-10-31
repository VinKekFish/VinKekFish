// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using maincrypto.keccak;
using vinkekfish;
using System.Xml;
using cryptoprime;
using static vinkekfish.CascadeSponge_1t_20230905;
using cryptoprime.VinKekFish;
using System.ComponentModel;
using static cryptoprime.BytesBuilderForPointers;

public unsafe partial class Enc_std_1_202510: IDisposable
{
    /// <summary>Расшифровывает файл, пользуясь уже установленными параметрами. Все примитивы создаёт сам.</summary>
    public ProgramErrorCode Decrypt()
    {
        var allocator = Keccak_abstract.allocator;
        if (command.isDebugMode)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString());
            Console.WriteLine(L("Decryption started. Wait random data from") + " " + command.autoCrypt.RandomSocketPoint.ToString());
        }

        CreateKeyCascadeSponge();

        lock (command)
        try
        {
            nint EncFileLength;
            using (var encFileStream = File.Open(command.EncryptedFileName!.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                command.EncryptedFileName.Refresh();
                EncFileLength = (nint)command.EncryptedFileName!.Length;
                if (EncFileLength <= OIV_Length + HashLength + 2)
                {
                    Console.WriteLine(L("ERROR") + ": " + L("The file for decryption have zero length") + ".");
                    return ProgramErrorCode.Abandoned;
                }

                encFile = allocator.AllocMemory(EncFileLength, "enc-file-1");

                var readedBytes = encFileStream.Read(encFile);
                if (readedBytes != EncFileLength)
                {
                    throw new Exception($"readedBytes != EncFileLength [{readedBytes} != {EncFileLength}]");
                }
            }

            // Выделяем массив под синхропосылку
            using var OIV = encFile ^ OIV_Length;
            InitSpongesFirst(allocator, OIV);
            using var encFileData = encFile >> OIV_Length;

            // Делаем второй-восьмой проходы
            DecStep0208(encFileData);
            byte[]? DecFileLenData = encFileData.CloneToSafeBytes(0, 20);
            var size = BytesBuilder.BytesToVariableULong(out ulong DecFileLenght, DecFileLenData, 0);

            using var efd = encFileData >> size;
            using var res = efd ^ ( (nint) DecFileLenght + HashLength );

            Cascade_1f!.Step(data: encFileData, dataLen: size, regime: 3);

            DecStep01(res, (nint) DecFileLenght);
            var hashMem = res >> (nint) DecFileLenght;
            if (!hashMem.IsNull())
            {
                Console.WriteLine();

                using (new VinKekFish_Utils.console.ErrorConsoleOptions())
                {
                    Console.Write(L("Incorrect hash of file: this is either a fake file, an incorrect key, an incorrect key file order, or an incorrect 'alg' name") + ".");
                }
                Console.WriteLine();
                return ProgramErrorCode.wrongCryptoHash;
            }
            using (var decFileStream = File.Open(command.DecryptedFileName!.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                decFileStream.Write(res << HashLength);
            }

            Console.WriteLine(L("Decrypted") + ". " + DateTime.Now.ToLongTimeString());
        }
        finally
        {
            // Завершение работы программы
            Dispose();
        }

        return ProgramErrorCode.success;
    }

    private void DecStep01(Record decFileAndData, nint len)
    {
        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "01. " + DateTime.Now.ToLongTimeString());

        // Инициализация губки длиной произведена вне этой функции
        DecApplyCSGamma(decFileAndData, len, Cascade_1f!, 0, HashLength);
    }


    private void DecStep0208(Record encFile)
    {
        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "08. " + DateTime.Now.ToLongTimeString());

        DecApplyVKFGamma(encFile, encFile.len, VinKekFish_2r!, 0); BytesBuilder.ReverseBytes(encFile.len, encFile);
        DecApplyVKFGamma(encFile, encFile.len, VinKekFish_2f!, 0); BytesBuilder.ReverseBytes(encFile.len, encFile);

        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "06. " + DateTime.Now.ToLongTimeString());

        DecApplyCSGamma(encFile, encFile.len, Cascade_2r!, 0); BytesBuilder.ReverseBytes(encFile.len, encFile);
        DecApplyCSGamma(encFile, encFile.len, Cascade_2f!, 0); BytesBuilder.ReverseBytes(encFile.len, encFile);

        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "04. " + DateTime.Now.ToLongTimeString());

        DecApplyVKFGamma(encFile, encFile.len, VinKekFish_1r!, 0); BytesBuilder.ReverseBytes(encFile.len, encFile);
        DecApplyVKFGamma(encFile, encFile.len, VinKekFish_1f!, 0); BytesBuilder.ReverseBytes(encFile.len, encFile);

        // Делаем шаг перемешивания - очень долгий
        // DecStep02p(encFile); // TODO:

        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "02. " + DateTime.Now.ToLongTimeString());

        // Обращаем порядок байтов и накладываем гамму с обратной связью без хеша
        // Начало данного массива в обращённом порядке байтов - это шум, который дополнительно инициализирует губку
        DecApplyCSGamma(encFile, encFile.len, Cascade_1r!, 0);
        BytesBuilder.ReverseBytes(encFile.len, encFile);
    }
}
