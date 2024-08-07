﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ::test:VOWNOWU4qu1Al9x07uh0:
// ::test:Tv1yP0c8X47nQXOjkXSt:

namespace cryptoprime
{
    /// <summary>Реализует функциональность битового массива (BitArray)</summary>
    public unsafe static class BitToBytes
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

        /// <summary>Получить бит из битово массива</summary>
        /// <param name="array">Массив битов</param><param name="index">Индекс получаемого элемента</param>
        public static bool GetBit(byte[] array, nint index)
        {
            fixed (byte * b = array)
                return GetBit(b, index);
        }

        /// <summary>Получить бит из битово массива</summary>
        /// <param name="array">Массив битов</param><param name="index">Индекс получаемого элемента</param>
        public static bool GetBit(byte* array, nint index)
        {
            var i = index >> 3;
            var s = (byte) (index & 0x07);
            var r = array[i];

            r >>= s;

            return (r & 1) != 0;
        }

        /// <summary>Установить бит в битовом массиве</summary>
        /// <param name="array">Массив битов</param><param name="index">Индекс задаваемого элемента</param>
        public static void SetBit(byte[] array, nint index)
        {
            fixed (byte * b = array)
                SetBit(b, index);
        }

        /// <summary>Установить бит в битовом массиве</summary>
        /// <param name="array">Массив битов</param><param name="index">Индекс задаваемого элемента</param>
        public static void SetBit(byte* array, nint index)
        {
            var i = index >> 3;
            var s = (byte) (index & 0x07);

            array[i] |= BitMask[s];
        }

        /// <summary>Сбросить бит в битовом массиве</summary>
        /// <param name="array">Массив битов</param><param name="index">Индекс задаваемого элемента</param>
        public static void ResetBit(byte[] array, nint index)
        {
            fixed (byte * b = array)
                ResetBit(b, index);
        }

        /// <summary>Сбросить бит в битовом массиве</summary>
        /// <param name="array">Массив битов</param><param name="index">Индекс задаваемого элемента</param>
        public static void ResetBit(byte* array, nint index)
        {
            var i = index >> 3;
            var s = (byte) (index & 0x07);

            array[i] &= (byte) ~BitMask[s];
        }
    }
}
