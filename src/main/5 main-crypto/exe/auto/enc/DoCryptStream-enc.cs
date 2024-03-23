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
using maincrypto.keccak;
using cryptoprime.VinKekFish;
using vinkekfish;

public partial class Main_PWD_2024_1
{
    public partial class EncryptDataStream: IDisposable
    {
        public static nint Align(nint size)
        {
            if (size <= 65536)
                return AlignUtils.AlignDegree(size, 4, 1024);

            return AlignUtils.Align(size, 65536, 65536);
        }

        public Record Encrypt()
        {
            var aLen = AlignedStream.len;
            var fLen = aLen + 16 * VinKekFishBase_etalonK1.BLOCK_SIZE * vkfOpt.K;   // Хеш после пункта 2.1
            fLen    += 2 * CascadeSponge_1t_20230905.MaxInputForKeccak * tall;      // Хеш каскадной губки после 2.3

            var r1 = Keccak_abstract.allocator.AllocMemory(fLen);
            var r2 = Keccak_abstract.allocator.AllocMemory(fLen);

            // Губка для шага 2.1 и специальная губка для инициализации VinKekFish
            var vkf1  = new VinKekFishBase_KN_20210525(vkfOpt.Rounds, vkfOpt.K, 1);
            var csc0  = new CascadeSponge_mt_20230930(cscOpt.StrengthInBytes);
            var csc2p = new CascadeSponge_mt_20230930(cscOpt.StrengthInBytes);  // Губка для перемешивания открытого текста на шаге 2.2
            var csc3  = new CascadeSponge_mt_20230930(cscOpt.StrengthInBytes);  // Губка для шага 2.3
            var vkf4  = new VinKekFishBase_KN_20210525(vkfOpt.Rounds, vkfOpt.K, 1); // Губки для шага 2.4
            var csc4  = new CascadeSponge_mt_20230930(cscOpt.StrengthInBytes);
            var vkf4w = new GetDataFromVinKekFishSponge(vkf4);
            var csc4w = new GetDataFromCascadeSponge(csc4);
            var sp4   = new GetDataByAdd();
            sp4.AddSponge(csc4w);
            sp4.AddSponge(vkf4w);

            var vkf5  = new VinKekFishBase_KN_20210525(vkfOpt.Rounds, vkfOpt.K, 1); // Губка для шага 2.5

            Parallel.Invoke
            (
                () =>
                {
                    csc0.initKeyAndOIV(Key0ICsc);

                    vkf1.Init1(vkfOpt.PreRounds, prngToInit: csc0);
                    vkf1.Init2(Key1Vkf);

                    vkf5.Init1(vkfOpt.PreRounds, prngToInit: csc0);
                    vkf5.Init2(Key5Vkf);
                },
                () =>
                {
                    csc2p.initKeyAndOIV(Key2PCsc);
                },
                () =>
                {
                    csc3.initKeyAndOIV(Key3Csc);
                },
                () =>
                {
                    csc4.initKeyAndOIV(Key4Csc);
                    vkf4.Init1(vkfOpt.PreRounds, prngToInit: csc4);
                    vkf4.Init2(Key4Vkf);
                }
            );

            return r1;
        }
    }
}
