namespace vinkekfish;

using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using System.Threading.Tasks;

// code::docs:rQN6ZzeeepyOpOnTPKAT:

public partial class CascadeSponge_1t_20230905
{
    public class CascadeSpongeException: Exception
    {
        public CascadeSpongeException(string? message = null): base(message)
        {}
    }
}
