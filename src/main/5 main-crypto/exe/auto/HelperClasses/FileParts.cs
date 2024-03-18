// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Reflection;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

/// <summary>Представляет часть файла, которая может содержать другие части файла.</summary>
public unsafe partial class FileParts
{
    public readonly List<FileParts> innerParts = new List<FileParts>();

    public nint StartAddress = 0;

    public virtual nint fullLen
    {
        get
        {
            nint result = 0;
            foreach (var part in innerParts)
            {
                result += part.fullLen;
            }

            return result;
        }
    }

    public virtual nint selfLen => 0;
}
