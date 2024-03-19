// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

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

            var main     = new GetDataByAdd();
            var vkf      = new GetDataFromVinKekFishSponge(VinKekFish_Key!);
            var csc      = new GetDataFromCascadeSponge(Cascade_Key!);
            vkf.blockLen = VinKekFish_Key!.BLOCK_SIZE_KEY_K;
            csc.blockLen = Cascade_Key!.lastOutput.len >> 1;

            main.AddSponge(vkf);
            main.AddSponge(csc);

            main.NameForRecord = "GenKeyCommand.CreateKeyFiles";

            var file = new FileParts { Name = "Entire file" };

            // Эти ключи - это информация для шифрования. Это ключи, которыми программа будет шифровать какие-либо файлы в будущем, то есть они будут сохранены в файле и зашифрованы.
            var keyVKF2 = main.getBytes(newKeyLen, regime: 11);
            var keyCSC1 = main.getBytes(newKeyLen, regime: 13);
            var keyCSC2 = main.getBytes(newKeyLen, regime: 13);
            var keyVKF4 = main.getBytes(newKeyLen, regime: 11);
            var keyCSCP = main.getBytes(newKeyLen, regime: 13);     // Ключ для перестановок
            var keyCSCN = main.getBytes(newKeyLen, regime: 11);     // Ключ для шума
            var keyVKFN = main.getBytes(newKeyLen, regime: 13);     // Ключ для шума

            // Отбиваем основные ключи от информации, которую будем генерировать далее
            // На всякий случай проводим полную отбивку, потому что синхропосылки доступны злоумышленнику
            Cascade_Key.InitThreeFishByCascade();

            var OIV = main.getBytes(newKeyLen, regime: 17);

            List<Record> OIV_parts = new List<Record>(this.outParts.Count);

            VinKekFishBase_KN_20210525? VinKekFish_KeyGenerator = null;
            CascadeSponge_mt_20230930?  Cascade_KeyGenerator    = null;
            Record? obfRegimeName = null;
            var keys = new List<Record>(6);
            GetDataByAdd? gdKeyGenerator = null;

            try
            {
                // Генерация отдельных частей синхропосылки
                var oiv_part_len = AlignUtils.Align(newKeyLen, 2, 65536);
                GenerateAndWriteOivParts(main, OIV_parts, oiv_part_len);

                // Добавляем описания начала файла и генерируем синхропосылку
                addStartPart(main, file, OIV, out obfRegimeName);

                status++;                   // 13
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                gdKeyGenerator = InitKeyGenerators(obfRegimeName, OIV, OIV_parts, out VinKekFish_KeyGenerator, out Cascade_KeyGenerator, oiv_part_len);

                // Генерируем ключи шифрования для шифрования ключевого файла
                keys.Add(gdKeyGenerator.getBytes(VinKekFish_KeyGenerator!.BLOCK_SIZE_K*3, regime: 1));  // Гаммирование
                keys.Add(gdKeyGenerator.getBytes(VinKekFish_KeyGenerator!.BLOCK_SIZE_K*3, regime: 2));  // Окончательное гаммирование с обратной связью
                keys.Add(gdKeyGenerator.getBytes(Cascade_KeyOpts.StrengthInBytes,         regime: 3));  // Первичное гаммирование с обратной связью
                keys.Add(gdKeyGenerator.getBytes(Cascade_KeyOpts.StrengthInBytes,         regime: 4));  // Гаммирование
                keys.Add(gdKeyGenerator.getBytes(Cascade_KeyOpts.StrengthInBytes,         regime: 5));  // Перестановки
                keys.Add(gdKeyGenerator.getBytes(Cascade_KeyOpts.StrengthInBytes,         regime: 6));  // Генерация шума
                keys.Add(gdKeyGenerator.getBytes(VinKekFish_KeyGenerator!.BLOCK_SIZE_K*3, regime: 7));  // Генерация шума
            }
            finally
            {
                vkf.sponge = null;
                csc.sponge = null;

                foreach (var key in keys)
                    TryToDispose(key);
                keys.Clear();
                TryToDispose(gdKeyGenerator);

                TryToDispose(main);
                //TryToDispose(Cascade_KeyGenerator);
                //TryToDispose(VinKekFish_KeyGenerator);
                TryToDispose(obfRegimeName);
                TryToDispose(OIV);

                TryToDispose(keyVKF2);
                TryToDispose(keyCSC1);
                TryToDispose(keyVKF4);
                TryToDispose(keyCSC2);
                TryToDispose(keyCSCP);
                TryToDispose(keyCSCN);
                TryToDispose(keyVKFN);

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
            var inputLen = Math.Max(Cascade_KeyGenerator.maxDataLen, newKeyLen * 2);
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
        protected void GenerateAndWriteOivParts(GetDataByAdd main, List<Record> OIV_parts, nint oiv_part_len)
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

        protected void addStartPart(GetDataByAdd main, FileParts file, Record OIV, out Record obfRegimeName)
        {
                  var asciiRegimeName = new ASCIIEncoding().GetBytes(RegimeName);
                      obfRegimeName   = main.getBytes(asciiRegimeName.Length, regime: 23);
            using var recRegimeName   = Record.getRecordFromBytesArray(asciiRegimeName);
            BytesBuilder.ArithmeticAddBytes(obfRegimeName.len, recRegimeName, obfRegimeName);

            // Рассчитываем длину байтов, содержащих длины записываемых массивов
            byte[]? OIV_len_record = null, asciiRegimeName_len_record = null;
            BytesBuilder.VariableULongToBytes((ulong)asciiRegimeName.Length, ref asciiRegimeName_len_record);
            BytesBuilder.VariableULongToBytes((ulong)OIV.len, ref OIV_len_record);

            file.AddFilePart("Regime name len", asciiRegimeName_len_record!);
            file.AddFilePart("Regime name",     recRegimeName);
            file.AddFilePart("Regime name add", obfRegimeName);

            file.AddFilePart("OIV len", OIV_len_record!);
            file.AddFilePart("OIV",     OIV);
        }
    }
}
