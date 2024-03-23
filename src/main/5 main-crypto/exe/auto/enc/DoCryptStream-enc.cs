// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using static AutoCrypt;
using static cryptoprime.BytesBuilderForPointers;
using VinKekFish_Utils;

public partial class Main_PWD_2024_1
{
    public partial class DoCryptDataStream: IDisposable
    {
        public static nint Align(nint size)
        {
            if (size <= 65536)
                return AlignUtils.AlignDegree(size, 4, 1024);

            return AlignUtils.Align(size, 65536, 65536);
        }
    }
}