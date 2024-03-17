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
    /// <summary>Представляет некоторую абстрактную губку, из которой можно получать данные</summary>
    public interface GetDataFromSponge: IDisposable
    {
        public void   getBytes(byte * forData, nint len);
        public void   getBytes(Record r);
        public Record getBytes(nint len);

        /// <summary>Общее исключение для данного интерфейса</summary>
        public class GetDataFromSpongeException: Exception
        {
            public GetDataFromSpongeException(string message, Exception? inner = null): base(message, inner)
            {
            }
        }
    }

    /// <summary>Представляет некоторую абстрактную губку, из которой можно получать данные</summary>
    public abstract class GetDataFromSpongeClass: GetDataFromSponge
    {
        public virtual string NameForRecord {get; protected set;} = "GetDataFromSpongeClass.getBytes";

        /// <summary>Число показывает, сколько байтов берётся за один шаг из губки</summary>
        public nint blockLen = -1;

        /// <summary>Получить байты из губки в предварительно сформированную запись</summary>
        /// <param name="r">Запись, в которую будет записан результат. Результат получается длиной на всю запись.</param>
        public virtual void getBytes(Record r)
        {
            getBytes(r, r.len);
        }

        /// <summary>Получить байты из губки.</summary>
        /// <param name="len">Количество байтов для получения.</param>
        /// <returns>Запись, которая содержит результат (необходимо удалить через Dispose после использования).</returns>
        public virtual Record getBytes(nint len)
        {
            var r = Keccak_abstract.allocator.AllocMemory(len, RecordName: NameForRecord);

            getBytes(r);
            return r;
        }

        /// <summary>Получить байты из губки.</summary>
        /// <param name="forData">Адрес массива для вывода результата.</param>
        /// <param name="len">Длина запрашиваемого результата.</param>
        public abstract void getBytes(byte* forData, nint len);

        void IDisposable.Dispose()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            // TODO: реализовать деструктор и т.п.
        }
    }

    /// <summary>Класс, абстрагирующий класс каскадной губки CascadeSponge_mt_20230930</summary>
    public class GetDataFromCascadeSponge: GetDataFromSpongeClass
    {
        public override string NameForRecord {get; protected set;} = "GetDataFromCascadeSponge.getBytes";

        public readonly CascadeSponge_mt_20230930 sponge;
        public GetDataFromCascadeSponge(CascadeSponge_mt_20230930 sponge)
        {
            this.sponge = sponge;
            blockLen    = sponge.lastOutput.len >> 1;
        }

        public override void getBytes(byte* forData, nint len)
        {
            // TODO: !!!
        }
    }

    /// <summary>Класс, абстрагирующий класс губки VinKekFish: VinKekFishBase_KN_20210525</summary>
    public class GetDataFromVinKekFishSponge: GetDataFromSpongeClass
    {
        public override string NameForRecord {get; protected set;} = "GetDataFromVinKekFishSponge.getBytes";

        public readonly VinKekFishBase_KN_20210525 sponge;
        public GetDataFromVinKekFishSponge(VinKekFishBase_KN_20210525 sponge)
        {
            this.sponge = sponge;
            blockLen    = sponge.BLOCK_SIZE_KEY_K;
        }

        public override void getBytes(byte* forData, nint len)
        {
            if (sponge.output is null)
            {
                if (blockLen <= 0)
                    throw new ArgumentOutOfRangeException("GetDataFromVinKekFishSponge.getBytes: blockLen <= 0");

                sponge.output = new BytesBuilderStatic(blockLen);
            }

            sponge.output!.Clear();
            var reqLen  = len;
            var current = forData;

            do
            {
                sponge.doStepAndIO(outputLen: sponge.BLOCK_SIZE_KEY_K, regime: 1);

                var reqLenCurrent = Math.Min(reqLen, sponge.BLOCK_SIZE_KEY_K);
                sponge.output.getBytesAndRemoveIt(current, reqLenCurrent);

                reqLen  -= reqLenCurrent;
                current += reqLenCurrent;
            }
            while (reqLen > 0);
        }
    }
}
