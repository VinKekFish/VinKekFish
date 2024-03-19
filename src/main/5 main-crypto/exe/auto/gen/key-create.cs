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
            // Создать абстрактный генератор данных для того, чтобы можно было с ним работать без особенностей губок
            // Сделать функцию ввода пароля
            // Сгенерировать синхропосылки и распределить их по частям файла, если нужно: для этого мне надо создать функцию или вспомогательный класс для генерации данных с помощью сложения из двух функций
            // Выделить место в оперативной памяти для шифрования
            // Рассчитать с помощью иерархических классов потребное место
            // Записать байты, характеризующие режим шифрования ключа
            // Записать синхропосылки
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

            var keyVKF0 = main.getBytes(newKeyLen, regime: 11);
            var keyCSC0 = main.getBytes(newKeyLen, regime: 13);

            // Отбиваем основные ключи от информации, которую будем генерировать далее
            // На всякий случай проводим полную отбивку, потому что синхропосылки доступны злоумышленнику
            Cascade_Key.InitThreeFishByCascade();

            var OIV = main.getBytes(newKeyLen, regime: 17);

            List<Record> OIV_parts = new List<Record>(this.outParts.Count);

            VinKekFishBase_KN_20210525? VinKekFish_KeyGenerator = null;
            CascadeSponge_mt_20230930?  Cascade_KeyGenerator    = null;

            try
            {
                // Генерация отдельных частей синхропосылки
                var oiv_part_len = AlignUtils.Align(newKeyLen, 2, 65536);
                GenerateAndWriteOivParts(main, OIV_parts, oiv_part_len);

                // Добавляем описания начала файла и генерируем синхропосылку
                addStartPart(main, file, OIV);

                status++;                   // 13
                if (isDebugMode)
                    Console.WriteLine($"{status,2}/{countOfTasks}. " + DateTime.Now.ToLongTimeString());

                // Инициализируем генераторы ключей
                var regime_KG = 3;
                Cascade_KeyGenerator = new CascadeSponge_mt_20230930(Cascade_KeyOpts.StrengthInBytes);
                Cascade_KeyGenerator.step(data: OIV, dataLen: OIV.len, regime: 1);
                foreach (var part in OIV_parts)
                    Cascade_KeyGenerator.step(data: part, dataLen: part.len, regime: (byte) regime_KG++);

                Cascade_KeyGenerator.InitThreeFishByCascade(countOfSteps: 1, doCheckSafty: false);

                var inputLen = Math.Max(Cascade_KeyGenerator.maxDataLen, newKeyLen*2);
                    inputLen = Math.Max(inputLen, oiv_part_len);

                VinKekFish_KeyGenerator = new VinKekFishBase_KN_20210525(VinKekFish_KeyOpts.Rounds, K: VinKekFish_KeyOpts.K, ThreadCount: 1);
                VinKekFish_KeyGenerator.input = new BytesBuilderStatic(inputLen);

                VinKekFish_KeyGenerator.Init1(VinKekFish_KeyOpts.PreRounds, prngToInit: Cascade_KeyGenerator);
                VinKekFish_KeyGenerator.Init2(key: keyVKF0, OpenInitializationVector: OIV);

                regime_KG = 3;
                foreach (var part in OIV_parts)
                {
                    regime_KG++;
                    VinKekFish_KeyGenerator.input.add(part);
                    while (VinKekFish_KeyGenerator.input.Count > 0)
                        VinKekFish_KeyGenerator.doStepAndIO(regime: (byte) regime_KG);
                }

                if (!noPwd)
                    new PasswordEnter(Cascade_KeyGenerator!, VinKekFish_KeyGenerator!, regime: 1, doErrorMessage: true);
            }
            finally
            {
                vkf.sponge = null;
                csc.sponge = null;

                TryToDispose(main);
                TryToDispose(Cascade_KeyGenerator);
                TryToDispose(VinKekFish_KeyGenerator);
                TryToDispose(OIV);

                TryToDispose(keyVKF0);
                TryToDispose(keyCSC0);

                foreach (var part in OIV_parts)
                    TryToDispose(part);
            }
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

        protected void addStartPart(GetDataByAdd main, FileParts file, Record OIV)
        {
                  var asciiRegimeName = new ASCIIEncoding().GetBytes(RegimeName);
            using var obfRegimeName   = main.getBytes(asciiRegimeName.Length, regime: 23);
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
