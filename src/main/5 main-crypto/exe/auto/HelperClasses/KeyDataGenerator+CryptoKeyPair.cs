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
using static VinKekFish_EXE.AutoCrypt.IGetDataFromSponge;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

// В этом файле объявлены KeyDataGenerator и GetDataByAdd (сумма губок)
public unsafe partial class AutoCrypt
{
    public class CryptoKeyPair: IDisposable
    {
                                                                /// <summary>Ключ для каскадной губки.</summary>
        public Record? CSC {get; protected set;} = null;        /// <summary>Ключ для губки VinKekFisg.</summary>
        public Record? VKF {get; protected set;} = null;

        /// <summary>Создаёт описатель пары ключей для дальнейшего их использования в генераторах.</summary>
        /// <param name="generator">Уже проинициализированный пользователем генератор, который будет использован для генерации пары ключей.</param>
        /// <param name="keyLenCsc">Длина ключа для каскадной губки.</param>
        /// <param name="keyLenVkf">Длина ключа для губки VinKekFish.</param>
        /// <param name="regime">Режимы, которые будут использованы для генерации.</param>
        public CryptoKeyPair(KeyDataGenerator generator, nint keyLenCsc, nint keyLenVkf, (byte csc, byte vkf) regime)
        {
            CSC = generator.GetBytes(keyLenCsc, regime: regime.csc);
            VKF = generator.GetBytes(keyLenVkf, regime: regime.vkf);
        }

        /// <summary>Получает оба ключа, представленные в описателе файла. Сначала идёт секция "csc" (каскадный ключ), затем "vkf" (ключ VinKekFish).</summary>
        public FileParts GetFilePartsForPair()
        {
            var file = new FileParts(Name: "CryptoKeyPair.getRecordForPair", doNotDispose: true);
            file.AddFilePart("csc", CSC!);
            file.AddFilePart("vkf", VKF!);

            return file;
        }

        /// <summary>Получает оба ключа один за другим. Каждый ключ предваряется его длиной. Сначала идёт каскадный ключ, затем ключ VinKekFish.</summary>
        public Record GetRecordForPair()
        {
            using var file = GetFilePartsForPair();

            return file.WriteToRecord();
        }

        public void Dispose()
        {
            TryToDispose(VKF);
            TryToDispose(CSC);

            VKF = null;
            CSC = null;

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Используется для генерации ключей шифрования</summary>
    public class KeyDataGenerator: IDisposable
    {
        public GetDataByAdd                main;
        public GetDataFromVinKekFishSponge vkf;
        public GetDataFromCascadeSponge    csc;
        /// <summary>Сгенерированные в конструкторе ключи шифрования. data_keyCSC - ключ для использования в каскадной губке, data_keyVKF - ключ для использования в губке VinKekFish. Эти ключи генерируются для пользователя, здесь они не используются.</summary>
        public List<CryptoKeyPair> keys = new();
                                                                        /// <summary>Длина генерируемого ключа шифрования для дальнейшего использованя в каскадной губке</summary>
        public required nint KeyLenCsc  {get; init;}                    /// <summary>Длина генерируемого ключа шифрования для дальнейшего использованя в губке VinKekFish</summary>
        public required nint KeyLenVkf  {get; init;}

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

            vkf.BlockLen = vkf_keyGenerator.BLOCK_SIZE_KEY_K;        // Ключевой режим генерации: малые блоки генерации
            csc.BlockLen = csc_keyGenerator.lastOutput.len >> 1;

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
                var keyPair = new CryptoKeyPair(this, KeyLenCsc, KeyLenVkf, (13, 15));
                keys.Add(keyPair);
            }
        }

        /// <summary>Получает псевдослучайные криптостойкие байты (например, ключи или синхропосылки). Полностью аналогично GetDataByAdd.getBytes.</summary>
        /// <param name="len">Длина получаемых данных.</param>
        /// <param name="regime">Числовой режим генерации (вводится в губки)</param>
        /// <returns></returns>
        public Record GetBytes(nint len, byte regime)
        {
            return main.GetBytes(len: len, regime: regime);
        }

        /// <summary>Делает необратимое преобразование в обеих губках ("отбивает" предыдущие состояния от будущих). (InitThreeFishByCascade и doStepAndIO с Overwrite: true).</summary>
        public void DoChopRegime()
        {
            csc.sponge!.InitThreeFishByCascade();
            vkf.sponge!.DoStepAndIO(Overwrite: true, regime: 255, nullPadding: true);
        }
                                                                                                    /// <summary>true, если объект прошёл через Dispose</summary>
        public bool IsDisposed {get; protected set;} = false;                                       /// <summary>Если true, до первичные губки, переданные в класс, будут уничтожены при уничтожении объекта. Если false, то эти губки должны быть уничтожены вручную программистом позже уничтожения этого объекта.</summary>
        public required bool willDisposeSponges = true;
        public void Dispose()
        {
            if (IsDisposed)
            {
                Record.ErrorsInDispose = true;
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
                FormatException(e);
            }
Console.WriteLine("!!(((((((((((((((((((((((((((!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            TryToDispose(main);
            IsDisposed = true;

            GC.SuppressFinalize(this);
        }

        ~KeyDataGenerator()
        {
            if (!IsDisposed)
            {
                Record.ErrorsInDispose = true;
                Console.Error.WriteLine("AutoCrypt.GenKeyCommand.~DataGenerator: !isDisposed");

                Dispose();
            }
        }
    }


    /// <summary>Представляет сумму губок. Результат генерируется как арифметическая сумма результатов губок.</summary>
    public class GetDataByAdd: GetDataFromSpongeClass
    {
        protected readonly List<IGetDataFromSponge> list = new(2);
        public GetDataByAdd()
        {
            NameForRecord = "GetDataByAdd.getBytes";
        }

        public void AddSponge(IGetDataFromSponge sponge)
        {
            lock (list)
                list.Add(sponge);
        }

        public override void GetBytes(byte* forData, nint len, byte regime)
        {
            if (list.Count <= 0)
                throw new GetDataFromSpongeException("GetDataByAdd.getBytes: list.Count <= 0");

            if (list.Count == 1)
            {
                list[0].GetBytes(forData, len, regime);
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
                        list[i].GetBytes(sub, regime);

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

        public override nint BlockLen { get => throw new InvalidOperationException(NameForRecord); set => throw new InvalidOperationException(NameForRecord); }
        protected override void DoCorrectBlockLen()
        {
            throw new InvalidOperationException(NameForRecord);
        }
    }
}
