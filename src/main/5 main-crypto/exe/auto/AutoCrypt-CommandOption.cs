// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;

public partial class AutoCrypt
{
    /// <summary>Класс представляет основную команду для парсинга, отдаваемую через auto-режим. Например, команды enc, dec.</summary>
    public abstract partial class Command
    {
        public bool isDebugMode = false;
        public bool Terminated  = false;
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
            public CommandNameNotFoundException(string Line): base($"{L("Command name not found for line")}: '{Line}'. {L("Right")}: name:value")
            {}
        }

        public class CommandInputStreamClosedException: CommandException
        {
            public CommandInputStreamClosedException(): base($"Command stream closed")
            {}
        }

        public abstract ProgramErrorCode Exec();

        public interface isCorrectAvailable
        {
            public CommandOption.ParseResult isCorrect();
        }

        public class VinKekFishOptions: isCorrectAvailable
        {                                             /// <summary>Коэффициент стойкости VinKekFish (K=1,3,5,7,...)</summary>
            public int   K         = 1;               /// <summary>Количество раундов</summary>
            public int   Rounds    = 0;               /// <summary>Количество раундов со стандартными таблицами перестановки</summary>
            public int   PreRounds = 0;               /// <summary>Коэффициент использования выхода (K = 2 означает, что будет использовано только половина байтов выхода относительно стандартного выхода)</summary>
            public float KOut      = 1;

            public void SetK(int K)
            {
                bool isKey = Rounds == -1;

                if (isKey)
                    Rounds    = VinKekFishBase_KN_20210525.Calc_EXTRA_ROUNDS_K(K);
                else
                    Rounds    = VinKekFishBase_KN_20210525.Calc_NORMAL_ROUNDS_K(K);

                PreRounds = Rounds - VinKekFishBase_KN_20210525.Calc_OptimalRandomPermutationCountK(K);
                if (PreRounds < VinKekFishBase_KN_20210525.Calc_MIN_ROUNDS_K(K))
                    PreRounds = VinKekFishBase_KN_20210525.Calc_MIN_ROUNDS_K(K);

                if (isKey)
                    KOut = VinKekFishBase_KN_20210525.CalcBlockSize(K) / VinKekFishBase_KN_20210525.CalcBlockSizeForKey(K);
            }

            public override string ToString()
            {
                return $"K={K};Rounds={Rounds};PreRounds={PreRounds};KOut={KOut};";
            }

            /// <summary>Проверяет корректность инициализации структуры</summary>
            /// <returns>Возвращает пустой CommandOption.ParseResult в случае успеха (error == null). Если неуспешно, то возвращает ParseResult с установленным значением error</returns>
            public CommandOption.ParseResult isCorrect()
            {
                if ((K & 1) != 1)
                    return new CommandOption.ParseError($"(K & 1) != 1 ({K})");

                if (K < 1 || K > 19)
                    return new CommandOption.ParseError($"K < 1 || K > 19 ({K})");

                if (Rounds < VinKekFishBase_KN_20210525.Calc_MIN_ROUNDS_K(K))
                    return new CommandOption.ParseError($"Rounds < MIN_ROUNDS_K ({Rounds} < {VinKekFishBase_KN_20210525.Calc_MIN_ROUNDS_K(K)})");

                if (PreRounds > Rounds)
                    return new CommandOption.ParseError($"PreRounds > Rounds ({PreRounds} > {Rounds})");

                if (KOut < 1f)
                    return new CommandOption.ParseError($"KOut < 1.0 ({KOut})");

                return new CommandOption.ParseResult();
            }
        }

        public class CascadeOptions: isCorrectAvailable
        {                                           /// <summary>Стойкость каскадной губки в байтах</summary>
            public int   StrengthInBytes = 512;     /// <summary>Коэффициент использования выхода (K = 2 означает, что будет использовано только половина байтов выхода относительно стандартного выхода)</summary>
            public float KOut = 1;

            public override string ToString()
            {
                return $"StrengthInBytes={StrengthInBytes};KOut={KOut};";
            }

            /// <summary>Проверяет корректность инициализации структуры</summary>
            /// <returns>Возвращает пустой CommandOption.ParseResult в случае успеха (error == null). Если неуспешно, то возвращает ParseResult с установленным значением error</returns>
            public CommandOption.ParseResult isCorrect()
            {
                if (KOut < 1f)
                    return new CommandOption.ParseError($"KOut < 1.0 ({KOut})");

                return new CommandOption.ParseResult();
            }
        }

        public class CommandOption
        {
            public readonly string name;
            public readonly string value;
            public CommandOption(string name, string value)
            {
                this.name  = name;
                this.value = value;
            }

            public static ParseResult? ParseLine(string? Line)
            {
                if (Line == null)
                    return null;

                var trimmed = Line.Trim();
                if (trimmed.Length <= 0)
                    return ParseError.MustSkipped;

                var colon = Line.IndexOf(':');
                if (colon < 0)
                    return new ParseError(L("The command line must contain a colon"));

                var name = Line.Substring(0, colon).Trim();
                if (name == null || name.Length <= 0)
                    return new ParseError(L("The command name not found")) {ex = new CommandNameNotFoundException(Line)};

                var value = colon < Line.Length -1 ? Line.Substring(colon+1) : "";

                return new CommandOption
                (
                    name:  name.Trim().ToLowerInvariant(),
                    value: value
                );
            }

            public delegate void HelpToConsoleDelegate();

            public class ParseResult
            {
                public CommandOption? opts;
                public ParseError?    error;

                public static implicit operator ParseResult(CommandOption opts)
                {
                    return new ParseResult() {opts = opts};
                }

                public static implicit operator ParseResult(ParseError error)
                {
                    return new ParseResult() {error = error};
                }

                public static implicit operator CommandOption(ParseResult result)
                {
                    if (result.opts != null)
                        return result.opts;

                    if (result.error?.ex != null)
                        throw result.error.ex;

                    if (result.error?.ParseMessage != null)
                        throw new CommandException(result.error.ParseMessage);

                    throw new CommandException("implicit operator CommandOption(ParseResult result)");
                }
            }

            public class ParseError
            {                                                                                       /// <summary>Сообщение для пользователя, описывающее ошибку парсинга</summary>
                public string?    ParseMessage;                                                        /// <summary>true, если строка является комментарием, пустой строкой или другой строкой, которая должна быть пропущена (проигнорирована), но которая не является ошибкой</summary>
                public bool       LineMustSkipped = false;
                public Exception? ex;

                public static readonly ParseError MustSkipped =
                                       new ParseError() {LineMustSkipped = true};

                /// <summary>Создаёт описатель ошибки</summary>
                /// <param name="message">Сообщение для пользователя, описывающее ошибку</param>
                public ParseError(string message)
                {
                    this.ParseMessage = message;
                }

                protected ParseError()
                {}
            }

            /// <summary>Получает строку с консоли и парсит её. Если поток ввода закрыт, возвращает исключение CommandInputStreamClosedException</summary>
            /// <param name="helpAction">Делегат, который вызывается, если парсинг был неудачным (например, он может выдавать справку для пользователя по ожидаемой команде)</param>
            /// <param name="isDebugMode">Если true, то будет вызываться helpAction без исключений, иначе будут выдаваться исключения после вызова helpAction</param>
            /// <returns>Возвращает объект, представляющий команду (имя команды и её значение)</returns>
            /// <exception cref="CommandInputStreamClosedException">Если поток ввода-вывода больше не даёт строк (закрыт), то генерируется исключение CommandInputStreamClosedException</exception>
            public static ParseResult ReadAndParseLine(HelpToConsoleDelegate? helpAction = null, bool isDebugMode = false)
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
                        else
                            Console.WriteLine(L("Incorrect line"));

                        continue;
                    }

                    if (commandOption.error != null)
                    {
                        if (commandOption.error.LineMustSkipped)
                        {
                            helpAction?.Invoke();
                            continue;
                        }

                        Console.WriteLine(L("Incorrect line") + $": {commandOption.error.ParseMessage}");
                        helpAction?.Invoke();

                        if (!isDebugMode)
                        {
                            throw new CommandException(commandOption.error.ParseMessage!);
                        }
                        else
                            continue;
                    }

                    return commandOption!;
                }
                while (true);
            }
        }
    }
}
