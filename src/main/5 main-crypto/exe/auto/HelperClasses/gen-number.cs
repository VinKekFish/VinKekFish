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
using static VinKekFish_EXE.AutoCrypt.GetDataFromSponge;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

// В этом файле объявлены KeyDataGenerator и GetDataByAdd (сумма губок)
public unsafe partial class AutoCrypt
{
    public class CryptoKeyPair: IDisposable
    {
        public Record? csc {get; protected set;} = null;
        public Record? vkf {get; protected set;} = null;

        public CryptoKeyPair(KeyDataGenerator generator, nint keyLenCsc, nint keyLenVkf, (byte, byte) regime)
        {
            csc = generator.getBytes(keyLenCsc, regime: regime.Item1);
            vkf = generator.getBytes(keyLenVkf, regime: regime.Item2);
        }

        public void Dispose()
        {
            TryToDispose(vkf);
            TryToDispose(csc);

            vkf = null;
            csc = null;
        }
    }

    /// <summary>Используется для генерации ключей шифрования</summary>
    public class KeyDataGenerator: IDisposable
    {
        public GetDataByAdd                main;
        public GetDataFromVinKekFishSponge vkf;
        public GetDataFromCascadeSponge    csc;
        /// <summary>Сгенерированные в конструкторе ключи шифрования. data_keyCSC - ключ для использования в каскадной губке, data_keyVKF - ключ для использования в губке VinKekFish. Эти ключи генерируются для пользователя, здесь они не используются.</summary>
        public List<CryptoKeyPair> keys = new List<CryptoKeyPair>();
                                                                        /// <summary>Длина генерируемого ключа шифрования для дальнейшего использованя в каскадной губке</summary>
        public required nint keyLenCsc  {get; init;}                    /// <summary>Длина генерируемого ключа шифрования для дальнейшего использованя в губке VinKekFish</summary>
        public required nint keyLenVkf  {get; init;}

        /// <summary>Создаёт объект и сразу же генерирует два ключа шифрования из переданных губок</summary>
        /// <param name="vkf_keyGenerator">Проинициализированная губка VinKekFish, готовая к генерации ключа шифрования (режимы 13 и 15)</param>
        /// <param name="csc_keyGenerator">Проинициализированная каскадная губка, готовая к генерации ключа шифрования (режимы 13 и 15)</param>
        /// <param name="ArmoringSteps">Количество дополнительных холостых шагов, усиливающих шифрование</param>
        /// <param name="NameForRecord">Отладочное имя для выделения памяти</param>
        public KeyDataGenerator(VinKekFishBase_KN_20210525 vkf_keyGenerator, CascadeSponge_mt_20230930 csc_keyGenerator, int ArmoringSteps, string NameForRecord)
        {
            GC.ReRegisterForFinalize(this);

            main     = new GetDataByAdd();
            vkf      = new GetDataFromVinKekFishSponge(vkf_keyGenerator);
            csc      = new GetDataFromCascadeSponge   (csc_keyGenerator);

            vkf.blockLen = vkf_keyGenerator.BLOCK_SIZE_KEY_K;        // Ключевой режим генерации: малые блоки генерации
            csc.blockLen = csc_keyGenerator.lastOutput.len >> 1;

            csc.ArmoringSteps = ArmoringSteps;

            main.AddSponge(vkf);
            main.AddSponge(csc);

            main.NameForRecord = NameForRecord;
        }

        /// <summary>Сгенерировать пару ключей шифрования и записать и в data_key</summary>
        /// <param name="count">Количество пар ключей, которое нужно сгенерировать.</param>
        public void Generate(nint count = 1)
        {
            for (nint i = 0; i < count; i++)
            {
                var keyPair = new CryptoKeyPair(this, keyLenCsc, keyLenVkf, (13, 15));
                keys.Add(keyPair);
            }
        }

        /// <summary>Получает псевдослучайные криптостойкие байты (например, ключи или синхропосылки). Полностью аналогично GetDataByAdd.getBytes.</summary>
        /// <param name="len">Длина получаемых данных.</param>
        /// <param name="regime">Числовой режим генерации (вводится в губки)</param>
        /// <returns></returns>
        public Record getBytes(nint len, byte regime)
        {
            return main.getBytes(len: len, regime: regime);
        }

        /// <summary>Делает необратимое преобразование в обеих губках ("отбивает" предыдущие состояния от будущих). (InitThreeFishByCascade и doStepAndIO с Overwrite: true).</summary>
        public void doChopRegime()
        {
            csc.sponge!.InitThreeFishByCascade();
            vkf.sponge!.doStepAndIO(Overwrite: true, regime: 255, nullPadding: true);
        }
                                                                                                    /// <summary>true, если объект прошёл через Dispose</summary>
        public bool isDisposed {get; protected set;} = false;                                       /// <summary>Если true, до первичные губки, переданные в класс, будут уничтожены при уничтожении объекта. Если false, то эти губки должны быть уничтожены вручную программистом позже уничтожения этого объекта.</summary>
        public required bool willDisposeSponges = true;
        public void Dispose()
        {
            if (isDisposed)
            {
                Record.errorsInDispose = true;
                Console.Error.WriteLine("AutoCrypt.GenKeyCommand.DataGenerator.Dispose: isDisposed (Dispose executed twiced)");
                return;
            }

            try
            {
                if (!willDisposeSponges)
                {
                    vkf.sponge = null;
                    csc.sponge = null;
                }

                foreach (var key in keys)
                {
                    TryToDispose(key);
                }
            }
            catch (Exception e)
            {
                formatException(e);
            }

            TryToDispose(main);
            isDisposed = true;
        }

        ~KeyDataGenerator()
        {
            if (!isDisposed)
            {
                Record.errorsInDispose = true;
                Console.Error.WriteLine("AutoCrypt.GenKeyCommand.~DataGenerator: !isDisposed");

                Dispose();
            }
        }
    }


    /// <summary>Представляет сумму губок. Результат генерируется как арифметическая сумма результатов губок.</summary>
    public class GetDataByAdd: GetDataFromSpongeClass
    {
        protected readonly List<GetDataFromSponge> list = new List<GetDataFromSponge>(2);
        public GetDataByAdd()
        {
            NameForRecord = "GetDataByAdd.getBytes";
        }

        public void AddSponge(GetDataFromSponge sponge)
        {
            lock (list)
                list.Add(sponge);
        }

        public override void getBytes(byte* forData, nint len, byte regime)
        {
            if (list.Count <= 0)
                throw new GetDataFromSpongeException("GetDataByAdd.getBytes: list.Count <= 0");

            if (list.Count == 1)
            {
                list[0].getBytes(forData, len, regime);
                return;
            }

            BytesBuilder.ToNull(len, forData);

            Parallel.For
            (
                0, list.Count,
                (int i) =>
                {
                    var sub = Keccak_abstract.allocator.AllocMemory(len, "GetDataByAdd.getBytes." + NameForRecord + "." + i);
                    try
                    {
                        list[i].getBytes(sub, regime);

                        lock (this)
                        BytesBuilder.ArithmeticAddBytes(len, forData, sub);
                    }
                    finally
                    {
                        sub.Dispose();
                    }
                }
            );
        }
/*
        protected void CreateKeyFiles(ref int status, int countOfTasks)
        {
            // Что мне надо сделать?
            // Создать абстрактный генератор данных для того, чтобы можно было с ним работать без особенностей губок
            // Сделать функцию ввода пароля
            // Сгенерировать синхропосылки и распределить их по частям файла, если нужно: для этого мне надо создать функцию или вспомогательный класс для генерации данных с помощью сложения из двух функций
            // Выделить место в оперативной памяти для шифрования
            // Рассчитать с помощью иерархических классов потребное место
            throw new NotImplementedException();
        }
*/
        public void ClearList(bool doDispose = true)
        {
            if (doDispose)
                DisposeSponge();
            else
                list.Clear();
        }

        protected override void DisposeSponge()
        {
            foreach (var sponge in list)
            {
                TryToDispose(sponge);
            }

            list.Clear();
        }

        public override nint blockLen { get => throw new InvalidOperationException(NameForRecord); set => throw new InvalidOperationException(NameForRecord); }
        protected override void doCorrectBlockLen()
        {
            throw new InvalidOperationException(NameForRecord);
        }
    }
}
