namespace VinKekFish_Utils;

// ::test:VOWNOWU4qu1Al9x07uh0:
// ::test:U2ZqhKq1NoPFwcP9yyLB:
// ::test:FOibRZrv5zW1lwU2AT7y:


using cryptoprime;
using Record = cryptoprime.BytesBuilderForPointers.Record;

public unsafe static partial class Utils
{
    /// <summary>Сравнивает две записи: well - эталонная запись, подлежит защите</summary>
    /// <param name="well">Эталонная запись, с которой сравнивается запись devil</param>
    /// <param name="devil">Вторая запись для сравнения. Вторая запись - запись, переданная злоумышленником</param>
    /// <returns>true, если значения массивов в записях равны</returns>
    public static bool SecureCompare(Record well, Record devil)
    {
        var aa  = well .array;
        var ba  = devil.array;
        var len = well.len;

        nint r = 0;
        for (nint i = 0; i < devil.len; i++)
        {
            r |= aa[i % len] ^ ba[i];                        
        }

        r |= well.len ^ devil.len;

        return r == 0;
    }

    /// <summary>Безопасно сравнивает два массива. Функция немного хуже, но раз в 5 быстрее, чем SecureCompare</summary>
    /// <param name="r1">Первый массив</param>
    /// <param name="r2">Второй массив</param>
    /// <returns><see langword="true"/>, если массивы совпадают.</returns>
    public unsafe static bool SecureCompareFast(Record r1, Record r2)
    {
        return SecureCompareFast(r1, r2, 0, 0, r1.len, r2.len);
    }

    /// <summary>Безопасно сравнивает два массива. Функция немного хуже, но раз в 5 быстрее, чем SecureCompare</summary>
    /// <param name="r1">Первый массив</param>
    /// <param name="r2">Второй массив</param>
    /// <param name="start1">Начальный индекс для сравнения в первом массиве</param>
    /// <param name="start2">Начальный индекс для сравнения во втором массиве</param>
    /// <param name="len1">Длина подмассива для сравнивания</param>
    /// <param name="len2">Длина подмассива для сравнивания</param>
    /// <returns><see langword="true"/>, если массивы совпадают.</returns>
    public unsafe static bool SecureCompareFast(Record r1, Record r2, nint start1, nint start2, nint len1, nint len2)
    {
        var len = len1;
        if (len > len2)
            len = len2;
        if (start1 + len1 > r1.len)
            throw new ArgumentOutOfRangeException(nameof(r1));
        if (start2 + len2 > r2.len)
            throw new ArgumentOutOfRangeException(nameof(r2));

        byte * r1a = r1.array + start1, r2a = r2.array + start2, End1 = r1a + len;

        nint V = 0;
        for (; r1a < End1; r1a++, r2a++)
        {
            V |= *r1a ^ *r2a;
        }

        V |= len1 ^ len2;

        return V == 0;
    }
// TODO: tests
    /// <summary>езопасно сравнивает два массива. Функция немного хуже, но раз в 5 быстрее, чем SecureCompare</summary>
    /// <param name="len1">Длина массива r1</param>
    /// <param name="len2">Длина массива r2</param>
    /// <param name="r1">Массив для сравнения</param>
    /// <param name="r2">Второй массив для сравнения</param>
    /// <returns>true, если значения равны</returns>
    public static bool SecureCompareFast(nint len1, nint len2, byte* r1, byte* r2)
    {
        var len = len1;
        if (len > len2)
            len = len2;

        byte * End1 = r1 + len;
        nint V = 0;
        for (; r1 < End1; r1++, r2++)
        {
            V |= *r1 ^ *r2;
        }

        V |= len1 ^ len2;
        return V == 0;
    }
}
