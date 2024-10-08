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
        #pragma warning disable SYSLIB1045 // Используйте "GeneratedRegexAttribute", чтобы создавать реализацию регулярного выражения во время компиляции. [maincrypto]
        protected static readonly Regex SpaceRegex = new("[ \t]+", RegexOptions.Compiled | RegexOptions.Singleline);
        protected static readonly Regex CommaRegex = new("[,;]",   RegexOptions.Compiled | RegexOptions.Singleline);
        #pragma warning restore SYSLIB1045

        public static string[] ToSpaceSeparated(string value)
        {
            lock (CommaRegex)
            value = CommaRegex.Replace(value, " ");
            lock (SpaceRegex)
            value = SpaceRegex.Replace(value, " ");

            var values = value.Split(" ");
            return values;
        }

        protected enum FileMustExists { Error = 0, Exists = 1, NotExists = 2, Indifferent = 4 };

        /// <summary>Распарсить опции команды regime</summary>
        /// <param name="PathToFile">Опции, разделённые пробелом.</param>
        /// <param name="isDebugMode">Если true, выводит сообщение на stderr.</param>
        /// <param name="mustExists">Если exists, то файл должен существовать. Функция окончится неуспехом (вернёт null), если файл не существует или имеет нулевой размер. Аналогично с notExists. Если indifferent, то функция не будет проверять факт существования файла.</param>
        /// <param name="fileList">Список файлов для добавления созданного описателя. Может быть null.</param>
        /// <param name="title">Заголовок окна zenity. По умолчанию - пустая строка.</param>
        /// <returns>Описатель файла, располагающегося по пути PathToFile или null.</returns>
        protected static FileInfo? ParseFileOptions(string PathToFile, bool isDebugMode = false, FileMustExists mustExists = FileMustExists.Indifferent, List<FileInfo>? fileList = null, string title = "")
        {
            if (string.IsNullOrEmpty(PathToFile))
            {
                var str = "";
                if (!string.IsNullOrEmpty(title))
                    str += $"--title '{title}' ";
                if (mustExists.HasFlag(FileMustExists.NotExists) || mustExists.HasFlag(FileMustExists.Indifferent))
                    str += "--save ";

                var psi = new ProcessStartInfo("zenity", $"--file-selection {str}");
                psi.RedirectStandardOutput = true;

                using var p = Process.Start(psi);
                p!.WaitForExit();
                PathToFile = p.StandardOutput.ReadLine()!;

                if (isDebugMode)
                    Console.WriteLine(PathToFile);
            }

            var r = new FileInfo(PathToFile);
            r.Refresh();
            if (mustExists.HasFlag(FileMustExists.Exists))
                if (!r.Exists || r.Length <= 0)
                    r = null;

            if (r is not null)
            if (mustExists.HasFlag(FileMustExists.NotExists))
            {
                if (r.Exists)
                    r = null;

                if (fileList is not null)
                foreach (var f in fileList)
                {
                    if (f.FullName == r?.FullName)
                    {
                        r = null;
                        break;
                    }
                }
            }

            if (r is not null)
            {
                fileList?.Add(r);
            }
            else
            {
                if (isDebugMode)
                {
                    if (mustExists.HasFlag(FileMustExists.NotExists))
                        Console.Error.WriteLine(L("File already exists or an another file system error occured") + $": {PathToFile}");
                    else
                        Console.Error.WriteLine(L("File not found or an another file system error occured") + $": {PathToFile}");
                }
            }

            return r;
        }


        /// <summary>Распарсить опции</summary>
        /// <param name="PathToDir">Опции, разделённые пробелом.</param>
        /// <param name="isDebugMode">Если true, выводит сообщение на stderr.</param>
        /// <param name="mustExists">Если exists, то файл должен существовать. Функция окончится неуспехом (вернёт null), если файл не существует или имеет нулевой размер. Аналогично с notExists. Если indifferent, то функция не будет проверять факт существования файла.</param>
        /// <param name="dirList">Список папок для добавления созданного описателя. Может быть null.</param>
        /// <param name="title">Заголовок окна zenity. По умолчанию - пустая строка.</param>
        /// <returns>Описатель файла, располагающегося по пути PathToFile или null.</returns>
        protected static DirectoryInfo? ParseDirOptions(string PathToDir, bool isDebugMode = false, FileMustExists mustExists = FileMustExists.Indifferent, List<DirectoryInfo>? dirList = null, string title = "")
        {
            if (string.IsNullOrEmpty(PathToDir))
            {
                var str = "";
                if (!string.IsNullOrEmpty(title))
                    str += $"--title '{title}' ";
                if (mustExists.HasFlag(FileMustExists.NotExists) || mustExists.HasFlag(FileMustExists.Indifferent))
                    str += "--save ";

                var psi = new ProcessStartInfo("zenity", $"--file-selection --directory {str}");
                psi.RedirectStandardOutput = true;

                using var p = Process.Start(psi);
                p!.WaitForExit();
                PathToDir = p.StandardOutput.ReadLine()!;

                if (isDebugMode)
                    Console.WriteLine(PathToDir);
            }

            var r = new DirectoryInfo(PathToDir);
            r.Refresh();
            if (mustExists.HasFlag(FileMustExists.Exists))
                if (!r.Exists)
                    r = null;

            if (r is not null)
            if (mustExists.HasFlag(FileMustExists.NotExists))
            {
                if (r.Exists)
                    r = null;

                if (dirList is not null)
                foreach (var f in dirList)
                {
                    if (f.FullName == r?.FullName)
                    {
                        r = null;
                        break;
                    }
                }
            }

            if (r is not null)
            {
                dirList?.Add(r);
            }
            else
            {
                if (isDebugMode)
                {// TODO: здесь не файлы, а директории
                    if (mustExists.HasFlag(FileMustExists.NotExists))
                        Console.Error.WriteLine(L("File already exists or an another file system error occured") + $": {PathToDir}");
                    else
                        Console.Error.WriteLine(L("File not found or an another file system error occured") + $": {PathToDir}");
                }
            }

            return r;
        }

    }
}
