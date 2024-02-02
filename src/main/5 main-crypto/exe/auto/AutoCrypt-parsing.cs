// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Text.RegularExpressions;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет основную команду для парсинга, отдаваемую через auto-режим. Например, команды enc, dec.</summary>
    public abstract partial class Command
    {
        protected static Regex SpaceRegex = new Regex("[ \t]+", RegexOptions.Compiled | RegexOptions.Singleline);
        protected static Regex CommaRegex = new Regex("[,;]",   RegexOptions.Compiled | RegexOptions.Singleline);
        public static string[] ToSpaceSeparated(string value)
        {
            lock (CommaRegex)
            value = CommaRegex.Replace(value, " ");
            lock (SpaceRegex)
            value = SpaceRegex.Replace(value, " ");

            var values = value.Split(" ");
            return values;
        }

        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="value">Опции, разделённые пробелом.</param>
        protected FileInfo? ParseFileOptions(string value)
        {
            return null;
        }

    }
}
