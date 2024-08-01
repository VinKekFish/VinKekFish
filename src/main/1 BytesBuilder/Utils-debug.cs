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
    public static string GetLogFileName(string logName = "")
    {
        return $"log-{logName}.log";
    }

    public readonly static object syncArrayToFile = new();

    /// <summary>Записывает массив в файл</summary>
    /// <param name="buffer">Массив для логирования</param>
    /// <param name="len">Длина массива</param>
    /// <param name="logName">Логическое имя лог-файла</param>
    public static void ArrayToFile(byte * buffer, nint len, string logName = "")
    {
        lock (syncArrayToFile)
        {
            File.AppendAllText(GetLogFileName(logName), ArrayToHex(buffer, len) + "\n\n");
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
            File.AppendAllText(GetLogFileName(logName), msg + "\n\n");
        }
    }

    /// <summary>Возвращает строковое представление исключения, вместе с вложенными исключениями.</summary>
    /// <param name="ex">Исключение</param>
    /// <param name="toConsole">Если true, то форматированное исключение будет выдано на стандартный вывод ошибок</param>
    public static string FormatException(Exception ex, bool toConsole = true)
    {
        var sb = new System.Text.StringBuilder(16 + ex.Message.Length + ex.StackTrace?.Length ?? 0);

        sb.AppendLine("----------------------------------------------------------------");
        sb.AppendLine(ex.Message);
        sb.AppendLine(ex.StackTrace);
        if (ex.InnerException is not null)
        {
            sb.AppendLine("Inner exception");
            sb.AppendLine(FormatException(ex.InnerException));
        }

        sb.AppendLine("----------------------------------------------------------------");
        sb.AppendLine();

        var msg = sb.ToString();
        if (toConsole)
            Console.Error.WriteLine(msg);

        return msg;
    }

    public static void TryToDispose(IDisposable? vkf)
    {
        try
        {
            vkf?.Dispose();
        }
        catch (Exception ex)
        {
            FormatException(ex);
        }
    }
}
