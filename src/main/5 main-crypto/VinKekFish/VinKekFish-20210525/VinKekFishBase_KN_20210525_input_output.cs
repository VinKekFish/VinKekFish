// TODO: tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using cryptoprime;
using cryptoprime.VinKekFish;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        /// <summary>Сюда выводятся данные, полученные в ходе выполнения функции doStepAndOutput. Инициализируется пользователем, очищается либо пользователем (перезаписать null), либо в Displose автоматически. Если ёмкости не хватает, новые данные перезаписывают старые</summary>
        public    BytesBuilderStatic? output      = null;    /// <summary>Отсюда вводятся в криптографическое состояние данные, полученные в ходе выполнения функции doStepAndOutput. Инициализируется пользователем, очищается либо пользователем (перезаписать null), либо в Displose автоматически</summary>
        public    BytesBuilderStatic? input       = null;
        protected Record?             inputRecord = null;

        public void doStepAndIO(int countOfRounds = -1, int outputLen = -1, bool Overwrite = false, byte regime = 0, bool nullPadding = true)
        {
            if (!isInit1 || !isInit2)
                throw new Exception("VinKekFishBase_KN_20210525.step: you must call Init1 and Init2 before doing this");

            if (outputLen < 0)
                outputLen = BLOCK_SIZE_K;
            else
            if (outputLen > BLOCK_SIZE_K)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.doStep: outputLen > BLOCK_SIZE_K");

            if (input != null)
            lock (input)
            {
                if (inputRecord == null)
                    inputRecord = allocator.AllocMemory(BLOCK_SIZE_K);

                int inputLen = input.Count > BLOCK_SIZE_K ? BLOCK_SIZE_K : (int) input.Count;
                input.getBytesAndRemoveIt(inputRecord, inputLen);

                if (Overwrite)
                {
                    InputData_Overwrite(inputRecord, inputLen, regime: regime, nullPadding: nullPadding);
                }
                else
                {
                    InputData_Xor(inputRecord, inputLen, regime: regime);
                }
            }

            step(countOfRounds: countOfRounds);

            if (output != null)
            lock (output)
            {
                isHaveOutputData = false;
                // Если новые данные уже не смогут поместиться в буфер
                if (output.Count + outputLen > output.size)
                {
                    // var freePlace = output.size - output.Count;
                    // output.RemoveBytes(outputLen - freePlace);
                    throw new ArgumentOutOfRangeException("outputLen", "VinKekFishBase_KN_20210525.doStepAndIO: output.Count + outputLen > output.size");
                }
                output.add(this.st1, outputLen);
            }
        }
    }
}
