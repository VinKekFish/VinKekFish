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
        /// <summary>Сюда выводятся данные, полученные в ходе выполнения функции doStepAndOutput. Инициализируется пользователем, очищается либо пользователем (вызвать Dispose и перезаписать в null указатель), либо в Displose автоматически. Если ёмкости не хватает, новые данные перезаписывают старые.</summary>
        public    BytesBuilderStatic? output      = null;    /// <summary>Отсюда данные вводятся в криптографическое состояние. Данные используются в ходе выполнения функции doStepAndOutput. Инициализируется пользователем, очищается либо пользователем (вызвать Dispose и перезаписать в null указатель), либо в Displose автоматически</summary>
        public    BytesBuilderStatic? input       = null;
        protected Record?             inputRecord = null;

        /// <summary>Выполняет ввод и шаг VinKekFish и вывод результата. Данные для ввода в шаги берутся из переменной input. while (VinKekFish.input!.Count > 0) doStepAndIO();</summary>
        /// <param name="countOfRounds">Количество раундов шифрования, не менее MIN_ROUNDS_K. -1 - взять максимальное количество раундов, указанное при конструировании объекта.</param>
        /// <param name="outputLen">Количество байтов, которое нужно получить. Не более BLOCK_SIZE_K</param>
        /// <param name="Overwrite">Если true - режим overwrite. Если false - режим xor</param>
        /// <param name="regime">Номер режима работы схемы шифрования</param>
        /// <param name="nullPadding">Если true - включён режим nullPadding (при overwrite будет перезаписано BLOCK_SIZE_K байтов вне зависимости от длины ввода)</param>
        /// <exception cref="Exception">Неверное состояние губки или другое</exception>
        /// <exception cref="ArgumentOutOfRangeException">Неверные аргументы</exception>
        public void doStepAndIO(int countOfRounds = -1, int outputLen = -1, bool Overwrite = false, byte regime = 0, bool nullPadding = true)
        {
            if (!isInit1 || !isInit2)
                throw new Exception("VinKekFishBase_KN_20210525.step: you must call Init1 and Init2 before doing this");

            if (countOfRounds == -1)
                countOfRounds = this.CountOfRounds;

            if (outputLen < 0)
                outputLen = BLOCK_SIZE_K;
            else
            if (outputLen > BLOCK_SIZE_K)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.doStepAndIO: outputLen > BLOCK_SIZE_K");
            if (countOfRounds < MIN_ROUNDS_K)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.doStepAndIO: countOfRounds < MIN_ROUNDS_K");

            if (input != null && input.Count > 0)
            {
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
            }
            else
                if (Overwrite)
                    InputData_Overwrite(null, 0, regime, nullPadding);
                else
                    NoInputData_ChangeTweakAndState(regime);

            step(askedCountOfRounds: countOfRounds);

            if (output != null && outputLen > 0)
            lock (output)
            {
                if (!isHaveOutputData)
                    throw new Exception("VinKekFishBase_KN_20210525.doStepAndIO: !isHaveOutputData");

                isHaveOutputData = false;
                // Если новые данные уже не смогут поместиться в буфер
                if (output.Count + outputLen > output.size)
                {
                    var freePlace = output.size - output.Count;
                    output.RemoveBytes(outputLen - freePlace);
                    // throw new ArgumentOutOfRangeException("outputLen", "VinKekFishBase_KN_20210525.doStepAndIO: output.Count + outputLen > output.size");
                }
                output.add(this.st1, outputLen);
            }
        }

        /// <summary>Если вместо функции doStepAndIO используется step, то необходимо получить результат с помощью этой функции. Либо если this.output равен нулю и хочется получить данные напрямую</summary>
        /// <param name="forData">Массив для получения результата</param>
        /// <param name="outputLen">Желаемая длина результата, не более BLOCK_SIZE_K. 0 не позволяется</param>
        public void doOutput(byte * forData, nint outputLen)
        {
            if (!isHaveOutputData)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.doOutput: !isHaveOutputData");
            if (outputLen <= 0)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.doOutput: outputLen <= 0");
            if (outputLen > BLOCK_SIZE_K)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525.doOutput: outputLen > BLOCK_SIZE_K");

            isHaveOutputData = false;
            BytesBuilder.CopyTo(Len, outputLen, st1, forData);
        }
    }
}
