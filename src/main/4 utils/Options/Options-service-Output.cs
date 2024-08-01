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
            random = canonicalName switch
            {
                "random" => new Random(this, block.blocks, block),
                _ => throw new Options_Service_Exception($"At line {1 + block.startLine} in the '{GetFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'random'"),
            };
        }

        public override void Check()
        {
            if (random == null)
                throw new Options_Service_Exception($"In the '{GetFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'random' element. Have no 'random' element");

            base.Check();
        }

                                                                                /// <summary>Определяет, в какой директории будет находится файл "random" с выходом программы</summary>
        public Random.UnixStream.Path? out_random;

        public class Random: Element
        {
            public override  Output? Parent => parent as Output;
            public Random(Output? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
            {
            }

            public UnixStream?      unixStream;
            public CharacterDevice? charDevice;

            public override void SelectBlock(Options.Block block, string canonicalName)
            {
                #pragma warning disable IDE0059
                Element e = canonicalName switch
                {
                    "unix stream"              => unixStream = new UnixStream     (this, block.blocks, block),
                    "character device in /dev" => charDevice = new CharacterDevice(this, block.blocks, block),
                    _ => throw new Options_Service_Exception($"At line {1+block.startLine} in the '{GetFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'unix stream'")
                };
                #pragma warning restore IDE0059
            }

            public override void Check()
            {
                if (unixStream == null)
                    throw new Options_Service_Exception($"In the '{GetFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options must have 'unix stream' element. Have no 'unix stream' element");
                if (charDevice == null)
                    this.GetRoot()!.warns.AddWarning($"Warning: In the '{GetFullElementName()}' element (at line {1+thisBlock.startLine}) of the service options was not found a 'character device in /dev' element");

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
                    #pragma warning disable IDE0059
                    Element e = canonicalName switch
                    {
                        "path"  => path = new Path(this, block.blocks, block),
                        _ => throw new Options_Service_Exception($"At line {1+block.startLine} in the '{GetFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'path'")
                    };
                    #pragma warning restore IDE0059
                }

                public override void Check()
                {
                    if (path == null)
                        throw new Options_Service_Exception($"In the '{GetFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options must have 'path' element. Have no 'path' element");

                    base.Check();
                }

                public class Path : Element
                {                                                                   /// <summary>Определяет директорию для выхода потоков с псевдослучайными криптостойкими данными</summary>
                    public DirectoryInfo? dir;                                      /// <summary>Путь к файлу random</summary>
                    public FileInfo?      file;
                    public FileInfo?      fileForParams;
                    public override  UnixStream? Parent => parent as UnixStream;
                    public Path(UnixStream? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {}

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        this!.Parent!.Parent!.Parent!.out_random = this;

                        dir    = new DirectoryInfo(block.Name);
                        file   = new FileInfo(System.IO.Path.Combine(dir.FullName, "random"));

                        fileForParams = new FileInfo(System.IO.Path.Combine(dir.FullName, "params"));
                    }

                    public override void Check()
                    {
                        if (file == null)
                            throw new Options_Service_Exception($"In the '{GetFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options must have a string with the path value. No found path value");

                        base.Check();
                    }
                }
            }

            public class CharacterDevice : Element
            {
                public override  Random? Parent => parent as Random;
                public CharacterDevice(Random? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public string? path;

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    if (path != null)
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{GetFullElementName()}' element found two or more elements. Only one element required (/dev/value).");

                    path = block.Name;
                    if (path.StartsWith("/dev/"))
                    {
                        path = path.Substring("/dev/".Length);
                    }
                }

                public override void Check()
                {
                    if (path == null)
                        throw new Options_Service_Exception($"In the '{GetFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options must have 'path' element. Have no 'path' element");

                    base.Check();
                }
            }
        }
    }
}
