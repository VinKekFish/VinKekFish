using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ::test:1fRTvWtKthXIXmYoJkBj:

/*
 * Это отдельная библиотека, где разрешена оптимизация кода
 * Здесь никто ничего не обнуляет, только раундовые вычисления
 * */
namespace cryptoprime
{
    /// <summary>Статический класс, предоставляющий базовые функции и константы keccak. Пример использования см. в vinkekfish.Keccak_PRNG_20201128</summary>
    public static class KeccakPrime
    {                                                       /// <summary>Длина строки матрицы в ulong: 5 элементов ulong. См. также c_size</summary>
        public const int S_len  = 5;                        /// <summary>Размер матрицы в значениях ulong: 5*5=25 элементов ulong. См. также b_size</summary>
        public const int S_len2 = S_len*S_len;
                                                            /// <summary>rate в битах - 576 (размер внешней части криптографического состояния - блок вводимых данных)</summary>
        public const int r_512  = 576;                      /// <summary>rate в байтах = 72, это размер блока ввода/вывода за один раз</summary>
        public const int r_512b = 72;                       /// <summary>rate в ulong = 9</summary>
        public const int r_512s = 9;
                                                            /// <summary>25*8=200. Размер основной матрицы S (или "a") и вспомогательной матрицы b в байтах - равен размеру криптографического состояния</summary>
        public const int b_size = 25*8;                     /// <summary>5*8=40. Размер вспомогательной матрицы c в байтах - равен размеру строки криптографического состояния. Используется для транспонирования</summary>
        public const int c_size =  5*8;

        /// <summary>Раундовые коэффициенты для шага ι</summary>
        public static readonly ulong[] RC =
        {
            0x0000000000000001,
            0x0000000000008082,
            0x800000000000808A,
            0x8000000080008000,
            0x000000000000808B,
            0x0000000080000001,

            0x8000000080008081,
            0x8000000000008009,
            0x000000000000008A,
            0x0000000000000088,


            0x0000000080008009,
            0x000000008000000A,
            0x000000008000808B,
            0x800000000000008B,
            0x8000000000008089,

            0x8000000000008003,
            0x8000000000008002,
            0x8000000000000080,
            0x000000000000800A,
            0x800000008000000A,


            0x8000000080008081,
            0x8000000000008080,
            0x0000000080000001,
            0x8000000080008008
        };

        /// <summary>Реализация раундового преобразования</summary>
        /// <param name="a">Матрица S (a) - внутреннее состояние. Размер b_size (25*8=200 байтов) либо S_len2 в ulong (25 ulong)</param>
        /// <param name="c">Вспомогательная матрица размером c_size (5*8=40 байтов)</param>
        /// <param name="b">Вспомогательная матрица размером b_size (25*8=200 байтов)</param>
        public static unsafe void roundB(ulong * a, ulong * c, ulong * b)
        {
            //шаг θ
            *(c + 0) = *(a +  0) ^ *(a +  1) ^ *(a +  2) ^ *(a +  3) ^ *(a +  4);
            *(c + 1) = *(a +  5) ^ *(a +  6) ^ *(a +  7) ^ *(a +  8) ^ *(a +  9);
            *(c + 2) = *(a + 10) ^ *(a + 11) ^ *(a + 12) ^ *(a + 13) ^ *(a + 14);
            *(c + 3) = *(a + 15) ^ *(a + 16) ^ *(a + 17) ^ *(a + 18) ^ *(a + 19);
            *(c + 4) = *(a + 20) ^ *(a + 21) ^ *(a + 22) ^ *(a + 23) ^ *(a + 24);

            var d = *(c + 4) ^ ((*(c + 1) << 1) | (*(c + 1) >> 63));
            *(a +  0) ^= d; // D[0];
            *(a +  1) ^= d; // D[0];
            *(a +  2) ^= d; // D[0];
            *(a +  3) ^= d; // D[0];
            *(a +  4) ^= d; // D[0];

            d = *(c + 0) ^ ((*(c + 2) << 1) | (*(c + 2) >> 63));
            *(a +  5) ^= d; // D[1];
            *(a +  6) ^= d; // D[1];
            *(a +  7) ^= d; // D[1];
            *(a +  8) ^= d; // D[1];
            *(a +  9) ^= d; // D[1];

            d = *(c + 1) ^ ((*(c + 3) << 1) | (*(c + 3) >> 63));
            *(a + 10) ^= d; // D[2];
            *(a + 11) ^= d; // D[2];
            *(a + 12) ^= d; // D[2];
            *(a + 13) ^= d; // D[2];
            *(a + 14) ^= d; // D[2];

            d = *(c + 2) ^ ((*(c + 4) << 1) | (*(c + 4) >> 63));
            *(a + 15) ^= d; // D[3];
            *(a + 16) ^= d; // D[3];
            *(a + 17) ^= d; // D[3];
            *(a + 18) ^= d; // D[3];
            *(a + 19) ^= d; // D[3];

            d = *(c + 3) ^ ((*(c + 0) << 1) | (*(c + 0) >> 63));
            *(a + 20) ^= d; // D[4];
            *(a + 21) ^= d; // D[4];
            *(a + 22) ^= d; // D[4];
            *(a + 23) ^= d; // D[4];
            *(a + 24) ^= d; // D[4];
            

            //шаги ρ и π

            *(b +  0) =  *(a +  0);                             // rot(A[0, 0], r[0, 0]);
            *(b +  8) = (*(a +  1) << 36) | (*(a +  1) >> 28);  // rot(A[0, 1], r[0, 1]);
            *(b + 11) = (*(a +  2) <<  3) | (*(a +  2) >> 61);  // rot(A[0, 2], r[0, 2]);
            *(b + 19) = (*(a +  3) << 41) | (*(a +  3) >> 23);  // rot(A[0, 3], r[0, 3]);
            *(b + 22) = (*(a +  4) << 18) | (*(a +  4) >> 46);  // rot(A[0, 4], r[0, 4]);

            *(b +  2) = (*(a +  5) <<  1) | (*(a +  5) >> 63);  // rot(A[1, 0], r[1, 0]);
            *(b +  5) = (*(a +  6) << 44) | (*(a +  6) >> 20);  // rot(A[1, 1], r[1, 1]);
            *(b + 13) = (*(a +  7) << 10) | (*(a +  7) >> 54);  // rot(A[1, 2], r[1, 2]);
            *(b + 16) = (*(a +  8) << 45) | (*(a +  8) >> 19);  // rot(A[1, 3], r[1, 3]);
            *(b + 24) = (*(a +  9) <<  2) | (*(a +  9) >> 62);  // rot(A[1, 4], r[1, 4]);

            *(b +  4) = (*(a + 10) << 62) | (*(a + 10) >>  2);  // rot(A[2, 0], r[2, 0]);
            *(b +  7) = (*(a + 11) <<  6) | (*(a + 11) >> 58);  // rot(A[2, 1], r[2, 1]);
            *(b + 10) = (*(a + 12) << 43) | (*(a + 12) >> 21);  // rot(A[2, 2], r[2, 2]);
            *(b + 18) = (*(a + 13) << 15) | (*(a + 13) >> 49);  // rot(A[2, 3], r[2, 3]);
            *(b + 21) = (*(a + 14) << 61) | (*(a + 14) >>  3);  // rot(A[2, 4], r[2, 4]);

            *(b +  1) = (*(a + 15) << 28) | (*(a + 15) >> 36);  // rot(A[3, 0], r[3, 0]);
            *(b +  9) = (*(a + 16) << 55) | (*(a + 16) >>  9);  // rot(A[3, 1], r[3, 1]);
            *(b + 12) = (*(a + 17) << 25) | (*(a + 17) >> 39);  // rot(A[3, 2], r[3, 2]);
            *(b + 15) = (*(a + 18) << 21) | (*(a + 18) >> 43);  // rot(A[3, 3], r[3, 3]);
            *(b + 23) = (*(a + 19) << 56) | (*(a + 19) >>  8);  // rot(A[3, 4], r[3, 4]);

            *(b +  3) = (*(a + 20) << 27) | (*(a + 20) >> 37);  // rot(A[4, 0], r[4, 0]);
            *(b +  6) = (*(a + 21) << 20) | (*(a + 21) >> 44);  // rot(A[4, 1], r[4, 1]);
            *(b + 14) = (*(a + 22) << 39) | (*(a + 22) >> 25);  // rot(A[4, 2], r[4, 2]);
            *(b + 17) = (*(a + 23) <<  8) | (*(a + 23) >> 56);  // rot(A[4, 3], r[4, 3]);
            *(b + 20) = (*(a + 24) << 14) | (*(a + 24) >> 50);  // rot(A[4, 4], r[4, 4]);

            //шаг χ

            *(a +  0) = *(b +  0) ^ ((~*(b +  5)) & *(b + 10));
            *(a +  1) = *(b +  1) ^ ((~*(b +  6)) & *(b + 11));
            *(a +  2) = *(b +  2) ^ ((~*(b +  7)) & *(b + 12));
            *(a +  3) = *(b +  3) ^ ((~*(b +  8)) & *(b + 13));
            *(a +  4) = *(b +  4) ^ ((~*(b +  9)) & *(b + 14));

            *(a +  5) = *(b +  5) ^ ((~*(b + 10)) & *(b + 15));
            *(a +  6) = *(b +  6) ^ ((~*(b + 11)) & *(b + 16));
            *(a +  7) = *(b +  7) ^ ((~*(b + 12)) & *(b + 17));
            *(a +  8) = *(b +  8) ^ ((~*(b + 13)) & *(b + 18));
            *(a +  9) = *(b +  9) ^ ((~*(b + 14)) & *(b + 19));

            *(a + 10) = *(b + 10) ^ ((~*(b + 15)) & *(b + 20));
            *(a + 11) = *(b + 11) ^ ((~*(b + 16)) & *(b + 21));
            *(a + 12) = *(b + 12) ^ ((~*(b + 17)) & *(b + 22));
            *(a + 13) = *(b + 13) ^ ((~*(b + 18)) & *(b + 23));
            *(a + 14) = *(b + 14) ^ ((~*(b + 19)) & *(b + 24));

            *(a + 15) = *(b + 15) ^ ((~*(b + 20)) & *(b +  0));
            *(a + 16) = *(b + 16) ^ ((~*(b + 21)) & *(b +  1));
            *(a + 17) = *(b + 17) ^ ((~*(b + 22)) & *(b +  2));
            *(a + 18) = *(b + 18) ^ ((~*(b + 23)) & *(b +  3));
            *(a + 19) = *(b + 19) ^ ((~*(b + 24)) & *(b +  4));

            *(a + 20) = *(b + 20) ^ ((~*(b +  0)) & *(b +  5));
            *(a + 21) = *(b + 21) ^ ((~*(b +  1)) & *(b +  6));
            *(a + 22) = *(b + 22) ^ ((~*(b +  2)) & *(b +  7));
            *(a + 23) = *(b + 23) ^ ((~*(b +  3)) & *(b +  8));
            *(a + 24) = *(b + 24) ^ ((~*(b +  4)) & *(b +  9));

            //шаг ι - выполняется во внешнйе подпрограмме
        }

        // Полный keccak
        /// <summary>Все раунды keccak (24 раунда). a == S, c= C, b = B</summary>
        /// <param name="a">Зафиксированное внутреннее состояние S: 25 * ulong (константа b_size или S_len2*ulong)</param>
        /// <param name="c">Массив  C (значения не важны):  5 * ulong (константа c_size=40)</param>
        /// <param name="b">Матрица B (значения не важны): 25 * ulong (константа b_size=200)</param>
        public static unsafe void Keccackf(ulong * a, ulong * c, ulong * b)
        {
            roundB(a, c, b);
            //шаг ι
            *a ^= 0x0000000000000001;

            roundB(a, c, b); *a ^= 0x0000000000008082;
            roundB(a, c, b); *a ^= 0x800000000000808A;
            roundB(a, c, b); *a ^= 0x8000000080008000;

            roundB(a, c, b); *a ^= 0x000000000000808B;
            roundB(a, c, b); *a ^= 0x0000000080000001;
            roundB(a, c, b); *a ^= 0x8000000080008081;
            roundB(a, c, b); *a ^= 0x8000000000008009;

            roundB(a, c, b); *a ^= 0x000000000000008A;
            roundB(a, c, b); *a ^= 0x0000000000000088;
            roundB(a, c, b); *a ^= 0x0000000080008009;
            roundB(a, c, b); *a ^= 0x000000008000000A;

            roundB(a, c, b); *a ^= 0x000000008000808B;
            roundB(a, c, b); *a ^= 0x800000000000008B;
            roundB(a, c, b); *a ^= 0x8000000000008089;
            roundB(a, c, b); *a ^= 0x8000000000008003;

            roundB(a, c, b); *a ^= 0x8000000000008002;
            roundB(a, c, b); *a ^= 0x8000000000000080;
            roundB(a, c, b); *a ^= 0x000000000000800A;
            roundB(a, c, b); *a ^= 0x800000008000000A;

            roundB(a, c, b); *a ^= 0x8000000080008081;
            roundB(a, c, b); *a ^= 0x8000000000008080;
            roundB(a, c, b); *a ^= 0x0000000080000001;
            roundB(a, c, b); *a ^= 0x8000000080008008;
        }

        /// <summary>Неполнораундовый keccack. Параметры аналогичны Keccackf</summary>
        /// <param name="a">Внутреннее состояние S</param>
        /// <param name="c">Массив C (состояние не важно)</param>
        /// <param name="b">Матрица B (состояние не важно)</param>
        /// <param name="start">Начальный раунд, считается от нуля</param>
        /// <param name="count">Количество шагов (всего шагов столько, сколько констант в RC)</param>
        public static unsafe void Keccack_i(ulong * a, ulong * c, ulong * b, int start, int count)
        {
            var end = start + count;
            for (int i = start; i < end; i++)
            {
                roundB(a, c, b); *a ^= RC[i];
            }
        }

        /// <summary>Ввод данных в состояние keccak. Предназначен только для версии 512 битов</summary>
        /// <param name="message">Указатель на очередную порцию данных</param>
        /// <param name="len">Количество байтов для записи (не более 72-х; константа r_512b)</param>
        /// <param name="S">Внутреннее состояние S</param>
        /// <param name="setPaddings">Если <see langword="true"/> - ввести padding в массив (при вычислении хеша делать на последнем блоке <![CDATA[<=]]> 71 байта)</param>
        // НИЖЕ КОПИЯ Keccak_InputOverwrite_512 (небольшая разница, но методы, в целом, идентичны)
        public static unsafe void Keccak_Input_512(byte * message, byte len, byte * S, bool setPaddings = false)
        {
            if (len > r_512b || len < 0)
            {
                throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_Input_512: len > r_512b || len < 0");
            }

            // В конце 72-хбайтового блока нужно поставить оконечный padding
            // Матрица S размера 5x5*ulong
            // Вычисление адреса конца 72-хбайтового блока (последнего байта блока):
            // Мы пропустили 8 ulong (64-ре байта), пишем в последний, 9-ый, ulong
            // то есть 8-5=3 сейчас индекс у нас 3, но т.к. матрица транспонирована, то нам нужен не индекс [1, 3], а индекс [3, 1]
            // В индекс [3, 1] мы должны в старший байт записать 0x80. Значит, 3*5*8 + 1*8 + 7 = 135
            byte * es    = S + 135;
            byte * lastS = S;           // Если len = 0, то записываем в первый байт
            // Общий смысл инициализации
            // Массив информации в размере 72 байта записывается в начало состояния из 25-ти 8-мибайтовых слов; однако матрица S при этом имеет транспонированные индексы для повышения криптостойкости. То есть запись идёт не в начало матрицы S, а в начало транспонированной матрицы S
            int i1 = 0, i2 = 0, i3 = 0, ss = c_size;
            for (int i = 0; i < len; i++)
            {
                lastS = S + (i1 << 3) + i2*ss + i3;   // i2*ss - не ошибка, т.к. индексы в матрице транспонированны
                *lastS ^= *message;
                message++;

                // Выполняем приращения индексов в матрице
                i3++;
                if (i3 >= 8)
                {
                    i3 = 0;
                    i2++;   // Приращаем следующий индекс
                }
                if (i2 >= S_len)
                {
                    i2 = 0;
                    i1++;
                }

                if (i1 > 1)
                {
                    throw new Exception("cryptoprime.KeccakPrime.Keccak_Input_512: Fatal algorithmic error");
                }

                // Это вычисление нужно для того, чтобы потом записать верно padding
                // Для len = 71 значение lastS должно совпасть с es
                lastS = S + (i1 << 3) + i2*ss + i3;
            }

            if (setPaddings)
            {
                var SE = S + b_size;

                // На всякий случай, проверка на выход за пределы массива
                if (lastS >= SE || es >= SE)
                    throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_Input_512: lastS >= SE || es >= SE");

                 *lastS ^= 0x01;
                 *es    ^= 0x80;
            }
        }

        /// <summary>Ввод данных в состояние keccak для SHA-3 512</summary>
        /// <param name="message">Указатель на очередную порцию данных</param>
        /// <param name="len">Количество байтов для записи (не более 72-х; константа r_512b)</param>
        /// <param name="S">Внутреннее состояние S</param>
        /// <param name="setPaddings">Если <see langword="true"/> - ввести padding в массив (при вычислении хеша делать на последнем блоке <![CDATA[<=]]> 71 байта)</param>
        // НИЖЕ КОПИЯ Keccak_InputOverwrite_512 (небольшая разница, но методы, в целом, идентичны)
        public static unsafe void Keccak_Input_SHA512(byte * message, byte len, byte * S, bool setPaddings = false)
        {
            if (len > r_512b || len < 0)
            {
                throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_Input_512: len > r_512b || len < 0");
            }

            // В конце 72-хбайтового блока нужно поставить оконечный padding
            // Матрица S размера 5x5*ulong
            // Вычисление адреса конца 72-хбайтового блока (последнего байта блока):
            // Мы пропустили 8 ulong (64-ре байта), пишем в последний, 9-ый, ulong
            // то есть 8-5=3 сейчас индекс у нас 3, но т.к. матрица транспонирована, то нам нужен не индекс [1, 3], а индекс [3, 1]
            // В индекс [3, 1] мы должны в старший байт записать 0x80. Значит, 3*5*8 + 1*8 + 7 = 135
            byte * es    = S + 135;
            byte * lastS = S;           // Если len = 0, то записываем в первый байт
            // Общий смысл инициализации
            // Массив информации в размере 72 байта записывается в начало состояния из 25-ти 8-мибайтовых слов; однако матрица S при этом имеет транспонированные индексы для повышения криптостойкости. То есть запись идёт не в начало матрицы S, а в начало транспонированной матрицы S
            int i1 = 0, i2 = 0, i3 = 0, ss = c_size;
            for (int i = 0; i < len; i++)
            {
                lastS = S + (i1 << 3) + i2*ss + i3;   // i2*ss - не ошибка, т.к. индексы в матрице транспонированны
                *lastS ^= *message;
                message++;

                // Выполняем приращения индексов в матрице
                i3++;
                if (i3 >= 8)
                {
                    i3 = 0;
                    i2++;   // Приращаем следующий индекс
                }
                if (i2 >= S_len)
                {
                    i2 = 0;
                    i1++;
                }

                if (i1 > 1)
                {
                    throw new Exception("cryptoprime.KeccakPrime.Keccak_Input_512: Fatal algorithmic error");
                }

                // Это вычисление нужно для того, чтобы потом записать верно padding
                // Для len = 71 значение lastS должно совпасть с es
                lastS = S + (i1 << 3) + i2*ss + i3;
            }

            if (setPaddings)
            {
                var SE = S + b_size;

                // На всякий случай, проверка на выход за пределы массива
                if (lastS >= SE || es >= SE)
                    throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_Input_512: lastS >= SE || es >= SE");

                 *lastS ^= 0x06;
                 *es    ^= 0x80;
            }
        }


        /// <summary>
        /// Эта конструкция разработана по мотивам keccak Overwrite, но немного от неё отличается. Здесь нет padding, нет framebit. Количество вводимых байтов вводится xor с внутренним состоянием, а логический режим ввода, заменяющий framebit, вводится как xor с ещё одним байтом внутреннего состояния. Это рекомендуемая разработчиком VinKekFish функция.
        /// </summary>
        /// <param name="message">64 байта или менее для ввода с помощью перезаписи. Может быть null, если len == 0</param>
        /// <param name="len">длина массива message, 64 или менее</param>
        /// <param name="S">Внутреннее состояние keccak</param>
        /// <param name="regime">Режим ввода: аналог framebit, но в виде байта</param>

        // Этот метод должен быть почти полной копией Keccak_Input_512, за исключением небольших изменений
        // Ниже ещё один аналог!
        public static unsafe void Keccak_InputOverwrite64_512(byte * message, byte len, byte * S, byte regime = 0)
        {
            const byte RB = 64;
            if (len > RB || len < 0)
            {
                throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_InputOverwrite64_512: len > 64 || len < 0");
            }

            byte * lastS  = S;           // Если len = 0, то записываем в первый байт

            // Общий смысл инициализации
            // Массив информации в размере 64 байта записывается в начало состояния из 25-ти 8-мибайтовых слов; однако матрица S при этом имеет транспонированные индексы
            // Байты, которые не введены, просто считаются нулями (то есть состояние всё равно перезаписывается)
            // Дописываем len после конца: это не paddings, это отличие от оригинальной версии keccak
            int i1 = 0, i2 = 0, i3 = 0, ss = c_size;
            int i = 0;
            for (; i < RB; i++)
            {
                lastS = S + (i1 << 3) + i2*ss + i3;
                if (i < len)
                    *lastS = *message;   // ЗДЕСЬ ИЗМЕНЕНИЕ! Это впитывание не для Sponge, а для Overwrite
                else
                    *lastS = 0;

                message++;

                // Выполняем приращения индексов в матрице
                i3++;
                if (i3 >= 8)
                {
                    i3 = 0;
                    i2++;   // Приращаем следующий индекс
                }
                if (i2 >= S_len)
                {
                    i2 = S_len;
                    i2 = 0;
                    i1++;
                }

                if (i1 > 1)
                {
                    throw new Exception("cryptoprime.KeccakPrime.Keccak_InputOverwrite64_512: Fatal algorithmic error");
                }
            }

            // Дописываем после конца ввода размер введённых данных
            lastS = S + (i1 << 3) + i2*ss + i3;

            // На всякий случай, проверка на выход за пределы границ матрицы S
            if (lastS >= S + b_size)
                throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_InputOverwrite64_512: lastS >= S + b_size");

            *lastS ^= len; lastS++;
            *lastS ^= regime;
        }
                                                    /// <summary>Размер блока keccak в данной реализации (на ввод и на вывод)</summary>
        public const byte BlockLen = 64;

        /// <summary>
        /// Эта конструкция разработана по мотивам keccak Sponge, но немного от неё отличается. Здесь нет padding, нет framebit. Количество вводимых байтов вводится xor с внутренним состоянием, а логический режим ввода, заменяющий framebit, вводится как xor с ещё одним байтом внутреннего состояния. Это рекомендуемая разработчиком VinKekFish функция.
        /// </summary>
        /// <param name="message">64 байта или менее для ввода с помощью перезаписи. Может быть null, если len == 0</param>
        /// <param name="len">длина массива message, 64 или менее</param>
        /// <param name="S">Внутреннее состояние keccak</param>
        /// <param name="regime">Режим ввода: аналог framebit, но в виде байта</param>
        public static unsafe void Keccak_Input64_512(byte * message, byte len, byte * S, byte regime = 0)
        {
            if (len > BlockLen || len < 0)
            {
                throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_InputOverwrite64_512: len > 64 || len < 0");
            }

            byte * lastS  = S;           // Если len = 0, то записываем в первый байт

            // Общий смысл инициализации
            // Массив информации в размере 64 байта записывается в начало состояния из 25-ти 8-мибайтовых слов; однако матрица S при этом имеет транспонированные индексы
            // Байты, которые не введены, просто считаются нулями (то есть состояние всё равно перезаписывается)
            // Дописываем len после конца: это не paddings, это отличие от оригинальной версии keccak
            int i1 = 0, i2 = 0, i3 = 0, ss = c_size;
            int i = 0;
            for (; i < BlockLen; i++)
            {
                lastS = S + (i1 << 3) + i2*ss + i3;
                if (i < len)
                    *lastS ^= *message;   // ЗДЕСЬ ИЗМЕНЕНИЕ! Это впитывание для Sponge, а не для Overwrite
                else
                {}

                message++;

                // Выполняем приращения индексов в матрице
                i3++;
                if (i3 >= 8)
                {
                    i3 = 0;
                    i2++;   // Приращаем следующий индекс
                }
                if (i2 >= S_len)
                {
                    i2 = S_len;
                    i2 = 0;
                    i1++;
                }

                if (i1 > 1)
                {
                    throw new Exception("cryptoprime.KeccakPrime.Keccak_InputOverwrite64_512: Fatal algorithmic error");
                }
            }

            // Дописываем после конца ввода размер введённых данных
            lastS = S + (i1 << 3) + i2*ss + i3;

            // На всякий случай, проверка на выход за пределы границ матрицы S
            if (lastS >= S + b_size)
                throw new ArgumentOutOfRangeException("cryptoprime.KeccakPrime.Keccak_InputOverwrite64_512: lastS >= S + b_size");

            *lastS ^= len; lastS++;
            *lastS ^= regime;
        }

        /// <summary>Вывод данных из состояния keccak. Предназначен только для версии 512 битов</summary>
        /// <param name="output">Указатель на массив, готовый принять данные :[0, 72]</param>
        /// <param name="len">Количество байтов для записи (не более 72-х; константа r_512b). Обычно используется 64 - это стойкость данного криптографического преобразования. <para>При использовании Keccak_Input64_512 и Keccak_InputOverwrite64_512 вывод не более 64-х байтов</para></param>
        /// <param name="S">Внутреннее состояние S :[200]</param>
        /// <remarks>При вызове надо проверить, что output всегда нужной длины</remarks>
        public static unsafe void Keccak_Output_512(byte * output, byte len, byte * S)
        {
            if (len > r_512b || len < 0)
            {
                throw new ArgumentOutOfRangeException("len > r_512b || len < 0");
            }

            // Матрица S - это матрица 5x5 по 8 байтов. Мы проходим по первому столбцу, и собираем оттуда данные
            // Потом - по второму столбцу, и собираем оттуда данные
            for (int i = 0; i < 40;  i += 8)  // 40 = 8*5
            for (int j = 0; j < 200; j += 40) // 200 = 40*5
            for (int k = 0; k < 8; k++)
            {
                if (len == 0)
                    goto End;
                
                *output = *(S + i + j + k);

                output++;
                len--;
            }

            End: ;
        }

        /// <summary>Вычисляет хеш SHA-3 с длиной 512 битов</summary>
        /// <param name="message">Сообщение для хеширования</param>
        /// <param name="forHash">Массив размером 64 байта (может быть null). После выполнения функции заполнен хешем SHA-3 512, размер 64 байта</param>
        /// <returns>Хеш SHA-3 512, размер 64 байта (если forHash не был равен нулю, то это ссылка на массив forHash)</returns>
        public static unsafe byte[] getSHA3_512(byte[] message, byte[]? forHash = null)
        {
            forHash ??= new byte[64];
            if (forHash.Length < 64)
                throw new ArgumentOutOfRangeException("KeccakPrime.getSHA3_512.forHash must be a 64 bytes long at least");

            var hash  = forHash;
            var all   = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();
            // var bc_b  = new ulong[KeccakPrime.b_size/8 + KeccakPrime.c_size/8];
            // var Slong = new ulong[KeccakPrime.S_len2];
            using var bc_b  = all.AllocMemory(KeccakPrime.b_size + KeccakPrime.c_size); bc_b .Clear();
            using var Slong = all.AllocMemory(sizeof(ulong) * KeccakPrime.S_len2);      Slong.Clear();

            fixed (byte  * bt0 = message)
            fixed (byte  * ap  = hash)
            {
                ulong * bc  = (ulong *) bc_b.array;
                ulong * SL  = (ulong *) Slong.array;

                bool PaddingWasSetted = false;
                byte * S = (byte *) SL;

                for (int i = 0; i < message.Length; i += 72)
                {
                    int  len        = message.Length - i;
                    bool doPaddings = len < 72;
                    if (len > 72)
                        len = 72;

                    PaddingWasSetted = doPaddings;
                    KeccakPrime.Keccak_Input_SHA512(bt0 + i, (byte) len, S, doPaddings);
                    KeccakPrime.Keccackf(a: SL, c: bc, b : bc + KeccakPrime.c_size/8);
                }

                // Если введена пустая строка
                if (!PaddingWasSetted)
                {
                    KeccakPrime.Keccak_Input_SHA512(bt0, 0, S, true);
                    KeccakPrime.Keccackf(a: SL, c: bc, b : bc + KeccakPrime.c_size/8);
                }

                KeccakPrime.Keccak_Output_512(ap, 64, S);
            }

            return forHash;
        }
    }
}
