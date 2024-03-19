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

            VinKekFishBase_KN_20210525? VinKekFish_Cipher = null;
            CascadeSponge_mt_20230930?  Cascade_Cipher    = null;

            try
            {
                Cascade_Cipher    = new CascadeSponge_mt_20230930(Cascade_CipherOpts.StrengthInBytes);
                VinKekFish_Cipher = new VinKekFishBase_KN_20210525(VinKekFish_CipherOpts.Rounds, K: VinKekFish_CipherOpts.K, ThreadCount: 1);
                VinKekFish_Cipher.Init1(VinKekFish_CipherOpts.PreRounds, prngToInit: Cascade_Cipher);
                VinKekFish_Cipher.Init2(key: null);

                VinKekFish_Cipher.input = new BytesBuilderStatic(Cascade_Cipher.maxDataLen);

                new PasswordEnter(Cascade_Cipher!, VinKekFish_Cipher!, regime: 1, doErrorMessage: true);
            }
            finally
            {
                TryToDispose(Cascade_Cipher);
                TryToDispose(VinKekFish_Cipher);
            }
        }
    }
}
