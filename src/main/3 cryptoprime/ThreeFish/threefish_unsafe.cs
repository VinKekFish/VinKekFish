﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Реализация Trheefish 1024 бита. Реализовано только шифрование
 * */
namespace cryptoprime
{
    // Threefish 1024 bits realization with optimization
    // Реализация Threefish 1024 бита с оптимизацией
    public unsafe class Threefish1024
    {
        public readonly ulong[] tweak = new ulong[3];
        public readonly ulong[] key   = new ulong[threefish_slowly.Nw + 1];
        public Threefish1024(byte[] Key, byte[] Tweak)
        {
            fixed (byte  * k  = Key,      t  = Tweak)
            fixed (ulong * tk = this.key, tt = this.tweak)
            {
                BytesBuilder.CopyTo(Key  .Length, Key  .Length, k, (byte *) tk);
                BytesBuilder.CopyTo(Tweak.Length, Tweak.Length, t, (byte *) tt);
//                Prepare((ulong *) k, (ulong *) t, tk, tt);

                // Вычисление расширения ключа и tweak
                tk[16] = threefish_slowly.C240;
                for (int i = 0; i < threefish_slowly.Nw; i++)
                    tk[16] ^= tk[i];

                tt[2] = tt[0] ^ tt[1];
            }
        }
/*
        public const int klen = 128;
        public const int tlen = 16;
        public static void Prepare(ulong * key, ulong * tweak, ulong * keye, ulong * tweake)
        {
            BytesBuilder.CopyTo(klen, klen, (byte *) key,   (byte *) keye);
            BytesBuilder.CopyTo(tlen, tlen, (byte *) tweak, (byte *) tweake);

            // Вычисление расширения ключа и tweak
            keye[threefish_slowly.Nw] = threefish_slowly.C240;
            for (int i = 0; i < threefish_slowly.Nw; i++)
                keye[threefish_slowly.Nw] ^= keye[i];

            tweake[2] = tweake[0] ^ tweake[1];
        }*/
    }
}