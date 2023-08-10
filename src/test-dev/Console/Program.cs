
namespace ConsoleTest;
class Program
{
    static void Main(string[] args)
    {
        // 24bit or truecolor
        var tc = System.Environment.GetEnvironmentVariable("COLORTERM");
        Console.WriteLine(tc);
        Console.WriteLine($"isTrueColor: {isTrueColor(tc)}");
        Console.WriteLine($"Эти цвета различны, если TrueColor поддерживается:");
        // \x1b то же, что и echo -e "\033" в терминале
        Console.WriteLine("\x1b[1m\x1b[48;2;255;0;0m\x1b[38;2;0;0;255mTRUECOLOR\x1b[0m\x1b[0m");
        Console.WriteLine("\x1b[1m\x1b[48;2;224;0;0m\x1b[38;2;0;0;224mTRUECOLOR\x1b[0m\x1b[0m");
        Console.WriteLine("\x1b[1m\x1b[48;2;192;0;0m\x1b[38;2;0;0;192mTRUECOLOR\x1b[0m\x1b[0m");
    }

    public static bool isTrueColor(string? COLORTERM)
    {
        if (COLORTERM == null)
            return false;

        COLORTERM = COLORTERM.ToLowerInvariant().Trim();
        if (COLORTERM == "24bit" || COLORTERM == "truecolor")
            return true;

        return false;
    }
}
