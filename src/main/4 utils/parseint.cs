using System.Text.RegularExpressions;

namespace VinKekFish_Utils;

/// <summary>Класс для временного переопределения параметров консоли</summary>
public static class ParseUtils
{
    /// <summary>Множители файловых размеров</summary>
    public const long km = 1024, mm = 1024*1024, gm = 1024*1024*1024, tm = 1024L*1024L*1024L*1024L;
    public readonly static (string suffix, long k)[] SizeSuffixes = 
    {
        ("k", km), ("kb", km), ("ki", km), ("kib", km),
        ("m", mm), ("mb", mm), ("mi", mm), ("mib", mm),
        ("g", gm), ("gb", gm), ("gi", gm), ("gib", gm),
        ("t", tm), ("tb", tm), ("ti", tm), ("tib", tm)
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
                return ParseSize(nval, (nint) (k*suffixDescriptor.k));
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
