// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет основную команду для парсинга, отдаваемую через auto-режим. Например, команды enc, dec.</summary>
    public abstract partial class Command
    {
        public bool Terminated = false;
        public AutoCrypt autoCrypt;
        public Command(AutoCrypt autoCrypt)
        {
            this.autoCrypt = autoCrypt;
        }

        public class CommandException: Exception
        {
            public CommandException(string message): base(message)
            {}
        }

        public class CommandNameNotFoundException: CommandException
        {
            public CommandNameNotFoundException(string Line): base($"Command name not found for line: '{Line}'. Right: name:value pair")
            {}
        }

        public class CommandInputStreamClosedException: CommandException
        {
            public CommandInputStreamClosedException(): base($"Command stream closed")
            {}
        }

        public abstract ProgramErrorCode Exec();

        public class CommandOption
        {
            public readonly string name;
            public readonly string value;
            public CommandOption(string name, string value)
            {
                this.name  = name;
                this.value = value;
            }

            public static CommandOption? ParseLine(string? Line)
            {
                if (Line == null)
                    return null;

                var trimmed = Line.Trim();
                if (trimmed.Length <= 0)
                    return null;
                
                var colon = Line.IndexOf(':');
                if (colon < 0)
                    return null;

                var name = Line.Substring(0, colon).Trim();
                if (name == null || name.Length <= 0)
                    throw new CommandNameNotFoundException(Line);

                var value = colon < Line.Length -1 ? Line.Substring(colon+1) : "";

                return new CommandOption
                (
                    name:  name.Trim().ToLowerInvariant(),
                    value: value
                );
            }

            public delegate void HelpToConsoleDelegate();

            /// <summary>Получает строку с консоли и парсит её. Если поток ввода закрыт, возвращает исключение CommandInputStreamClosedException</summary>
            /// <param name="helpAction">Делегат, который вызывается, если парсинг был неудачным (например, он может выдавать справку для пользователя по ожидаемой команде)</param>
            /// <param name="isDebugMode">Если true, то будет вызываться helpAction без исключений, иначе будут выдаваться исключения после вызова helpAction</param>
            /// <returns>Возвращает объект, представляющий команду (имя команды и её значение)</returns>
            /// <exception cref="CommandInputStreamClosedException">Если поток ввода-вывода больше не даёт строк (закрыт), то генерируется исключение CommandInputStreamClosedException</exception>
            public static CommandOption ReadAndParseLine(HelpToConsoleDelegate? helpAction = null, bool isDebugMode = false)
            {
                do
                {
                    var line = Console.ReadLine();
                    if (line == null)
                        throw new CommandInputStreamClosedException();

                    var commandOption = CommandOption.ParseLine(line);
                    if (commandOption == null)
                    {
                        helpAction?.Invoke();

                        if (!isDebugMode)
                            throw new CommandInputStreamClosedException();

                        continue;
                    }

                    return commandOption!;
                }
                while (true);
            }
        }
    }
}
