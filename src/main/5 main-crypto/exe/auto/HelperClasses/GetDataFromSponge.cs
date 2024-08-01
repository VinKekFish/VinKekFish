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
    /// <summary>Представляет некоторую абстрактную губку, из которой можно получать данные. Не препятствует тому, чтобы работать также и напрямую с губкой.</summary>
    public interface IGetDataFromSponge: IDisposable
    {
        public void   GetBytes(byte * forData, nint len, byte regime);
        public void   GetBytes(Record r, byte regime);
        public Record GetBytes(nint len, byte regime);

        /// <summary>Общее исключение для данного интерфейса</summary>
        public class GetDataFromSpongeException: Exception
        {
            public GetDataFromSpongeException(string message, Exception? inner = null): base(message, inner)
            {
            }
        }
    }

    /// <summary>Представляет некоторую абстрактную губку, из которой можно получать данные. Не препятствует тому, чтобы работать также и напрямую с губкой.</summary>
    public abstract class GetDataFromSpongeClass: IGetDataFromSponge
    {
        public virtual string NameForRecord {get; set;} = "GetDataFromSpongeClass.getBytes";

        public GetDataFromSpongeClass()
        {
            GC.ReRegisterForFinalize(this);
        }

        protected nint _blockLen = -1;
        protected abstract void DoCorrectBlockLen();
        /// <summary>Число показывает, сколько байтов берётся за один шаг из губки</summary>
        public virtual nint BlockLen
        {
            get
            {
                DoCorrectBlockLen();
                return _blockLen;
            }
            set
            {
                _blockLen = value;
                DoCorrectBlockLen();
            }
        }

        /// <summary>Получить байты из губки в предварительно сформированную запись</summary>
        /// <param name="r">Запись, в которую будет записан результат. Результат получается длиной на всю запись.</param>
        public virtual void GetBytes(Record r, byte regime)
        {
            GetBytes(r, r.len, regime);
        }

        /// <summary>Получить байты из губки.</summary>
        /// <param name="len">Количество байтов для получения.</param>
        /// <returns>Запись, которая содержит результат (необходимо удалить через Dispose после использования).</returns>
        public virtual Record GetBytes(nint len, byte regime)
        {
            var r = Keccak_abstract.allocator.AllocMemory(len, RecordName: NameForRecord + ".getBytes");

            GetBytes(r, regime);
            return r;
        }

        /// <summary>Получить байты из губки.</summary>
        /// <param name="forData">Адрес массива для вывода результата.</param>
        /// <param name="len">Длина запрашиваемого результата.</param>
        public abstract void GetBytes(byte* forData, nint len, byte regime);

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        protected bool isDisposed = false;
        public virtual void Dispose(bool fromDestructor = false)
        {
            var id = isDisposed;
            if (!isDisposed)
            {
                DisposeSponge();
                isDisposed = true;
            }

            if (!id)
            if (fromDestructor)
            {
                var msg = $"Destructor for {NameForRecord} executed with a not disposed state (GetDataFromSpongeClass).";
                if (BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor)
                    throw new Exception(msg);
                else
                    Console.Error.WriteLine(msg);
            }
        }

        protected abstract void DisposeSponge();

        ~GetDataFromSpongeClass()
        {
            Dispose(true);
        }
    }

    /// <summary>Класс, абстрагирующий класс каскадной губки CascadeSponge_mt_20230930. Не препятствует тому, чтобы работать также и напрямую с губкой.</summary>
    public class GetDataFromCascadeSponge: GetDataFromSpongeClass
    {
        public override string NameForRecord {get; set;} = "GetDataFromCascadeSponge.getBytes";

        public CascadeSponge_mt_20230930? sponge;
        public GetDataFromCascadeSponge(CascadeSponge_mt_20230930 sponge)
        {
            this.sponge = sponge;
            BlockLen    = sponge.lastOutput.len >> 1;
        }

        public int ArmoringSteps = 0;
        public override void GetBytes(byte* forData, nint len, byte regime)
        {
            var reqLen  = len;
            var current = forData;
            do
            {
                sponge!.Step(ArmoringSteps: ArmoringSteps, regime: regime);
                BytesBuilder.CopyTo(BlockLen, reqLen, sponge.lastOutput, current);

                reqLen  -= BlockLen;
                current += BlockLen;
            }
            while (reqLen > 0);
        }

        protected override void DoCorrectBlockLen()
        {
            if (sponge is null)
                return;

            if (_blockLen > sponge.lastOutput.len)
                _blockLen = sponge.lastOutput.len;
            if (_blockLen < 1)
                _blockLen = sponge.lastOutput.len >> 1;
        }

        protected override void DisposeSponge()
        {
            TryToDispose(sponge);
        }
    }

    /// <summary>Класс, абстрагирующий класс губки VinKekFish: VinKekFishBase_KN_20210525. Не препятствует тому, чтобы работать также и напрямую с губкой.</summary>
    public class GetDataFromVinKekFishSponge: GetDataFromSpongeClass
    {
        public override string NameForRecord {get; set;} = "GetDataFromVinKekFishSponge.getBytes";

        public VinKekFishBase_KN_20210525? sponge;
        public GetDataFromVinKekFishSponge(VinKekFishBase_KN_20210525 sponge)
        {
            this.sponge = sponge;
            BlockLen    = sponge.BLOCK_SIZE_KEY_K;
        }

        public override void GetBytes(byte* forData, nint len, byte regime)
        {
            if (sponge!.output is null)
            {
                if (BlockLen <= 0)
                    throw new ArgumentOutOfRangeException("GetDataFromVinKekFishSponge.getBytes: blockLen <= 0");

                sponge.output = new BytesBuilderStatic(BlockLen);
            }

            sponge.output!.Clear();
            var reqLen  = len;
            var current = forData;

            do
            {
                sponge.DoStepAndIO(outputLen: (int) BlockLen, regime: 1);

                var reqLenCurrent = Math.Min(reqLen, BlockLen);
                sponge.output.GetBytesAndRemoveIt(current, reqLenCurrent);

                reqLen  -= reqLenCurrent;
                current += reqLenCurrent;
            }
            while (reqLen > 0);
        }

        protected override void DoCorrectBlockLen()
        {
            if (sponge is null)
                return;

            if (_blockLen > sponge.BLOCK_SIZE_K)
                _blockLen = sponge.BLOCK_SIZE_K;
            if (_blockLen < 1)
                _blockLen = sponge.BLOCK_SIZE_KEY_K;
        }

        protected override void DisposeSponge()
        {
            TryToDispose(sponge);
        }
    }
}
