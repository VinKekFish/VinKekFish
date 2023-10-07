// TODO: tests
using System.Diagnostics;
using System.Text;
// TODO: сделать здесь локализацию
namespace VinKekFish_Utils.ProgramOptions;

public partial class Options_Service
{
    public class Input: Element
    {
        public Input(Root parent, List<Options.Block> blocks, Options.Block thisBlock): base(parent, blocks, thisBlock)
        {}

        public Entropy? entropy;

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            switch(canonicalName)
            {
                case "entropy": entropy = new Entropy(this, block.blocks, block); break;
                default:        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'entropy'");
            }
        }

        public override void Check()
        {
            if (entropy == null)
                throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'entropy' element. No have 'entropy' element");

            base.Check();
        }

        public class Entropy : Element
        {
            public Entropy(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
            {}

            public OS? os;

            public override void SelectBlock(Options.Block block, string canonicalName)
            {
                switch(canonicalName)
                {
                    case "os": os = new OS(this, block.blocks, block); break;
                    default:        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'OS'");
                }
            }

            public override void Check()
            {
                if (os == null)
                    // throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'entropy' element. No have 'entropy' element");
                    this.getRoot()!.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options was not found 'OS' element");

                base.Check();
            }

            public class OS : Element
            {
                public OS(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public readonly List<InputFileElement> random = new List<InputFileElement>();

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    var rnd = new InputFileElement(this, block.blocks, block);
                    random.Add(rnd);
                }

                public override void Check()
                {
                    if (random.Count <= 0)
                        this.getRoot()!.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options was found no one element");

                    base.Check();
                }
            }

            public abstract class InputElemement: Element
            {
                public InputElemement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    throw new Options_Service_Exception($"Fatal error: InputElemement must not be called");
                }

                public static InputElemement getInputElemement(Element parent, Options.Block block, string canonicalName)
                {
                    switch(canonicalName)
                    {
                        case "file":    return new InputFileElement(parent, block.blocks, block);
                        case "cmd" :    return new InputFileElement(parent, block.blocks, block); // TODO: !!!
                        default:        throw  new Options_Service_Exception($"At line {1+block.startLine} in the '{parent.getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'file', 'cmd'");
                    }
                }
            }

            /// <summary>Представляет источник энтропии, являющийся файлов (или совместимым с ним устройством)</summary>
            public class InputFileElement: InputElemement
            {
                public InputFileElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    switch(canonicalName)
                    {
                        // case "os": os = new OS(this, block.blocks, block); break;
                        default:        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'OS'");
                    }
                }

                public override void Check()
                {
                    base.Check();
                }
            }
        }
    }
}
