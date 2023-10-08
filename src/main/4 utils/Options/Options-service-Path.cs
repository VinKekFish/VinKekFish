// TODO: tests
using System.Diagnostics;
using System.Text;
// TODO: сделать здесь локализацию
namespace VinKekFish_Utils.ProgramOptions;

public partial class Options_Service
{
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
                throw new Options_Service_Exception($"In the '{getFullElementName()}' of service options must have 'random at start folder' element. Have no 'random at start folder' element");

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
