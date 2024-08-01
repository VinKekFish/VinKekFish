// TODO: tests
using System.Diagnostics;
using System.Text;
// TODO: сделать здесь локализацию
namespace VinKekFish_Utils.ProgramOptions;

public partial class Options_Service
{
    public class OptionsBlock: Element
    {
        public OptionsBlock(Root parent, List<Options.Block> blocks, Options.Block thisBlock): base(parent, blocks, thisBlock)
        {}

        public bool doLogEveryInputEntropyToSponge = false;

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            doLogEveryInputEntropyToSponge = canonicalName switch
            {
                "do log every input entropy to sponge" => true,
                _ => throw new Options_Service_Exception($"At line {1 + block.startLine} in the root of service options found the unknown element '{block.Name}'. Acceptable is 'do log every input entropy to sponge'"),
            };
        }

        public override void Check()
        {
            base.Check();
        }
    }
}
