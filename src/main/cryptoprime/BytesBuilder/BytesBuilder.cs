using System;
using System.Collections.Generic;
using System.Text;

namespace cryptoprime
{
    /// <summary>
    /// BytesBuilder
    /// Класс позволяет собирать большой блок байтов из более мелких
    /// Класс непотокобезопасный (при его использовании необходимо синхронизировать доступ к классу вручную)
    /// </summary>
    public partial class BytesBuilder
    {
        /// <summary>Добавленные блоки байтов</summary>
        public readonly List<byte[]> bytes = new List<byte[]>();

        /// <summary>Количество всех сохранённых байтов в этом объекте</summary>
        public long Count  => count;

        /// <summary>Количество всех сохранённых блоков, как они были добавлены в этот объект</summary>
        public long countOfBlocks => bytes.Count;

        /// <summary>Получает сохранённых блок с определённым индексом в списке сохранения</summary><param name="number">Индекс в списке</param><returns>Сохранённый блок (не копия, подлинник)</returns>
        public byte[] getBlock(int number)
        {
            return bytes[number];
        }

        /// <summary>Количество сохранённых байтов</summary>
        long count = 0;

        /// <summary>Добавляет блок в объект</summary><param name="bytesToAdded">Добавляемый блок данных</param>
        /// <param name="index">Куда добавляется блок. По-умолчанию, в конец (index = -1)</param>
        /// <param name="MakeCopy">MakeCopy = true говорит о том, что данные блока будут скопированы (создан новый блок и он будет добавлен). По-умолчанию false - блок будет добавлен без копирования. Это значит, что при изменении исходного блока, изменится и выход, даваемый объектом. Если исходный блок будет обнулён, то будет обнулены и выходные байты из этого объекта, соответствующие этому блоку</param>
        // При добавлении блока важно проверить, верно выставлен параметр MakeCopy и если MakeCopy = false, то блок не должен изменяться
        public void add(byte[] bytesToAdded, int index = -1, bool MakeCopy = false)
        {
            if (MakeCopy)
            {
                var b = new byte[bytesToAdded.LongLength];
                BytesBuilder.CopyTo(bytesToAdded, b);
                bytesToAdded = b;
            }

            if (index == -1)
                bytes.Add(bytesToAdded);
            else
                bytes.Insert((int) index, bytesToAdded);

            count += bytesToAdded.LongLength;
        }

        /// <summary>Копирует данные блока и добавляет его в объект</summary><param name="bytesToAdded">Добавляемый блок</param><param name="index">Индекс для добавления.  index = -1 - добавление в конец</param>
        public void addCopy(byte[] bytesToAdded, int index = -1)
        {
            add(bytesToAdded, index, true);
        }

        /// <summary>Добавляет в объект один байт</summary><param name="number">Добавляемое значение</param><param name="index">Индекс добавляемого блока. -1 - в конец</param>
        public void addByte(byte number, int index = -1)
        {
            var n = new byte[1];
            n[0] = number;
            add(n, index);
        }

        /// <summary>Добавляет в объект двухбайтовое беззнаковое целое. Младший байт по младшему адресу</summary>
        public void addUshort(ushort number, int index = -1)
        {
            var n = new byte[2];
            n[1] = (byte) (number >> 8);
            n[0] = (byte) (number     );
            add(n, index);
        }

        /// <summary>Добавляет в объект 4-хбайтовое беззнаковое целое. Младший байт по младшему адресу</summary>
        public void addInt(int number, int index = -1)
        {
            var n = new byte[4];
            n[3] = (byte) (number >> 24);
            n[2] = (byte) (number >> 16);
            n[1] = (byte) (number >> 8);
            n[0] = (byte) (number     );

            add(n, index);
        }

        /// <summary>Добавляет в объект 8-хбайтовое беззнаковое целое. Младший байт по младшему адресу</summary>
        public void addULong(ulong number, int index = -1)
        {
            var n = new byte[8];
            n[7] = (byte) (number >> 56);
            n[6] = (byte) (number >> 48);
            n[5] = (byte) (number >> 40);
            n[4] = (byte) (number >> 32);
            n[3] = (byte) (number >> 24);
            n[2] = (byte) (number >> 16);
            n[1] = (byte) (number >> 8);
            n[0] = (byte) (number     );

            add(n, index);
        }

        /// <summary>Добавляет в объект специальную кодировку 8-байтового числа, см. функцию VariableULongToBytes</summary>
        public void addVariableULong(ulong number, int index = -1)
        {
            byte[]? target = null;
            BytesBuilder.VariableULongToBytes(number, ref target);

            var t = target ?? throw new ArgumentNullException();
            add(t, index);
        }

        /// <summary>Добавляет в объект строку UTF-8</summary>
        public void add(string utf8String, int index = -1)
        {
            add(UTF8Encoding.UTF8.GetBytes(utf8String), index);
        }

        /// <summary>Обнуляет объект</summary>
        /// <param name="fast">fast = <see langword="false"/> - обнуляет все байты сохранённые в массиве</param>
        public void Clear(bool fast = false)
        {
            if (!fast)
            {
                foreach (byte[] e in bytes)
                    BytesBuilder.ToNull(e);
            }

            count = 0;
            bytes.Clear();
        }
        /*
        /// <summary>Удаляет последний блок из объекта, блок очищается нулями</summary>
        /// <returns>Возвращает длину последнего блока</returns>
        public long RemoveLastBlock()
        {
            if (bytes.Count <= 0)
                return 0;

            return RemoveBlockAt(bytes.Count - 1);
        }

        /// <summary>Удаляет несколько блоков с позиции position до позиции endPosition включительно</summary>
        /// <param name="position">Индекс первого удаляемого блока</param><param name="endPosition">Индекс последнего удаляемого блока</param>
        /// <returns>Количество удалённых байтов</returns>
        public long RemoveBlocks(int position, int endPosition)
        {
            if (position < 0)
                throw new ArgumentException("position must be >= 0");

            if (position >= bytes.Count)
                throw new ArgumentException("position must be in range");

            if (position > endPosition)
                throw new ArgumentException("position must be position <= endPosition");

            if (endPosition >= bytes.Count)
                throw new ArgumentException("endPosition must be endPosition < bytes.Count");

            long removedLength = 0;

            for (int i = position; i <= endPosition; i++)
            {
                removedLength += RemoveBlockAt(position);
            }

            count -= removedLength;
            return removedLength;
        }
        */

        public class ResultCountIsTooLarge: System.ArgumentOutOfRangeException
        {
            public ResultCountIsTooLarge(long resultCount, long count):
                                        base
                                        (
                                            "resultCount",
                                            $"resultCount is too large: resultCount > count ({resultCount} > {count})"
                                        )
            {}
        }

        public class ResultAIsTooSmall: System.ArgumentOutOfRangeException
        {
            public ResultAIsTooSmall(byte[] resultA, long resultCount):
                                        base
                                        (
                                            "resultA",
                                            $"resultA is too small: ({resultA.LongLength} < {resultCount})"
                                        )
            {}
        }

        /// <summary>Создаёт массив байтов, включающий в себя все сохранённые массивы</summary>
        /// <param name="resultCount">Размер массива-результата (если нужны все байты resultCount = -1)</param>
        /// <param name="resultA">Массив, в который будет записан результат. Если resultA = null, то массив создаётся</param>
        /// <returns></returns>
        public byte[] getBytes(long resultCount = -1, byte[]? resultA = null)
        {
            if (resultCount == -1)
                resultCount = count;

            if (resultCount > count)
            {
                throw new ResultCountIsTooLarge(resultCount: resultCount, count: count);
            }

            if (resultA != null && resultA.Length < resultCount)
                throw new ResultAIsTooSmall(resultA, resultCount);

            byte[] result = resultA ?? new byte[resultCount];

            long cursor = 0;
            for (int i = 0; i < bytes.Count; i++)
            {
                if (cursor >= result.LongLength)
                    break;

                CopyTo(bytes[i], result, cursor);
                cursor += bytes[i].LongLength;
            }

            return result;
        }
        
        /// <summary>Удаляет блок из объекта с позиции position, блок очищается нулями</summary>
        /// <returns>Возвращает длину удалённого блока</returns>
        public long RemoveBlockAt(int position, bool doClear = true)
        {
            if (position < 0)
                throw new ArgumentException("position must be >= 0");

            if (position >= bytes.Count)
                throw new ArgumentException("position must be in range");

            var tmp = bytes[position];

            long removedLength = tmp.LongLength;
            bytes.RemoveAt(position);

            if (doClear)
                BytesBuilder.ToNull(tmp);

            count -= removedLength;
            return removedLength;
        }

        /// <summary>Создаёт массив байтов, включающий в себя resultCount символов, и удаляет их с очисткой из BytesBuilder</summary>
        /// <param name="resultA">Массив, в который будет записан результат. Если resultA = null, то массив создаётся</param>
        /// <param name="resultCount">Размер массива-результата</param>
        /// <returns>Запрошенный результат (первые resultCount байтов)</returns>
        // Эта функция может неожиданно обнулить часть массива или массив, сохранённый без копирования (если он где-то используется в другом месте)
        public byte[] getBytesAndRemoveIt(byte[]? resultA = null, long resultCount = -1)
        {
            if (resultCount == -1)
            {
                if (resultA == null)
                    resultCount = count;
                else
                    resultCount = resultA.LongLength;
            }

            if (resultCount > count)
            {
                throw new ResultCountIsTooLarge(resultCount: resultCount, count: count);
            }

            if (resultA != null && resultA.Length < resultCount)
                throw new ResultAIsTooSmall(resultA, resultCount);

            byte[] result = resultA ?? new byte[resultCount];

            long cursor = 0;
            for (int i = 0; i < bytes.Count; )
            {
                if (cursor == resultCount)
                    break;

                if (cursor > resultCount)
                    throw new System.Exception("Fatal algorithmic error (getBytesAndRemoveIt): cursor > resultCount");

                if (cursor + bytes[i].LongLength > resultCount)
                {
                    // Делим массив на две части. Левая уходит наружу, правая остаётся в массиве
                    var left  = resultCount - cursor;
                    var right = bytes[i].LongLength - left;

                    var bLeft  = CloneBytes(bytes[i], 0, left);
                    var bRight = CloneBytes(bytes[i], left);

                    RemoveBlockAt(i);

                    bytes.Insert(0, bLeft );
                    bytes.Insert(1, bRight);

                    count += left + right;
                }

                CopyTo(bytes[i], result, cursor);
                cursor += bytes[i].LongLength;

                RemoveBlockAt(i);
            }

            return result;
        }

        /*
        public byte[] getBytes(long resultCount, long index)
        {
            if (resultCount == -1 || resultCount > count)
                resultCount = count;

            byte[] result = new byte[resultCount];

            long cursor = 0;
            long tindex = 0;
            for (int i = 0; i < bytes.Count; i++)
            {
                if (cursor >= result.Length)
                    break;

                if (tindex + bytes[i].LongLength < index)
                {
                    tindex += bytes[i].LongLength;
                    continue;
                }

                CopyTo(bytes[i], result, cursor, resultCount - cursor, index - tindex);
                cursor += bytes[i].LongLength;
                tindex += bytes[i].LongLength;
            }

            return result;
        }
        */

        /// <summary>Клонирует массив, начиная с элемента start, до элемента с индексом PostEnd (не включая)</summary><param name="B">Массив для копирования</param>
        /// <param name="start">Начальный элемент для копирования</param>
        /// <param name="PostEnd">Элемент, расположенный после последнего элемента для копирования. -1 - до конца</param>
        /// <returns>Новый массив</returns>
        public static unsafe byte[] CloneBytes(byte[] B, long start = 0, long PostEnd = -1)
        {
            if (PostEnd < 0)
                PostEnd = B.LongLength;

            var result = new byte[PostEnd - start];

            if (result.LongLength > 0)
            fixed (byte * r = result, b = B)
                BytesBuilder.CopyTo(PostEnd, PostEnd - start, b, r, 0, -1, start);

            return result;
        }

        /// <summary>Клонирует массив, начиная с элемента start, до элемента с индексом PostEnd (не включая)</summary><param name="b">Массив для копирования</param>
        /// <param name="start">Начальный элемент для копирования</param>
        /// <param name="PostEnd">Элемент, расположенный после последнего элемента для копирования</param>
        /// <returns>Новый массив</returns>
        public static unsafe byte[] CloneBytes(byte * b, long start, long PostEnd)
        {
            var result = new byte[PostEnd - start];
            fixed (byte * r = result)
                BytesBuilder.CopyTo(PostEnd, PostEnd - start, b, r, 0, -1, start);

            return result;
        }

        /// <summary>
        /// Копирует массив source в массив target. Если запрошенное количество байт скопировать невозможно, копирует те, что возможно
        /// </summary>
        /// <param name="source">Источник копирования</param>
        /// <param name="target">Приёмник</param>
        /// <param name="targetIndex">Начальный индекс копирования в приёмник</param>
        /// <param name="count">Максимальное количество байт для копирования (-1 - все доступные)</param>
        /// <param name="index">Начальный индекс копирования из источника</param>
        public unsafe static long CopyTo(byte[] source, byte[] target, long targetIndex = 0, long count = -1, long index = 0)
        {
            long sl = source.LongLength;

            fixed (byte * s = source, t = target)
            {
                return CopyTo(sl, target.LongLength, s, t, targetIndex, count, index);
            }
        }

        /// <summary>Копирует массивы по указателям из s в t</summary>
        /// <param name="sourceLength">Длина массива s</param><param name="targetLength">Длина массива t</param>
        /// <param name="s">Источник для копирования</param><param name="t">Приёмник для копирования</param>
        /// <param name="targetIndex">Начальный индекс, с которого будет происходить запись в t</param>
        /// <param name="count">Количество байтов для записи в t. Count = -1 - копирует столько, сколько возможно, учитывая размеры источника и приёмника. count не может быть 0</param>
        /// <param name="index">Начальный индекс копирования из источника s</param>
        /// <returns>Количество скопированных байтов</returns>
        unsafe public static long CopyTo(long sourceLength, long targetLength, byte* s, byte* t, long targetIndex = 0, long count = -1, long index = 0)
        {
            // Вычисляем указатели за концы элементов массивов (это недостижимые указатели)
            byte* se = s + sourceLength;
            byte* te = t + targetLength;

            if (count == -1)
            {
                count = Math.Min(sourceLength - index, targetLength - targetIndex);
            }

            if (count <= 0)
                throw new ArgumentOutOfRangeException("count <= 0");

            // Вычисляем значения, указывающие на недостижимый элемент (после копируемых)
            byte* sec = s + index + count;
            byte* tec = t + targetIndex + count;

            byte* sbc = s + index;
            byte* tbc = t + targetIndex;

            if (sec > se)
            {
                throw new ArgumentOutOfRangeException("sec > se");
                // tec -= sec - se;
                // sec = se;
            }

            if (tec > te)
            {
                throw new ArgumentOutOfRangeException("tec > te");
                // sec -= tec - te;
                // tec = te;
            }

            if (tbc < t)
                throw new ArgumentOutOfRangeException("tbc < t");

            if (sbc < s)
                throw new ArgumentOutOfRangeException("sbc < s");

            if (sec - sbc != tec - tbc || sbc >= sec || tbc >= tec)
                throw new OverflowException("BytesBuilder.CopyTo: fatal algorithmic error");


            ulong* sbw = (ulong*)sbc;
            ulong* tbw = (ulong*)tbc;

            ulong* sew = sbw + ((sec - sbc) >> 3);

            for (; sbw < sew; sbw++, tbw++)
                *tbw = *sbw;

            byte toEnd = (byte)(((int)(sec - sbc)) & 0x7);

            byte* sbcb = (byte*)sbw;
            byte* tbcb = (byte*)tbw;
            byte* sbce = sbcb + toEnd;

            for (; sbcb < sbce; sbcb++, tbcb++)
                *tbcb = *sbcb;


            return sec - sbc;
        }

        /// <summary>Заполняет массив t байтами со значением value</summary><param name="value">Значение для заполнения</param>
        /// <param name="t">Массив для заполнения</param><param name="index">Индекс первого элемента, с которого будет начато заполнение</param>
        /// <param name="count">Количество элементов для заполнения. count = -1 - заполнять до конца</param>
        public static void FillByBytes(byte value, byte[] t, long index = 0, long count = -1)
        {
            if (count < 0)
                count = t.LongLength - index;

            var ic = index + count;
            for (long i = index; i < ic; i++)
                t[i] = value;
        }

        /// <summary>Обнуляет массив байтов</summary>
        /// <param name="t">Массив для обнуления</param>
        /// <param name="val">Значение, которое задаётся массиву (последние 7-мь байтов массиву может задаваться значение младшего байта).
        /// Для примера, это может быть значение 0x3737_3737__3737_3737 (в x64 значение 0x37 - это invalide OpCode)
        /// Младший байт по младшему адресу</param>
        /// <param name="index">Индекс начального элемента для обнуления</param>
        /// <param name="count">Количество элементов для обнуления, -1 - обнулять до конца</param>
        /// <returns>Количество обнулённых байтов</returns>
        unsafe public static long ToNull(byte[] t, ulong val = 0, long index = 0, long count = -1)
        {
            fixed (byte* tb = t)
            {
                return ToNull(t.LongLength, tb, val, index, count);
            }
        }

        /// <summary>Обнуляет массив байтов по указателю</summary>
        /// <param name="targetLength">Размер массива для обнуления</param>
        /// <param name="t">Массив для обнуления</param>
        /// <param name="val">Значение, которое задаётся массиву (последние 7-мь байтов массиву может задаваться значение младшего байта).
        /// Для примера, это может быть значение 0x3737_3737__3737_3737 (в x64 значение 0x37 - это invalide OpCode)</param>
        /// <param name="index">Индекс начального элемента для обнуления</param>
        /// <param name="count">Количество элементов для обнуления, -1 - обнулять до конца</param>
        /// <returns>Количество обнулённых байтов</returns>
        unsafe public static long ToNull(long targetLength, byte* t, ulong val = 0, long index = 0, long count = -1)
        {
            if (count < 0)
                count = targetLength - index;

            byte* te = t + targetLength;

            byte* tec = t + index + count;
            byte* tbc = t + index;

            if (tec > te)
            {
                throw new ArgumentOutOfRangeException("tec > te");
                // tec = te;
            }

            if (tbc < t)
                throw new ArgumentOutOfRangeException();

            ulong* tbw = (ulong*)tbc;

            ulong* tew = tbw + ((tec - tbc) >> 3);

            for (; tbw < tew; tbw++)
                *tbw = val;

            byte toEnd = (byte)(((int)(tec - tbc)) & 0x7);

            byte* tbcb = (byte*)tbw;
            byte* tbce = tbcb + toEnd;

            var bval = (byte) val;
            for (; tbcb < tbce; tbcb++)
                *tbcb = bval;


            return tec - tbc;
        }
        /*
        public unsafe static void BytesToNull(byte[] bytes, long firstNotNull = long.MaxValue, long start = 0)
        {
            if (firstNotNull > bytes.LongLength)
                firstNotNull = bytes.LongLength;

            if (start < 0)
                start = 0;

            fixed (byte * b = bytes)
            {
                ulong * lb = (ulong *) (b + start);

                ulong * le = lb + ((firstNotNull - start) >> 3);

                for (; lb < le; lb++)
                    *lb = 0;

                byte toEnd = (byte) (  ((int) (firstNotNull - start)) & 0x7  );

                byte * bb = (byte *) lb;
                byte * be = bb + toEnd;

                for (; bb < be; bb++)
                    *bb = 0;
            }
        }
        */

        /// <summary>Преобразует 4-хбайтовое целое в 4 байта в target по индексу start</summary>
        /// <param name="data">4-х байтовое беззнаковое целое для преобразования. Младший байт по младшему адресу</param>
        /// <param name="target">Массив для записи, может быть null</param>
        /// <param name="start">Начальный индекс для записи числа</param>
        public unsafe static void UIntToBytes(uint data, ref byte[] target, long start = 0)
        {
            if (target == null)
                target = new byte[4];

            if (start < 0 || start + 4 > target.LongLength)
                throw new IndexOutOfRangeException();

            fixed (byte * t = target)
            {
                for (long i = start; i < start + 4; i++)
                {
                    *(t + i) = (byte) data;
                    data >>= 8;
                }
            }
        }

        /// <summary>Преобразует 8-хбайтовое целое в 8 байта в target по индексу start</summary>
        /// <param name="data">8-х байтовое беззнаковое целое для преобразования. Младший байт по младшему адресу</param>
        /// <param name="target">Массив для записи, может быть null</param>
        /// <param name="start">Начальный индекс для записи числа</param>
        public unsafe static void ULongToBytes(ulong data, ref byte[] target, long start = 0)
        {
            if (target == null)
                target = new byte[8];

            if (start < 0 || start + 8 > target.LongLength)
                throw new IndexOutOfRangeException();

            fixed (byte * t = target)
            {
                for (long i = start; i < start + 8; i++)
                {
                    *(t + i) = (byte) data;
                    data >>= 8;
                }
            }
        }

        /// <summary>Получает 8-мибайтовое целое число из массива. Младший байт по младшему индексу</summary>
        /// <param name="data">Полученное число</param>
        /// <param name="target">Массив с числом</param>
        /// <param name="start">Начальный элемент, по которому расположено число</param>
        public unsafe static void BytesToULong(out ulong data, byte[] target, long start)
        {
            data = 0;
            if (start < 0 || start + 8 > target.LongLength)
                throw new IndexOutOfRangeException();

            fixed (byte * t = target)
            {
                for (long i = start + 8 - 1; i >= start; i--)
                {
                    data <<= 8;
                    data += *(t + i);
                }
            }
        }

        /// <summary>Получает 4-хбайтовое целое число из массива. Младший байт по младшему индексу</summary>
        /// <param name="data">Полученное число</param>
        /// <param name="target">Массив с числом</param>
        /// <param name="start">Начальный элемент, по которому расположено число</param>
        public unsafe static void BytesToUInt(out uint data, byte[] target, long start)
        {
            data = 0;
            if (start < 0 || start + 4 > target.LongLength)
                throw new IndexOutOfRangeException();

            fixed (byte * t = target)
            {
                for (long i = start + 4 - 1; i >= start; i--)
                {
                    data <<= 8;
                    data += *(t + i);
                }
            }
        }

        /// <summary>Считывает из массива специальную сжатую кодировку числа</summary>
        /// <param name="data">Считанне число</param>
        /// <param name="target">Массив</param>
        /// <param name="start">Стартовый индекс, по которому расположено число</param>
        /// <returns>Количество байтов, которое было считано (размер кодированного числа)</returns>
        public unsafe static int BytesToVariableULong(out ulong data, byte[] target, long start)
        {
            data = 0;
            if (start < 0 || start >= target.Length)
                throw new IndexOutOfRangeException();

            // Вычисляем, сколько именно байтов занимает данное число
            int j = 0;
            for (long i = start; i < target.LongLength; i++, j++)
            {
                int b = target[i] & 0x80;
                if (b == 0)
                    break;
            }
            // Сейчас в j размер числа -1

            if ((target[start + j] & 0x80) > 0)
                throw new IndexOutOfRangeException();

            for (long i = start + j; i >= start; i--)
            {
                byte b = target[i];
                int  c = b & 0x7F;

                data <<= 7;
                data += (byte) c;
            }

            data = 0;
            // Возвращаем полный размер числа
            return j + 1;
        }

        /// <summary>Записывает в массив специальную сжатую кодировку числа</summary>
        /// <param name="data">Число для записи</param>
        /// <param name="target">Массив для записи</param>
        /// <param name="start">Индекс в массиве для записи туда числа</param>
        public unsafe static void VariableULongToBytes(ulong data, ref byte[]? target, long start = 0)
        {
            if (start < 0)
                throw new IndexOutOfRangeException();

            BytesBuilder bb = new BytesBuilder();
            for (long i = start; ; i++)
            {
                byte b = (byte) (data & 0x7F);

                data >>= 7;
                if (data > 0)
                    b |= 0x80;

                if (target == null)
                    bb.addByte(b);
                else
                    target[i] = b;

                if (data == 0)
                    break;
            }

            if (target == null)
            {
                target = new byte[bb.Count];
                BytesBuilder.CopyTo(bb.getBytes(), target, start);
                bb.Clear();
            }

            data = 0;
        }

        /// <summary>Сравнивает два массива. Тайминг-небезопасный метод</summary>
        /// <param name="wellHash">Первый массив</param>
        /// <param name="hash">Второй массив</param>
        /// <param name="count">Количество элементов для сравнения</param>
        /// <param name="indexWell">Начальный индекс для сравнения в массиве wellHash</param>
        /// <returns><see langword="true"/> - если массивы совпадают</returns>
        public unsafe static bool UnsecureCompare(byte[] wellHash, byte[] hash, int count = -1, int indexWell = 0)
        {
            if (count == -1)
            {
                if (wellHash.LongLength != indexWell + hash.LongLength || wellHash.LongLength < indexWell)
                    return false;

                fixed (byte * w1 = wellHash, h1 = hash)
                {
                    byte * w = w1 + indexWell, h = h1, S = w1 + wellHash.LongLength;

                    for (; w < S; w++, h++)
                    {
                        if (*w != *h)
                            return false;
                    }
                }

                return true;
            }
            else
            {
                if (wellHash.LongLength < indexWell + count || hash.LongLength < count)
                    return false;

                fixed (byte * w1 = wellHash, h1 = hash)
                {
                    byte * w = w1 + indexWell, h = h1, S = w1 + indexWell + count;

                    for (; w < S; w++, h++)
                    {
                        if (*w != *h)
                            return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Сравнивает два массива. Тайминг-небезопасный метод</summary>
        /// <param name="wellHash">Первый массив для сравнения</param>
        /// <param name="hash">Второй массив для сравнения</param>
        /// <param name="i">Индекс эленемта, который не совпадает</param>
        /// <returns><see langword="true"/> - если массивы совпадают</returns>
        public unsafe static bool UnsecureCompare(byte[] wellHash, byte[] hash, out int i)
        {
            i = -1;
            if (wellHash.LongLength != hash.LongLength || wellHash.LongLength < 0)
                return false;

            i++;
            fixed (byte * w1 = wellHash, h1 = hash)
            {
                byte * w = w1, h = h1, S = w1 + wellHash.LongLength;

                for (; w < S; w++, h++, i++)
                {
                    if (*w != *h)
                        return false;
                }
            }

            return true;
        }

        /// <summary>Попытка обнулить UTF-8 строку</summary>
        /// <param name="resultText">Строка для обнуления. Осторожно, resultText.substring(0) может возвращать указатель на ту же строку, т.к. .NET считает строки неизменяемыми</param>
        unsafe public static void ClearString(string resultText)
        {
            if (resultText == null)
                return;

            fixed (char * b = resultText)
            {
                for (int i = 0; i < resultText.Length; i++)
                {
                    // Здесь мы имеем индексацию прямо по символам, не по байтам
                    // То есть он здесь работает по строке, выравниваясь на границы символов, даже если они двухбайтовые
                    *(b + i) = ' ';
                }
            }
        }
    }
}
