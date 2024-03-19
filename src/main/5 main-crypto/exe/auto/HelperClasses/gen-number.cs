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

public unsafe partial class AutoCrypt
{
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
