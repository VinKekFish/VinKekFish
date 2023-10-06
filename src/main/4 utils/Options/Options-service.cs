// TODO: tests
using System.Diagnostics;
using System.Text;
// TODO: сделать опции для сервиса здесь и переписать получение опций в сервисе через этот класс
// TODO: сделать здесь локализацию
namespace VinKekFish_Utils.ProgramOptions;

public class Options_Service
{
    public readonly Options options;
    public readonly Root    root;
    public Options_Service(Options options)
    {
        this.options = options;
        root = Analize();

        root.Check();
    }

    protected virtual Root Analize()
    {
        return new Root(options.options.blocks, new Options.Block());
    }

    public override string ToString()
    {
        return options.ToString();
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

        public readonly List<Element> elements = new List<Element>(4);
        public readonly Options.Block thisBlock;

        public Element(Element? parent, List<Options.Block> blocks, Options.Block thisBlock)
        {
            this.blocks    = blocks;
            this.parent    = parent;
            this.thisBlock = thisBlock;

            parent?.elements.Add(this);

            Select();
        }

        public virtual void Select()
        {
            foreach (var b in blocks)
                SelectBlock(b, getCanonicalName(b));
        }

        public virtual void Check()
        {
            foreach (var e in elements)
                e.Check();
        }

        public static string getCanonicalName(Options.Block b)
        {
            return b.Name.ToLowerInvariant().Trim();
        }

        public abstract void SelectBlock (Options.Block block, string canonicalName);

        public virtual string getFullElementName()
        {
            if (this.Parent == null)
                return "";

            var sb = new StringBuilder();
            if (this.Parent != null)
                sb.Append(this.Parent.getFullElementName());

            sb.Append("." + this.thisBlock.Name);

            return sb.ToString();
        }
    }

    public class Root: Element
    {
        public Root(List<Options.Block> blocks, Options.Block thisBlock): base(null, blocks, thisBlock)
        {}

        public Output? output;
        public Input?  input;
        public Path?   Path;

        /// <summary>Функция, разбирающая блоки из парсера на конкретные блоки настроек</summary>
        /// <param name="block">Подблок из парсера</param>
        /// <param name="canonicalName">Каноническое имя блока: пробелы и табуляции удалены, регистр преобразован в нижний</param>
        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            switch(canonicalName)
            {
                case "input" : input  = new Input (this, block.blocks, block); break;
                case "output": output = new Output(this, block.blocks, block); break;
                case "path"  : Path   = new Path  (this, block.blocks, block); break;
                default:       throw new Options_Service_Exception($"At line {1+block.startLine} in the root of service options found the unknown element '{block.Name}'. Acceptable is 'Output', 'Input', 'Path'");
            }
        }

        public override void Check()
        {
            if (output == null)
                throw new Options_Service_Exception($"In the root of service options must have 'Output', 'Input', 'Path' elements. No have 'output' element");
            if (input == null)
                throw new Options_Service_Exception($"In the root of service options must have 'Output', 'Input', 'Path' elements. No have 'input' element");
            if (Path == null)
                throw new Options_Service_Exception($"In the root of service options must have 'Output', 'Input', 'Path' elements. No have 'Path' element");

            base.Check();
        }
    }

    public class Output: Element
    {
        public override  Root? Parent => parent as Root;
        public Output(Root parent, List<Options.Block> blocks, Options.Block thisBlock): base(parent, blocks, thisBlock)
        {}

        public Random? random;

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            switch(canonicalName)
            {
                case "random": random = new Random(this, block.blocks, block); break;
                default:       throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'random'");
            }
        }

        public override void Check()
        {
            if (random == null)
                throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'random' element. No have 'random' element");

            base.Check();
        }

        public Random.UnixStream.Path? out_random;

        public class Random: Element
        {
            public override  Output? Parent => parent as Output;
            public Random(Output? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
            {
            }

            public UnixStream? unixStream;

            public override void SelectBlock(Options.Block block, string canonicalName)
            {
                Element e = canonicalName switch
                {
                    "unix stream"  => unixStream = new UnixStream(this, block.blocks, block),
                    _ => throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'unix stream'")
                };
            }

            public override void Check()
            {
                if (unixStream == null)
                    throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options must have 'unix stream' element. No have 'unix stream' element");

                base.Check();
            }

            public class UnixStream : Element
            {
                public override  Random? Parent => parent as Random;
                public UnixStream(Random? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {
                }

                public Path? path;

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    Element e = canonicalName switch
                    {
                        "path"  => path = new Path(this, block.blocks, block),
                        _ => throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'path'")
                    };
                }

                public override void Check()
                {
                    if (path == null)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options must have 'path' element. No have 'path' element");

                    base.Check();
                }

                public class Path : Element
                {
                    public DirectoryInfo? dir;
                    public FileInfo?      file;
                    public override  UnixStream? Parent => parent as UnixStream;
                    public Path(UnixStream? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {}

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        this!.Parent!.Parent!.Parent!.out_random = this;

                        dir  = new DirectoryInfo(block.Name);
                        file = new FileInfo(System.IO.Path.Combine(dir.FullName, "random"));
                    }

                    public override void Check()
                    {
                        if (file == null)
                            throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options must have a string with the path value. No found path value");

                        base.Check();
                    }
                }
            }
        }
    }

    public class Input: Element
    {
        public Input(Root parent, List<Options.Block> blocks, Options.Block thisBlock): base(parent, blocks, thisBlock)
        {}

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            Console.Error.WriteLine("input block not implemented");
        }
    }

    public class Path: Element
    {
        public Path(Root parent, List<Options.Block> blocks, Options.Block thisBlock): base(parent, blocks, thisBlock)
        {}

        public RandomAtStartFolder? randomAtStartFolder;

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            switch(canonicalName)
            {
                case "random at start folder": randomAtStartFolder = new RandomAtStartFolder(this, block.blocks, block); break;
                default:                       throw new Options_Service_Exception($"At line {1+block.startLine} in the root of service options found the unknown element '{block.Name}'. Acceptable is 'Output', 'Input', 'Path'");
            }
        }

        public override void Check()
        {
            if (randomAtStartFolder == null)
                throw new Options_Service_Exception($"In the '{getFullElementName()}' of service options must have 'random at start folder' element. No have 'random at start folder' element");

            base.Check();
        }

        public class RandomAtStartFolder: Element
        {
            public RandomAtStartFolder(Path parent, List<Options.Block> blocks, Options.Block thisBlock): base(parent, blocks, thisBlock)
            {}

            public DirectoryInfo? dir;

            public override void SelectBlock(Options.Block block, string canonicalName)
            {
                dir = new DirectoryInfo(block.Name);

                if (!dir.Exists)
                    dir.Create();
            }

            public override void Check()
            {
                if (dir == null)
                    throw new Options_Service_Exception($"In the '{getFullElementName()}' of service options require a value. The value not found");

                base.Check();
            }
        }
    }
}
