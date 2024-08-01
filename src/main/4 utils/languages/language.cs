#pragma warning disable CA2211 // Поля, не являющиеся константами, не должны быть видимыми [4 utils]

// TODO: tests
namespace VinKekFish_Utils;

using System.Globalization;
using VinKekFish_Utils.ProgramOptions;

// using static VinKekFish_Utils.Language

public class Language
{
    public static CultureInfo culture  = GetParentCulture(Thread.CurrentThread.CurrentCulture);
    public static Language    language = new();
    public        Options     stringsInOptions;

    public Dictionary<String, String> strings = new(1024);

    public static CultureInfo GetParentCulture(CultureInfo culture)
    {
        while (culture.Parent is not null && culture.Parent != culture && culture.Parent.Name.Length > 0)
        {
            culture = culture.Parent;
        }

        return culture;
    }

    public Language(string defaultCultureFile = "en")
    {
        var dir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locales"));
        CheckAndRedefineCulture();

        var fi = new FileInfo(Path.Combine(dir.FullName, culture.Name + ".loc")); fi.Refresh();
        if (!fi.Exists)
        {
            fi = new FileInfo(Path.Combine(dir.FullName, defaultCultureFile + ".loc")); fi.Refresh();
        }

        var lines = File.ReadAllLines(fi.FullName);
        stringsInOptions = new Options(new List<string>(lines), isAllStrings: true);

        foreach (var name in stringsInOptions.options.blocks)
        {
            if (name.blocks.Count < 1)
                continue;

            if (name.blocks[0] is not Options.StringBlock sb)
                continue;

            if (strings.ContainsKey(name.Name))
            {
                Console.Error.WriteLine($"Localization error: Language have twiced key \"{name.Name}\"");
                continue;
            }

            strings.Add(name.Name, sb.Name);
        }
    }

    public static void CheckAndRedefineCulture()
    {
        // Читаем файл, который переопределяет культуру. Он должен быть в текущей директории программы, а не в locales
        try
        {
            var redefinFile = "REDEFINE.loc";
            if (File.Exists(redefinFile))
            {
                var redefinedCulture = File.ReadAllText(redefinFile).Trim();
                if (redefinedCulture.Length > 0)
                {
                    culture = new CultureInfo(redefinedCulture, true);
                }
            }
        }
        catch
        { }
    }

    public static string L(string stringName)
    {
        var dict = language.strings;
        if (dict.ContainsKey(stringName))
            return dict[stringName];

        return stringName;
    }
}
