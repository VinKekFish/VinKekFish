
namespace ConsoleTest;
class Program
{
    // [{( - скобки убираем, проще запоминать просто слово "скобка"
    // < - меньше и больше легко перепутать, оставляем только один из них
    // ! слишком похож на черту и имеет сложное наименование
    // Ч - похоже на 4. ± - сложное наименование
    static string GrantedSymbols = "qwertyuiopasdfghjkLzxcvbnm1234567890,.<?;':(+-*/=|&^%$#@ΣΔΨλШЫЭЯ";
    static void Main(string[] args)
    {
        // 24bit or truecolor
        var tc = System.Environment.GetEnvironmentVariable("COLORTERM");
        Console.WriteLine(tc);
        Console.WriteLine($"isTrueColor: {isTrueColor(tc)}");
        Console.WriteLine($"Эти цвета явно различны, если TrueColor поддерживается:");
        // \x1b то же, что и echo -e "\033" в терминале
        Console.WriteLine("\x1b[1m\x1b[48;2;255;0;0m\x1b[38;2;0;0;255mTRUECOLOR\x1b[0m\x1b[0m");
        Console.WriteLine("\x1b[1m\x1b[48;2;224;0;0m\x1b[38;2;0;0;224mTRUECOLOR\x1b[0m\x1b[0m");
        Console.WriteLine("\x1b[1m\x1b[48;2;192;0;0m\x1b[38;2;0;0;192mTRUECOLOR\x1b[0m\x1b[0m");

        Console.WriteLine("Символ лямбда: λ");
        Console.WriteLine("Другие символы");
        foreach (var c in GrantedSymbols)
            Console.Write(c + " ");
        Console.WriteLine();
        Console.WriteLine("Нажмите любую клавишу...");
        var key = Console.ReadKey(true);
        Console.WriteLine("Введена клавиша " + key.Key.ToString());
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
