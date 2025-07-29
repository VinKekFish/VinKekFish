// Здесь определены преобразователи цветов консоли
namespace VinKekFish_Utils.console;

/// <summary>Класс для определения состояния консоли</summary>
/// <remarks>
/// <para></para>
/// </remarks>
public static class ConsoleState
{
    public static bool IsHasTerminal()
    {
        if (Console.IsOutputRedirected || Console.IsErrorRedirected)
            return false;

        if (Console.WindowHeight <= 0)
            return false;

        if (Console.WindowWidth <= 0)
            return false;
        
        return true;
    }
}
