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

public unsafe sealed partial class Enc_std_1_202510: IDisposable
{
    public CascadeSponge_mt_20230930?  Cascade_Key    = null;
    public VinKekFishBase_KN_20210525? VinKekFish_Key = null;
    public Record?                     decFileAndData = null;
    public Record?                     encFile        = null;

    public CascadeSponge_mt_20230930?  Cascade_vkf    = null;
    public CascadeSponge_mt_20230930?  Cascade_p      = null;
    public CascadeSponge_mt_20230930?  Cascade_noise  = null;
    public VinKekFishBase_KN_20210525? VinKekFish_n   = null;
    public CascadeSponge_mt_20230930?  Cascade_1f     = null;
    public CascadeSponge_mt_20230930?  Cascade_1r     = null;
    public VinKekFishBase_KN_20210525? VinKekFish_1f  = null;
    public VinKekFishBase_KN_20210525? VinKekFish_1r  = null;
    public CascadeSponge_mt_20230930?  Cascade_2f     = null;
    public CascadeSponge_mt_20230930?  Cascade_2r     = null;
    public VinKekFishBase_KN_20210525? VinKekFish_2f  = null;
    public VinKekFishBase_KN_20210525? VinKekFish_2r  = null;

    public AutoCrypt.KeyDataGenerator? NoiseGenerator = null;


    public readonly AutoCrypt.DecEncCommand command;
    public Enc_std_1_202510(AutoCrypt.DecEncCommand enc_dec_command)
    {
        this.command = enc_dec_command;
    }

    public void Dispose()
    {
        TryToDispose(Cascade_Key);
        TryToDispose(VinKekFish_Key);
        TryToDispose(decFileAndData);
        TryToDispose(encFile);

        Cascade_Key    = null;
        VinKekFish_Key = null;
        decFileAndData        = null;
        encFile        = null;

        TryToDispose(Cascade_vkf);TryToDispose(Cascade_p);TryToDispose(Cascade_noise);
        TryToDispose(Cascade_1f);TryToDispose(Cascade_1r);TryToDispose(Cascade_2f);TryToDispose(Cascade_2r);
        Cascade_vkf = null;Cascade_p  = null;Cascade_noise = null;
        Cascade_1f  = null;Cascade_1r = null;Cascade_2f    = null;Cascade_2r = null;

        TryToDispose(VinKekFish_n);
        TryToDispose(VinKekFish_1f);TryToDispose(VinKekFish_1r);
        TryToDispose(VinKekFish_2f);TryToDispose(VinKekFish_2r);
        VinKekFish_n = null;
        VinKekFish_1f = null;VinKekFish_1r = null;VinKekFish_2f = null;VinKekFish_2r = null;

        TryToDispose(NoiseGenerator);
        NoiseGenerator = null;
    }
                                                                                            /// <summary>Длина синхропосылки (располагается в начале файла)</summary>
    public const nint OIV_Length = 64;                                                      /// <summary>Стойкость шифрования и стойкость генератора ключа (коэффициенты K в VinKekFish)</summary>
    public const byte VKF_K = 3, VKF_KEY_K = 5;                                             /// <summary>Стойкость шифрования для каскадной губки в байтах</summary>
    public const int  KeyStrenght    = VKF_K    *VinKekFishBase_etalonK1.BLOCK_SIZE,        /// <summary>Стойкость генератора ключа для каскадной губки в байтах</summary>
                      KeyKeyStrenght = VKF_KEY_K*VinKekFishBase_etalonK1.BLOCK_SIZE;        /// <summary>Длина хеша в байтах</summary>
    public const int  HashLength     = KeyStrenght*2;                                         /// <summary>Выравнивание длины файла (граница в байтах)</summary>
    public const int  FileAlignment  = 1 << 16;

    public const string Position_END        = "END";
    public const string Position_IOV        = "OIV";
    public const string Position_DataLength = "DataLength";
    public const string Position_Data       = "Data";

    /// <summary>Шифрует файл, пользуясь уже установленными параметрами. Все примитивы создаёт сам.</summary>
    public ProgramErrorCode Encrypt()
    {
        var allocator = Keccak_abstract.allocator;
        if (command.isDebugMode)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString());
            Console.WriteLine(L("Encryption started. Wait random data from") + " " + command.autoCrypt.RandomSocketPoint.ToString());
        }

        CreateKeyCascadeSponge();

        lock (command)
        try
        {
            while (command.bbp.Count == 0)
            {
                if (command.isDebugMode)
                    Console.WriteLine("Waiting for random from /dev/vkf/random");

                Monitor.Wait(this);
            }

            // Выделяем массив под синхропосылку
            // FileShare.Read не нужен, но, почему-то, иногда возникает исключение "file being used by another process".
            using var OIV = command.bbp.GetBytesAndRemoveIt(allocator.AllocMemory(OIV_Length, "InitSpongesFirst.OIV"));
            using var encF = File.Open(command.EncryptedFileName!.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

            nint DecFileLength;
            byte[]? DecFileLenData = null;
            Record decFileRawData, DFLD;
            using (var decFileStream = File.Open(command.DecryptedFileName!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DecFileLength = (nint)command.DecryptedFileName!.Length;
                if (DecFileLength <= 0)
                {
                    Console.WriteLine(L("ERROR") + ": " + L("The file for encryption have zero length") + ".");
                    return ProgramErrorCode.Abandoned;
                }

                // Вычисляем длину поля длины исходного файла
                BytesBuilder.VariableULongToBytes((ulong)DecFileLength, ref DecFileLenData);
                DFLD = Record.GetRecordFromBytesArray(DecFileLenData!, allocator, "DecFileLenData");

                decFileAndData = allocator.AllocMemory((nint)command.DecryptedFileName!.Length + HashLength + DecFileLenData!.Length, "dec-file-1");
                
                BytesBuilder.CopyTo(DFLD, decFileAndData);
                decFileRawData = decFileAndData >> DFLD.len;

                var readedBytes = decFileStream.Read(decFileRawData);
                if (readedBytes != DecFileLength)
                {
                    throw new Exception($"readedBytes != DecFileLength [{readedBytes} != {DecFileLength}]");
                }
            }

            InitSpongesFirst(allocator, OIV, DecFileLength, DecFileLenData!.Length);

            // Делаем первый проход, вычисляем хеш
            Cascade_1f!.Step(data: DFLD, dataLen: DFLD.len, regime: 3);
            DFLD.Dispose();
            EncStep01(decFileAndData, DecFileLength, decFileRawData);
            encFile = allocator.AllocMemory
            (
                decFileAndData.len +
                NoiseGenerator!.keys[0].csc!.len,
                "dec-file-1"
            );

            // Вставка шума
            if (command.isDebugMode)
                Console.WriteLine(L("Step") + "01n. " + DateTime.Now.ToLongTimeString());

            // Выделяем место для массива шифротекста
            // Копируем туда данные, полученные после первого прохода, и шум
            BytesBuilder.CopyTo(decFileAndData, encFile);
            using (var NoisePlacement = encFile >> decFileAndData.len)
            {
                if (NoiseGenerator!.keys[0].csc!.len != NoisePlacement.len)
                    throw new Exception("Enc_std_1_202510: FATAL ERROR: NoiseGenerator!.keys[0].csc!.len != NoisePlacement.len");

                BytesBuilder.CopyTo(NoiseGenerator!.keys[0].csc!, NoisePlacement);
            }

            // Делаем второй-восьмой проходы
            // EncStep0208(encFile);
            if (command.isDebugMode)
                Console.WriteLine(L("Dumping encrypted data to the disk") + ". " + DateTime.Now.ToLongTimeString());

            // Записываем открытый вектор инициализации
            encF.Write(OIV);
            encF.Write(encFile);

            Console.WriteLine(L("Encrypted") + ". " + DateTime.Now.ToLongTimeString());
        }
        finally
        {
            // Завершение работы программы
            Dispose();
        }

        return ProgramErrorCode.success;
    }

    private void EncStep01(Record decFileAndData, nint len, Record decFileRawData)
    {
        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "01. " + DateTime.Now.ToLongTimeString());

        EncApplyCSGamma(decFileRawData, len, Cascade_1f!, 0, HashLength);
    }

    private void EncStep0208(Record encFile)
    {
        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "02. " + DateTime.Now.ToLongTimeString());

        // Обращаем порядок байтов и накладываем гамму с обратной связью без хеша
        // Начало данного массива в обращённом порядке байтов - это шум, который дополнительно инициализирует губку
        BytesBuilder.ReverseBytes(encFile.len, encFile);
        EncApplyCSGamma(encFile, encFile.len, Cascade_1r!, 0);

        // Делаем шаг перемешивания - очень долгий
        // EncStep02p(encFile);

        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "03. " + DateTime.Now.ToLongTimeString());

        BytesBuilder.ReverseBytes(encFile.len, encFile); EncApplyVKFGamma(encFile, encFile.len, VinKekFish_1f!, 0);
        BytesBuilder.ReverseBytes(encFile.len, encFile); EncApplyVKFGamma(encFile, encFile.len, VinKekFish_1r!, 0);

        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "05. " + DateTime.Now.ToLongTimeString());

        BytesBuilder.ReverseBytes(encFile.len, encFile); EncApplyCSGamma(encFile, encFile.len, Cascade_2f!, 0);
        BytesBuilder.ReverseBytes(encFile.len, encFile); EncApplyCSGamma(encFile, encFile.len, Cascade_2r!, 0);

        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "07. " + DateTime.Now.ToLongTimeString());

        BytesBuilder.ReverseBytes(encFile.len, encFile); EncApplyVKFGamma(encFile, encFile.len, VinKekFish_2f!, 0);
        BytesBuilder.ReverseBytes(encFile.len, encFile); EncApplyVKFGamma(encFile, encFile.len, VinKekFish_2r!, 0);
    }

    private void EncStep02p(Record encFile)
    {
        // Перемешивание
        if (command.isDebugMode)
            Console.WriteLine(L("Step") + "02p. " + DateTime.Now.ToLongTimeString());

        CascadeSponge_1t_20230905.StepProgress? progressCsc = new();
        progressCsc.allSteps = encFile.len;
        ThreadPool.QueueUserWorkItem
        (
            delegate
            {
                Cascade_p!.DoRandomPermutationForBytes(encFile.len, encFile, progressCsc: progressCsc);

                lock (progressCsc)
                    Monitor.PulseAll(progressCsc);
            }
        );

        lock (progressCsc)
        while (progressCsc.processedSteps < progressCsc.allSteps)
        {
            if (Monitor.Wait(progressCsc, 30_000))
            {
                Console.WriteLine();
                break;
            }
            else
                Console.Write($"{progressCsc.processedSteps * 100.0 / progressCsc.allSteps:f1}%" + "\t");
        }
    }

    private void EncApplyCSGamma(Record decFile, nint len, CascadeSponge_mt_20230930 csc, byte regime, int HashLenght = 0)
    {
        if (decFile.len < len + HashLenght || HashLenght < 0 || len <= 0)
            throw new ArgumentOutOfRangeException("Enc_std_1_202510.ApplyCSGamma: decFile.len < len + HashLenght");

        csc.Step(regime: regime);
        var tmp  = stackalloc byte[(int) csc.lastOutput.len];
        var blen = csc.lastOutput.len;

        byte * data = decFile.array;
        nint   ProcessedLen = 0;
        while (ProcessedLen < len)
        {
            // Вычисляем длину данных на этом шаге гаммирования
            var DataLenInCurrentStep = len - ProcessedLen;
            if (DataLenInCurrentStep > blen)
                DataLenInCurrentStep = blen;

            BytesBuilder.CopyTo(blen, blen, csc.lastOutput, tmp);
            csc.Step(regime: regime, data: data, dataLen: DataLenInCurrentStep);
            BytesBuilder.Xor(DataLenInCurrentStep, data, tmp);

            data         += DataLenInCurrentStep;
            ProcessedLen += DataLenInCurrentStep;
        }

        // Вычисляем хеш, если запрошено
        ProcessedLen = 0;
        while (ProcessedLen < HashLenght)
        {
            var DataLenInCurrentStep = HashLenght - ProcessedLen;
            if (DataLenInCurrentStep > blen)
                DataLenInCurrentStep = blen;

            csc.Step(regime: regime);
            BytesBuilder.CopyTo(blen, DataLenInCurrentStep, csc.lastOutput, data);

            data         += DataLenInCurrentStep;
            ProcessedLen += DataLenInCurrentStep;
        }
    }

    private void DecApplyCSGamma(Record decFile, nint len, CascadeSponge_mt_20230930 csc, byte regime, int HashLenght = 0)
    {
        if (decFile.len < len + HashLenght || HashLenght < 0 || len <= 0)
            throw new ArgumentOutOfRangeException("Enc_std_1_202510.ApplyCSGamma: decFile.len < len + HashLenght");

        csc.Step(regime: regime);
        var tmp  = stackalloc byte[(int) csc.lastOutput.len];
        var blen = csc.lastOutput.len;

        byte * data = decFile.array;
        nint   ProcessedLen = 0;
        while (ProcessedLen < len)
        {
            // Вычисляем длину данных на этом шаге гаммирования
            var DataLenInCurrentStep = len - ProcessedLen;
            if (DataLenInCurrentStep > blen)
                DataLenInCurrentStep = blen;

            BytesBuilder.CopyTo(blen, blen, csc.lastOutput, tmp);
            BytesBuilder.Xor(DataLenInCurrentStep, data, tmp);
            csc.Step(regime: regime, data: data, dataLen: DataLenInCurrentStep);

            data         += DataLenInCurrentStep;
            ProcessedLen += DataLenInCurrentStep;
        }

        // Вычисляем хеш, если запрошено
        ProcessedLen = 0;
        while (ProcessedLen < HashLenght)
        {
            var DataLenInCurrentStep = HashLenght - ProcessedLen;
            if (DataLenInCurrentStep > blen)
                DataLenInCurrentStep = blen;

            csc.Step(regime: regime);
            BytesBuilder.Xor(DataLenInCurrentStep, data, csc.lastOutput);

            data         += DataLenInCurrentStep;
            ProcessedLen += DataLenInCurrentStep;
        }
    }

    private void EncApplyVKFGamma(Record decFile, nint len, VinKekFishBase_KN_20210525 vkf, byte regime)
    {
        if (decFile.len < len || len <= 0)
            throw new ArgumentOutOfRangeException("Enc_std_1_202510.ApplyVKFGamma: decFile.len < len");

        var blen   = vkf.BLOCK_SIZE_K;
        vkf.input  = new BytesBuilderStatic(blen);
        vkf.output = new BytesBuilderStatic(blen);
        var tmp    = stackalloc byte[blen];

        vkf.DoStepAndIO(regime: regime, outputLen: blen);

        byte * data = decFile.array;
        nint   ProcessedLen = 0;
        while (ProcessedLen < len)
        {
            // Вычисляем длину данных на этом шаге гаммирования
            var DataLenInCurrentStep = len - ProcessedLen;
            if (DataLenInCurrentStep > blen)
                DataLenInCurrentStep = blen;

            vkf.output.GetBytesAndRemoveIt(tmp, blen);
            vkf.DoStepAndIO(regime: regime, outputLen: blen);
            vkf.input.Add(data, DataLenInCurrentStep);
            
            BytesBuilder.Xor(DataLenInCurrentStep, data, tmp);

            data         += DataLenInCurrentStep;
            ProcessedLen += DataLenInCurrentStep;
        }

        vkf. input.Clear();
        vkf.output.Clear();
    }

    private void DecApplyVKFGamma(Record decFile, nint len, VinKekFishBase_KN_20210525 vkf, byte regime)
    {
        if (decFile.len < len || len <= 0)
            throw new ArgumentOutOfRangeException("Enc_std_1_202510.ApplyVKFGamma: decFile.len < len");

        var blen   = vkf.BLOCK_SIZE_K;
        vkf.input  = new BytesBuilderStatic(blen);
        vkf.output = new BytesBuilderStatic(blen);
        var tmp    = stackalloc byte[blen];

        vkf.DoStepAndIO(regime: regime, outputLen: blen);

        byte * data = decFile.array;
        nint   ProcessedLen = 0;
        while (ProcessedLen < len)
        {
            // Вычисляем длину данных на этом шаге гаммирования
            var DataLenInCurrentStep = len - ProcessedLen;
            if (DataLenInCurrentStep > blen)
                DataLenInCurrentStep = blen;

            vkf.output.GetBytesAndRemoveIt(tmp, blen);
            BytesBuilder.Xor(DataLenInCurrentStep, data, tmp);
            vkf.DoStepAndIO(regime: regime, outputLen: blen);
            vkf.input.Add(data, DataLenInCurrentStep);

            data         += DataLenInCurrentStep;
            ProcessedLen += DataLenInCurrentStep;
        }

        vkf. input.Clear();
        vkf.output.Clear();
    }
}
