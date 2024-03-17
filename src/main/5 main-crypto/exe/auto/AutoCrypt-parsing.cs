// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Diagnostics;
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

        protected enum FileMustExists { error = 0, exists = 1, notExists = 2, indifferent = 3 };

        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="PathToFile">Опции, разделённые пробелом.</param>
        /// <param name="isDebugMode">Если true, выводит сообщение на stderr.</param>
        /// <param name="mustExists">Если exists, то файл должен существовать. Функция окончится неуспехом (вернёт null), если файл не существует или имеет нулевой размер. Аналогично с notExists. Если indifferent, то функция не будет проверять факт существования файла.</param>
        /// <param name="fileList">Список файлов для добавления созданного описателя. Может быть null.</param>
        /// <param name="title">Заголовок окна zenity. По умолчанию - пустая строка.</param>
        /// <returns>Описатель файла, располагающегося по пути PathToFile или null.</returns>
        protected FileInfo? ParseFileOptions(string PathToFile, bool isDebugMode = false, FileMustExists mustExists = FileMustExists.indifferent, List<FileInfo>? fileList = null, string title = "")
        {
            if (string.IsNullOrEmpty(PathToFile))
            {
                var str = "";
                if (!string.IsNullOrEmpty(title))
                    str += $"--title '{title}' ";
                if (mustExists.HasFlag(FileMustExists.notExists))
                    str += "--save ";

                var psi = new ProcessStartInfo("zenity", $"--file-selection {str}");
                psi.RedirectStandardOutput = true;

                var p = Process.Start(psi);
                p!.WaitForExit();
                PathToFile = p.StandardOutput.ReadLine()!;

                if (isDebugMode)
                    Console.WriteLine("rnd:" + PathToFile);
            }

            var r = new FileInfo(PathToFile);
            r.Refresh();
            if (mustExists.HasFlag(FileMustExists.exists))
                if (!r.Exists || r.Length <= 0)
                    r = null;
            else
            if (mustExists.HasFlag(FileMustExists.notExists))
                if (r.Exists)
                    r = null;

            if (r is not null)
            {
                if (fileList is not null)
                    fileList.Add(r);
            }
            else
            {
                if (isDebugMode)
                    Console.Error.WriteLine(L("File not found or an another file system error occured") + $": {PathToFile}");
            }

            return r;
        }

    }
}
