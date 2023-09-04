// TODO: tests
using System.Text;
// TODO: сделать опции для сервиса здесь и переписать получение опций в сервисе через этот класс
namespace VinKekFish_Utils.Options;

public class Options_Service
{
    public readonly Options options;
    public readonly Root    root;
    public Options_Service(Options options)
    {
        this.options = options;
        root = Analize();
    }

    protected virtual Root Analize()
    {
        return new Root(options.options.blocks);
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

        public Element(Element? parent, List<Options.Block> blocks)
        {
            this.blocks = blocks;
            this.parent = parent;

            Select();
        }

        public virtual void Select()
        {
            foreach (var b in blocks)
                SelectBlock(b, getCanonicalName(b));
        }

        public static string getCanonicalName(Options.Block b)
        {
            return b.Name.ToLowerInvariant().Trim();
        }

        public abstract void SelectBlock (Options.Block block, string canonicalName);
    }

    public class Root: Element
    {
        public Root(List<Options.Block> blocks): base(null, blocks)
        {}

        public Output? output;
        public Input?  input;

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            Element e = canonicalName switch
            {
                "input"  => new Input (this, block.blocks),
                "output" => new Output(this, block.blocks),
                _ => throw new Options_Service_Exception($"At line {block.startLine} in the root of service options found the unknown element '{block.Name}'. Acceptable is 'Output' or 'Input'")
            };
        }
    }

    public class Output: Element
    {
        public override  Root? Parent => parent as Root;
        public Output(Root parent, List<Options.Block> blocks): base(parent, blocks)
        {}

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            Element e = canonicalName switch
            {
                "random"  => new Random(this, block.blocks),
                _ => throw new Options_Service_Exception($"At line {block.startLine} in the output element found the unknown element '{block.Name}'. Acceptable is 'random'")
            };
        }

        public Random.UnixStream.Path? out_random;

        public class Random: Element
        {
            public override  Output? Parent => parent as Output;
            public Random(Output? parent, List<Options.Block> blocks) : base(parent, blocks)
            {
            }

            public override void SelectBlock(Options.Block block, string canonicalName)
            {
                Element e = canonicalName switch
                {
                    "unix stream"  => new UnixStream(this, block.blocks),
                    _ => throw new Options_Service_Exception($"At line {block.startLine} in the output.random element found the unknown element '{block.Name}'. Acceptable is 'unix stream'")
                };
            }

            public class UnixStream : Element
            {
                public override  Random? Parent => parent as Random;
                public UnixStream(Random? parent, List<Options.Block> blocks) : base(parent, blocks)
                {
                }

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    Element e = canonicalName switch
                    {
                        "path"  => new Path(this, block.blocks),
                        _ => throw new Options_Service_Exception($"At line {block.startLine} in the 'output.random.unix stream' element found the unknown element '{block.Name}'. Acceptable is 'path'")
                    };
                }

                public class Path : Element
                {
                    public DirectoryInfo? dir;
                    public FileInfo?      file;
                    public override  UnixStream? Parent => parent as UnixStream;
                    public Path(UnixStream? parent, List<Options.Block> blocks) : base(parent, blocks)
                    {}

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        this!.Parent!.Parent!.Parent!.out_random = this;

                        dir  = new DirectoryInfo(block.Name);
                        file = new FileInfo(System.IO.Path.Combine(dir.FullName, "random"));
                    }
                }
            }
        }
    }

    public class Input: Element
    {
        public Input(Root parent, List<Options.Block> blocks): base(parent, blocks)
        {}

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            Console.Error.WriteLine("input block not implemented");
        }
    }
}
