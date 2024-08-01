// TODO: tests
using System.Diagnostics;
using System.Text;
// TODO: сделать опции для сервиса здесь и переписать получение опций в сервисе через этот класс
// TODO: сделать здесь локализацию
namespace VinKekFish_Utils.ProgramOptions;

public partial class Options_Service
{
    public readonly Options rawOptionsFile;
    public readonly Root    root;

    /// <summary>Создаёт объект настроек, полученных из файла</summary>
    /// <param name="options">Парсер опций. new Options(fileString), где fileString - содержимое файла настроек</param>
    /// <param name="doNotOutputWarningsToConsole">Если false, то выводит на Console.Error предупреждения</param>
    public Options_Service(Options options, bool doNotOutputWarningsToConsole = false)
    {
        this.rawOptionsFile = options;
        root = Analize();

        root.Check();

        // Выводим предупреждения парсера на экран
        if (!doNotOutputWarningsToConsole)
            Console.Error.WriteLine(root.warns.ToString());
    }

    protected virtual Root Analize()
    {
        return new Root(rawOptionsFile.options.blocks, new Options.Block());
    }

    public override string ToString()
    {
        return rawOptionsFile.ToString();
    }

    public class Options_Service_Exception: Exception
    {
        public Options_Service_Exception(string? message): base(message)
        {}
    }

    public abstract class Element
    {
        public readonly List<Options.Block> blocks;
        public readonly Element? parent;
        public virtual  Element? Parent => parent;

        public readonly List<Element> elements = new(4);
        public readonly Options.Block thisBlock;

        public Element(Element? parent, List<Options.Block> blocks, Options.Block thisBlock)
        {
            this.blocks    = blocks;
            this.parent    = parent;
            this.thisBlock = thisBlock;

            parent?.elements.Add(this);

            this.Select();
        }

        public virtual void Select()
        {
            foreach (var b in blocks)
                SelectBlock(b, GetCanonicalName(b));
        }

        public virtual void Check()
        {
            foreach (var e in elements)
                e.Check();
        }

        public static string GetCanonicalName(Options.Block b)
        {
            return b.Name.ToLowerInvariant().Trim();
        }

        /// <summary>Проходит по дочерним блокам. Вызывается прямо в конструкторе, поэтому этот вызов происходит до вызова дочерних конструкторов.</summary>
        /// <param name="block">Подчинённый блок опций для парсинга</param>
        /// <param name="canonicalName">Каноническое имя подчинённого блока block</param>
        public abstract void SelectBlock (Options.Block block, string canonicalName);

        public virtual string GetFullElementName()
        {
            if (this.Parent == null)
                return "";

            var sb = new StringBuilder();
            if (this.Parent != null)
                sb.Append(this.Parent.GetFullElementName());

            sb.Append("." + this.thisBlock.Name);

            return sb.ToString();
        }

        public virtual Root? GetRoot()
        {
            if (this.Parent == null)
                return this as Root;

            return this.Parent.GetRoot();
        }
    }

    public class Root: Element
    {
        public Root(List<Options.Block> blocks, Options.Block thisBlock): base(null, blocks, thisBlock)
        {}

        public Output?       output;
        public Input?        input;
        public Path?         Path;
        public OptionsBlock? Options;

        /// <summary>Функция, разбирающая блоки из парсера на конкретные блоки настроек</summary>
        /// <param name="block">Подблок из парсера</param>
        /// <param name="canonicalName">Каноническое имя блока: пробелы и табуляции удалены, регистр преобразован в нижний</param>
        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            switch(canonicalName)
            {
                case "input"   : input   = new Input        (this, block.blocks, block); break;
                case "output"  : output  = new Output       (this, block.blocks, block); break;
                case "path"    : Path    = new Path         (this, block.blocks, block); break;
                case "options" : Options = new OptionsBlock (this, block.blocks, block); break;
                default:       throw new Options_Service_Exception($"At line {1+block.startLine} in the root of service options found the unknown element '{block.Name}'. Acceptable is 'Output', 'Input', 'Path'");
            }
        }

        public override void Check()
        {
            if (output == null)
                throw new Options_Service_Exception($"In the root of service options must have 'Output', 'Input', 'Path' elements. Have no 'output' element");
            if (input == null)
                throw new Options_Service_Exception($"In the root of service options must have 'Output', 'Input', 'Path' elements. Have no 'input' element");
            if (Path == null)
                throw new Options_Service_Exception($"In the root of service options must have 'Output', 'Input', 'Path' elements. Have no 'Path' element");

            base.Check();
        }

        public class Warning
        {
            public required string Message { get; init; }

            public override string ToString() => Message;
        }

        public class Warnings
        {
            protected List<Warning> warnings = new();

            public void AddWarning(string message)
            {
                lock (warnings)
                    warnings.Add(new Warning() {Message = message});
            }

            public override string ToString()
            {
                var sb = new StringBuilder();

                foreach (var warn in warnings)
                {
                    sb.AppendLine(warn.ToString());
                }

                return sb.ToString();
            }

            public void Clear()
            {
                warnings.Clear();
            }
        }

        public readonly Warnings warns = new();
    }
}
