// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using cryptoprime;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public class GenKeyCommand: Command, IDisposable
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

        public override ProgramErrorCode Exec()
        {
            start:

            var command = (CommandOption) CommandOption.ReadAndParseLine
            (
                () => Console.WriteLine
                    (
                        """
                        Commands (not all):
                        start:
                        out:path_to_file
                        rnd:path_to_file
                        regime:1.0
                        cascade-c:512
                        vkf-c:1
                        cascade-k:11264 2
                        vkf-k:11
                        """
                    )
            );

            switch (command.name)
            {
                case "vinkekfish-k":
                case "vkf-k":
                        VinKekFish_KeyOpts.Rounds = -1;
                        ParseVinKekFishOptions(command.value.Trim(), VinKekFish_KeyOpts);
                    goto start;
                case "cascade-k":
                        Cascade_KeyOpts.KOut = 2;
                        ParseCascadeOptions(command.value.Trim(), Cascade_KeyOpts);
                    goto start;
                case "vinkekfish-c":
                case "vkf-c":
                        ParseVinKekFishOptions(command.value.Trim(), VinKekFish_CipherOpts);
                    goto start;
                case "cascade-c":
                        ParseCascadeOptions(command.value.Trim(), Cascade_CipherOpts);
                    goto start;
                case "random":
                case "rnd":
                        ParseFileOptions(command.value.TrimStart());
                    goto start;
                case "out":
                        ParseFileOptions(command.value.TrimStart());
                    goto start;
                case "regime":
                        ParseRegimeOptions(command.value.Trim());
                    goto start;
                case "start":
                    if (Terminated)
                        return ProgramErrorCode.Abandoned;

                    InitOptions();
                    InitSponges();
                    break;
                case "end":
                    return ProgramErrorCode.AbandonedByUser;
                default:
                    if (!isDebugMode)
                        throw new CommandException("Command is unknown");
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

        /// <summary>Распарсить опции команды vinkekfish</summary>
        /// <param name="value">Опции, разделённые пробелом. K Rounds PreRounds KOut</param>
        protected void ParseVinKekFishOptions(string value, VinKekFishOptions opts)
        {
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)     // K
            {
                var val = values[0].Trim();
                var K   = int.Parse(val);
                if (K == 0)
                {
                    if (opts.Rounds == -1)
                        opts.SetK(11);
                    else
                        opts.SetK(1);
                }

                if ((K & 1) != 1)
                    throw new Exception(L("K may be only is 1, 3, 5, 7, 9, 11, 13, 15, 17, 19"));

                if (K >= 1 && K <= 19)
                {
                    opts.K = K;
                    opts.SetK(K);
                }
                else
                    throw new Exception(L("K may be only is 1, 3, 5, 7, 9, 11, 13, 15, 17, 19"));
            }

            if (values.Length >= 2)     // Rounds
            {
                var val    = values[1].Trim();
                var Rounds = int.Parse(val);
                if (Rounds != 0)
                {
                    opts.Rounds = Rounds;
                }
            }

            if (values.Length >= 3)     // PreRounds
            {
                var val = values[2].Trim();
                var PR  = int.Parse(val);
                if (PR != 0)
                {
                    opts.PreRounds = PR;
                }
            }

            if (values.Length >= 4)     // KOut
            {
                var val = values[3].Trim();
                var KO  = float.Parse(val, System.Globalization.NumberStyles.Float);
                if (KO != 0)
                {
                    opts.KOut = KO;
                }
            }

            if (isDebugMode)
                Console.WriteLine(opts);
        }

        /// <summary>Распарсить опции команды cascade</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected void ParseCascadeOptions(string value, CascadeOptions opts)
        {
            var values = ToSpaceSeparated(value);
            if (values.Length >= 1)
            {
                var val   = values[0].Trim();
                var bytes = int.Parse(val);
                if (bytes > 0)
                {
                    opts.StrengthInBytes = bytes;
                }
            }

            if (values.Length >= 2)
            {
                var val = values[1].Trim();
                var K   = int.Parse(val);
                if (K > 0)
                {
                    opts.KOut = K;
                }
            }

            if (isDebugMode)
                Console.WriteLine(opts);
        }

        /// <summary>Инициализирует вспомогательные губки для инициализации ключей</summary>
        public void InitSponges()
        {
            PrintOptionsToConsole();

            Record? br = new Record("GenKeyCommand.InitSponges.br") { len = 32 };
            byte* b = stackalloc byte[(int)br.len];
            br.array = b;

            try
            {
                Cascade_Key = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);
                Cascade_Cipher = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);        // Cascade_KeyOpts - это правильно, т.к. это шифровальщик ключа, а не шифровальщик пользовательского текста

                // ------------------------------------------------
                // Вводим данные из /dev/random и получаем в буфер bbp данные из сервиса vkf
                // от br будет далее проинициализированы обе губки
                using (var fs = new FileStream(autoCrypt.RandomNameFromOS, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    fs.Read(br);

                Parallel.Invoke
                (
                    Connect,    // Соединяемся с VinKekFish (сервис vkf) и записываем их в bbp
                    () =>
                    {           // Впитываем данные из /dev/random
                        Cascade_Key.step(data: br, dataLen: br.len);
                        Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 1, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);
                    }
                );

                // TODO: добавить ввод из rnd

                // ------------------------------------------------
                // Подготавливаем данные, полученные из сервиса vkf
                using var br2 = bbp.getBytes(RecordDebugName: "GenKeyCommand.InitSponges.br2");
                bbp.Clear();

                // Впитываем данные из сервиса vkf и переинициализируем ключи и таблицы подстановок. Инициализация губки, при этом, не теряется.
                Cascade_Key.step(data: br2, dataLen: br2.len, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 2, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);

                // Набираем данные, достаточные для полной инициализации каскадной губки.
                // Берём из сервиса vkf.
                while (bbp.Count < Cascade_KeyOpts.StrengthInBytes)
                    Connect();

                // Вводим полученную из vkf первичную информацию в каскадную губку.
                // Снова переинициализируем ключи и таблицу подстановок быстрым способом.
                Cascade_Key.step(data: br2, dataLen: br2.len, ArmoringSteps: Cascade_Key.countStepsForKeyGeneration);
                Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 1, countOfSteps: 2, dataLenFromStep: Cascade_Key.lastOutput.len, doCheckSafty: false);

                // Это дополнительная информация, полученная из vkf
                using var br3 = bbp.getBytes(RecordDebugName: "GenKeyCommand.InitSponges.br3");


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
                // Инициализируем из /dev/random
                VinKekFish_Key.Init2(key: br, RoundsForFirstKeyBlock: VinKekFish_Key.REDUCED_ROUNDS_K, RoundsForTailsBlock: VinKekFish_Key.REDUCED_ROUNDS_K);

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

                // Вводим подготовленные данные в губку VinKekFish
                // Данные вводим в режиме Overwrite, чтобы выполнить необратимое перезатирание части данных.
                // Теперь предыдущие данные (полученные из vkf и введённые сразу в обе губки) будет тяжело воссоздать даже в случае уязвимости губки VinKekFish.
                while (VinKekFish_Key.input.Count > 0)
                    VinKekFish_Key.doStepAndIO(regime: 2, Overwrite: true, countOfRounds: VinKekFish_Key.REDUCED_ROUNDS_K);

                // Считаем губку VinKekFish_Key проинициализированной, но дальше ещё будем с ней работать при перекрёстной инициализации каскадной губки


                // ------------------------------------------------
                // Перекрёстная инициализация: теперь делаем ввод из VinKekFish в каскадную губку
                // Теперь введём из губки VinKekFish_Key данные в каскадную губку.
                // Это нужно для того, чтобы если каскадная губка окажется недостаточно стойкой,
                // рандомизировать её данными из VinKekFish.
                // Обратное уже сделано: губка VinKekFish рандомизирована данными из каскадной губки.
                while (VinKekFish_Key.output.Count < VinKekFish_Key.BLOCK_SIZE_K * 2)
                    VinKekFish_Key.doStepAndIO(outputLen: VinKekFish_Key.BLOCK_SIZE_KEY_K, regime: 1); // Получаем данные в режиме генерации ключа

                var s = stackalloc byte[(int)VinKekFish_Key.output.Count];
                using var sr = new Record() { len = VinKekFish_Key.output.Count, array = s, Name = "GenKeyCommand.InitSponges.s" };
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

                Cascade_Key.InitThreeFishByCascade
                (
                    stepToKeyConst: 1,
                    dataLenFromStep: Cascade_Key.lastOutput.len >> 1,
                    countOfStepsForSubstitutionTable: 2,     // Это увеличение количества шагов при генерации, т.к. обычно генерация идёт с одним шагом
                    doCheckSafty: false
                );

                // ------------------------------------------------
                // Обе губки готовы для генерации ключевой информации и синхропосылки
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
        }

        public void PrintOptionsToConsole()        
        {
            if (isDebugMode)
            {
                Console.WriteLine("vkf-k: "   + VinKekFish_KeyOpts);
                Console.WriteLine("vkf-c: "   + VinKekFish_CipherOpts);
                Console.WriteLine("cascade-k" + Cascade_Key);
                Console.WriteLine("cascade-c" + Cascade_Cipher);
            }
        }

        /// <summary>Проверить, что все опции заданы и задать, если не заданы.</summary>
        public void InitOptions()
        {
            try
            {
                if (VinKekFish_KeyOpts.Rounds <= 0)
                    ParseVinKekFishOptions("11", VinKekFish_KeyOpts);
                if (VinKekFish_CipherOpts.Rounds <= 0)
                    ParseVinKekFishOptions("11", VinKekFish_CipherOpts);

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
