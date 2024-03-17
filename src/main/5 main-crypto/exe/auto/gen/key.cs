// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Reflection;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public partial class GenKeyCommand: Command, IDisposable
    {                                                                               /// <summary>Опции шифрования ключа</summary>
        public VinKekFishOptions VinKekFish_KeyOpts    = new VinKekFishOptions();       /// <summary>Опции шифрования открытого текста</summary>
        public VinKekFishOptions VinKekFish_CipherOpts = new VinKekFishOptions();       /// <summary>Опции шифрования ключа</summary>
        public CascadeOptions    Cascade_KeyOpts       = new CascadeOptions();          /// <summary>Опции шифрования открытого текста</summary>
        public CascadeOptions    Cascade_CipherOpts    = new CascadeOptions();

        public VinKekFishBase_KN_20210525? VinKekFish_Key;
        public VinKekFishBase_KN_20210525? VinKekFish_Cipher;
        public CascadeSponge_mt_20230930?  Cascade_Key;
        public CascadeSponge_mt_20230930?  Cascade_Cipher;

        public isCorrectAvailable[] CryptoOptions;
        public bool                 isSimpleOutKey = false;
        public int                  newKeyLen      = 11264;

        public GenKeyCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {
            CryptoOptions = new isCorrectAvailable[]
                            {
                                VinKekFish_KeyOpts,
                                VinKekFish_CipherOpts,
                                Cascade_KeyOpts,
                                Cascade_CipherOpts
                            };
        }

        protected readonly List<FileInfo> rnd        = new List<FileInfo>(0);
        protected readonly List<FileInfo> outParts   = new List<FileInfo>(0);
        protected               FileInfo? outKeyFile = null;

        public override ProgramErrorCode Exec()
        {
            VinKekFish_KeyOpts   .Rounds = -1;
            VinKekFish_CipherOpts.Rounds = -1;

            Cascade_KeyOpts   .ArmoringSteps = (int) CascadeSponge_mt_20230930.CalcCountStepsForKeyGeneration(176);
            Cascade_CipherOpts.ArmoringSteps = Cascade_KeyOpts.ArmoringSteps;

            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine
            (
                () => Console.WriteLine
                    (
                        """
                        Commands (not all):
                        out:path_to_file
                        out-part:path_to_file
                        rnd:path_to_file
                        regime:1.0
                        fregime:1.0
                        cascade-c:11264 2
                        cascade-c:11264 2 0
                        vkf-c:11
                        cascade-k:11264 2
                        vkf-k:11
                        len:11264
                        start:
                        end:

                        Example:
                        out:/inRam/new.key
                        start:

                        Example:
                        rnd:/inRamA/random.file
                        rnd:
                        out:/inRamA/new.simple.key
                        simple:true
                        start:


                        """
                    )
            );

            switch (command.name)
            {
                case "vinkekfish-k":
                case "vkf-k":
                        VinKekFish_KeyOpts.Rounds = -1;     // Показываем, что это - генерация ключа
                        VinKekFishOptions.ParseVinKekFishOptions(isDebugMode, command.value.Trim(), VinKekFish_KeyOpts);
                    goto start;
                case "cascade-k":
                        CascadeOptions.ParseCascadeOptions(isDebugMode, command.value.Trim(), Cascade_KeyOpts);
                    goto start;
                case "vinkekfish-c":
                case "vkf-c":
                        VinKekFish_CipherOpts.Rounds = -1;
                        VinKekFishOptions.ParseVinKekFishOptions(isDebugMode, command.value.Trim(), VinKekFish_CipherOpts);
                    goto start;
                case "cascade-c":
                        CascadeOptions.ParseCascadeOptions(isDebugMode, command.value.Trim(), Cascade_CipherOpts);
                    goto start;
                case "len":
                        newKeyLen = int.Parse(command.value.Trim());
                    goto start;
                case "random":
                case "rnd":
                        // Парсим опцию и добавляем файл в список rnd. Если введно "rnd:", то вызывается визуальное окно выбора файла
                        var rndFile = ParseFileOptions(command.value.TrimStart(), isDebugMode, FileMustExists.exists, rnd);
                        if (rndFile == null)
                        {
                            if (!isDebugMode)
                                throw new CommandException(L("File not found or an another file system error occured"));

                            goto start;
                        }

                    goto start;
                case "out":
                        outKeyFile = ParseFileOptions(command.value.TrimStart(), isDebugMode, FileMustExists.notExists)!;
                    goto start;
                case "out-part":
                        ParseFileOptions(command.value.TrimStart(), isDebugMode, FileMustExists.notExists, outParts);
                    goto start;
                case "regime":
                        ParseRegimeOptions(command.value.Trim());
                    goto start;
                case "fregime":
                        ParseRegimeOptions(command.value.Trim());
                    goto start;
                case "issimple":
                case "simple":
                    if (command.value.Trim() == "true" || command.value.Trim() == "yes")
                        isSimpleOutKey = true;
                    else
                        isSimpleOutKey = false;

                    if (isDebugMode)
                        Console.WriteLine("simple:" + isSimpleOutKey);

                    goto start;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;
                    if (outKeyFile is null)
                    {
                        if (!isDebugMode)
                            throw new CommandException("outKeyFile is null");
                        else
                            Console.WriteLine(L("outKeyFile is null"));

                        goto start;
                    }

                    InitOptions();
                    InitSponges(out int _, out int _);
                    break;
                case "end":
                    Terminated = true;
                    return ProgramErrorCode.AbandonedByUser;
                default:
                    if (!isDebugMode)
                        throw new CommandException(L("Command is unknown"));

                    goto start;
            }

            return ProgramErrorCode.success;
        }


        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected void ParseRegimeOptions(string value)
        {
                value  = value.Replace('.', ' ');
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)
            {
                var val = values[0].Trim();
            }
        }

        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected void ParseFRegimeOptions(string value)
        {
                value  = value.Replace('.', ' ');
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)
            {
                var val = values[0].Trim();
            }
        }

        /// <summary>Инициализирует вспомогательные губки для инициализации ключей</summary>
        /// <param name="status">Количество выполненных задач.</param>
        /// <param name="countOfTasks">Общее количество задач.</param>
        public void InitSponges(out int status, out int countOfTasks)
        {
            PrintOptionsToConsole();

            Record? br = new Record("GenKeyCommand.InitSponges.br") { len = 32 };
            byte* b = stackalloc byte[(int)br.len];
            br.array = b;

            status = 0;
            countOfTasks = 15;
            if (isDebugMode)
            {
                Console.WriteLine(L("The primary initialization has started. This may take a long time."));
                Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());
            }

            try
            {
                Cascade_Key    = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);
                Cascade_Cipher = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);        // Cascade_KeyOpts - это правильно, т.к. это шифровальщик ключа, а не шифровальщик пользовательского текста

                Record? br2 = null, br3 = null;
                try
                {
                    InitKeyGenerationSponges(ref status, countOfTasks, br, out br2, out br3);

                    if (isSimpleOutKey)
                        GenerateSimpleKey(ref status, countOfTasks);
                    else
                        CreateKeyFiles(ref status, countOfTasks);
                }
                finally
                {
                    TryToDispose(br2);
                    TryToDispose(br3);
                }
            }
            catch (Exception ex)
            {
                formatException(ex);
                Terminated = true;
            }
            finally
            {
                TryToDispose(this.Cascade_Key);       Cascade_Key       = null;
                TryToDispose(this.Cascade_Cipher);    Cascade_Cipher    = null;
                TryToDispose(this.VinKekFish_Key);    VinKekFish_Key    = null;       // input тоже тут освобождается
                TryToDispose(this.VinKekFish_Cipher); VinKekFish_Cipher = null;

                TryToDispose(br);
            }

            void InitKeyGenerationSponges(ref int status, int countOfTasks, Record br, out Record br2, out Record br3)
            {
                // ------------------------------------------------
                // Вводим данные из /dev/random и получаем в буфер bbp данные из сервиса vkf
                // от br будет далее проинициализированы обе губки
                using (var fs = new FileStream(autoCrypt.RandomNameFromOS, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    fs.Read(br);

                status++;                   // 1
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                Parallel.Invoke
                (
                    Connect,    // Соединяемся с VinKekFish (сервис vkf) и записываем их в bbp
                    () =>
                    {           // Впитываем данные из /dev/random
                        Cascade_Key.step(data: br, dataLen: br.len);
                        Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 1, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);
                    }
                );

                status++;                   // 2
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());
                br2 = bbp.getBytes(RecordDebugName: "GenKeyCommand.InitSponges.br2");
                bbp.Clear();

                // Впитываем данные из сервиса vkf и переинициализируем ключи и таблицы подстановок. Инициализация губки, при этом, не теряется.
                Cascade_Key.step(data: br2, dataLen: br2.len, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 2, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);


                status++;                   // 3
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Набираем данные, достаточные для полной инициализации каскадной губки.
                // Берём из сервиса vkf.
                while (bbp.Count < Cascade_KeyOpts.StrengthInBytes)
                    Connect();

                // Читаем данные из дополнительных файлов с рандомизирующей информацией.
                var rndFilesLen = getRndFileLen(rnd);
                if (rndFilesLen > 0)
                {
                    var rndBuffp = stackalloc byte[rndFilesLen];
                    var rndBuff  = new Record { array = rndBuffp, len = rndFilesLen, Name = "rndFilesLen > 0" };

                    var inputted = 0L;
                    foreach (var rndFile in rnd)
                    {
                        var fileLen = rndFile.Length;
                        using (var content = rndFile.OpenRead())
                        {
                            do
                            {
                                var readed = content.Read(rndBuff);

                                // Отладочный вывод
                                if (isDebugMode)
                                {
                                    Console.WriteLine($"{readed} bytes from {rndFile.FullName}");
                                    // using var ff = File.OpenWrite("/inRamA/tmp");
                                    // ff.Write(new ReadOnlySpan<byte>(rndBuff, readed));
                                }

                                bbp.addWithCopy(rndBuff, (nint) readed, Keccak_abstract.allocator);
                                fileLen  -= readed;
                                inputted += readed;
                            }
                            while (fileLen > 0);
                        }

                        rndBuff.Clear();
                    }

                    rndBuff.Dispose();

                    if (isDebugMode)
                        Console.WriteLine(L("Bytes received from additional random files") + $": {inputted}");
                }

                status++;                   // 4
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Вводим полученную из vkf первичную информацию в каскадную губку.
                // Снова переинициализируем ключи и таблицу подстановок быстрым способом.
                Cascade_Key.step(data: br2, dataLen: br2.len, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 2, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);

                status++;                   // 5
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                br3 = bbp.getBytes(RecordDebugName: "GenKeyCommand.InitSponges.br3");


                // Проводим инициализацию губки VinKekFish с использованием каскадной губки (перекрёстная инициализация)
                VinKekFish_Key = new VinKekFishBase_KN_20210525
                (
                    K: VinKekFish_KeyOpts.K,
                    CountOfRounds: VinKekFish_KeyOpts.Rounds,
                    ThreadCount: 1
                );
                VinKekFish_Key.Init1
                (
                    PreRoundsForTranspose: VinKekFish_KeyOpts.PreRounds,
                    prngToInit: Cascade_Key
                );

                status++;                   // 6
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Инициализируем из /dev/random
                VinKekFish_Key.Init2(key: br, RoundsForFirstKeyBlock: VinKekFish_Key.REDUCED_ROUNDS_K, RoundsForTailsBlock: VinKekFish_Key.REDUCED_ROUNDS_K);

                status++;                   // 7
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Подготавливаем буфер ввода в губку VinKekFish к приёму данных для инициализации от vkf.
                // Реальная стойкость VinKekFish примерно в два раза больше номинальной длины ключа.
                // На всякий случай, делаем небольшой запас (в один блок) для ввода
                nint initLen = VinKekFish_Key.BLOCK_SIZE_K * 3;
                if (initLen < br2.len || initLen < br3.len)
                    initLen = Math.Max(br2.len, br3.len);
                VinKekFish_Key.input = new BytesBuilderStatic(initLen + Cascade_Key.lastOutput.len);

                nint outLen = VinKekFish_Key.BLOCK_SIZE_K * 3;
                if (Cascade_Key.strenghtInBytes > outLen)
                    outLen = Cascade_Key.strenghtInBytes;

                VinKekFish_Key.output = new BytesBuilderStatic(outLen);     // Делаем запас, чтобы выводить сразу по три блока

                // Вводим данные из vkf в губку VinKekFish.
                VinKekFish_Key.input.add(br2, br2.len);
                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.doStepAndIO(regime: 1, countOfRounds: VinKekFish_Key.REDUCED_ROUNDS_K);      // Режим может быть любой, главное, чтобы он не совпадал с последующим и предыдущим режимами

                // Это дополнительные данные, обрабатываются аналогично
                VinKekFish_Key.input.add(br3, br3.len);
                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.doStepAndIO(regime: 3, countOfRounds: VinKekFish_Key.REDUCED_ROUNDS_K);      // Режим может быть любой, главное, чтобы он не совпадал с последующим и предыдущим режимами


                status++;                   // 8
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // ------------------------------------------------
                // Дополнительно генерируем ещё один блок из каскадной губки
                // и вводим этот блок в буфер ввода губки VinKekFish.
                // Таким образом осуществляем часть перекрёстной инициализации
                initLen = VinKekFish_Key.BLOCK_SIZE_K;
                while (VinKekFish_Key.input.Count < initLen)
                {
                    Cascade_Key.step(Cascade_Key.countStepsForKeyGeneration, regime: 1);
                    VinKekFish_Key.input.add(Cascade_Key.lastOutput, Cascade_Key.lastOutput.len >> 1);  // Получаем данные в режиме генерации ключа каскадной губкой: пол блока и увеличенное количество раундов
                }

                status++;                   // 9
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Вводим подготовленные данные в губку VinKekFish
                // Данные вводим в режиме Overwrite, чтобы выполнить необратимое перезатирание части данных.
                // Теперь предыдущие данные (полученные из vkf и введённые сразу в обе губки) будет тяжело воссоздать даже в случае уязвимости губки VinKekFish.
                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.doStepAndIO(regime: 2, Overwrite: true, countOfRounds: VinKekFish_Key.REDUCED_ROUNDS_K);

                // Считаем губку VinKekFish_Key проинициализированной, но дальше ещё будем с ней работать при перекрёстной инициализации каскадной губки

                status++;                   // 10
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // ------------------------------------------------
                // Перекрёстная инициализация: теперь делаем ввод из VinKekFish в каскадную губку
                // Теперь введём из губки VinKekFish_Key данные в каскадную губку.
                // Это нужно для того, чтобы если каскадная губка окажется недостаточно стойкой,
                // рандомизировать её данными из VinKekFish.
                // Обратное уже сделано: губка VinKekFish рандомизирована данными из каскадной губки.
                while (VinKekFish_Key.output.Count < VinKekFish_Key.BLOCK_SIZE_K * 2)
                    VinKekFish_Key.doStepAndIO(outputLen: VinKekFish_Key.BLOCK_SIZE_KEY_K, regime: 1); // Получаем данные в режиме генерации ключа

                status++;                   // 11
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                var s = stackalloc byte[(int)VinKekFish_Key.output.Count];
                using (var sr = new Record() { len = VinKekFish_Key.output.Count, array = s, Name = "GenKeyCommand.InitSponges.s" })
                {
                    VinKekFish_Key.output.getBytesAndRemoveIt(sr);

                    VinKekFish_Key.doStepAndIO(Overwrite: true);    // Обеспечиваем необратимость, перезатирая часть данных губки

                    // Также делаем ввод в режиме необратимой перезаписи для того,
                    // чтобы затруднить восстановление данных, которые были введены для инициализации,
                    // даже если каскадная губка будет уязвима
                    Cascade_Key.step
                    (
                        ArmoringSteps: Cascade_Key.countStepsForKeyGeneration - 1,
                        regime: 2, inputRegime: CascadeSponge_1t_20230905.InputRegime.overwrite,
                        data: sr, dataLen: sr.len
                    );
                }

                Cascade_Key.InitThreeFishByCascade
                (
                    stepToKeyConst: 1,
                    dataLenFromStep: Cascade_Key.lastOutput.len >> 1,
                    countOfStepsForSubstitutionTable: 1,    // 1 - это нормальное количество шагов для генерации таблицы подстановок
                    doCheckSafty: false
                );

                status++;                   // 12
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // ------------------------------------------------
                // Обе губки готовы для генерации ключевой информации и синхропосылки
            }

        }

        protected void GenerateSimpleKey(ref int status, int countOfTasks)
        {
            // VinKekFish_Key
            // Cascade_Key
            // newKeyLen

            var keyVKF     = Keccak_abstract.allocator.AllocMemory(newKeyLen, "GenerateSimpleKey.keyVKF");
            var keyCascade = Keccak_abstract.allocator.AllocMemory(newKeyLen, "GenerateSimpleKey.keyCascade");

            try
            {
                VinKekFish_Key!.output!.Clear();
                var reqLen  = keyVKF.len;
                var current = keyVKF.array;
                do
                {
                    VinKekFish_Key.doStepAndIO(outputLen: VinKekFish_Key.BLOCK_SIZE_KEY_K, regime: 1);

                    var reqLenCurrent = Math.Min(reqLen, VinKekFish_Key.BLOCK_SIZE_KEY_K);
                    VinKekFish_Key.output.getBytesAndRemoveIt(current, reqLenCurrent);

                    reqLen  -= reqLenCurrent;
                    current += reqLenCurrent;
                }
                while (reqLen > 0);

                status++;   // 13
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                reqLen    = keyCascade.len;
                current   = keyCascade.array;
                var block = Cascade_Key!.lastOutput.len / 2;
                do
                {
                    Cascade_Key.step(ArmoringSteps: Cascade_KeyOpts.ArmoringSteps, regime: 1);
                    BytesBuilder.CopyTo(block, reqLen, Cascade_Key.lastOutput, current);

                    reqLen  -= block;
                    current += block;
                }
                while (reqLen > 0);

                status++;   // 14
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Складываем два сгенерированных числа
                // keyVKF содержит результат.
                BytesBuilder.ArithmeticAddBytes(newKeyLen, keyVKF, keyCascade);

                using (var fs = new FileStream(outKeyFile!.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    fs.Write(keyVKF);
                    fs.Flush();
                }

                if (isDebugMode)
                    Console.WriteLine(L("Simple key was writed to") + " " + outKeyFile!.FullName);

                status++;   // 15
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());
            }
            finally
            {
                TryToDispose(keyVKF);
                TryToDispose(keyCascade);
            }
        }

        /// <summary>Функция вычисляет максимальный размер файла, но не учитывает файлы с размером более чем maxLen.</summary>
        /// <param name="rnd">Список файлов. К каждому элементу списка применяется Refresh() перед получением длины файла.</param>
        /// <param name="maxLen">Если файл более, чем maxLen, то его размер не будет учтён.</param>
        protected int getRndFileLen(List<FileInfo> rnd, int maxLen = 65536)
        {
            var result = 0;
            foreach (var rndFile in rnd)
            {
                rndFile.Refresh();
                var len = rndFile.Length;

                if (len > result)
                if (len <= maxLen)
                    result = (int) len;
            }

            return result;
        }

        public void PrintOptionsToConsole()
        {
            if (isDebugMode)
            {
                Console.WriteLine("vkf-k:\t\t"   + VinKekFish_KeyOpts);
                Console.WriteLine("vkf-c:\t\t"   + VinKekFish_CipherOpts);
                Console.WriteLine("cascade-k:\t" + Cascade_KeyOpts);
                Console.WriteLine("cascade-c:\t" + Cascade_CipherOpts);
            }
        }

        /// <summary>Проверить, что все опции заданы, и задать, если не заданы.</summary>
        public void InitOptions()
        {
            try
            {
                if (VinKekFish_KeyOpts.Rounds <= 0)
                    VinKekFishOptions.ParseVinKekFishOptions(false, "11", VinKekFish_KeyOpts);  // K = 11
                if (VinKekFish_CipherOpts.Rounds <= 0)
                    VinKekFishOptions.ParseVinKekFishOptions(false, "11", VinKekFish_CipherOpts);
                if (Cascade_KeyOpts.StrengthInBytes <= 0)
                    CascadeOptions.ParseCascadeOptions(false, "11264 2", Cascade_KeyOpts, true);    // Стойкость 11 кибибайтов
                if (Cascade_CipherOpts.StrengthInBytes <= 0)
                    CascadeOptions.ParseCascadeOptions(false, "11264 2", Cascade_CipherOpts, true);

                foreach (var opt in CryptoOptions)
                {
                    var t = opt.isCorrect();
                    if (t.error is not null)
                        throw new CommandException("InitOptions.CommandException: " + t.error.ParseMessage ?? "");
                }
            }
            catch (Exception ex)
            {
                formatException(ex);
                Terminated = true;
            }
        }

        public override void Dispose(bool fromDestructor = false)
        {
            base.Dispose(fromDestructor);
        }
    }
}
