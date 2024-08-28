// Здесь определены преобразователи цветов консоли
namespace VinKekFish_Utils.console;

/// <summary>Класс для временного переопределения параметров консоли</summary>
/// <remarks>Использование:
/// <para>Отнаследовать класс. В конструкторе задать нужные условия. Если нужно, переопределить Disposing()</para>
/// Применять с ключевым словом using
/// <para>using var console_opts = new ConsoleOptionsChild();</para>
/// </remarks>
public abstract class ConsoleOptions: IDisposable
{
                                                    /// <summary>Первоначальный цвет фона консоли</summary>
    public ConsoleColor InitialBackgroundColor;     /// <summary>Первоначальный цвет текста консоли</summary>
    public ConsoleColor InitialForegroundColor;

    public ConsoleOptions()
    {
        InitialBackgroundColor = Console.BackgroundColor;
        InitialForegroundColor = Console.ForegroundColor;
    }

    public void Dispose()
    {
        Disposing();
        GC.SuppressFinalize(this);
    }

    public virtual void Disposing()
    {
        Console.BackgroundColor = InitialBackgroundColor;
        Console.ForegroundColor = InitialForegroundColor;

        // Делаем для того, чтобы цвет фона не распространился на следующую строку
        Console.WriteLine();
    }
}

public class ErrorConsoleOptions : ConsoleOptions
{
    public ErrorConsoleOptions() => Console.BackgroundColor = ConsoleColor.DarkRed;
}

public class NotErrorConsoleOptions : ConsoleOptions
{
    public NotErrorConsoleOptions()
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
    }
}

public class NotImportantConsoleOptions : ConsoleOptions
{
    public NotImportantConsoleOptions()
    {
        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.ForegroundColor = ConsoleColor.Gray;
    }
}

public class RedTextConsole : ConsoleOptions
{
    public RedTextConsole()
    {
        Console.ForegroundColor = ConsoleColor.Red;
    }
}

public class YellowTextConsole : ConsoleOptions
{
    public YellowTextConsole()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
    }
}

public class GreenTextConsole : ConsoleOptions
{
    public GreenTextConsole()
    {
        Console.ForegroundColor = ConsoleColor.Green;
    }
}
