// TODO: tests
namespace VinKekFish_Utils;

using cryptoprime;
using Record = cryptoprime.BytesBuilderForPointers.Record;

public unsafe static partial class Utils
{
    /// <summary>Функция для расчёта выравнивания</summary>
    /// <param name="size">Размер массива для выравнивания</param>
    /// <param name="alignment">Размер границ, на который выравнивается</param>
    /// <returns>Выравненное значение size</returns>
    public static nint CalcAlignment(nint size, int alignment = 64)
    {
        var bmod = size % alignment;
        if (bmod == 0)
            return size;

        return size - bmod + alignment;
    }

    /// <summary>Выравнивает массив Record по значению 64-ре байта. Массив должен быть выделен с запасом 64 байта. Проще использовать alignmentDegree в AllocHGlobal_AllocatorForUnsafeMemory</summary>
    /// <param name="a">Массив для выравнивания. Должен быть выделен с запасом 64-ре байта</param>
    /// <returns>Выравненный массив (может как совпадать с "a", так и нет)</returns>
    public static Record GetAlignment64(Record a)
    {
        var bmod = (nint) a.array & 63;
        if (bmod == 0)
            return a;

        var correction = 64 - bmod;
        return a >> correction;
    }
}
