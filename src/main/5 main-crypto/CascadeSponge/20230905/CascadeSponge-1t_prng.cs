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
    public unsafe void doRandomPermutationForUShorts(nint len, ushort* T, nint countOfSteps = 0, byte regime = 1)
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
                step(countOfSteps: countOfSteps, regime: regime);
                bb.add(lastOutput);
                this.haveOutput = false;
            }

            // Исключение может случиться, если getUnsignedInteger отбросит слишком много значений
            try
            {
                index = (nint) getUnsignedInteger((nuint) (len - i - 1), bb) + i;
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
    public unsafe static nuint getUnsignedInteger(nuint max, BytesBuilderStatic entropy)
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
            entropy.getBytesAndRemoveIt((byte*) e, cnt);

            r  = *e;
            r &= mask;
        }
        while (r > max);

        e[0] = 0;

        return r;
    }

    public unsafe void doRandomPermutationForBytes(nint len, byte* T, nint countOfSteps = 0, byte regime = 1)
    {
        byte a, err = 0;
        nint index;

        using var bb = new BytesBuilderStatic(this.maxDataLen*4);

        // Алгоритм тасования Дурштенфельда
        // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
        for (nint i = 0; i < len - 1; i++)
        {
            // var cutoff = getCutoffForUnsignedInteger(0, (ulong)len - i - 1);ulong
            // index = getUnsignedInteger(0, cutoff) + i;

            // Берём сразу много байтов, чтобы getUnsignedInteger потом не вылетало с лишними исключениями: так байтов почти всегда будет хватать
            if (bb.Count < bb.size - this.maxDataLen)
            {
                step(countOfSteps: countOfSteps, regime: regime);
                bb.add(lastOutput);
                this.haveOutput = false;
            }

            // Исключение может случиться, если getUnsignedInteger отбросит слишком много значений
            try
            {
                index = (nint) getUnsignedInteger((nuint)(len - i - 1), bb) + i;
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
}
