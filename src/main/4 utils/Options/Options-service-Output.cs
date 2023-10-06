// TODO: tests
using System.Diagnostics;
using System.Text;
// TODO: сделать опции для сервиса здесь и переписать получение опций в сервисе через этот класс
// TODO: сделать здесь локализацию
namespace VinKekFish_Utils.ProgramOptions;

public partial class Options_Service
{
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
}
