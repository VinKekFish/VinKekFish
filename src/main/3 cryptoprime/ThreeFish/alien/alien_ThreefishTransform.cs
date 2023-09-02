// Этот ЧУЖОЙ код. Используется для тестирования
// Взят из следующего репозитория https://github.com/nitrocaster/SkeinFish/
// Копия (кажется, это более оригинальная) есть в репозитории https://github.com/andrewkrug/skeinfish/

// ::test:O0s1QcshQ7zCGVMMKZtf:

/*
Copyright (c) 2010 Alberto Fajardo
Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:
The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
Bug fixes:
Copyright (c) 2015 Pavel Kovalenko
Same licence, etc. applies.
*/

using System;

namespace alien_SkeinFish
{
    internal abstract class ThreefishCipher
    {
        protected const ulong KeyScheduleConst = 0x1BD11BDAA9FC1A22;
        protected const int ExpandedTweakSize = 3;

        protected ulong[]? ExpandedKey;
        protected ulong[]? ExpandedTweak;

        protected ThreefishCipher()
        {
            ExpandedTweak = new ulong[ExpandedTweakSize];
        }

        protected static ulong RotateLeft64(ulong v, int b)
        {
            return (v << b) | (v >> (64 - b));
        }

        protected static ulong RotateRight64(ulong v, int b)
        {
            return (v >> b) | (v << (64 - b));
        }

        protected static void Mix(ref ulong a, ref ulong b, int r)
        {
            a += b;
            b = RotateLeft64(b, r) ^ a;
        }

        protected static void Mix(ref ulong a, ref ulong b, int r, ulong k0, ulong k1)
        {
            b += k1;
            a += b + k0;
            b = RotateLeft64(b, r) ^ a;
        }

        protected static void UnMix(ref ulong a, ref ulong b, int r)
        {
            b = RotateRight64(b ^ a, r);
            a -= b;
        }

        protected static void UnMix(ref ulong a, ref ulong b, int r, ulong k0, ulong k1)
        {
            b = RotateRight64(b ^ a, r);
            a -= b + k0;
            b -= k1;
        }

        public void SetTweak(ulong[] tweak)
        {
            if (ExpandedTweak == null) throw new ArgumentNullException();

            ExpandedTweak[0] = tweak[0];
            ExpandedTweak[1] = tweak[1];
            ExpandedTweak[2] = tweak[0] ^ tweak[1];
        }

        public void SetKey(ulong[] key)
        {
            if (ExpandedKey == null) throw new ArgumentNullException();

            int i;
            ulong parity = KeyScheduleConst;

            for (i = 0; i < ExpandedKey.Length - 1; i++)
            {
                ExpandedKey[i] = key[i];
                parity ^= key[i];
            }

            ExpandedKey[i] = parity;
        }

        public static ThreefishCipher CreateCipher(int stateSize)
        {
            switch (stateSize)
            {
                case 1024: return new Threefish1024();
            }

            throw new NotImplementedException();
        }

        abstract public void Encrypt(ulong[] input, ulong[] output);
        abstract public void Decrypt(ulong[] input, ulong[] output);
    }

    internal class Threefish1024 : ThreefishCipher
    {
        const int CIPHER_SIZE = 1024;
        const int CIPHER_QWORDS = CIPHER_SIZE / 64;
        const int EXPANDED_KEY_SIZE = CIPHER_QWORDS + 1;

        public Threefish1024()
        {
            // Create the expanded key array
            ExpandedKey = new ulong[EXPANDED_KEY_SIZE];
            ExpandedKey[EXPANDED_KEY_SIZE - 1] = KeyScheduleConst;
        }

        public override void Encrypt(ulong[] input, ulong[] output)
        {
            if (ExpandedKey   == null) throw new ArgumentNullException();
            if (ExpandedTweak == null) throw new ArgumentNullException();

            // Cache the block, key, and tweak
            ulong b0 = input[0], b1 = input[1],
                  b2 = input[2], b3 = input[3],
                  b4 = input[4], b5 = input[5],
                  b6 = input[6], b7 = input[7],
                  b8 = input[8], b9 = input[9],
                  b10 = input[10], b11 = input[11],
                  b12 = input[12], b13 = input[13],
                  b14 = input[14], b15 = input[15];
            ulong k0 = ExpandedKey[0], k1 = ExpandedKey[1],
                  k2 = ExpandedKey[2], k3 = ExpandedKey[3],
                  k4 = ExpandedKey[4], k5 = ExpandedKey[5],
                  k6 = ExpandedKey[6], k7 = ExpandedKey[7],
                  k8 = ExpandedKey[8], k9 = ExpandedKey[9],
                  k10 = ExpandedKey[10], k11 = ExpandedKey[11],
                  k12 = ExpandedKey[12], k13 = ExpandedKey[13],
                  k14 = ExpandedKey[14], k15 = ExpandedKey[15],
                  k16 = ExpandedKey[16];
            ulong t0 = ExpandedTweak[0], t1 = ExpandedTweak[1],
                  t2 = ExpandedTweak[2];


            Mix(ref b0, ref b1, 24, k0, k1);
            Mix(ref b2, ref b3, 13, k2, k3);
            Mix(ref b4, ref b5, 8, k4, k5);
            Mix(ref b6, ref b7, 47, k6, k7);
            Mix(ref b8, ref b9, 8, k8, k9);
            Mix(ref b10, ref b11, 17, k10, k11);
            Mix(ref b12, ref b13, 22, k12, k13 + t0);
            Mix(ref b14, ref b15, 37, k14 + t1, k15);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k1, k2);
            Mix(ref b2, ref b3, 9, k3, k4);
            Mix(ref b4, ref b5, 37, k5, k6);
            Mix(ref b6, ref b7, 31, k7, k8);
            Mix(ref b8, ref b9, 12, k9, k10);
            Mix(ref b10, ref b11, 47, k11, k12);
            Mix(ref b12, ref b13, 44, k13, k14 + t1);
            Mix(ref b14, ref b15, 30, k15 + t2, k16 + 1);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k2, k3);
            Mix(ref b2, ref b3, 13, k4, k5);
            Mix(ref b4, ref b5, 8, k6, k7);
            Mix(ref b6, ref b7, 47, k8, k9);
            Mix(ref b8, ref b9, 8, k10, k11);
            Mix(ref b10, ref b11, 17, k12, k13);
            Mix(ref b12, ref b13, 22, k14, k15 + t2);
            Mix(ref b14, ref b15, 37, k16 + t0, k0 + 2);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k3, k4);
            Mix(ref b2, ref b3, 9, k5, k6);
            Mix(ref b4, ref b5, 37, k7, k8);
            Mix(ref b6, ref b7, 31, k9, k10);
            Mix(ref b8, ref b9, 12, k11, k12);
            Mix(ref b10, ref b11, 47, k13, k14);
            Mix(ref b12, ref b13, 44, k15, k16 + t0);
            Mix(ref b14, ref b15, 30, k0 + t1, k1 + 3);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k4, k5);
            Mix(ref b2, ref b3, 13, k6, k7);
            Mix(ref b4, ref b5, 8, k8, k9);
            Mix(ref b6, ref b7, 47, k10, k11);
            Mix(ref b8, ref b9, 8, k12, k13);
            Mix(ref b10, ref b11, 17, k14, k15);
            Mix(ref b12, ref b13, 22, k16, k0 + t1);
            Mix(ref b14, ref b15, 37, k1 + t2, k2 + 4);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k5, k6);
            Mix(ref b2, ref b3, 9, k7, k8);
            Mix(ref b4, ref b5, 37, k9, k10);
            Mix(ref b6, ref b7, 31, k11, k12);
            Mix(ref b8, ref b9, 12, k13, k14);
            Mix(ref b10, ref b11, 47, k15, k16);
            Mix(ref b12, ref b13, 44, k0, k1 + t2);
            Mix(ref b14, ref b15, 30, k2 + t0, k3 + 5);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k6, k7);
            Mix(ref b2, ref b3, 13, k8, k9);
            Mix(ref b4, ref b5, 8, k10, k11);
            Mix(ref b6, ref b7, 47, k12, k13);
            Mix(ref b8, ref b9, 8, k14, k15);
            Mix(ref b10, ref b11, 17, k16, k0);
            Mix(ref b12, ref b13, 22, k1, k2 + t0);
            Mix(ref b14, ref b15, 37, k3 + t1, k4 + 6);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k7, k8);
            Mix(ref b2, ref b3, 9, k9, k10);
            Mix(ref b4, ref b5, 37, k11, k12);
            Mix(ref b6, ref b7, 31, k13, k14);
            Mix(ref b8, ref b9, 12, k15, k16);
            Mix(ref b10, ref b11, 47, k0, k1);
            Mix(ref b12, ref b13, 44, k2, k3 + t1);
            Mix(ref b14, ref b15, 30, k4 + t2, k5 + 7);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k8, k9);
            Mix(ref b2, ref b3, 13, k10, k11);
            Mix(ref b4, ref b5, 8, k12, k13);
            Mix(ref b6, ref b7, 47, k14, k15);
            Mix(ref b8, ref b9, 8, k16, k0);
            Mix(ref b10, ref b11, 17, k1, k2);
            Mix(ref b12, ref b13, 22, k3, k4 + t2);
            Mix(ref b14, ref b15, 37, k5 + t0, k6 + 8);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k9, k10);
            Mix(ref b2, ref b3, 9, k11, k12);
            Mix(ref b4, ref b5, 37, k13, k14);
            Mix(ref b6, ref b7, 31, k15, k16);
            Mix(ref b8, ref b9, 12, k0, k1);
            Mix(ref b10, ref b11, 47, k2, k3);
            Mix(ref b12, ref b13, 44, k4, k5 + t0);
            Mix(ref b14, ref b15, 30, k6 + t1, k7 + 9);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k10, k11);
            Mix(ref b2, ref b3, 13, k12, k13);
            Mix(ref b4, ref b5, 8, k14, k15);
            Mix(ref b6, ref b7, 47, k16, k0);
            Mix(ref b8, ref b9, 8, k1, k2);
            Mix(ref b10, ref b11, 17, k3, k4);
            Mix(ref b12, ref b13, 22, k5, k6 + t1);
            Mix(ref b14, ref b15, 37, k7 + t2, k8 + 10);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k11, k12);
            Mix(ref b2, ref b3, 9, k13, k14);
            Mix(ref b4, ref b5, 37, k15, k16);
            Mix(ref b6, ref b7, 31, k0, k1);
            Mix(ref b8, ref b9, 12, k2, k3);
            Mix(ref b10, ref b11, 47, k4, k5);
            Mix(ref b12, ref b13, 44, k6, k7 + t2);
            Mix(ref b14, ref b15, 30, k8 + t0, k9 + 11);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k12, k13);
            Mix(ref b2, ref b3, 13, k14, k15);
            Mix(ref b4, ref b5, 8, k16, k0);
            Mix(ref b6, ref b7, 47, k1, k2);
            Mix(ref b8, ref b9, 8, k3, k4);
            Mix(ref b10, ref b11, 17, k5, k6);
            Mix(ref b12, ref b13, 22, k7, k8 + t0);
            Mix(ref b14, ref b15, 37, k9 + t1, k10 + 12);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k13, k14);
            Mix(ref b2, ref b3, 9, k15, k16);
            Mix(ref b4, ref b5, 37, k0, k1);
            Mix(ref b6, ref b7, 31, k2, k3);
            Mix(ref b8, ref b9, 12, k4, k5);
            Mix(ref b10, ref b11, 47, k6, k7);
            Mix(ref b12, ref b13, 44, k8, k9 + t1);
            Mix(ref b14, ref b15, 30, k10 + t2, k11 + 13);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k14, k15);
            Mix(ref b2, ref b3, 13, k16, k0);
            Mix(ref b4, ref b5, 8, k1, k2);
            Mix(ref b6, ref b7, 47, k3, k4);
            Mix(ref b8, ref b9, 8, k5, k6);
            Mix(ref b10, ref b11, 17, k7, k8);
            Mix(ref b12, ref b13, 22, k9, k10 + t2);
            Mix(ref b14, ref b15, 37, k11 + t0, k12 + 14);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k15, k16);
            Mix(ref b2, ref b3, 9, k0, k1);
            Mix(ref b4, ref b5, 37, k2, k3);
            Mix(ref b6, ref b7, 31, k4, k5);
            Mix(ref b8, ref b9, 12, k6, k7);
            Mix(ref b10, ref b11, 47, k8, k9);
            Mix(ref b12, ref b13, 44, k10, k11 + t0);
            Mix(ref b14, ref b15, 30, k12 + t1, k13 + 15);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k16, k0);
            Mix(ref b2, ref b3, 13, k1, k2);
            Mix(ref b4, ref b5, 8, k3, k4);
            Mix(ref b6, ref b7, 47, k5, k6);
            Mix(ref b8, ref b9, 8, k7, k8);
            Mix(ref b10, ref b11, 17, k9, k10);
            Mix(ref b12, ref b13, 22, k11, k12 + t1);
            Mix(ref b14, ref b15, 37, k13 + t2, k14 + 16);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k0, k1);
            Mix(ref b2, ref b3, 9, k2, k3);
            Mix(ref b4, ref b5, 37, k4, k5);
            Mix(ref b6, ref b7, 31, k6, k7);
            Mix(ref b8, ref b9, 12, k8, k9);
            Mix(ref b10, ref b11, 47, k10, k11);
            Mix(ref b12, ref b13, 44, k12, k13 + t2);
            Mix(ref b14, ref b15, 30, k14 + t0, k15 + 17);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);
            Mix(ref b0, ref b1, 24, k1, k2);
            Mix(ref b2, ref b3, 13, k3, k4);
            Mix(ref b4, ref b5, 8, k5, k6);
            Mix(ref b6, ref b7, 47, k7, k8);
            Mix(ref b8, ref b9, 8, k9, k10);
            Mix(ref b10, ref b11, 17, k11, k12);
            Mix(ref b12, ref b13, 22, k13, k14 + t0);
            Mix(ref b14, ref b15, 37, k15 + t1, k16 + 18);
            Mix(ref b0, ref b9, 38);
            Mix(ref b2, ref b13, 19);
            Mix(ref b6, ref b11, 10);
            Mix(ref b4, ref b15, 55);
            Mix(ref b10, ref b7, 49);
            Mix(ref b12, ref b3, 18);
            Mix(ref b14, ref b5, 23);
            Mix(ref b8, ref b1, 52);
            Mix(ref b0, ref b7, 33);
            Mix(ref b2, ref b5, 4);
            Mix(ref b4, ref b3, 51);
            Mix(ref b6, ref b1, 13);
            Mix(ref b12, ref b15, 34);
            Mix(ref b14, ref b13, 41);
            Mix(ref b8, ref b11, 59);
            Mix(ref b10, ref b9, 17);
            Mix(ref b0, ref b15, 5);
            Mix(ref b2, ref b11, 20);
            Mix(ref b6, ref b13, 48);
            Mix(ref b4, ref b9, 41);
            Mix(ref b14, ref b1, 47);
            Mix(ref b8, ref b5, 28);
            Mix(ref b10, ref b3, 16);
            Mix(ref b12, ref b7, 25);
            Mix(ref b0, ref b1, 41, k2, k3);
            Mix(ref b2, ref b3, 9, k4, k5);
            Mix(ref b4, ref b5, 37, k6, k7);
            Mix(ref b6, ref b7, 31, k8, k9);
            Mix(ref b8, ref b9, 12, k10, k11);
            Mix(ref b10, ref b11, 47, k12, k13);
            Mix(ref b12, ref b13, 44, k14, k15 + t1);
            Mix(ref b14, ref b15, 30, k16 + t2, k0 + 19);
            Mix(ref b0, ref b9, 16);
            Mix(ref b2, ref b13, 34);
            Mix(ref b6, ref b11, 56);
            Mix(ref b4, ref b15, 51);
            Mix(ref b10, ref b7, 4);
            Mix(ref b12, ref b3, 53);
            Mix(ref b14, ref b5, 42);
            Mix(ref b8, ref b1, 41);
            Mix(ref b0, ref b7, 31);
            Mix(ref b2, ref b5, 44);
            Mix(ref b4, ref b3, 47);
            Mix(ref b6, ref b1, 46);
            Mix(ref b12, ref b15, 19);
            Mix(ref b14, ref b13, 42);
            Mix(ref b8, ref b11, 44);
            Mix(ref b10, ref b9, 25);
            Mix(ref b0, ref b15, 9);
            Mix(ref b2, ref b11, 48);
            Mix(ref b6, ref b13, 35);
            Mix(ref b4, ref b9, 52);
            Mix(ref b14, ref b1, 23);
            Mix(ref b8, ref b5, 31);
            Mix(ref b10, ref b3, 37);
            Mix(ref b12, ref b7, 20);

            // Final key schedule
            output[0] = b0 + k3;
            output[1] = b1 + k4;
            output[2] = b2 + k5;
            output[3] = b3 + k6;
            output[4] = b4 + k7;
            output[5] = b5 + k8;
            output[6] = b6 + k9;
            output[7] = b7 + k10;
            output[8] = b8 + k11;
            output[9] = b9 + k12;
            output[10] = b10 + k13;
            output[11] = b11 + k14;
            output[12] = b12 + k15;
            output[13] = b13 + k16 + t2;
            output[14] = b14 + k0 + t0;
            output[15] = b15 + k1 + 20;
        }

        public override void Decrypt(ulong[] input, ulong[] output)
        {
            if (ExpandedKey   == null) throw new ArgumentNullException();
            if (ExpandedTweak == null) throw new ArgumentNullException();

            // Cache the block, key, and tweak
            ulong b0 = input[0], b1 = input[1],
                  b2 = input[2], b3 = input[3],
                  b4 = input[4], b5 = input[5],
                  b6 = input[6], b7 = input[7],
                  b8 = input[8], b9 = input[9],
                  b10 = input[10], b11 = input[11],
                  b12 = input[12], b13 = input[13],
                  b14 = input[14], b15 = input[15];
            ulong k0 = ExpandedKey[0], k1 = ExpandedKey[1],
                  k2 = ExpandedKey[2], k3 = ExpandedKey[3],
                  k4 = ExpandedKey[4], k5 = ExpandedKey[5],
                  k6 = ExpandedKey[6], k7 = ExpandedKey[7],
                  k8 = ExpandedKey[8], k9 = ExpandedKey[9],
                  k10 = ExpandedKey[10], k11 = ExpandedKey[11],
                  k12 = ExpandedKey[12], k13 = ExpandedKey[13],
                  k14 = ExpandedKey[14], k15 = ExpandedKey[15],
                  k16 = ExpandedKey[16];
            ulong t0 = ExpandedTweak[0], t1 = ExpandedTweak[1],
                  t2 = ExpandedTweak[2];

            b0 -= k3;
            b1 -= k4;
            b2 -= k5;
            b3 -= k6;
            b4 -= k7;
            b5 -= k8;
            b6 -= k9;
            b7 -= k10;
            b8 -= k11;
            b9 -= k12;
            b10 -= k13;
            b11 -= k14;
            b12 -= k15;
            b13 -= k16 + t2;
            b14 -= k0 + t0;
            b15 -= k1 + 20;
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k16 + t2, k0 + 19);
            UnMix(ref b12, ref b13, 44, k14, k15 + t1);
            UnMix(ref b10, ref b11, 47, k12, k13);
            UnMix(ref b8, ref b9, 12, k10, k11);
            UnMix(ref b6, ref b7, 31, k8, k9);
            UnMix(ref b4, ref b5, 37, k6, k7);
            UnMix(ref b2, ref b3, 9, k4, k5);
            UnMix(ref b0, ref b1, 41, k2, k3);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k15 + t1, k16 + 18);
            UnMix(ref b12, ref b13, 22, k13, k14 + t0);
            UnMix(ref b10, ref b11, 17, k11, k12);
            UnMix(ref b8, ref b9, 8, k9, k10);
            UnMix(ref b6, ref b7, 47, k7, k8);
            UnMix(ref b4, ref b5, 8, k5, k6);
            UnMix(ref b2, ref b3, 13, k3, k4);
            UnMix(ref b0, ref b1, 24, k1, k2);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k14 + t0, k15 + 17);
            UnMix(ref b12, ref b13, 44, k12, k13 + t2);
            UnMix(ref b10, ref b11, 47, k10, k11);
            UnMix(ref b8, ref b9, 12, k8, k9);
            UnMix(ref b6, ref b7, 31, k6, k7);
            UnMix(ref b4, ref b5, 37, k4, k5);
            UnMix(ref b2, ref b3, 9, k2, k3);
            UnMix(ref b0, ref b1, 41, k0, k1);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k13 + t2, k14 + 16);
            UnMix(ref b12, ref b13, 22, k11, k12 + t1);
            UnMix(ref b10, ref b11, 17, k9, k10);
            UnMix(ref b8, ref b9, 8, k7, k8);
            UnMix(ref b6, ref b7, 47, k5, k6);
            UnMix(ref b4, ref b5, 8, k3, k4);
            UnMix(ref b2, ref b3, 13, k1, k2);
            UnMix(ref b0, ref b1, 24, k16, k0);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k12 + t1, k13 + 15);
            UnMix(ref b12, ref b13, 44, k10, k11 + t0);
            UnMix(ref b10, ref b11, 47, k8, k9);
            UnMix(ref b8, ref b9, 12, k6, k7);
            UnMix(ref b6, ref b7, 31, k4, k5);
            UnMix(ref b4, ref b5, 37, k2, k3);
            UnMix(ref b2, ref b3, 9, k0, k1);
            UnMix(ref b0, ref b1, 41, k15, k16);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k11 + t0, k12 + 14);
            UnMix(ref b12, ref b13, 22, k9, k10 + t2);
            UnMix(ref b10, ref b11, 17, k7, k8);
            UnMix(ref b8, ref b9, 8, k5, k6);
            UnMix(ref b6, ref b7, 47, k3, k4);
            UnMix(ref b4, ref b5, 8, k1, k2);
            UnMix(ref b2, ref b3, 13, k16, k0);
            UnMix(ref b0, ref b1, 24, k14, k15);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k10 + t2, k11 + 13);
            UnMix(ref b12, ref b13, 44, k8, k9 + t1);
            UnMix(ref b10, ref b11, 47, k6, k7);
            UnMix(ref b8, ref b9, 12, k4, k5);
            UnMix(ref b6, ref b7, 31, k2, k3);
            UnMix(ref b4, ref b5, 37, k0, k1);
            UnMix(ref b2, ref b3, 9, k15, k16);
            UnMix(ref b0, ref b1, 41, k13, k14);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k9 + t1, k10 + 12);
            UnMix(ref b12, ref b13, 22, k7, k8 + t0);
            UnMix(ref b10, ref b11, 17, k5, k6);
            UnMix(ref b8, ref b9, 8, k3, k4);
            UnMix(ref b6, ref b7, 47, k1, k2);
            UnMix(ref b4, ref b5, 8, k16, k0);
            UnMix(ref b2, ref b3, 13, k14, k15);
            UnMix(ref b0, ref b1, 24, k12, k13);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k8 + t0, k9 + 11);
            UnMix(ref b12, ref b13, 44, k6, k7 + t2);
            UnMix(ref b10, ref b11, 47, k4, k5);
            UnMix(ref b8, ref b9, 12, k2, k3);
            UnMix(ref b6, ref b7, 31, k0, k1);
            UnMix(ref b4, ref b5, 37, k15, k16);
            UnMix(ref b2, ref b3, 9, k13, k14);
            UnMix(ref b0, ref b1, 41, k11, k12);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k7 + t2, k8 + 10);
            UnMix(ref b12, ref b13, 22, k5, k6 + t1);
            UnMix(ref b10, ref b11, 17, k3, k4);
            UnMix(ref b8, ref b9, 8, k1, k2);
            UnMix(ref b6, ref b7, 47, k16, k0);
            UnMix(ref b4, ref b5, 8, k14, k15);
            UnMix(ref b2, ref b3, 13, k12, k13);
            UnMix(ref b0, ref b1, 24, k10, k11);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k6 + t1, k7 + 9);
            UnMix(ref b12, ref b13, 44, k4, k5 + t0);
            UnMix(ref b10, ref b11, 47, k2, k3);
            UnMix(ref b8, ref b9, 12, k0, k1);
            UnMix(ref b6, ref b7, 31, k15, k16);
            UnMix(ref b4, ref b5, 37, k13, k14);
            UnMix(ref b2, ref b3, 9, k11, k12);
            UnMix(ref b0, ref b1, 41, k9, k10);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k5 + t0, k6 + 8);
            UnMix(ref b12, ref b13, 22, k3, k4 + t2);
            UnMix(ref b10, ref b11, 17, k1, k2);
            UnMix(ref b8, ref b9, 8, k16, k0);
            UnMix(ref b6, ref b7, 47, k14, k15);
            UnMix(ref b4, ref b5, 8, k12, k13);
            UnMix(ref b2, ref b3, 13, k10, k11);
            UnMix(ref b0, ref b1, 24, k8, k9);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k4 + t2, k5 + 7);
            UnMix(ref b12, ref b13, 44, k2, k3 + t1);
            UnMix(ref b10, ref b11, 47, k0, k1);
            UnMix(ref b8, ref b9, 12, k15, k16);
            UnMix(ref b6, ref b7, 31, k13, k14);
            UnMix(ref b4, ref b5, 37, k11, k12);
            UnMix(ref b2, ref b3, 9, k9, k10);
            UnMix(ref b0, ref b1, 41, k7, k8);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k3 + t1, k4 + 6);
            UnMix(ref b12, ref b13, 22, k1, k2 + t0);
            UnMix(ref b10, ref b11, 17, k16, k0);
            UnMix(ref b8, ref b9, 8, k14, k15);
            UnMix(ref b6, ref b7, 47, k12, k13);
            UnMix(ref b4, ref b5, 8, k10, k11);
            UnMix(ref b2, ref b3, 13, k8, k9);
            UnMix(ref b0, ref b1, 24, k6, k7);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k2 + t0, k3 + 5);
            UnMix(ref b12, ref b13, 44, k0, k1 + t2);
            UnMix(ref b10, ref b11, 47, k15, k16);
            UnMix(ref b8, ref b9, 12, k13, k14);
            UnMix(ref b6, ref b7, 31, k11, k12);
            UnMix(ref b4, ref b5, 37, k9, k10);
            UnMix(ref b2, ref b3, 9, k7, k8);
            UnMix(ref b0, ref b1, 41, k5, k6);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k1 + t2, k2 + 4);
            UnMix(ref b12, ref b13, 22, k16, k0 + t1);
            UnMix(ref b10, ref b11, 17, k14, k15);
            UnMix(ref b8, ref b9, 8, k12, k13);
            UnMix(ref b6, ref b7, 47, k10, k11);
            UnMix(ref b4, ref b5, 8, k8, k9);
            UnMix(ref b2, ref b3, 13, k6, k7);
            UnMix(ref b0, ref b1, 24, k4, k5);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k0 + t1, k1 + 3);
            UnMix(ref b12, ref b13, 44, k15, k16 + t0);
            UnMix(ref b10, ref b11, 47, k13, k14);
            UnMix(ref b8, ref b9, 12, k11, k12);
            UnMix(ref b6, ref b7, 31, k9, k10);
            UnMix(ref b4, ref b5, 37, k7, k8);
            UnMix(ref b2, ref b3, 9, k5, k6);
            UnMix(ref b0, ref b1, 41, k3, k4);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k16 + t0, k0 + 2);
            UnMix(ref b12, ref b13, 22, k14, k15 + t2);
            UnMix(ref b10, ref b11, 17, k12, k13);
            UnMix(ref b8, ref b9, 8, k10, k11);
            UnMix(ref b6, ref b7, 47, k8, k9);
            UnMix(ref b4, ref b5, 8, k6, k7);
            UnMix(ref b2, ref b3, 13, k4, k5);
            UnMix(ref b0, ref b1, 24, k2, k3);
            UnMix(ref b12, ref b7, 20);
            UnMix(ref b10, ref b3, 37);
            UnMix(ref b8, ref b5, 31);
            UnMix(ref b14, ref b1, 23);
            UnMix(ref b4, ref b9, 52);
            UnMix(ref b6, ref b13, 35);
            UnMix(ref b2, ref b11, 48);
            UnMix(ref b0, ref b15, 9);
            UnMix(ref b10, ref b9, 25);
            UnMix(ref b8, ref b11, 44);
            UnMix(ref b14, ref b13, 42);
            UnMix(ref b12, ref b15, 19);
            UnMix(ref b6, ref b1, 46);
            UnMix(ref b4, ref b3, 47);
            UnMix(ref b2, ref b5, 44);
            UnMix(ref b0, ref b7, 31);
            UnMix(ref b8, ref b1, 41);
            UnMix(ref b14, ref b5, 42);
            UnMix(ref b12, ref b3, 53);
            UnMix(ref b10, ref b7, 4);
            UnMix(ref b4, ref b15, 51);
            UnMix(ref b6, ref b11, 56);
            UnMix(ref b2, ref b13, 34);
            UnMix(ref b0, ref b9, 16);
            UnMix(ref b14, ref b15, 30, k15 + t2, k16 + 1);
            UnMix(ref b12, ref b13, 44, k13, k14 + t1);
            UnMix(ref b10, ref b11, 47, k11, k12);
            UnMix(ref b8, ref b9, 12, k9, k10);
            UnMix(ref b6, ref b7, 31, k7, k8);
            UnMix(ref b4, ref b5, 37, k5, k6);
            UnMix(ref b2, ref b3, 9, k3, k4);
            UnMix(ref b0, ref b1, 41, k1, k2);
            UnMix(ref b12, ref b7, 25);
            UnMix(ref b10, ref b3, 16);
            UnMix(ref b8, ref b5, 28);
            UnMix(ref b14, ref b1, 47);
            UnMix(ref b4, ref b9, 41);
            UnMix(ref b6, ref b13, 48);
            UnMix(ref b2, ref b11, 20);
            UnMix(ref b0, ref b15, 5);
            UnMix(ref b10, ref b9, 17);
            UnMix(ref b8, ref b11, 59);
            UnMix(ref b14, ref b13, 41);
            UnMix(ref b12, ref b15, 34);
            UnMix(ref b6, ref b1, 13);
            UnMix(ref b4, ref b3, 51);
            UnMix(ref b2, ref b5, 4);
            UnMix(ref b0, ref b7, 33);
            UnMix(ref b8, ref b1, 52);
            UnMix(ref b14, ref b5, 23);
            UnMix(ref b12, ref b3, 18);
            UnMix(ref b10, ref b7, 49);
            UnMix(ref b4, ref b15, 55);
            UnMix(ref b6, ref b11, 10);
            UnMix(ref b2, ref b13, 19);
            UnMix(ref b0, ref b9, 38);
            UnMix(ref b14, ref b15, 37, k14 + t1, k15);
            UnMix(ref b12, ref b13, 22, k12, k13 + t0);
            UnMix(ref b10, ref b11, 17, k10, k11);
            UnMix(ref b8, ref b9, 8, k8, k9);
            UnMix(ref b6, ref b7, 47, k6, k7);
            UnMix(ref b4, ref b5, 8, k4, k5);
            UnMix(ref b2, ref b3, 13, k2, k3);
            UnMix(ref b0, ref b1, 24, k0, k1);

            output[15] = b15;
            output[14] = b14;
            output[13] = b13;
            output[12] = b12;
            output[11] = b11;
            output[10] = b10;
            output[9] = b9;
            output[8] = b8;
            output[7] = b7;
            output[6] = b6;
            output[5] = b5;
            output[4] = b4;
            output[3] = b3;
            output[2] = b2;
            output[1] = b1;
            output[0] = b0;
        }
    }

    public enum ThreefishTransformMode {Encrypt, Decrypt}

    public class ThreefishTransform : System.Security.Cryptography.ICryptoTransform
    {
        private delegate int TransformFunc(byte[] input, int inputOffset, int inputCount,
            byte[] output, int outputOffset);

        private readonly ThreefishCipher cipher;
        private readonly TransformFunc transformFunc;
        private readonly ThreefishTransformMode transformMode;
        private readonly int cipherBytes;
        private readonly int cipherWords;
        private readonly ulong[] block;
        // private readonly ulong[] iv;
        private readonly byte[] depadBuffer;
        private bool depadBufferFilled = false;

        public ThreefishTransform(byte[] key/*, byte[] iv*/, ThreefishTransformMode transformMode)
        {
            this.transformMode = transformMode;
            cipherBytes = key.Length;
            cipherWords = key.Length/8;
            // Allocate working blocks now so that we don't have to allocate them
            // each time Transform(Final)Block is called
            block = new ulong[cipherWords];
            // tempBlock = new ulong[cipherWords];
            // streamBytes = new byte[cipherBytes];
            depadBuffer = new byte[cipherBytes];
            //this.iv = new ulong[cipherWords];
            //GetBytes(iv, 0, this.iv, cipherBytes);
            switch (OutputBlockSize)
            {
            case 1024/8: cipher = new Threefish1024(); break;
            default: throw new System.Exception("Unsupported key/block size.");
            }
            bool e = transformMode==ThreefishTransformMode.Encrypt;
            transformFunc = EcbEncrypt;

            var keyWords = new ulong[cipherWords];
            GetBytes(key, 0, keyWords, cipherBytes);
            cipher.SetKey(keyWords);
            InitializeBlocks();
        }

        public void SetTweak(ulong[] tweak)
        {
            if (tweak.Length!=2)
                throw new ArgumentException("Tweak must be an array of two unsigned 64-bit integers.");
            InternalSetTweak(tweak);
        }

        public void InternalSetTweak(ulong[] tweak) { cipher.SetTweak(tweak); }

        // (Re)initializes the blocks for encryption
        private void InitializeBlocks()
        {
            depadBufferFilled = false;
            // usedStreamBytes = cipherBytes;
        }

        #region Utils

        private static ulong GetUInt64(byte[] buf, int offset)
        {
            ulong v = buf[offset];
            v |= (ulong)buf[offset+1]<<8;
            v |= (ulong)buf[offset+2]<<16;
            v |= (ulong)buf[offset+3]<<24;
            v |= (ulong)buf[offset+4]<<32;
            v |= (ulong)buf[offset+5]<<40;
            v |= (ulong)buf[offset+6]<<48;
            v |= (ulong)buf[offset+7]<<56;
            return v;
        }

        private static void PutUInt64(byte[] buf, int offset, ulong v)
        {
            buf[offset] = (byte)(v & 0xff);
            buf[offset+1] = (byte)(v>>8 & 0xff);
            buf[offset+2] = (byte)(v>>16 & 0xff);
            buf[offset+3] = (byte)(v>>24 & 0xff);
            buf[offset+4] = (byte)(v>>32 & 0xff);
            buf[offset+5] = (byte)(v>>40 & 0xff);
            buf[offset+6] = (byte)(v>>48 & 0xff);
            buf[offset+7] = (byte)(v>>56);
        }

        private static void GetBytes(byte[] input, int offset, ulong[] output, int byteCount)
        {
            for (int i = 0; i<byteCount; i += 8)
                output[i/8] = GetUInt64(input, i+offset);
        }

        private static void PutBytes(ulong[] input, byte[] output, int offset, int byteCount)
        {
            for (int i = 0; i<byteCount; i += 8)
                PutUInt64(output, i+offset, input[i/8]);
        }

        #endregion

        #region ModeTransformFunctions

        // ECB mode encryption
        private int EcbEncrypt(byte[] input, int inputOffset, int inputCount, byte[] output, int outputOffset)
        {
            if (inputCount<cipherBytes)
                return 0;
            GetBytes(input, inputOffset, block, cipherBytes);
            cipher.Encrypt(block, block);
            PutBytes(block, output, outputOffset, cipherBytes);
            return cipherBytes;
        }

        #endregion

        #region ICryptoTransform Members

        public bool CanReuseTransform
        {
            get { return true; }
        }

        public bool CanTransformMultipleBlocks
        {
            get { return true; }
        }

        public int InputBlockSize
        {
            get { return cipherBytes; }
        }

        public int OutputBlockSize
        {
            get { return cipherBytes; }
        }

        private int Transform(byte[] input, int inputOffset, int inputCount, byte[] output, int outputOffset)
        {
            int done, totalDone = 0;
            do
            {
                done = transformFunc(input, inputOffset+totalDone, inputCount-totalDone, output,
                    outputOffset+totalDone);
                totalDone += done;
            } while (done==cipherBytes);
            return totalDone;
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
            int outputOffset)
        {
            if ((inputCount & (cipherBytes-1))!=0)
                throw new Exception("inputCount must be divisible by the block size.");

            return Transform(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] output;
            // Do the padding and the final transform if there's any data left
            if (transformMode==ThreefishTransformMode.Encrypt)
            {
                if (inputCount%cipherBytes!=0)
                    throw new Exception("inputCount must be divisible by the block size.");
                output = new byte[inputCount];
                // int done = Transform(inputBuffer, inputOffset, inputCount, output, 0);
            }
            else // decrypt
            {
                if (inputCount%cipherBytes!=0)
                    throw new Exception("inputCount must be divisible by the block size.");
                if (!depadBufferFilled)
                {
                    output = new byte[inputCount];
                    Transform(inputBuffer, inputOffset, inputCount, output, 0);
                }
                else
                {
                    // XXX nitrocaster: could be optimized: copy to output and perform in-place decryption
                    var buf = new byte[cipherBytes+inputCount];
                    Buffer.BlockCopy(depadBuffer, 0, buf, 0, cipherBytes);
                    Buffer.BlockCopy(inputBuffer, inputOffset, buf, cipherBytes, inputCount);
                    output = new byte[cipherBytes+inputCount];
                    Transform(buf, 0, buf.Length, output, 0);
                }
            }
            // Reinitialize the cipher
            InitializeBlocks();
            return output;
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {  }

        #endregion
    }
}
