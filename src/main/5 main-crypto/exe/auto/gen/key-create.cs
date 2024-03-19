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

            var keyVKF1 = main.getBytes(newKeyLen, regime: 11);
            var keyCSC1 = main.getBytes(newKeyLen, regime: 13);
            var keyVKF2 = main.getBytes(newKeyLen, regime: 11);
            var keyCSC2 = main.getBytes(newKeyLen, regime: 13);

            // Отбиваем основные ключи от информации, которую будем генерировать далее
            // На всякий случай проводим полную отбивку, потому что синхропосылки доступны злоумышленнику
            Cascade_Key.InitThreeFishByCascade(stepToKeyConst: 2);

            var OIV = main.getBytes(newKeyLen, regime: 17);

            List<Record> OIV_parts = new List<Record>(this.outParts.Count);

            VinKekFishBase_KN_20210525? VinKekFish_Cipher = null;
            CascadeSponge_mt_20230930?  Cascade_Cipher    = null;

            try
            {
                // Генерация отдельных частей синхропосылки
                GenerateAndWriteOivParts(main, OIV_parts);

                addStartPart(main, file, OIV);

                Cascade_Cipher = new CascadeSponge_mt_20230930(Cascade_CipherOpts.StrengthInBytes);
                VinKekFish_Cipher = new VinKekFishBase_KN_20210525(VinKekFish_CipherOpts.Rounds, K: VinKekFish_CipherOpts.K, ThreadCount: 1);
                VinKekFish_Cipher.Init1(VinKekFish_CipherOpts.PreRounds, prngToInit: Cascade_Cipher);
                VinKekFish_Cipher.Init2(key: null);

                VinKekFish_Cipher.input = new BytesBuilderStatic(Cascade_Cipher.maxDataLen);

                if (!noPwd)
                    new PasswordEnter(Cascade_Cipher!, VinKekFish_Cipher!, regime: 1, doErrorMessage: true);
            }
            finally
            {
                TryToDispose(Cascade_Cipher);
                TryToDispose(VinKekFish_Cipher);

                foreach (var part in OIV_parts)
                    TryToDispose(part);
            }
        }

        /// <summary>Если у ключевого файла есть части, то этот метод генерирует эти части и записывает их в файлы.</summary>
        /// <param name="main">Описатель суммарной губки, которая используется для генерации синхропосылки.</param>
        /// <param name="OIV_parts"></param>
        protected void GenerateAndWriteOivParts(GetDataByAdd main, List<Record> OIV_parts)
        {
            var oiv_part_len = AlignUtils.Align(newKeyLen, 2, 65536);
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
            var obfRegimeName   = main.getBytes(asciiRegimeName.Length, regime: 23);
            var recRegimeName   = Record.getRecordFromBytesArray(asciiRegimeName);
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
