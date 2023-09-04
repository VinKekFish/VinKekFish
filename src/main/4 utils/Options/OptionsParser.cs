// TODO: tests
using System.Text;

namespace VinKekFish_Utils.ProgramOptions;

public class Options
{
    public readonly Block options;
    public readonly bool  isAllStrings;
    public Options(List<string> optionsStrings, bool isAllStrings = false)
    {
        this.isAllStrings = isAllStrings;

        int i = 0;
        options = new Block(optionsStrings, ref i, 0, true, isAllStrings: isAllStrings);
    }

    public override string ToString()
    {
        return options.ToString();
    }

    /*
    var pathBlock = opt.SearchBlock(new List<string> { "unix stream", "path" });
    var pathBlock = opt.SearchBlock("unix stream.path");
    */
    public virtual Block? SearchBlock(string path, int depth = 0, Block? block = null)
    {
        var pp = path.Split('.');
        return SearchBlock(  new List<string>(pp), depth, block  );
    }

    public virtual Block? SearchBlock(List<string> path, int depth = 0, Block? block = null)
    {
        block ??= this.options;

        foreach (var b in block.blocks)
        {
            if (b.Name == path[depth])
            {
                if (path.Count > depth + 1)
                    return SearchBlock(path, depth+1, b);
                else
                    return b;
            }
        }

        return null;
    }


    public class Block
    {
        public const    int          minIndent = 4;
        public const    int          tabIndent = 4;

        public readonly List<Block>  blocks = new List<Block>();
        public readonly string       Name;
        public readonly int          startLine;
        public readonly int          endLine;
        public readonly int          blockHeaderIndent;
        public readonly bool         isAllStrings;

        public override string ToString()
        {
            return this.ToString("");
        }

        public virtual string ToString(string indent)
        {
            var sb = new StringBuilder();

            sb.AppendLine(indent + Name);

            if (blocks is not null)
            foreach (var str in blocks)
            {
                sb.AppendLine(str.ToString(indent + "\t"));
            }

            return sb.ToString();
        }

        public Block(List<string> options, ref int currentLine, int blockHeaderIndent, bool isRootBlock = false, bool isAllStrings = false)
        {
            this.blockHeaderIndent = blockHeaderIndent;
            this.isAllStrings      = isAllStrings;
            startLine = currentLine;

            Name = Parse(options, ref currentLine, blockHeaderIndent, isRootBlock);
        }

        public virtual string Parse(List<string> options, ref int currentLine, int blockHeaderIndent, bool isRootBlock = false)
        {
            string? Name = isRootBlock ? "" : null;
            int     tab  = isRootBlock ? 0  : 1;
            for (; currentLine < options.Count; currentLine++)
            {
                var line  = options[currentLine];
                var tLine = line.Trim().ToLowerInvariant();

                // Комментарии
                if (tLine.StartsWith("#"))
                    continue;
                if (tLine.Length <= 0)
                    continue;

                // Вычисляем текущую глубину отступов
                var depth = CalcIndentationDepth(line);
                if (Name is null && depth == blockHeaderIndent)
                {
                    Name = doIndentationTrim(line, blockHeaderIndent);
                    continue;
                }

                if (depth <= blockHeaderIndent && !isRootBlock)
                {
                    // Console.WriteLine("return from " + blockHeaderIndent + $" (for depth {depth})");
                    break;
                }

                // Начинается блок определения строки
                if (tLine == "::string" || (isAllStrings && !isRootBlock))
                {
                    blocks.Add(new StringBlock(options, ref currentLine, blockHeaderIndent + tab));
                    currentLine--;
                    continue;
                }

                if (tLine == ":")
                {
                    continue;
                }

                blocks.Add(  new Block(options, ref currentLine, blockHeaderIndent + tab, isAllStrings: isAllStrings)  );
                currentLine--;
            }

            if (Name is null)
                throw new Exception($"Block.Block: Name is null (for line {currentLine+1} and indentation depth {blockHeaderIndent}: \"{options[currentLine]}\")");

            return Name;
        }

        public static int CalcIndentationDepth(string str)
        {
            int j = 0, BHI = 0;
            for (int i = 0; i < str.Length; i++)
            {
                CorrectBHI(ref j, ref BHI);

                var ch = str[i];
                if (ch == ' ')
                {
                    j++;
                    continue;
                }
                if (ch == '\t')
                {
                    j += tabIndent;
                    continue;
                }

                break;
            }

            CorrectBHI(ref j, ref BHI);

            return BHI;
        }

        protected static void CorrectBHI(ref int j, ref int BHI)
        {
            if (j >= minIndent)
            {
                BHI += 1;
                j = 0;
            }
        }

        public static string doIndentationTrim(string str, int blockHeaderIndent)
        {
            int j = 0, BHI = 0, i = 0;
            for (; i < str.Length; i++)
            {
                CorrectBHI(ref j, ref BHI);
                if (BHI >= blockHeaderIndent)
                    break;

                var ch = str[i];
                if (ch == ' ')
                {
                    j++;
                    continue;
                }
                if (ch == '\t')
                {
                    j += tabIndent;
                    continue;
                }

                break;
            }

            CorrectBHI(ref j, ref BHI);
            var result = str.Substring(startIndex: i);

            return result;
        }
    }

    public class StringBlock: Block
    {
        public StringBlock(List<string> options, ref int currentLine, int blockHeaderIndent)
                     :base(options, ref currentLine, blockHeaderIndent)
        {}

        public override string Parse(List<string> options, ref int currentLine, int blockHeaderIndent, bool _ = false)
        {
            var sb = new StringBuilder(64);
            for (; currentLine < options.Count; currentLine++)
            {
                var line  = options[currentLine];
                var tLine = line.Trim().ToLowerInvariant();

                if (tLine.Length <= 0)
                {
                    sb.AppendLine();
                    continue;
                }

                // Вычисляем текущую глубину отступов
                var depth  = CalcIndentationDepth(line);
                if (depth < blockHeaderIndent)
                    break;

                var newStr = doIndentationTrim(line, blockHeaderIndent);
                sb.AppendLine(newStr);
            }

            var str = sb.ToString();
            return str.Substring(0, str.Length - 1);    // Удаляем последний перевод строки, чтобы можно было делать переводы только части строк, которые никогда не оканчиваются на перевод строки
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
