using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cryptoprime
{
    public static class BitToBytes
    {
        public static readonly byte[] BitMask =
        {
            1,
            1 << 1,
            1 << 2,
            1 << 3,
            1 << 4,
            1 << 5,
            1 << 6,
            1 << 7,
        };

        public static bool getBit(byte[] array, ulong index)
        {
            var i = index >> 3;
            var s = (byte) (index & 0x07);
            var r = array[i];

            r >>= s;

            return (r & 1) != 0;
        }

        public static void setBit(byte[] array, ulong index)
        {
            var i = index >> 3;
            var s = (byte) (index & 0x07);

            array[i] |= BitMask[s];
        }

        public static void resetBit(byte[] array, ulong index)
        {
            var i = index >> 3;
            var s = (byte) (index & 0x07);

            array[i] &= (byte) ~BitMask[s];
        }
    }
}
