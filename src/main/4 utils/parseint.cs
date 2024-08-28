using System.Text.RegularExpressions;

namespace VinKekFish_Utils;

/// <summary>Класс для временного переопределения параметров консоли</summary>
public static class ParseUtils
{
    public readonly static (string suffix, int k)[] SizeSuffixes = 
    {
        ("k", 1024), ("kb", 1024), ("ki", 1024), ("kib", 1024), ("m", 1024*1024),
        ("mb", 1024*1024), ("mi", 1024*1024), ("mib", 1024*1024)
    };
    public static nint ParseSize(string value, nint k = 1)
    {
        value = value.ToLowerInvariant().Trim();
        value = value.Replace(" ", "");
        value = value.Replace("_", "");
        value = value.Replace("'", "");

        foreach (var suffixDescriptor in SizeSuffixes)
        {
            if (value.EndsWith(suffixDescriptor.suffix))
            {
                var nval = value.Substring(0, value.Length - suffixDescriptor.suffix.Length);
                return ParseSize(nval, k*suffixDescriptor.k);
            }
        }

        if (!nint.TryParse(value, out nint result))
            result = 0;
        else
            result *= k;

        return result;
    }

    public readonly static (string suffix, int k)[] MS_Suffixes = 
    {
        ("ms", 1), ("s", 1000), ("sec", 1000), ("second", 1000), ("seconds", 1000),
        ("m", 60_000), ("min", 60_000), ("h", 3600_000)
    };
    public static nint ParseMS(string value, nint k = 1)
    {
        value = value.ToLowerInvariant().Trim();
        value = value.Replace(" ", "");
        value = value.Replace("_", "");
        value = value.Replace("'", "");

        if (k == 1)
        foreach (var suffixDescriptor in MS_Suffixes)
        {
            if (value.EndsWith(suffixDescriptor.suffix))
            {
                var nval = value.Substring(0, value.Length - suffixDescriptor.suffix.Length);
                return ParseSize(nval, suffixDescriptor.k);
            }
        }

        if (!nint.TryParse(value, out nint result))
            result = 0;
        else
            result *= k;

        return result;
    }
}
