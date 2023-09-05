namespace VinKekFish_Utils;

// ::test:VOWNOWU4qu1Al9x07uh0:
// ::test:U2ZqhKq1NoPFwcP9yyLB:
// ::test:FOibRZrv5zW1lwU2AT7y:


using cryptoprime;
using Record = cryptoprime.BytesBuilderForPointers.Record;

public unsafe static partial class Utils
{
    /// <summary>Переводит массив в строковое представление для удобства отладки</summary>
    /// <param name="buffer">Массив для перевода в строковое представление</param>
    /// <param name="len">Длина массива</param>
    /// <returns>Строка, закодированная в шестнадцатиричном виде</returns>
    public static string ArrayToHex(byte * buffer, nint len)
    {
        var a = new ReadOnlySpan<byte>(buffer, (int) len);
        return Convert.ToHexString(a);
    }
}
