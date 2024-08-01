using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ::test:O0s1QcshQ7zCGVMMKZtf:

/*
 * Реализация Trheefish 1024 бита. Реализовано только шифрование
 * Это медленная safe-реализация
 * */
namespace cryptoprime
{
    // Медленная реализация Threefish 1024 бита
    public unsafe static class Threefish_slowly
    {
        public static ulong[] BytesToUlong(byte[] bt, ulong[]? result = null)
        {
            result ??= new ulong[bt.LongLength >> 3];

            fixed (byte  * b = bt)
            fixed (ulong * R = result)
            {
                byte * r = (byte *) R;
                var    l = checked( (nint) bt.LongLength );
                BytesBuilder.CopyTo(l, l, b, r);
            }

            return result;
        }

        public static byte[] UlongToBytes(ulong[] bt, byte[]? result = null)
        {
            result ??= new byte[bt.LongLength << 3];

            fixed (ulong * b = bt)
            fixed (byte  * R = result)
            {
                byte * B = (byte *) b;
                var    l = checked( (nint) bt.LongLength << 3 );
                BytesBuilder.CopyTo(l, l, B, R);
            }

            return result;
        }

        // Функция Mix[d, j](x0, x1) => (y0, y1)
        // y0 = (x0 + x1)
        // y1 = x1 rol RC[(d mod 8), j]
        // y1 = y1 xor y0

        public static void Mix(ref ulong a, ref ulong b, byte r)
        {
            a += b;
            b = b << r | b >> (64-r);   // rol b, r
            b ^= a;
        }
                                        /// <summary>Длина ключа в словах по 8-мь байтов</summary>
        public const int Nw     = 16;   /// <summary>Длина ключа в байтах</summary>
        public const int keyLen = 128;  /// <summary>Длина tweak в байтах</summary>
        public const int  twLen = 16;
        // Функция перестановок
        public static readonly byte[] Pi = {0, 9, 2, 13, 6, 11, 4, 15, 10, 7, 12, 3, 14, 5, 8, 1};
        // Rotation constants
        public static readonly byte[,] RC = 
        {
            {24, 13,  8, 47,  8, 17, 22, 37},
            {38, 19, 10, 55, 49, 18, 23, 52},
            {33,  4, 51, 13, 34, 41, 59, 17},
            { 5, 20, 48, 41, 47, 28, 16, 25},

            {41,  9, 37, 31, 12, 47, 44, 30},
            {16, 34, 56, 51,  4, 53, 42, 41},
            {31, 44, 47, 46, 19, 42, 44, 25},
            { 9, 48, 35, 52, 23, 31, 37, 20}
        };

        // C240 - constant, see page 11 (numbers in page, not in editor) of skein 1.3 specification
        public const ulong C240 = 0x1BD11BDAA9FC1A22;

        // d - round mod 8
        // Mixing and word permutation, see page 10 (numbers in page, not in editor)
        // (this is the round function)
        public static void MixRound(ulong[] e, ulong[] result, byte d)
        {
            for (int i = 0; i < Nw; i += 2)
            {
                Mix(ref e[i+0], ref e[i+1], RC[d & 0x07, i >> 1]);
            }

            // In e - f words (page 10 of skein 1.3)
            for (int i = 0; i < Nw; i += 1)
            {
                result[i] = e[Pi[i]];
            }
        }

        public static ulong[] Encrypt(ulong[] key, ulong[] tweak, ulong[] text)
        {
            ulong[] e  = (ulong[]) text.Clone();
            ulong[] er = new ulong[Nw];

            // 3.3.2. Key Shedule
            var keyNw = C240;
            for (int i = 0; i < Nw; i++)
                keyNw ^= key[i];

            var tweak2 = tweak[0] ^ tweak[1];

            // Rounds
            ulong[] subkey = new ulong[Nw];
            for (byte round = 0; round < 80; round++)
            {
                if ((round & 3) == 0)
                {
                    CalcSubkeys(subkey, key, keyNw, tweak, tweak2, round);
                    for (int i = 0; i < Nw; i++)
                        e[i] = e[i] + subkey[i];
                }

                MixRound(e, er, round);
                for (int i = 0; i < Nw; i++)
                    e[i] = er[i];
            }

            CalcSubkeys(subkey, key, keyNw, tweak, tweak2, 80);
            for (int i = 0; i < Nw; i++)
                e[i] = e[i] + subkey[i];

            return e;
        }

        // key shedule, page 12 (numbers in page, not in editor)
        public static void CalcSubkeys(ulong[] subkey, ulong[] key, ulong keyNw, ulong[] tweak, ulong tweak2, byte round)
        {
            var s = round >> 2;
            int i, index;
            // k[s, i]. s = round div 4. i = index in subkey
            for (i = 0; i <= Nw-4; i++)
            {
                index = s + i;
                // Осуществляем операцию mod (Nw + 1)
                if (index > Nw)
                    index -= Nw + 1;

                subkey[i] = index == Nw ? keyNw : key[index];
            }

            i = (Nw - 3);
            index = s + i;
            // Осуществляем операцию mod (Nw + 1)
            while (index > Nw)
                index -= Nw + 1;

            subkey[i] = index == Nw ? keyNw : key[index];
            int s3 = s % 3;
            if (s3 == 0 || s3 == 1)
                subkey[i] += tweak[s3];
            else
                subkey[i] += tweak2;

            i = (Nw - 2);
            index = s + i;
            // Осуществляем операцию mod (Nw + 1)
            while (index > Nw)
                index -= Nw + 1;

            subkey[i] = index == Nw ? keyNw : key[index];
            s3 = (s + 1) % 3;
            if (s3 == 0 || s3 == 1)
                subkey[i] += tweak[s3];
            else
                subkey[i] += tweak2;

            i = (Nw - 1);
            index = s + i;
            // Осуществляем операцию mod (Nw + 1)
            while (index > Nw)
                index -= Nw + 1;

            subkey[i] = index == Nw ? keyNw : key[index];
            subkey[i] += (ulong) s;
        }
    }
}
