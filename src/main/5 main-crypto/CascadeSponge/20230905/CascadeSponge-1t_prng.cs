// TODO: tests
namespace vinkekfish;

using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using System.Threading.Tasks;

// code::docs:rQN6ZzeeepyOpOnTPKAT:

public partial class CascadeSponge_1t_20230905
{
    public unsafe void doRandomPermutationForUShorts(nint len, ushort* T, nint countOfSteps = 0, byte regime = 1)
    {
        ushort a;
        nint   index;

        var bb = new BytesBuilderStatic(this.maxDataLen*2);

        // Алгоритм тасования Дурштенфельда
        // https://ru.wikipedia.org/wiki/Тасование_Фишера_—_Йетса
        for (nint i = 0; i < len - 1; i++)
        {
            // var cutoff = getCutoffForUnsignedInteger(0, (ulong)len - i - 1);ulong
            // index = getUnsignedInteger(0, cutoff) + i;

            if (bb.Count < 2)
            {
                step(countOfSteps: countOfSteps, regime: regime);
                bb.add(lastOutput);
                this.haveOutput = false;
            }

            index = getUnsignedInteger(len - i - 1, bb) + i;

            a        = T[i];
            T[i]     = T[index];
            T[index] = a;
        }

        a = 0;
    }

    /// <summary>Получает число от 0 до max включительно</summary>
    /// <param name="max">Максимальное число, которое ещё возможно получить</param>
    /// <param name="entropy">Байты, сгенерированные источником энтропии</param>
    /// <returns>Случайное число в диапазоне [0; max]</returns>
    public unsafe static nint getUnsignedInteger(nint max, BytesBuilderStatic entropy)
    {
        if (max < 1)
            throw new ArgumentOutOfRangeException("CascadeSponge_1t_20230905.getUnsignedInteger: max < 1");

        nint r;
        nint cnt = max < 256 ? 1 : 2;
        var  e   = stackalloc byte[2] {0, 0};

        nint mask = 2;
        while (mask <= max)
            mask <<= 1;

        mask--;
        do
        {
            entropy.getBytesAndRemoveIt(e, cnt);

            r  = e[0];
            r += e[1] << 8;

            r &= mask;
        }
        while (r > max);

        e[0] = 0;
        e[1] = 0;

        return r;
    }
}
