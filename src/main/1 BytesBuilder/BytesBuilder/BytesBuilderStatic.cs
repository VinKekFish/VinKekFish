using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using static cryptoprime.BytesBuilderForPointers;

namespace cryptoprime
{// TODO: Добавить документацию по использованию, можно прямо сюда
    /// <summary>
    /// BytesBuilderStatic
    /// BytesBuilderForPointers, реализованный с циклическим буфером
    /// Класс позволяет собирать большой блок байтов из более мелких
    /// Класс непотокобезопасный (при его использовании необходимо синхронизировать доступ к классу вручную)
    /// </summary>
    public unsafe partial class BytesBuilderStatic: IDisposable
    {                                                                           /// <summary>Размер циклического буфера. Это максимальный размер хранимых данных. Изменяется функцией Resize</summary>
        public nint size;                                                       /// <summary>Аллокатор для выделения памяти</summary>
        public readonly AllocatorForUnsafeMemoryInterface? allocator = null;
                                                                                /// <summary>Минимально возможный размер циклического буфера</summary>
        public const int MIN_SIZE = 2;
        public BytesBuilderStatic(nint Size, AllocatorForUnsafeMemoryInterface? allocator = null)
        {
            if (Size < MIN_SIZE)
                throw new ArgumentOutOfRangeException("BytesBuilderStatic.BytesBuilderStatic: Size < MIN_SIZE");

            this.allocator = allocator ?? new AllocHGlobal_AllocatorForUnsafeMemory();

            Resize(Size);
        }

        /// <summary>Изменяет размер циклического буфера без потери данных.<para>При многопоточных вызовах синхронизация остаётся на пользователе.</para></summary>
        /// <param name="Size">Новый размер</param>
        public void Resize(nint Size)
        {
            if (Size < MIN_SIZE)
                throw new ArgumentOutOfRangeException("BytesBuilderStatic.Resize: Size < MIN_SIZE");
            if (Count > Size)
                throw new ArgumentOutOfRangeException("BytesBuilderStatic.Resize: Count > Size");

            var newRegion = allocator?.AllocMemory(Size) ?? throw new ArgumentNullException("BytesBuilderStatic.Resize");
            var oldRegion = region;

            if (oldRegion != null)
            {
                if (count > 0)
                    ReadBytesTo(newRegion.array, count);

                oldRegion.Dispose();
                oldRegion = null;
            }

            region = newRegion;
            size   = Size;
            bytes  = region.array;
            after  = bytes + region.len;
            Start  = 0;
            End    = Count;

            if (Count == size)
            {
                End = 0;
            }
        }

        /// <summary>Прочитать count байтов из циклического буфера в массив target, не удаляя прочитанные значения</summary>
        /// <param name="target">Целевой массив, куда копируются значения</param>
        /// <param name="count">Количество байтов для копирования. Если меньше нуля, то копируются все байты</param>
        public void ReadBytesTo(byte* target, nint count = -1)
        {
            if (region == null)
                throw new ObjectDisposedException("BytesBuilderStatic");

            if (count < 0)
                count = Count;

            if (count > Count)
                throw new ArgumentOutOfRangeException("ReadBytesTo: count > Count");

            var s1 = bytes + Start;
            var l1 = len1;
            var l2 = len2;

            if (count <= 0)
                throw new ArgumentOutOfRangeException("ReadBytesTo: count <= 0");
            if (l1 + l2 != this.count)
                throw new Exception("ReadBytesTo: Fatal algorithmic error: l1 + l2 != this.count");

            if (l1 > 0)
            BytesBuilder.CopyTo(l1, count, s1, target);

            var lc = count - l1;
            if (lc > 0)
                if (l2 >= lc)
                    BytesBuilder.CopyTo(l2, lc, bytes, target + l1);
                else
                    throw new Exception("ReadBytesTo: Fatal algorithmic error: condition not met: l2 > 0");
        }

        /// <summary>Записывает байты в циклический буфер (добавляет байты в конец)</summary>
        /// <param name="source">Источник, из которого берутся данные.
        /// <para>Если передаётся Record, убедитесь, что не передаётся что-то типа "record + 16" - это будет неверное приведение типов; верное приведение типов "record.array+16"</para></param>
        /// <param name="countToWrite">Количество байтов для добавления</param>
        public void WriteBytes(byte* source, nint countToWrite)
        {
            if (region == null)
                throw new ObjectDisposedException("BytesBuilderStatic");

            if (count + countToWrite > size)
                throw new ArgumentOutOfRangeException("WriteBytes: count + countToWrite > size");
            if (countToWrite <= 0)
                throw new ArgumentOutOfRangeException("WriteBytes: countToWrite <= 0");

            if (End >= Start)
            {
                var s1 = bytes + End;
                var l1 = (nint) (after - s1);

                var A  = l1 > 0 ? BytesBuilder.CopyTo(countToWrite, l1, source, s1) : 0;
                count += A;
                End   += A;

                if (A != l1 && A != countToWrite)
                    throw new Exception("WriteBytes: Fatal algorithmic error: A != l1 && A != countToWrite");

                // Если мы записали всю первую половину
                if (End >= size)
                {
                    if (End == size)
                        End = 0;
                    else
                        throw new Exception("WriteBytes: Fatal algorithmic error: End > size");
                }
                else
                if (A < countToWrite)
                    throw new Exception("WriteBytes: Fatal algorithmic error: End < size && A < countToWrite");


                // Если ещё остались байты для записи
                if (A < countToWrite)
                    WriteBytes(source + A, countToWrite - A);
            }
            else    // End < Start, запись во вторую половину циклического буфера
            {
                var s1 = bytes + End;
                var l1 = (nint)(   (bytes + Start) - s1   );

                if (l1 < countToWrite)
                    throw new Exception("WriteBytes: Fatal algorithmic error: l1 < countToWrite");

                var A  = BytesBuilder.CopyTo(countToWrite, l1, source, s1);
                count += A;
                End   += A;

                if (A != countToWrite)
                    throw new Exception("WriteBytes: Fatal algorithmic error: A != countToWrite");

                if (End > Start)
                    throw new Exception("WriteBytes: Fatal algorithmic error: End > Start");
            }
        }

                                                                                /// <summary>Адрес циклического буфера == region.array</summary>
        protected byte *  bytes  = null;                                        /// <summary>Поле, указывающее на первый байт после конца региона памяти буфера. Он не меняется при добавлении или удалении данных. Только при изменении размера циклического буфера</summary>
        protected byte *  after  = null;                                        /// <summary>Обёртка для циклического буфера</summary>
        protected Record? region = null;
                                                                                /// <summary>Количество всех сохранённых байтов в этом объекте - сырое поле для корректировки значений</summary>
        protected nint count = 0;                                               /// <summary>Количество всех сохранённых байтов в этом объекте</summary>
        public nint Count => count;
                                                                                /// <summary>End - это индекс следующего добавляемого байта. Для Start = 0 поле End должно быть равно размеру сохранённых данных (End == Count); при полном заполнении буфера End = 0</summary>
        protected nint Start = 0, End = 0;

        /// <summary>Получает адрес элемента с индексом index (с учётом смещения первого элемента буфера)</summary>
        /// <param name="index">Индекс получаемого элемента</param>
        /// <returns>Адрес элемента массива</returns>
        public byte * this[nint index]
        {
            get
            {
                if (region == null)
                    throw new ObjectDisposedException("BytesBuilderStatic.RemoveBytes");

                if (index >= count)
                    throw new ArgumentOutOfRangeException();

                var p = bytes + Start + index;

                if (p < after)
                {
                    return p;
                }
                else // End <= Start
                {
                    var len1 = size - Start;    // Длина первой (правой) части массива в циклическом буфере
                    index   -= len1;

                    p = bytes + index;

                    if (p >= after || p < bytes)
                        throw new Exception("WriteBytes: Fatal algorithmic error: p >= after || p < bytes");

                    return p;
                }
            }
        }

        /// <summary>Длина данных, приходящихся на правый (первый) сегмент данных</summary>
        public nint len1
        {
            get
            {
                checked
                {
                    if (End > Start)
                    {
                        return End - Start;
                    }
                    else
                    {
                        if (count == 0)
                            return 0;

                        var r = (nint) (after - (bytes + Start));

                        return r;
                    }
                }
            }
        }

        /// <summary>Длина данных, приходящихся на левый сегмент данных</summary>
        public nint len2
        {
            get
            {
                if (End > Start || count == 0)
                    return 0;

                return End;
            }
        }

        /// <summary>Добавляет блок в объект</summary><param name="bytesToAdded">Добавляемый блок данных. Содержимое копируется</param><param name="len">Длина добавляемого блока данных</param>
        public void add(byte * bytesToAdded, nint len)
        {
            if (count + len > size)
                throw new IndexOutOfRangeException("BytesBuilderStatic.add: count + len > size: many bytes to add");

            WriteBytes(bytesToAdded, len);
        }

        /// <summary>Добавляет массив в сохранённые значения</summary>
        /// <param name="rec">Добавляемый массив (копируется)</param>
        public void add(Record rec)
        {
            add(rec.array, rec.len);
        }

        /// <summary>Очищает циклический буфер</summary>
        /// <param name="fast">fast = <see langword="false"/> - обнуляет выделенный под регион массив памяти</param>
        public void Clear(bool fast = false)
        {
            if (region == null)
                throw new ObjectDisposedException("BytesBuilderStatic.RemoveBytes");

            count = 0;
            Start = 0;
            End   = 0;

            if (!fast)
                BytesBuilder.ToNull(size, bytes);
        }

        /// <summary>Этот метод для тестов: показывает, что все значения внутреннего буфера равны нулю; проверяет все байты вне зависимости от значения count</summary>
        /// <returns>true, если все значения внутреннего буфера равны нулю</returns>
        public bool isEntireNull()
        {
            if (region == null)
                throw new ObjectDisposedException("BytesBuilderStatic.RemoveBytes");

            for (int i = 0; i < size; i++)
            {
                if (bytes[i] != 0)
                    return false;
            }

            return true;
        }

        /// <summary>Создаёт массив байтов, включающий в себя все сохранённые массивы</summary>
        /// <param name="resultCount">Размер массива-результата (если нужны все байты resultCount = -1)</param>
        /// <param name="resultA">Массив, в который будет записан результат. Если resultA = null, то массив создаётся</param>
        /// <param name="allocator">Аллокатор, который позволяет функции выделять память, если resultA == null. Если null, используется this.allocator</param>
        /// <returns></returns>
        public Record getBytes(nint resultCount = -1, Record? resultA = null, AllocatorForUnsafeMemoryInterface? allocator = null)
        {
            if (resultCount <= -1)
                resultCount = count;

            if (resultCount > count)
            {
                throw new System.ArgumentOutOfRangeException("resultCount", "resultCount is too large: resultCount > count || resultCount == 0");
            }

            if (resultCount == 0)
            {
                throw new System.ArgumentOutOfRangeException("resultCount", "resultCount == 0");
            }

            if (resultA != null && resultA.len < resultCount)
                throw new System.ArgumentOutOfRangeException("resultA", "resultA is too small");
            if (resultA != null && resultA.isDisposed)
                throw new ArgumentOutOfRangeException(nameof(resultA), "BytesBuilderStatic.getBytesAndRemoveIt: resultA.isDisposed");

            var result = resultA ?? allocator?.AllocMemory(resultCount) ?? this?.allocator?.AllocMemory(resultCount) ?? throw new ArgumentNullException("BytesBuilderStatic.getBytes");

            ReadBytesTo(result.array, resultCount);

            return result;
        }

        /// <summary>Удаляет байты из начала массива</summary>
        /// <param name="len">Количество байтов к удалению</param>
        public void RemoveBytes(nint len)
        {
            if (region == null)
                throw new ObjectDisposedException("BytesBuilderStatic.RemoveBytes");

            if (len > count)
                throw new ArgumentOutOfRangeException();

            // Обнуление удаляемых байтов
            // Не сказать, что это очень эффективно
            for (nint i = 0; i < len; i++)
            {
                *this[i] = 0;
            }

            Start += len;
            count -= len;
            if (Start >= size)
            {
                Start -= size;

                if (Start + count != End)
                    throw new Exception("BytesBuilderStatic.RemoveBytes: Fatal algorithmic error: Start + count != End");
            }
        }

        /// <summary>Создаёт массив байтов, включающий в себя count байтов из буфера, и удаляет их с очисткой</summary>
        /// <param name="result">Массив, в который будет записан результат. Уже должен быть выделен. result != <see langword="null"/>.</param>
        /// <param name="count">Длина запрашиваемых данных</param>
        /// <returns>Массив result</returns>
        public Record getBytesAndRemoveIt(Record result, nint count = -1)
        {
            if (count < 0)
                count = Math.Min(this.count, result.len);

            if (count > result.len)
                throw new ArgumentOutOfRangeException(nameof(result), "BytesBuilderStatic.getBytesAndRemoveIt: count > result.len");
            if (count > this.count)
                throw new ArgumentOutOfRangeException(nameof(count), "BytesBuilderStatic.getBytesAndRemoveIt: count > this.count");
            if (result.isDisposed)
                throw new ArgumentOutOfRangeException(nameof(result), "BytesBuilderStatic.getBytesAndRemoveIt: result.isDisposed");

            ReadBytesTo(result.array, count);
            RemoveBytes(count);

            return result;
        }

        /// <summary>Создаёт массив байтов, включающий в себя count байтов из буфера, и удаляет их с очисткой</summary>
        /// <param name="result">Массив, в который будет записан результат. Уже должен быть выделен. result != <see langword="null"/>.</param>
        /// <param name="count">Длина запрашиваемых данных</param>
        public void getBytesAndRemoveIt(byte * result, nint count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(result), "BytesBuilderStatic.getBytesAndRemoveIt: count < 0");
            if (count > this.count)
                throw new ArgumentOutOfRangeException(nameof(count), "BytesBuilderStatic.getBytesAndRemoveIt: count > this.count");

            ReadBytesTo(result, count);
            RemoveBytes(count);
        }

        /// <summary>Очищает и освобождает всю небезопасно выделенную под объект память</summary>
        /// <param name="disposing">Всегда true, кроме вызова из деструктора</param>
        public virtual void Dispose(bool disposing = true)
        {
            if (after  == null)
            if (region == null)
                return;

            region?.Dispose();
            region = null;
            after  = null;
            bytes  = null;
            count  = 0;
            Start  = 0;
            End    = 0;

            if (!disposing)
                throw new Exception("~BytesBuilderStatic: region != null");
        }
                                                                /// <summary>Очищает и освобождает всю небезопасно выделенную под объект память</summary>
        public void Dispose()
        {
            Dispose(true);
        }
                                                                 /// <summary>Деструктор</summary>
        ~BytesBuilderStatic()
        {
            Dispose(false);
        }
    }
}
