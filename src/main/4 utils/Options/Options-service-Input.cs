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

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            Console.Error.WriteLine("input block not implemented");
        }
    }
}
