// TODO: tests
using System.Text;

namespace VinKekFish_Utils.Options;

public class Options
{
    public readonly Block options;
    public Options(List<string> optionsStrings)
    {
        int i = 0;
        options = new Block(optionsStrings, ref i, 0, true);
    }

    public override string ToString()
    {
        return options.ToString();
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
            sb.AppendLine();

            return sb.ToString();
        }

        public Block(List<string> options, ref int currentLine, int blockHeaderIndent, bool isRootBlock = false)
        {
            this.blockHeaderIndent = blockHeaderIndent;
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
                if (tLine == ":string")
                {
                    blocks.Add(new StringBlock(options, ref currentLine, blockHeaderIndent + tab));
                    currentLine--;
                    continue;
                }

                blocks.Add(  new Block(options, ref currentLine, blockHeaderIndent + tab)  );
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
        public readonly string str;

        public StringBlock(List<string> options, ref int currentLine, int blockHeaderIndent)
                     :base(options, ref currentLine, blockHeaderIndent)
        {
            str = "";
        }

        public override string Parse(List<string> options, ref int currentLine, int blockHeaderIndent, bool _ = false)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return str;
        }
    }
}