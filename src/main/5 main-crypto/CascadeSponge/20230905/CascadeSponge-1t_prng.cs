// TODO: tests
namespace vinkekfish;

using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using System.Threading.Tasks;

// code::docs:rQN6ZzeeepyOpOnTPKAT:

public partial class CascadeSponge_1t_20230905
{
    // Ниже аналогичный код!
    public unsafe void DoRandomPermutationForUShorts(nint len, ushort* T, nint countOfSteps = 0, byte regime = 1)
    {
        ushort a, err = 0;
        nint   index;

        using var bb = new BytesBuilderStatic(this.maxDataLen*2);

        // Алгоритм тасования Дурштенфельда
        // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
        for (nint i = 0; i < len - 1; i++)
        {
            // var cutoff = getCutoffForUnsignedInteger(0, (ulong)len - i - 1);ulong
            // index = getUnsignedInteger(0, cutoff) + i;

            // Берём сразу 8 байтов, чтобы getUnsignedInteger потом не вылетало с лишними исключениями: так байтов почти всегда будет хватать
            if (bb.Count < 8)
            {
                Step(countOfSteps: countOfSteps, regime: regime);
                bb.Add(lastOutput);
                this.haveOutput = false;
            }

            // Исключение может случиться, если getUnsignedInteger отбросит слишком много значений
            try
            {
                index = (nint) GetUnsignedInteger((nuint) (len - i - 1), bb) + i;
                err = 0;
            }
            catch (NotEnoughtBytesException)
            {
                if (err > 64)
                    throw;

                err++;
                i--;
                continue;
            }

            a        = T[i];
            T[i]     = T[index];
            T[index] = a;
        }

        a = 0;
    }

    public class NotEnoughtBytesException: Exception
    {}

    /// <summary>Получает число от 0 до max включительно</summary>
    /// <param name="max">Максимальное число, которое ещё возможно получить</param>
    /// <param name="entropy">Байты, сгенерированные источником энтропии</param>
    /// <returns>Случайное число в диапазоне [0; max]</returns>
    public unsafe static nuint GetUnsignedInteger(nuint max, BytesBuilderStatic entropy)
    {
        if (max < 1)
            throw new ArgumentOutOfRangeException("CascadeSponge_1t_20230905.getUnsignedInteger: max < 1");

        nuint r   = 1;
        nint  cnt = 0;
        do
        {
            cnt++;
            r <<= 8;    // Сдвиг ещё на 8-мь битов
        }
        while (max >= r || r == 0);

        if (cnt > sizeof(nuint))
            throw new NotImplementedException($"getUnsignedInteger: cnt > {sizeof(nuint)} (max >= 1 << 64)");

        nuint* e = stackalloc nuint[1] {0};

        nuint mask = 2;
        while (mask <= max)
            mask <<= 1;

        mask--;
        do
        {
            if (entropy.Count < cnt)
                throw new NotEnoughtBytesException();

            *e = 0;     // e неполностью заполняется, нужно обнулить те байты, которые не будут заполнены
            entropy.GetBytesAndRemoveIt((byte*) e, cnt);

            r  = *e;
            r &= mask;
        }
        while (r > max);

        e[0] = 0;

        return r;
    }

    public unsafe void DoRandomPermutationForBytes(nint len, byte* T, nint countOfSteps = 0, byte regime = 1, nint maxDataLen = -1, CascadeSponge_1t_20230905.StepProgress? progressCsc = null)
    {
        byte a, err = 0;
        nint index;

        if (maxDataLen <= 0)
            maxDataLen = lastOutput.len;

        using var bb = new BytesBuilderStatic(this.maxDataLen*4);

        // Алгоритм тасования Дурштенфельда
        // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
        for (nint i = 0; i < len - 1; i++)
        {
            // var cutoff = getCutoffForUnsignedInteger(0, (ulong)len - i - 1);ulong
            // index = getUnsignedInteger(0, cutoff) + i;

            // Берём сразу много байтов, чтобы getUnsignedInteger потом не вылетало с лишними исключениями: так байтов почти всегда будет хватать
            if (bb.Count < bb.size - maxDataLen)
            {
                Step(countOfSteps: countOfSteps, regime: regime);
                bb.Add(lastOutput, maxDataLen);
                this.haveOutput = false;
            }

            // Исключение может случиться, если getUnsignedInteger отбросит слишком много значений
            try
            {
                index = (nint) GetUnsignedInteger((nuint)(len - i - 1), bb) + i;
                err = 0;
            }
            catch (NotEnoughtBytesException)
            {
                if (err > 64)
                    throw;

                err++;
                i--;
                continue;
            }

            a        = T[i];
            T[i]     = T[index];
            T[index] = a;

            if (progressCsc is not null)
            {
                progressCsc.processedSteps++;
            }
        }

        a = 0;
    }


    // Ниже копия для Int32
    /// <summary>Получает числа для перестановок в массиве.</summary>
    /// <param name="len">Длина массива для перестановок и массива T (массив T может быть на 1 элемент меньше).</param>
    /// <param name="T">Приёмник для чисел перестановок в массиве.</param>
    /// <param name="countOfSteps">Количество шагов, которое делает губка перед генерацией одного блока данных.</param>
    /// <param name="regime">Логический режим шифрования. (не должен совпадать с предыдущим логическим режимом шифроания).</param>
    /// <param name="maxDataLen">Максимальное количество данных, получаемое из губки за раз (после countOfSteps шагов).</param>
    public unsafe void GetRandomPermutationNumbers(nint len, nint* T, nint countOfSteps = 0, byte regime = 1, nint maxDataLen = -1)
    {
        byte err = 0;
        nint index;
        nint current = 0;

        if (maxDataLen <= 0)
            maxDataLen = lastOutput.len;

        using var bb = new BytesBuilderStatic(this.maxDataLen*4);

        // Алгоритм тасования Дурштенфельда
        // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
        for (nint i = 0; i < len - 1; i++)
        {
            // var cutoff = getCutoffForUnsignedInteger(0, (ulong)len - i - 1);ulong
            // index = getUnsignedInteger(0, cutoff) + i;

            // Берём сразу много байтов, чтобы getUnsignedInteger потом не вылетало с лишними исключениями: так байтов почти всегда будет хватать
            if (bb.Count < bb.size - maxDataLen)
            {
                Step(countOfSteps: countOfSteps, regime: regime);
                bb.Add(lastOutput, maxDataLen);
                this.haveOutput = false;
            }

            // Исключение может случиться, если getUnsignedInteger отбросит слишком много значений
            try
            {
                index = (nint) GetUnsignedInteger((nuint)(len - i - 1), bb) + i;
                T[current] = index;
                current++;
                err = 0;
            }
            catch (NotEnoughtBytesException)
            {
                if (err > 64)
                    throw;

                err++;
                i--;
                continue;
            }
        }
    }

    // Эта функция должна быь абсолютной копией предыдущей, за исключением 
    public unsafe void GetRandomPermutationNumbers(nint len, UInt32* T, nint countOfSteps = 0, byte regime = 1, nint maxDataLen = -1)
    {
        byte  err = 0;
        nint index;
        nint  current = 0;

        if (maxDataLen <= 0)
            maxDataLen = lastOutput.len;

        using var bb = new BytesBuilderStatic(this.maxDataLen*4);

        // Алгоритм тасования Дурштенфельда
        // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
        for (nint i = 0; i < len - 1; i++)
        {
            // var cutoff = getCutoffForUnsignedInteger(0, (ulong)len - i - 1);ulong
            // index = getUnsignedInteger(0, cutoff) + i;

            // Берём сразу много байтов, чтобы getUnsignedInteger потом не вылетало с лишними исключениями: так байтов почти всегда будет хватать
            if (bb.Count < bb.size - maxDataLen)
            {
                Step(countOfSteps: countOfSteps, regime: regime);
                bb.Add(lastOutput, maxDataLen);
                this.haveOutput = false;
            }

            // Исключение может случиться, если getUnsignedInteger отбросит слишком много значений
            try
            {
                index = (nint) GetUnsignedInteger((nuint)(len - i - 1), bb) + i;
                T[current] = (UInt32) index;
                current++;
                err = 0;
            }
            catch (NotEnoughtBytesException)
            {
                if (err > 64)
                    throw;

                err++;
                i--;
                continue;
            }
        }
    }
}
