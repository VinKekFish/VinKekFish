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
        public void   GetBytes(byte * forData, nint len, byte regime, bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null);
        public void   GetBytes(Record r, byte regime, bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null);
        public Record GetBytes(nint len, byte regime, string RecordNameSuffix = "", bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null);

        /// <summary>Общее исключение для данного интерфейса</summary>
        public class GetDataFromSpongeException: Exception
        {
            public GetDataFromSpongeException(string message, Exception? inner = null): base(message, inner)
            {
            }
        }
    }

    /// <summary>Представляет некоторую абстрактную губку, из которой можно получать данные. Не препятствует тому, чтобы работать также и напрямую с губкой. Обязательно переопределить GetBytes(byte* forData, nint len, byte regime). При этом, необходимо перед переопределением вызвать базовую функцию.</summary>
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

        protected byte lasRegime = 0;
        protected bool firstCall = true;
        /// <summary>Получить байты из губки в предварительно сформированную запись</summary>
        /// <param name="r">Запись, в которую будет записан результат. Результат получается длиной на всю запись.</param>
        /// <param name="regime">Логический режим шифрования. Не должен совпадать с предыдущим режимом.</param>
        /// <param name="doCheckLastRegime">Производить ли проверку совпадения режима с предыдущим или нет. true - производить (по умолчанию).</param>
        public virtual void GetBytes(Record r, byte regime, bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null)
        {
            GetBytes(r, r.len, regime, doCheckLastRegime, progress);
        }

        /// <summary>Получить байты из губки.</summary>
        /// <param name="len">Количество байтов для получения.</param>
        /// <param name="regime">Логический режим губок, в котором генерируются байты. Не используйте при генерации одинаковый режим два раза подряд: это позволит логически отделить разные данные друг от друга (противодействие атакам типа Padding Oracle и т.п.).</param>
        /// <param name="RecordNameSuffix">Суффикс, добавляемый к отладочному имени выделяемой записи.</param>
        /// <returns>Запись, которая содержит результат (необходимо удалить через Dispose после использования).</returns>
        public virtual Record GetBytes(nint len, byte regime, string RecordNameSuffix = "", bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null)
        {
            var r = Keccak_abstract.allocator.AllocMemory(len, RecordName: NameForRecord + ".getBytes" + RecordNameSuffix);

            GetBytes(r, regime, doCheckLastRegime, progress);
            return r;
        }

        /// <summary>Получить байты из губки.</summary>
        /// <param name="forData">Адрес массива для вывода результата.</param>
        /// <param name="len">Длина запрашиваемого результата.</param>
        /// <param name="regime">Длина запрашиваемого результата. Функция может, но не должна, проверять, что regime не одинаковый в вызовах поряд.</param>
        public abstract void GetBytes(byte* forData, nint len, byte regime, bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null);

        /// <summary>Функция проверяет, что последний режим не равен текущему и устанавливает последний режим в текущий.</summary>
        /// <param name="regime">Текущий режим</param>
        public void ExceptionIfLastRegimeIsEqual(byte regime)
        {
            if (!firstCall)
            if (lasRegime == regime)
                throw new ArgumentOutOfRangeException(nameof(regime), "regime must have != lastRegime: " + $"lasRegime = {lasRegime} == {regime} = regime");

            lasRegime = regime;
            firstCall = false;
        }

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
        public GetDataFromCascadeSponge(CascadeSponge_mt_20230930 sponge, nint setBlockLen = 0, nint setArmoringSteps = 0)
        {
            this.sponge  = sponge;
            if (setBlockLen <= 0)
                BlockLen = sponge.lastOutput.len >> 1;
            else
                BlockLen = setBlockLen;

            if (setArmoringSteps > 0)
                ArmoringSteps = setArmoringSteps;
            else
                ArmoringSteps = 0;
        }

        public long ArmoringSteps = 0;
        public override void GetBytes(byte* forData, nint len, byte regime, bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null)
        {
            if (doCheckLastRegime)
            ExceptionIfLastRegimeIsEqual(regime);

            var  current   = forData;
            nint reqLen    = len;
            nint generated = 0;
            do
            {
                sponge!.Step(ArmoringSteps: (nint) ArmoringSteps, regime: regime);
                BytesBuilder.CopyTo(BlockLen, reqLen, sponge.lastOutput, current);

                reqLen    -= BlockLen;
                current   += BlockLen;
                generated += BlockLen;

                if (progress is not null)
                {
                    if (generated < progress.allSteps)
                        progress.processedSteps = generated;
                    else
                        progress.processedSteps = progress.allSteps;
                }
            }
            while (reqLen > 0);

            if (progress is not null)
            lock (progress)
            Monitor.PulseAll(progress);
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
        public GetDataFromVinKekFishSponge(VinKekFishBase_KN_20210525 sponge, nint setBlockLen = 0, nint setArmoringSteps = 0)
        {
            this.sponge  = sponge;
            if (setBlockLen <= 0)
                BlockLen = sponge.BLOCK_SIZE_KEY_K;
            else
                BlockLen = setBlockLen;

            if (setArmoringSteps > 0)
                ArmoringSteps = setArmoringSteps;
            else
                ArmoringSteps = sponge.CountOfRounds;
        }

        public nint ArmoringSteps = 0;
        public override void GetBytes(byte* forData, nint len, byte regime, bool doCheckLastRegime = true, CascadeSponge_1t_20230905.StepProgress? progress = null)
        {
            // Защита от того, что байты будут сгенерированы в одном и том же режиме два раза подряд
            if (doCheckLastRegime)
            ExceptionIfLastRegimeIsEqual(regime);

            if (sponge!.output is null)
            {
                if (BlockLen <= 0)
                    throw new ArgumentOutOfRangeException("GetDataFromVinKekFishSponge.getBytes: blockLen <= 0");

                sponge.output = new BytesBuilderStatic(BlockLen);
            }

            sponge.output!.Clear();
            var  current   = forData;
            nint reqLen    = len;
            nint generated = 0;
            do
            {
                sponge.DoStepAndIO(ArmoringSteps, outputLen: (int) BlockLen, regime: 1);

                var reqLenCurrent = Math.Min(reqLen, BlockLen);
                sponge.output.GetBytesAndRemoveIt(current, reqLenCurrent);

                reqLen    -= reqLenCurrent;
                current   += reqLenCurrent;
                generated += reqLenCurrent;

                if (progress is not null)
                {
                    progress.processedSteps = generated;
                }
            }
            while (reqLen > 0);

            if (progress is not null)
            lock (progress)
            Monitor.PulseAll(progress);
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
