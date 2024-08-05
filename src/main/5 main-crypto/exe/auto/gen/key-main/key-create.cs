// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Net.Mime;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using cryptoprime;
using maincrypto.keccak;
using Microsoft.VisualBasic;
using vinkekfish;
using VinKekFish_Utils;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using Approximation = FileParts.Approximation;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет команду (для парсинга), которая назначает режим работы "расшифровать"</summary>
    public partial class GenKeyCommand: Command, IDisposable
    {
        // TODO: функция шифрует одинаково вне зависимости от указанного режима
        protected void CreateKeyFiles(ref int status, int countOfTasks)
        {
            // Что мне надо сделать?
            // Выделить место в оперативной памяти для шифрования
            // Рассчитать с помощью иерархических классов потребное место
            // Зашифровать байты, характеризующие режим шифрования ключа, с байтами, полученными после хеширования синхропосылки без ключа и пароля
            // Определить, сколько мне нужно губок для шифрования и чем я буду шифровать
            // Ввести ключ-файлы, синхропосылки в губку для широфвания
            // Ввести пароль в губки
            // Сгенерировать ключ
            // Зашифровать ключ
            // Выравнять файл на границу, кратную 16384
            // Что делать со вторым ключом? Как обеспечить отказуемое шифрование?
            // throw new NotImplementedException();

            if (VinKekFish_Key!.input!.Count > 0)
                throw new InvalidOperationException("GenKeyCommand.CreateKeyFiles: VinKekFish_Key.input.Count > 0");


            Record? obfRegimeName = null, OIV = null, keysToEncrypt = null;
            GetDataByAdd?     gdKeyGenerator = null, gdKeyGenerator2 = null;
            KeyDataGenerator? dataGenerator  = null;
            List<Record> OIV_parts = new(this.outParts.Count);
            FileParts file = new("Entire file");
            try
            {
                // Инициализируем губки для генерации шифруемой информации и синхропосылок файла.
                // Шифруемая информация - это ключи,
                // которые в дальнейшем будут использованы для генерации сессионных ключей
                // при других сессиях шифрования. То есть здесь эти ключи использованы не будут.
                dataGenerator = new KeyDataGenerator(VinKekFish_Key!, Cascade_Key!, (nint) Cascade_KeyOpts.ArmoringSteps, "GenKeyCommand.CreateKeyFiles.KeyDataGenerator")
                {
                    KeyLenCsc = newKeyLenCsc,
                    KeyLenVkf = newKeyLenVkf,
                    willDisposeSponges = false
                };

                // Генерируем ключи, которые мы будем записывать в ключевой файл для последующего использования
                dataGenerator.Generate(RecordNameSuffix: ":CreateKeyFiles.dataGenerator.Generate");

                // Отбиваем основные ключи от информации, которую будем генерировать далее
                // На всякий случай проводим полную отбивку, потому что синхропосылки доступны злоумышленнику
                dataGenerator.DoChopRegime();

                OIV = dataGenerator.GetBytes(newKeyLenMin, regime: 17, RecordNameSuffix: ":CreateKeyFiles.dataGenerator.OIV");

                // Генерация отдельных частей синхропосылки
                var oiv_part_len = AlignUtils.Align(newKeyLenMax, 2, 16384);       // 16384 - это минимальный размер синхропосылки из учёта того, что синхропосылка должна быть с высокой вероятностью кратна сектору, а ещё лучше - кластеру.
                try
                {
                    GenerateAndWriteOivParts(dataGenerator, OIV_parts, oiv_part_len);
                }
                catch (IOException e)
                {
                    Console.Error.WriteLine(L("File output error: may be file exists?"));
                    Console.Error.WriteLine(e.Message);

                    return;
                }

                // Добавляем описания начала файла и генерируем синхропосылку
                AddStartPart(dataGenerator, file, OIV, out obfRegimeName);


                // dataGenerator до этого (в Generate) сгенерировал одну пару ключей. Сейчас мы их возьмём себе.
                // Это именно та пара ключей, которая будет сохранена в ключевом файле.
                if (dataGenerator.keys.Count != 1)
                    throw new Exception("CreateKeyFiles: dataGenerator.keys.Count != 1");

                var keys      = dataGenerator.keys[0];
                keysToEncrypt = keys.GetRecordForPair();

                TryToDispose(dataGenerator); dataGenerator = null;

                status++;                   // 13
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());


                // ----------------------------------------------------------------
                // dataGenerator больше не нужен - всё сгенерированно, что будет записано в файл
                // Далее инициализируем уже губки для генерации сессионных ключей
                // ----------------------------------------------------------------

                // Пароль вводится здесь. Он вводится после генерации синхропосылки и её частей,
                // т.к. ввод сразу в губку,
                // а губка должна быть проинициализирована до этого синхропосылками
                gdKeyGenerator  = InitKeyGenerator(obfRegimeName, OIV, OIV_parts, out VinKekFishBase_KN_20210525? VinKekFish_KeyGenerator , out CascadeSponge_mt_20230930? Cascade_KeyGenerator, noPwd);
                // gdKeyGenerator2 = InitKeyGenerators(obfRegimeName, OIV, OIV_parts, out VinKekFish_KeyGenerator2, out Cascade_KeyGenerator2, oiv_part_len, !havePwd2);

                // var tmpBytes = new UTF8Encoding().GetBytes("Данные для шифрования: это открытый текст. 1234567890");
                // var tmpRec   = Record.GetRecordFromBytesArray(tmpBytes);
                using (var encrypt = new Main_1_PWD_2024_1.CryptDataClass(gdKeyGenerator, this.VinKekFish_KeyOpts, Cascade_CipherOpts))
                {
                    // encrypt.DoEncrypt(tmpRec, file);
                    encrypt.DoEncrypt(keysToEncrypt, file);
                    keysToEncrypt = null;   // Чтобы избежать двойной очистки
                }

                file.WriteToFile(outKeyFile!);
                ResetWriteByChmod(outKeyFile!.FullName);
            }
            finally
            {
                TryToDispose(gdKeyGenerator);
                TryToDispose(gdKeyGenerator2);

                //TryToDispose(Cascade_KeyGenerator); // Это и так сделает gdKeyGenerator
                //TryToDispose(VinKekFish_KeyGenerator);
                TryToDispose(obfRegimeName);
                TryToDispose(OIV);

                TryToDispose(dataGenerator);
                TryToDispose(keysToEncrypt);
                TryToDispose(file);

                foreach (var part in OIV_parts)
                    TryToDispose(part);
            }
        }

        /// <summary>Инициализирует генератор сессионных ключей для шифрования конкретного файла.</summary>
        /// <param name="obfRegimeName">Дополнительная рандомизирующая информация, полученная из функции AddStartPart.</param>
        /// <param name="OIV">Главная синхропосылка.</param>
        /// <param name="OIV_parts">Части синхропосылки из отдельных файлов.</param>
        /// <param name="VinKekFish_KeyGenerator">Созданный генератор ключей на основе VinKekFish. Для сведения. Может не использоваться и не удаляться.</param>
        /// <param name="Cascade_KeyGenerator">Созданный генератор ключей на основе каскадной губки. Для сведения. Может не использоваться и не удаляться.</param>
        /// <param name="noPwd"></param>
        /// <returns>Генератор ключей для шифрования</returns>
        public GetDataByAdd InitKeyGenerator(Record obfRegimeName, Record OIV, List<Record> OIV_parts, out VinKekFishBase_KN_20210525? VinKekFish_KeyGenerator, out CascadeSponge_mt_20230930? Cascade_KeyGenerator, bool noPwd)
        {
            // Инициализируем генераторы ключей синхропосылками
            var regime_KG = 3;
            Cascade_KeyGenerator = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);
            Cascade_KeyGenerator.Step(data: obfRegimeName, dataLen: obfRegimeName.len, regime: 1);
            Cascade_KeyGenerator.Step(data: OIV,           dataLen: OIV.len,           regime: 2);

            foreach (var part in OIV_parts)
                Cascade_KeyGenerator.Step(data: part, dataLen: part.len, regime: (byte)regime_KG++);

            Cascade_KeyGenerator.InitThreeFishByCascade(stepToKeyConst: 1, doCheckSafty: false, countOfSteps: 1, dataLenFromStep: Cascade_KeyGenerator.lastOutput.len);
            // Cascade_KeyGenerator проинициализирован всеми сихропосылками

            // Инициализируем VinKekFish
            var inputLen = Math.Max(Cascade_KeyGenerator.maxDataLen, newKeyLenMax * 2);
            inputLen = Math.Max(inputLen, OIV.len);
            foreach (var oiv_part in OIV_parts)
                inputLen = Math.Max(inputLen, oiv_part.len);

            inputLen = Math.Max(inputLen, Cascade_KeyOpts.StrengthInBytes);
            inputLen = Math.Max(inputLen, VinKekFishBase_KN_20210525.CalcBlockSize(VinKekFish_KeyOpts.K) * 4);  // Берём с запасом

            VinKekFish_KeyGenerator = new VinKekFishBase_KN_20210525(VinKekFish_KeyOpts.Rounds, K: VinKekFish_KeyOpts.K, ThreadCount: 1);
            VinKekFish_KeyGenerator.input  = new BytesBuilderStatic(inputLen);
            VinKekFish_KeyGenerator.output = new BytesBuilderStatic(inputLen);

            VinKekFish_KeyGenerator.Init1(VinKekFish_KeyOpts.PreRounds, prngToInit: Cascade_KeyGenerator);
            VinKekFish_KeyGenerator.Init2(key: OIV, OpenInitializationVector: obfRegimeName);

            regime_KG = 3;
            foreach (var part in OIV_parts)
            {
                regime_KG++;
                VinKekFish_KeyGenerator.input.Add(part);
                while (VinKekFish_KeyGenerator.input.Count > 0)
                    VinKekFish_KeyGenerator.DoStepAndIO(regime: (byte)regime_KG);
            }
            // VinKekFish_KeyGenerator полностью проинициализирован

            // Вводим пароль в обе губки
            if (!noPwd)
                _ = new PasswordEnter(Cascade_KeyGenerator!, VinKekFish_KeyGenerator!, regime: 1, doErrorMessage: true, countOfStepsForPermitations: (nint) Cascade_KeyOpts.ArmoringSteps, ArmoringSteps: (nint) Cascade_KeyOpts.ArmoringSteps);

            // Перекрёстная инициализация губок
            var gdVinKekFish_KeyGenerator = new GetDataFromVinKekFishSponge(VinKekFish_KeyGenerator);
            using (var cross = gdVinKekFish_KeyGenerator.GetBytes(VinKekFish_KeyGenerator.BLOCK_SIZE_K*2, regime: 69))
                Cascade_KeyGenerator.Step(data: cross, dataLen: cross.len, ArmoringSteps: (nint) Cascade_KeyOpts.ArmoringSteps, regime: 96);

            var gdCascade_KeyGenerator = new GetDataFromCascadeSponge(Cascade_KeyGenerator);
            using (var cross = gdCascade_KeyGenerator.GetBytes(VinKekFish_KeyGenerator.BLOCK_SIZE_K*2, regime: 69))
            {
                VinKekFish_KeyGenerator.input.Add(cross);
                while (VinKekFish_KeyGenerator.input.Count > 0)
                    VinKekFish_KeyGenerator.DoStepAndIO(regime: 96);
            }
            // Перекрёстная инициализация губок закончилась

            // Отбивка старых ключей от новых
            Cascade_KeyGenerator.InitThreeFishByCascade();

            // Объединяем губки в одном генераторе
            var gdKeyGenerator = new GetDataByAdd();
            gdKeyGenerator.AddSponge(gdCascade_KeyGenerator);
            gdKeyGenerator.AddSponge(gdVinKekFish_KeyGenerator);

            // Готовы к выработке ключей шифрования
            return gdKeyGenerator;
        }

        /// <summary>Если у ключевого файла есть части (части синхропосылки), то этот метод генерирует эти части и записывает их в файлы.</summary>
        /// <param name="main">Описатель суммарной губки, которая используется для генерации синхропосылки.</param>
        /// <param name="OIV_parts">Список для добавления сгенерированных частей синхропосылки.</param>
        /// <param name="oiv_part_len">Длина каждой части синхропосылки.</param>
        protected void GenerateAndWriteOivParts(KeyDataGenerator main, List<Record> OIV_parts, nint oiv_part_len)
        {
            for (int scNum = 0; scNum < outParts.Count; scNum++)
            {
                var partRegime = unchecked((byte)(13 + scNum));
                var OIV_part = main.GetBytes(oiv_part_len, regime: partRegime);
                using (var fs = new FileStream(outParts[scNum].FullName/* + "." + scNum.ToString()*/, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    fs.Write(OIV_part);
                    fs.Flush();
                }

                ResetWriteByChmod(outParts[scNum].FullName);
                OIV_parts.Add(OIV_part);
            }
        }

        public static void ResetWriteByChmod(string fullFileName)
        {
            try
            {
                System.Diagnostics.Process.Start("chmod", "a-w " + '"' + fullFileName + '"');
            }
            catch (Exception ex)
            {
                DoFormatException(ex);
            }
        }

        /// <summary>Эта константа указывает, какова может быть минимальная длина имени режима шифрования. Сейчас это значение можно уменьшить, если требуется.</summary>
        public const int MinLengthForRegimeName = 16;
        public const int MaxLengthForRegimeName = 32768;

        /// <summary>Создаёт начальную часть файла. Это включает в себя обфусцированное имя режима, обфускационную часть синхропосылки (создаётся внутри функции), синхропосылку (передаётся в функцию).</summary>
        /// <param name="main">Генератор ключей. Используется для генерации обфускационной части синхропосылки. Начинает в режиме 23, заканчивает в режиме 24.</param>
        /// <param name="file">FileParts в который добавляется начальная часть файла.</param>
        /// <param name="OIV">Открытый вектор инициализации (синхропосылка). Должен быть создан заранее вне функции. Нет ограничений на использование main для генерации этого (если OIV генерируется сразу перед функцией, не используйте режим 23). OIV должен быть ранее сгенерирован и может быть использован далее после функции. Пользователь сам освобождает OIV.</param>
        /// <param name="obfRegimeName">Это нужно удалить вручную после получения. Возвращается массив, сгенерированный внутри функции для обфускации имени режима: это отдельный массив, который генерируется так же, как и OIV, и он может быть для этого использован. Этот массив записан в секции файла "Regime name add". Возвращается из функции для использования в качестве дополнительной рандомизирующей информации (синхропосылки).</param>
        protected void AddStartPart(KeyDataGenerator main, FileParts file, Record OIV, out Record obfRegimeName)
        {
            var asciiRegimeName = new ASCIIEncoding().GetBytes(RegimeName);

            if (asciiRegimeName.Length > MaxLengthForRegimeName || asciiRegimeName.Length < MinLengthForRegimeName)
                throw new ArgumentOutOfRangeException("RegimeName", $"asciiRegimeName (RegimeName) length must be <- [{MinLengthForRegimeName}, {MaxLengthForRegimeName}]. Requested regime: \"{RegimeName}\".");

            byte[]? SIB64 = null, vkfRounds = null, vkfPreRounds = null, cscArmoringSteps = null, cscInitSteps = null, cscStepsForTable = null, OIVLength = null, Align = null;
            BytesBuilder.VariableULongToBytes((ulong) Cascade_CipherOpts.StrengthInBytes / 64, ref SIB64);
            BytesBuilder.VariableULongToBytes((ulong) VinKekFish_KeyOpts.Rounds,               ref vkfRounds);
            BytesBuilder.VariableULongToBytes((ulong) VinKekFish_KeyOpts.PreRounds,            ref vkfPreRounds);
            BytesBuilder.VariableULongToBytes((ulong) Cascade_CipherOpts.ArmoringSteps,        ref cscArmoringSteps);
            BytesBuilder.VariableULongToBytes((ulong) Cascade_CipherOpts.InitSteps,            ref cscInitSteps);
            BytesBuilder.VariableULongToBytes((ulong) Cascade_CipherOpts.StepsForTable,        ref cscStepsForTable);
            BytesBuilder.VariableULongToBytes((ulong) OIV.len,                                 ref OIVLength);
            BytesBuilder.VariableULongToBytes((ulong) 65536,                                   ref Align);      // TODO: вставить настраиваемый Align

            // Параметры шифрования
            var bb = new BytesBuilder();
            bb.AddByte((byte) VinKekFish_KeyOpts.K);
            bb.Add(OIVLength!);
            bb.Add(Align!);
            bb.Add(SIB64!);
            bb.Add(cscArmoringSteps!);
            bb.Add(cscInitSteps!);
            bb.Add(cscStepsForTable!);
            bb.Add(vkfRounds!);
            bb.Add(vkfPreRounds!);


            // Создаём массив с длинами массивов
            // Этот массив идёт самым первым
            var  regimeLen = new byte[4];
            nint val       = asciiRegimeName.Length;
            regimeLen[0]   = (byte) val; val >>= 8;
            regimeLen[1]   = (byte) val;

            val          = bb.Count;
            regimeLen[2] = (byte) val; val >>= 8;
            regimeLen[3] = (byte) val;

            // Генерируем массив, обфусцирующий байты длин. Этот массив идёт сразу за массивами длин
            var lenObfs = main.GetBytes(regimeLen.Length*2, regime: 23);

            // Накладываем на массив длин обфускацию
            nint cur = 0;
            fixed (byte * bp_regimeLen = regimeLen)
            {
                cur += BytesBuilder.ArithmeticAddBytes(regimeLen.Length, bp_regimeLen, lenObfs.array + cur);
                cur += BytesBuilder.ArithmeticAddBytes(regimeLen.Length, bp_regimeLen, lenObfs.array + cur);
            }

            // Собираем массив описания режима шифрования, содержащий: имя шифрования в начале и парамаметры шифрования после него
            bb.Add(asciiRegimeName, 0);

            var regimeDescriptionArray = bb.GetBytes(); bb.Clear(); bb = null;
            var recRegimeName          = Record.GetRecordFromBytesArray(regimeDescriptionArray, RecordDebugName: "AddStartPart.recRegimeName");

            obfRegimeName = main.GetBytes(recRegimeName.len * 2, regime: 24);

            // Накладываем обфускацию на остальные массивы
            cur = 0;
            cur += BytesBuilder.ArithmeticAddBytes(recRegimeName.len, recRegimeName, obfRegimeName.array + cur);
            cur += BytesBuilder.ArithmeticAddBytes(recRegimeName.len, recRegimeName, obfRegimeName.array + cur);

            file.AddFilePart("Regime len",      regimeLen,     createLengthArray: false);
            file.AddFilePart("Regime len obfs", lenObfs,       createLengthArray: false);
            file.AddFilePart("Regime obfs",     obfRegimeName, createLengthArray: false, doNotDisposeOption: FileParts.DoNotDisposeEnum.yes);
            file.AddFilePart("Regime",          recRegimeName, createLengthArray: false);

            // file.AddFilePart("OIV len", OIV_len_record!);
            file.AddFilePart("OIV", OIV, createLengthArray: false, doNotDisposeOption: FileParts.DoNotDisposeEnum.yes);
        }
    }
}
