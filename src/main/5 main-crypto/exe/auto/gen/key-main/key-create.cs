// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Net.Mime;
using System.Reflection;
using System.Text;
using cryptoprime;
using maincrypto.keccak;
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
            // Выравнять файл на границу, кратную 16, но не менее 4096-ти
            // Что делать со вторым ключом? Как обеспечить отказуемое шифрование?
            // throw new NotImplementedException();

            if (VinKekFish_Key!.input!.Count > 0)
                throw new InvalidOperationException("GenKeyCommand.CreateKeyFiles: VinKekFish_Key.input.Count > 0");


            VinKekFishBase_KN_20210525? VinKekFish_KeyGenerator;
            CascadeSponge_mt_20230930?  Cascade_KeyGenerator;
            Record? obfRegimeName = null, OIV = null;
            GetDataByAdd?     gdKeyGenerator = null;
            KeyDataGenerator? dataGenerator  = null;
            List<Record> OIV_parts = new List<Record>(this.outParts.Count);
            try
            {
                // Инициализируем губки для генерации шифруемой информации и синхропосылок файла.
                // Шифруемая информация - это ключи,
                // которые в дальнейшем будут использованы для генерации сессионных ключей
                // при других сессиях шифрования. То есть здесь эти ключи использованы не будут.
                dataGenerator = new KeyDataGenerator(VinKekFish_Key!, Cascade_Key!, Cascade_KeyOpts.ArmoringSteps, "GenKeyCommand.CreateKeyFiles.KeyDataGenerator")
                {
                    keyLenCsc = newKeyLenCsc,
                    keyLenVkf = newKeyLenVkf,
                    willDisposeSponges = false
                };

                var file = new FileParts { Name = "Entire file" };

                // Отбиваем основные ключи от информации, которую будем генерировать далее
                // На всякий случай проводим полную отбивку, потому что синхропосылки доступны злоумышленнику
                dataGenerator.doChopRegime();

                OIV = dataGenerator.getBytes(newKeyLenMin, regime: 17);

                // Генерация отдельных частей синхропосылки
                var oiv_part_len = AlignUtils.Align(newKeyLenMax, 2, 16384);       // 16384 - это минимальный размер синхропосылки из учёта того, что синхропосылка должна быть с высокой вероятностью кратна сектору, а ещё лучше - кластеру.
                GenerateAndWriteOivParts(dataGenerator, OIV_parts, oiv_part_len);

                // Добавляем описания начала файла и генерируем синхропосылку
                addStartPart(dataGenerator, file, OIV, out obfRegimeName);

                status++;                   // 13
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // ----------------------------------------------------------------
                // main больше не нужен - всё сгенерированно, что будет записано в файл
                // Далее инициализируем уже губки для генерации сессионных ключей
                // ----------------------------------------------------------------

                // Пароль вводится здесь. Он вводится после генерации синхропосылки и её частей,
                // т.к. ввод сразу в губку,
                // а губка должна быть проинициализирована до этого синхропосылками
                gdKeyGenerator = InitKeyGenerators(obfRegimeName, OIV, OIV_parts, out VinKekFish_KeyGenerator, out Cascade_KeyGenerator, oiv_part_len);
/*
                file.AddFilePart("keyCSC", data_keyCSC, true);
                file.AddFilePart("keyVKF", data_keyVKF, true);
*/
                // ЭТО НЕВЕРНО!!!
                // ВСЁ НЕВЕРНО!!!
                // var encrypt = new Main_PWD_2024_1.EncryptDataStream(new Record(), gdKeyGenerator, this.VinKekFish_KeyOpts, Cascade_CipherOpts);
            }
            finally
            {
                TryToDispose(gdKeyGenerator);

                //TryToDispose(Cascade_KeyGenerator); // Это и так сделает gdKeyGenerator
                //TryToDispose(VinKekFish_KeyGenerator);
                TryToDispose(obfRegimeName);
                TryToDispose(OIV);

                TryToDispose(dataGenerator);

                foreach (var part in OIV_parts)
                    TryToDispose(part);
            }
        }

        public GetDataByAdd InitKeyGenerators(Record obfRegimeName, Record OIV, List<Record> OIV_parts, out VinKekFishBase_KN_20210525? VinKekFish_KeyGenerator, out CascadeSponge_mt_20230930? Cascade_KeyGenerator, nint oiv_part_len)
        {
            // Инициализируем генераторы ключей синхропосылками
            var regime_KG = 3;
            Cascade_KeyGenerator = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);
            Cascade_KeyGenerator.step(data: obfRegimeName, dataLen: obfRegimeName.len, regime: 1);
            Cascade_KeyGenerator.step(data: OIV,           dataLen: OIV.len,           regime: 2);

            foreach (var part in OIV_parts)
                Cascade_KeyGenerator.step(data: part, dataLen: part.len, regime: (byte)regime_KG++);

            Cascade_KeyGenerator.InitThreeFishByCascade(stepToKeyConst: 1, doCheckSafty: false, countOfSteps: 1, dataLenFromStep: Cascade_KeyGenerator.lastOutput.len);
            // Cascade_KeyGenerator проинициализирован всеми сихропосылками

            // Инициализируем VinKekFish
            var inputLen = Math.Max(Cascade_KeyGenerator.maxDataLen, newKeyLenMax * 2);
            inputLen = Math.Max(inputLen, oiv_part_len);
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
                VinKekFish_KeyGenerator.input.add(part);
                while (VinKekFish_KeyGenerator.input.Count > 0)
                    VinKekFish_KeyGenerator.doStepAndIO(regime: (byte)regime_KG);
            }
            // VinKekFish_KeyGenerator полностью проинициализирован

            // Вводим пароль в обе губки
            if (!noPwd)
                new PasswordEnter(Cascade_KeyGenerator!, VinKekFish_KeyGenerator!, regime: 1, doErrorMessage: true, countOfStepsForPermitations: Cascade_KeyOpts.ArmoringSteps, ArmoringSteps: Cascade_KeyOpts.ArmoringSteps);

            // Перекрёстная инициализация губок
            var gdVinKekFish_KeyGenerator = new GetDataFromVinKekFishSponge(VinKekFish_KeyGenerator);
            using (var cross = gdVinKekFish_KeyGenerator.getBytes(VinKekFish_KeyGenerator.BLOCK_SIZE_K*2, regime: 69))
                Cascade_KeyGenerator.step(data: cross, dataLen: cross.len, ArmoringSteps: Cascade_KeyOpts.ArmoringSteps, regime: 96);

            var gdCascade_KeyGenerator = new GetDataFromCascadeSponge(Cascade_KeyGenerator);
            using (var cross = gdCascade_KeyGenerator.getBytes(VinKekFish_KeyGenerator.BLOCK_SIZE_K*2, regime: 69))
            {
                VinKekFish_KeyGenerator.input.add(cross);
                while (VinKekFish_KeyGenerator.input.Count > 0)
                    VinKekFish_KeyGenerator.doStepAndIO(regime: 96);
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

        /// <summary>Если у ключевого файла есть части, то этот метод генерирует эти части и записывает их в файлы.</summary>
        /// <param name="main">Описатель суммарной губки, которая используется для генерации синхропосылки.</param>
        /// <param name="OIV_parts"></param>
        /// <param name="oiv_part_len"></param>
        protected void GenerateAndWriteOivParts(KeyDataGenerator main, List<Record> OIV_parts, nint oiv_part_len)
        {
            for (int scNum = 0; scNum < outParts.Count; scNum++)
            {
                var partRegime = unchecked((byte)(13 + scNum));
                var OIV_part = main.getBytes(oiv_part_len, regime: partRegime);
                using (var fs = new FileStream(outParts[scNum].FullName + "." + scNum.ToString(), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    fs.Write(OIV_part);
                    fs.Flush();
                }

                OIV_parts.Add(OIV_part);
            }
        }

        protected void addStartPart(KeyDataGenerator main, FileParts file, Record OIV, out Record obfRegimeName)
        {
                  var asciiRegimeName = new ASCIIEncoding().GetBytes(RegimeName);
                      obfRegimeName   = main.getBytes(asciiRegimeName.Length, regime: 23);
            using var recRegimeName   = Record.getRecordFromBytesArray(asciiRegimeName);
            BytesBuilder.ArithmeticAddBytes(obfRegimeName.len, recRegimeName, obfRegimeName);

            // Рассчитываем длину байтов, содержащих длины записываемых массивов
            byte[]? asciiRegimeName_len_record = null;
            BytesBuilder.VariableULongToBytes((ulong)asciiRegimeName.Length, ref asciiRegimeName_len_record);

            file.AddFilePart("Regime name len", asciiRegimeName_len_record!);
            file.AddFilePart("Regime name",     recRegimeName);
            file.AddFilePart("Regime name add", obfRegimeName);

            // file.AddFilePart("OIV len", OIV_len_record!);
            file.AddFilePart("OIV", OIV, createLengthArray: true);
        }
    }
}
