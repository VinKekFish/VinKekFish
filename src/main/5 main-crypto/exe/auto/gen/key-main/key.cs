// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Reflection;
using cryptoprime;
using cryptoprime.VinKekFish;
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
    {                                                                 /// <summary>Опции шифрования ключа</summary>
        public VinKekFishOptions VinKekFish_KeyOpts    = new();       /// <summary>Опции шифрования открытого текста</summary>
        public VinKekFishOptions VinKekFish_CipherOpts = new();       /// <summary>Опции шифрования ключа</summary>
        public CascadeOptions    Cascade_KeyOpts       = new();       /// <summary>Опции шифрования открытого текста</summary>
        public CascadeOptions    Cascade_CipherOpts    = new();

        public VinKekFishBase_KN_20210525? VinKekFish_Key;
        public CascadeSponge_mt_20230930?  Cascade_Key;

        public IIsCorrectAvailable[] CryptoOptions;                                     /// <summary>Сгенерировать простой незашифрованный случайный файл</summary>
        public bool                 isSimpleOutKey = false;                             /// <summary>Если true, то не спрашивать пароль (в таком случае, файл будет доступен без пароля, то есть им сможет воспользоваться кто угодно).</summary>
        public bool                 noPwd          = false;                             /// <summary>Если true, то есть скрытый пароль на скрытые данные.</summary>
        public bool                 havePwd2       = false;
                                                                                        /// <summary>Если true, то существует скрытый (второй) поток данных.</summary>
        public bool                 HaveStream2    => havePwd2 || outParts2.Count > 0;

                                                                                        /// <summary>Режим шифрования файла с ключами шифрования.</summary>
        public string  RegimeName = "main.1.pwd.2024.1";                                /// <summary>Режим шифрования, который будет применяться при шифровании ключами, которые будут зашифрованы в файле-результате. Пустая строка означает, что шифруются не ключи.</summary>
        public string fRegimeName = "main.1.pwd.2024.1";

        public nint newKeyLenVkf = 512;
        public nint newKeyLenCsc = 512;
        public nint newKeyLenMin = 512;
        public nint newKeyLenMax = 512;

        public GenKeyCommand(AutoCrypt autoCrypt): base(autoCrypt)
        {
            CryptoOptions = new IIsCorrectAvailable[]
                            {
                                VinKekFish_KeyOpts,
                                VinKekFish_CipherOpts,
                                Cascade_KeyOpts,
                                Cascade_CipherOpts
                            };
        }

        protected readonly List<FileInfo> rnd        = new(0);
        protected readonly List<FileInfo> outParts   = new(0);
        protected readonly List<FileInfo> outParts2  = new(0);
        protected               FileInfo? outKeyFile = null;

        /// <summary>Запускает команду на выполнение.</summary>
        /// <param name="sr">Поток StreamReader, из которого берутся настройки для шифрования. Если null, то команды берутся с консоли.</param>
        /// <returns>Возвращает код ошибки.</returns>
        public override ProgramErrorCode Exec(StreamReader? sr)
        {
            VinKekFish_KeyOpts   .Rounds = -1;
            VinKekFish_CipherOpts.Rounds = -1;

            Cascade_KeyOpts   .ArmoringSteps = (int) CascadeSponge_mt_20230930.CalcCountStepsForKeyGeneration(176);
            Cascade_CipherOpts.ArmoringSteps = Cascade_KeyOpts.ArmoringSteps;

            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine
            (
                sr,
                () => Console.WriteLine
                    (
                        """
                        Commands (not all):
                        out:path_to_file
                        out-part:path_to_file
                        rnd:path_to_file
                        regime:main.1.pwd.2024.1
                        fregime:1.0
                        cascade-c:11264 2
                        cascade-c:11264 2 0
                        vkf-c:11
                        cascade-k:11264 2
                        vkf-k:11
                        simple:true
                        nopwd:true
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

            var value = command.value.Trim().ToLowerInvariant();

            switch (command.name)
            {
                case "vinkekfish-k":
                case "vkf-k":
                        VinKekFish_KeyOpts.Rounds = -1;     // Показываем, что это - генерация ключа
                        VinKekFishOptions.ParseVinKekFishOptions(isDebugMode, command.value.Trim(), VinKekFish_KeyOpts);
                    goto start;
                case "cascade-k":
                        CascadeOptions.ParseCascadeOptions(isDebugMode, command.value.Trim(), Cascade_KeyOpts, forKey: true);
                    goto start;
                case "vinkekfish-c":
                case "vkf-c":
                        VinKekFish_CipherOpts.Rounds = -1;     // Показываем, что это - генерация ключа
                        VinKekFishOptions.ParseVinKekFishOptions(isDebugMode, command.value.Trim(), VinKekFish_CipherOpts);
                    goto start;
                case "cascade-c":
                        CascadeOptions.ParseCascadeOptions(isDebugMode, command.value.Trim(), Cascade_CipherOpts, forKey: true);
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
                        outKeyFile = ParseFileOptions(command.value.TrimStart(), isDebugMode, FileMustExists.notExists);

                        if (isDebugMode)
                        {
                            if (outKeyFile is not null)
                                Console.WriteLine("out: " + outKeyFile.FullName);
                            else
                                Console.WriteLine(L("File output error: may be file exists?"));
                        }
                        else
                        if (outKeyFile is null)
                        {
                            Console.Error.WriteLine("out: ERROR");
                            return ProgramErrorCode.Abandoned;
                        }

                    goto start;
                case "out-part":
                        var @out = ParseFileOptions(command.value.TrimStart(), isDebugMode, FileMustExists.notExists, outParts);

                        if (isDebugMode)
                        {
                            if (@out is not null)
                                Console.WriteLine("out-part: " + @out.FullName);
                            else
                                Console.WriteLine(L("File output error: may be file exists?"));
                        }
                        else
                        if (@out is null)
                        {
                            Console.Error.WriteLine("out-part: ERROR");
                            return ProgramErrorCode.Abandoned;
                        }

                    goto start;
                case "regime":
                        RegimeName = ParseRegimeOptions(value);
                    goto start;
                case "fregime":
                        fRegimeName = ParseRegimeOptions(value, true);
                    goto start;
                case "issimple":
                case "simple":
                    if (value == "true" || value == "yes" || value == "1")
                        isSimpleOutKey = true;
                    else
                        isSimpleOutKey = false;

                    if (isDebugMode)
                        Console.WriteLine("simple:" + isSimpleOutKey);

                    goto start;
                case "nopwd":
                        if (value == "true" || value == "yes" || value == "1")
                            noPwd = true;
                        else
                            noPwd = false;

                        if (isDebugMode)
                            Console.WriteLine("nopwd:" + noPwd);

                    goto start;
                case "pwd2":
                        if (value == "true" || value == "yes" || value == "1")
                            havePwd2 = true;
                        else
                            havePwd2 = false;

                        if (isDebugMode)
                            Console.WriteLine("pwd2:" + noPwd);

                    goto start;
                case "out-part2":
                    ParseFileOptions(command.value.TrimStart(), isDebugMode, FileMustExists.notExists, outParts2);
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
                    if (string.IsNullOrEmpty(RegimeName))
                    {
                        if (!isDebugMode)
                            throw new CommandException("RegimeName is null");
                        else
                            Console.WriteLine(L("Regime name is undefined"));

                        goto start;
                    }

                    InitOptions();
                    DoFullEncrypt(out int _, out int _);
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

        /// <summary>Разрешённые режимы, которые должна понимать программа. Прочие режимы будут отброшены.</summary>
        public readonly string[] AllowedRegimes = {"main.1.pwd.2024.1", "simple.file.2024.1"};

        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected string ParseRegimeOptions(string value, bool isEmptyRegimeAllowed = false)
        {
            var val = value.Trim().ToLowerInvariant();
            foreach (var regime in AllowedRegimes)
                if (val == regime)
                {
                    return regime;
                }

            if (isEmptyRegimeAllowed && value.Length == 0)
                return "";

            var emsg = L("Regime is unknown") + $": {value}";
            if (isDebugMode)
            {
                Console.Error.WriteLine(emsg);
                return "";
            }
            else
                throw new CommandException($"Regime is unknown: {value}");
        }

        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected static void ParseFRegimeOptions(string value)
        {
            /*
                value  = value.Replace('.', ' ');
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)
            {
                var val = values[0].Trim();
            }*/
        }

        /// <summary>Инициализирует вспомогательные губки для инициализации ключей</summary>
        /// <param name="status">Количество выполненных задач.</param>
        /// <param name="countOfTasks">Общее количество задач.</param>
        public void DoFullEncrypt(out int status, out int countOfTasks)
        {
            PrintOptionsToConsole();

            Record? br = new("GenKeyCommand.InitSponges.br") { len = 32 };
            byte* b = stackalloc byte[(int)br.len];
            br.array = b;

            status = 0;
            countOfTasks = 15;
            if (isDebugMode)
            {
                Console.WriteLine(L("The primary initialization has started. This may take a long time."));
                Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());
            }

            // Примерная максимальная стойкость VinKekFish - это 2,5-3 раза по отношению к номиналу (к размеру блока)
            // Примерная максимальная оценка стойкости каскадной губки - это сумма всех стойкостей внутренних губок keccak
            newKeyLenVkf = VinKekFish_CipherOpts.K * VinKekFishBase_etalonK1.BLOCK_SIZE * 3;
            newKeyLenCsc = Cascade_CipherOpts.StrengthInBytes * (Cascade_CipherOpts.StrengthInBytes / 64);
            newKeyLenMax = Math.Max(newKeyLenVkf, newKeyLenCsc);
            newKeyLenMin = Math.Min(newKeyLenVkf, newKeyLenCsc);

            try
            {
                Cascade_Key = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);

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
                FormatException(ex);
                Terminated = true;
            }
            finally
            {
                TryToDispose(this.Cascade_Key);       Cascade_Key       = null;
                TryToDispose(this.VinKekFish_Key);    VinKekFish_Key    = null;       // input тоже тут освобождается

                TryToDispose(br);
            }
            // Конец функции

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
                        Cascade_Key.Step(data: br, dataLen: br.len);
                        Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 1, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);
                    }
                );

                status++;                   // 2
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());
                br2 = bbp.GetBytes(RecordDebugName: "GenKeyCommand.InitSponges.br2");
                bbp.Clear();

                // Впитываем данные из сервиса vkf и переинициализируем ключи и таблицы подстановок. Инициализация губки, при этом, не теряется.
                Cascade_Key.Step(data: br2, dataLen: br2.len, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 2, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);


                status++;                   // 3
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Набираем данные, достаточные для полной инициализации каскадной губки.
                // Берём из сервиса vkf.
                while (bbp.Count < Cascade_KeyOpts.StrengthInBytes)
                    Connect();

                // Читаем данные из дополнительных файлов с рандомизирующей информацией.
                var rndFilesLen = GetRndFileLen(rnd);
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

                                bbp.AddWithCopy(rndBuff, (nint) readed, Keccak_abstract.allocator);
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
                Cascade_Key.Step(data: br2, dataLen: br2.len, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 2, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);

                status++;                   // 5
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                br3 = bbp.GetBytes(RecordDebugName: "GenKeyCommand.InitSponges.br3");


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
                VinKekFish_Key.input.Add(br2, br2.len);
                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.DoStepAndIO(regime: 1, countOfRounds: VinKekFish_Key.REDUCED_ROUNDS_K);      // Режим может быть любой, главное, чтобы он не совпадал с последующим и предыдущим режимами

                // Это дополнительные данные, обрабатываются аналогично
                VinKekFish_Key.input.Add(br3, br3.len);
                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.DoStepAndIO(regime: 3, countOfRounds: VinKekFish_Key.REDUCED_ROUNDS_K);      // Режим может быть любой, главное, чтобы он не совпадал с последующим и предыдущим режимами


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
                    Cascade_Key.Step(Cascade_Key.countStepsForKeyGeneration, regime: 1);
                    VinKekFish_Key.input.Add(Cascade_Key.lastOutput, Cascade_Key.lastOutput.len >> 1);  // Получаем данные в режиме генерации ключа каскадной губкой: пол блока и увеличенное количество раундов
                }

                status++;                   // 9
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Вводим подготовленные данные в губку VinKekFish
                // Данные вводим в режиме Overwrite, чтобы выполнить необратимое перезатирание части данных.
                // Теперь предыдущие данные (полученные из vkf и введённые сразу в обе губки) будет тяжело воссоздать даже в случае уязвимости губки VinKekFish.
                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.DoStepAndIO(regime: 2, Overwrite: true, countOfRounds: VinKekFish_Key.REDUCED_ROUNDS_K);

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
                    VinKekFish_Key.DoStepAndIO(outputLen: VinKekFish_Key.BLOCK_SIZE_KEY_K, regime: 1); // Получаем данные в режиме генерации ключа

                status++;                   // 11
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                var s = stackalloc byte[(int)VinKekFish_Key.output.Count];
                using (var sr = new Record() { len = VinKekFish_Key.output.Count, array = s, Name = "GenKeyCommand.InitSponges.s" })
                {
                    VinKekFish_Key.output.GetBytesAndRemoveIt(sr);

                    VinKekFish_Key.DoStepAndIO(Overwrite: true);    // Обеспечиваем необратимость, перезатирая часть данных губки

                    // Также делаем ввод в режиме необратимой перезаписи для того,
                    // чтобы затруднить восстановление данных, которые были введены для инициализации,
                    // даже если каскадная губка будет уязвима
                    Cascade_Key.Step
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

            var main = new GetDataByAdd();
            var vkf  = new GetDataFromVinKekFishSponge(VinKekFish_Key!);
            var csc  = new GetDataFromCascadeSponge(Cascade_Key!);
            vkf.BlockLen = VinKekFish_Key!.BLOCK_SIZE_KEY_K;
            csc.BlockLen = Cascade_Key!.lastOutput.len >> 1;

            main.AddSponge(vkf);
            main.AddSponge(csc);

            main.NameForRecord = "GenKeyCommand.GenerateSimpleKey";

            var keyVKF = main.GetBytes(newKeyLenMax, regime: 11);

            try
            {
                using (var fs = new FileStream(outKeyFile!.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    fs.Write(keyVKF);
                    fs.Flush();
                }

                if (isDebugMode)
                    Console.WriteLine(L("Simple key was writed to") + " " + outKeyFile!.FullName);
            }
            finally
            {
                vkf.sponge = null;  // Мы не хотим сейчас очищать губку
                vkf.Dispose();
                csc.sponge = null;
                csc.Dispose();
                keyVKF.Dispose();
            }

            status += 3;   // 12+3=15
            if (isDebugMode)
                Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

        }

        /// <summary>Функция вычисляет максимальный размер файла, но не учитывает файлы с размером более чем maxLen.</summary>
        /// <param name="rnd">Список файлов. К каждому элементу списка применяется Refresh() перед получением длины файла.</param>
        /// <param name="maxLen">Если файл более, чем maxLen, то его размер не будет учтён.</param>
        protected static int GetRndFileLen(List<FileInfo> rnd, int maxLen = 65536)
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
                if (Cascade_KeyOpts.StrengthInBytes < 256)
                    Cascade_KeyOpts.StrengthInBytes = 256;
                if (Cascade_CipherOpts.StrengthInBytes < 256)
                    Cascade_CipherOpts.StrengthInBytes = 256;

                foreach (var opt in CryptoOptions)
                {
                    var t = opt.IsCorrect();
                    if (t.error is not null)
                        throw new CommandException("InitOptions.CommandException: " + t.error.ParseMessage ?? "");
                }
            }
            catch (Exception ex)
            {
                FormatException(ex);
                Terminated = true;
            }
        }

        public override void Dispose(bool fromDestructor = false)
        {
            base.Dispose(fromDestructor);
        }
    }
}
