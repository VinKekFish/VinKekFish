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

    /// <summary>Получает имя лог-файла в файловой системе для ArrayToFile</summary>
    /// <param name="logName">Логическое имя лог-файла</param>
    public static string getLogFileName(string logName = "")
    {
        return $"log-{logName}.log";
    }

    public readonly static object syncArrayToFile = new Object();

    /// <summary>Записывает массив в файл</summary>
    /// <param name="buffer">Массив для логирования</param>
    /// <param name="len">Длина массива</param>
    /// <param name="logName">Логическое имя лог-файла</param>
    public static void ArrayToFile(byte * buffer, nint len, string logName = "")
    {
        lock (syncArrayToFile)
        {
            File.AppendAllText(getLogFileName(logName), ArrayToHex(buffer, len) + "\n\n");
        }
    }

    /// <summary>Записывает массив в файл</summary>
    /// <param name="buffer">Массив для логирования</param>
    /// <param name="logName">Логическое имя лог-файла</param>
    public static void ArrayToFile(byte[] buffer, string logName = "")
    {
        lock (syncArrayToFile)
        {
            fixed (byte * buff = buffer)
            ArrayToFile(buff, buffer.Length, logName);
        }
    }

    /// <summary>Записывает массив в файл</summary>
    /// <param name="buffer">Массив для логирования</param>
    /// <param name="logName">Логическое имя лог-файла</param>
    public static void ArrayToFile(int[] buffer, string logName = "")
    {
        lock (syncArrayToFile)
        {
            fixed (int * buff = buffer)
            ArrayToFile((byte*) buff, buffer.Length * sizeof(int), logName);
        }
    }

    public static void MsgToFile(string msg, string logName = "")
    {
        lock (syncArrayToFile)
        {
            File.AppendAllText(getLogFileName(logName), msg + "\n\n");
        }
    }
}
