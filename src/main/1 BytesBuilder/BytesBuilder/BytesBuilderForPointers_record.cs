// #define RECORD_DEBUG

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

// #pragma warning disable CA1034 // Nested types should not be visible
namespace cryptoprime
{
    public unsafe partial class BytesBuilderForPointers
    {
        // Документация по состояниям ./Documentation/BytesBuilderForPointers.Record.md
        /// <summary>Класс-обёртка для массивов, доступных по указателю</summary>
        public unsafe class Record: IDisposable, ICloneable
        {                                                               /// <summary>Массив с данными</summary>
            public          byte *   array = null;                      /// <summary>Длина массива с данными</summary>
            public          nint     len   = 0;
                                                                        /// <summary>Данные для удаления, если этот массив выделен с помощью Fixed_AllocatorForUnsafeMemory</summary>
            public GCHandle handle = default;                           /// <summary>Данные для удаления, если этот массив выделен с помощью AllocHGlobal_AllocatorForUnsafeMemory</summary>
            public IntPtr   ptr    = default;
                                                                        /// <summary>Аллокатор, используемый для освобождения памяти в Dispose</summary>
            public AllocatorForUnsafeMemoryInterface? allocator = null;

            // Отладочный код
            #if RECORD_DEBUG
            /// <summary>Имя записи для отладки</summary>
            public        string? DebugName = null;
                                                                /// <summary>Номер записи для отладки</summary>
            public        nint   DebugNum  = 0;                 /// <summary>Следующий номер записи для отладки</summary>
            public static nint   CurrentDebugNum = 0;
            #endif

            // Конструктор. Не вызывается напрямую
            /// <summary>Этот метод вызывать не надо. Используйте AllocatorForUnsafeMemoryInterface.AllocMemory</summary>
            public Record(byte * base_array = null)
            {
                #if RECORD_DEBUG
                DebugNum = CurrentDebugNum++;
                // if (DebugNum == 7)
                DebugName = new System.Diagnostics.StackTrace(true).ToString();
                #endif
            }

            /// <summary>Создать запись и скопировать туда содержимое массива байтов</summary>
            /// <param name="allocator">Аллокатор памяти, который предоставит выделение памяти посредством вызова AllocMemory</param>
            /// <param name="sourceArray"></param>
            /// <returns></returns>
            public static Record getRecordFromBytesArray(byte[] sourceArray, AllocatorForUnsafeMemoryInterface? allocator = null)
            {
                if (allocator == null)
                    allocator = new AllocHGlobal_AllocatorForUnsafeMemory();

                var r = allocator.AllocMemory((nint) sourceArray.LongLength);

                fixed (byte * s = sourceArray)
                {
                    var len = r.len;
                    BytesBuilder.CopyTo(len, len, s, r);
                }

                return r;
            }

            /// <summary>Выводит строковое представление для отладки в формате "{длина}; элемент элемент элемент"</summary>
            public override string ToString()
            {
                if (isDisposed)
                    throw new ObjectDisposedException("Record.ToString");

                var sb = new StringBuilder();

                sb.Append($"length = {len}; ");
                if (array != null)
                {
                    for (int i = 0; i < len; i++)
                    {
                        sb.Append(array[i].ToString("D3") + "  ");
                    }
                }
                else
                {
                    sb.Append("array == null");
                }

                return sb.ToString();
            }

            public const int ToString_LineBlock = 8;
            public const int ToString_Line      = 16;
            public const int ToString_Block     = 4*ToString_Line;
            public const int ToString_Block2    = 4*ToString_Block;
            /// <summary>Выводит строковое представление для отладки в формате "{длина}\n элемент элемент элемент"</summary>
            /// <param name="maxLen">Максимальное количество элементов массива для вывода в строку</param>
            /// <param name="maxStrLen">Максимальная длина строки для вывода результата</param>
            public string ToString(int maxLen = int.MaxValue, int maxStrLen = int.MaxValue)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("Record.ToString");

                var sb = new StringBuilder();

                sb.AppendLine($"length = {len}");
                if (array != null)
                {
                    string? tmp = null;
                    for (int i = 0; i < len && i < maxLen; i++)
                    {
                        tmp = array[i].ToString("D3") + " ";
                        if (sb.Length + tmp.Length > maxStrLen)
                            break;

                        sb.Append(tmp);


                        if (i % ToString_LineBlock == ToString_LineBlock-1)
                            sb.Append("  ");
                        if (i % ToString_Line == ToString_Line-1)
                            sb.AppendLine();
                        if (i % ToString_Block == ToString_Block-1)
                            sb.AppendLine();

                        if (i % ToString_Block2 == ToString_Block2-1)
                        if (i != len - 1)
                        if (i != maxLen - 1)
                        {
                            sb.AppendLine();
                            sb.AppendLine($"{i+1}:");
                        }
                    }
                }
                else
                {
                    sb.Append("{array == null}");
                }

                var str = sb.ToString();
                if (str.Length > maxStrLen)
                    str = str.Substring(0, length: maxStrLen);

                return str;
            }

            /// <summary>Клонирует запись. Данные внутри записи копируются</summary>
            /// <returns>Возвращает полностью скопированный массив, независимый от исходного</returns>
            public object Clone()
            {
                return CloneBytes(this);
            }

            /// <summary>Клонирует запись. Данные внутри записи копируются из диапазона [start .. PostEnd - 1]</summary>
            /// <param name="start">Начальный элемент для копирования</param>
            /// <param name="PostEnd">Первый элемент, который не надо копировать</param>
            /// <param name="allocator">Аллокатор для выделения памяти, может быть <see langword="null"/>, если у this установлен аллокатор</param>
            /// <param name="destroyRecord">Удалить запись this после того, как она будет склонирована</param>
            /// <returns>Возвращает новую запись, являющуюся независимой копией старой записи</returns>
            public Record Clone(nint start = 0, nint PostEnd = -1, AllocatorForUnsafeMemoryInterface? allocator = null, bool destroyRecord = false)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("Record.Clone");

                if (allocator == null && this.allocator == null)
                    throw new ArgumentNullException("BytesBuilderForPointers.Record.Clone: allocator == null && this.allocator == null");

                // allocator будет взят из this, если он null
                var r = CloneBytes(this, allocator, start, PostEnd);

                if (destroyRecord)
                    this.Dispose();

                return r;
            }

            /// <summary>Копирует запись, но без копированя массива и без возможности его освободить. Массив должен быть освобождён в оригинальной записи только после того, как будет закончено использование копии (если это будет не так, возникнет исключение)</summary>
            /// <param name="len">Длина массива либо 0, если длина массива от shift до конца исходного массива, либо иное значение не более this.len. Отрицательное значение будет интерпретировано как исключение определённой длины из массива, дополнительно к shift (newLen = this.len - shift + len)</param>
            /// <param name="shift">Сдвиг начала массива относительно исходной записи</param>
            /// <returns>Новая запись, указывающая на тот же самый массив</returns>
            public Record NoCopyClone(nint len = 0, nint shift = 0)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("Record.NoCopyClone");
                if (shift < 0 || shift >= this.len)
                    throw new ArgumentOutOfRangeException("shift");

                if (len <= 0)
                {
                    len = this.len - shift + len;
                }

                if (len + shift > this.len || len == 0)
                    throw new ArgumentOutOfRangeException("len");

                var r = new Record()
                {
                    len       = len,
                    array     = this.array + shift,
                };

                r.allocator = new AllocHGlobal_NoCopy(this, r);
                return r;
            }

            /// <summary>Копирует содержимое объекта в безопасный массив байтов</summary>
            /// <param name="start">Начальный индекс источника, с которого нужно копировать байты</param>
            /// <param name="PostEnd">Индекс первого байта, который уже не нужно копировать</param>
            /// <param name="destroyRecord">Если true, то this будет уничтожена после этого метода</param>
            /// <returns>Результирующий массив байтов</returns>
            public byte[] CloneToSafeBytes(nint start = 0, nint PostEnd = -1, bool destroyRecord = false)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("Record.CloneToSafeBytes");

                if (PostEnd < 0)
                    PostEnd = this.len;
                if (PostEnd > this.len)
                    throw new ArgumentOutOfRangeException("PostEnd", "BytesBuilderForPointers.Record.CloneToSafeBytes: PostEnd out of range");

                var result = new byte[PostEnd - start];
                fixed (byte * r = result)
                {
                    var len = (nint) result.LongLength;
                    BytesBuilder.CopyTo(this.len, len, this.array, r, 0, len, start);
                }

                if (destroyRecord)
                {
                    this.Dispose();
                }

                return result;
            }

            /// <summary>Очищает выделенную область памяти (пригодно для последующего использования). Для освобождения памяти используйте Dispose()</summary>
            public void Clear()
            {
                if (array != null)
                if (len > 0)
                    BytesBuilder.ToNull(len, array);
            }

            /// <summary>Если true, то объект уже уничтожен</summary>
            public bool isDisposed = false;     // Оставлено public, чтобы обеспечить возможность повторного использования того же объекта

            /// <summary>Количество входящих ссылок, полученные NoCopyClone и т.п.
            /// <para>Синхронизация осуществляется при помощи lock (inLinks) либо при помощи класса AllocHGlobal_NoCopy</para></summary>
            public readonly List<Record> inLinks = new List<Record>(0);

            /// <summary>Очищает и освобождает выделенную область памяти</summary>
            public void Dispose()
            {
                Dispose(true);
            }

            /// <summary>Вызывает Dispose()</summary>
            public void Free()
            {
                Dispose();
            }

            /// <summary>Очищает массив и освобождает выделенную под него память</summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                if (isDisposed)
                {
                    if (disposing == false)
                        return;

                    throw new Exception("BytesBuilderForPointers.Record Dispose() executed twice");
                }
                else
                    GC.SuppressFinalize(this);

                bool allocatorExists = allocator != null || array != null;

                lock (inLinks)
                {
                    for (int i = 0; i < inLinks.Count; i++)
                    {
                        try
                        {
                            inLinks[i].Dispose();
                        }
                        catch
                        {}
                    }
                    inLinks.Clear();
                }

                if (allocator is AllocHGlobal_NoCopy)
                    allocatorExists = false;
                else
                if (allocatorExists)
                    Clear();

                allocator?.FreeMemory(this);

                len       = 0;
                array     = null;
                ptr       = default;
                handle    = default;
                allocator = null;

                isDisposed = true;
                // .NET может избегать вызова десктруктора, а исключение может быть не залогировано при завершении программы.
                // Если аллокатора нет, то и вызывать Dispose не обязательно
                if (!disposing && allocatorExists)
                    throw new Exception("BytesBuilderForPointers.Record ~Record() executed with a not disposed Record");
            }

            /// <summary>Деструктор. Выполняет очистку памяти, если она ещё не была вызвана (с исключением)</summary>
            ~Record()
            {
                Dispose(false);
            }
                                                                                    /// <summary>Возвращает ссылку на массив</summary>
            public static implicit operator void * (Record? t)
            {
                if (t == null)
                    return null;

                return t.array;
            }
                                                                                    /// <summary>Возвращает ссылку на массив</summary>
            public static implicit operator byte * (Record? t)
            {
                if (t == null)
                    return null;

                return t.array;
            }
                                                                                    /// <summary>Возвращает ссылку на массив, преобразованную в тип ushort * </summary>
            public static implicit operator ushort * (Record? t)
            {
                if (t == null)
                    return null;

                return (ushort *) t.array;
            }
                                                                                    /// <summary>Возвращает ссылку на массив, преобразованную в тип unint * </summary>
            public static implicit operator ulong * (Record? t)
            {
                if (t == null)
                    return null;

                return (ulong *) t.array;
            }
                                                                                    /// <summary>Смещает начало записи на len. Например, r &gt;&gt; 128 возвращает запись res: res.array=r.array+128, res.len=r.len-128</summary>
            public static Record operator >>(Record a, nint len)
            {
                if (a.len <= len)
                    throw new ArgumentOutOfRangeException("len", "in '>>' operator");

                var r = new Record()
                {
                    array     = a.array + len,
                    len       = a.len   - len
                };

                r.allocator = new AllocHGlobal_NoCopy(a, r);
                return r;
            }

                                                                                    /// <summary>Уменьшает длину записи. Например, r &lt;&lt; 128 возвращает запись res: res.array=r.array, res.len=r.len-128</summary>
            public static Record operator <<(Record a, nint len)
            {
                if (a.len <= len)
                    throw new ArgumentOutOfRangeException("len", "in '<<' operator");

                var r = new Record()
                {
                    array     = a.array,
                    len       = a.len - len
                };

                r.allocator = new AllocHGlobal_NoCopy(a, r);
                return r;
            }

                                                                                    /// <summary>Смещает запись за конец старой записи, новая запись длиной len. var r = a &amp; Len возвратит запись r, длиной Len, начинающуюся после конца записи a. То есть r.array = a.array + a.len, r.len = Len</summary>
            public static Record operator &(Record a, nint len)
            {
                var r = new Record()
                {
                    array     = a.array + a.len,
                    len       = len
                };

                r.allocator = new AllocHGlobal_NoCopy(a, r);
                return r;
            }
            /* Это преобразование типов входит в конфликт с удобством и другими преобразованиями
                                                                                /// <summary>Возвращает длину данных</summary>
            public static implicit operator long (Record t)
            {
                if (t == null)
                    return 0;

                return t.len;
            }*/


            /// <summary>Сравнивает две записи</summary>
            /// <param name="b">Вторая запись для сравнения</param>
            /// <returns>true, если значения массивов в записях равны</returns>
            public bool UnsecureCompare(Record b)
            {
                if (this.len != b.len)
                    return false;

                var aa = this.array;
                var ba = b   .array;

                for (nint i = 0; i < len; i++)
                {
                    if (aa[i] != ba[i])
                        return false;
                }

                return true;
            }

            /// <summary>Сравнивает две записи</summary>
            /// <param name="b">Вторая запись для сравнения. Если размеры записей разные, то в b нужно передавать большую запись (т.к. иначе размеры записей не совпадут)</param>
            /// <param name="start">Самый первый элемент для сравнения в массиве b</param>
            /// <param name="postEnd">Элемент, идущий после последнего элемента для сравнения. 0 == b.len. Отрицательное значение равно b.len+postEnd</param>
            /// <returns>true, если значения массивов в записях равны</returns>
            public bool UnsecureCompare(Record b, nint start, nint postEnd = 0)
            {
                if (postEnd <= 0)
                    postEnd = b.len + postEnd;

                var lenb = postEnd - start;
                if (lenb > b.len)
                    throw new ArgumentOutOfRangeException("start", "postEnd - start > b.len");

                if (this.len != lenb)
                    return false;

                var aa = this.array;
                var ba = b   .array;

                for (nint i = 0; i < len; i++)
                {
                    if (aa[i] != ba[i + start])
                        return false;
                }

                return true;
            }

            /// <summary>Проверяет, что индексы start и end лежат внутри массива. start &lt;= end. Если условия не выполнены, то генерируется исключение.</summary>
            /// <param name="start">Индекс для проверки в границах массива</param>
            /// <param name="end">Индекс для проверки в границах массива</param>
            public void checkRange(nint start, nint end)
            {
                if (end < start)
                    throw new ArgumentOutOfRangeException("end < start");

                if (start >= len)
                    throw new ArgumentOutOfRangeException("start >= len");

                if (end >= len)
                    throw new ArgumentOutOfRangeException("end >= len");

                if (start < 0)
                    throw new ArgumentOutOfRangeException("start > len");

                if (end < 0)
                    throw new ArgumentOutOfRangeException("end > len");
            }

            public byte this[nint index]
            {
                get
                {
                    if (index >= len)
                        throw new ArgumentOutOfRangeException("index >= len");
                    if (index < 0)
                        throw new ArgumentOutOfRangeException("index >= len");

                    return this.array[index];
                }
                set
                {
                    if (index >= len)
                        throw new ArgumentOutOfRangeException("index >= len");
                    if (index < 0)
                        throw new ArgumentOutOfRangeException("index >= len");

                    this.array[index] = value;
                }
            }
        }


        /// <summary>Интерфейс описывает способ выделения памяти. Реализация: AllocHGlobal_AllocatorForUnsafeMemory</summary>
        public interface AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Размер выделяемого блока памяти</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public Record AllocMemory(nint len);

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями). Должен вызываться автоматически в Record</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public void   FreeMemory (Record recordToFree);

            /// <summary>Производит фиксацию в памяти массива (интерфейс должен реализовывать либо AllocMemory(nint), либо этот метод, либо оба)</summary>
            /// <param name="array">Исходный массив</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(byte[] array);

            /// <summary>Производит фиксацию в памяти объекта, длиной length байтов</summary>
            /// <param name="array">Закрепляемый объект</param>
            /// <param name="length">Длина объекта в байтах. Длины массивов необходимо домножать на размер элемента массива</param>
            /// <returns></returns>
            public Record FixMemory(object array, nint length);
        }

        public class AllocHGlobal_NoCopy : AllocatorForUnsafeMemoryInterface
        {
            public readonly Record sourceRecord;
            public AllocHGlobal_NoCopy(Record sourceRecord, Record newRecord)
            {
                this.sourceRecord = sourceRecord;
                lock (sourceRecord.inLinks)
                    sourceRecord.inLinks.Add(newRecord);
            }

            public Record AllocMemory(nint len)
            {
                throw new NotImplementedException();
            }

            public Record FixMemory(byte[] array)
            {
                throw new NotImplementedException();
            }

            public Record FixMemory(object array, nint length)
            {
                throw new NotImplementedException();
            }

            public void FreeMemory(Record recordToFree)
            {
                lock (sourceRecord.inLinks)
                    sourceRecord.inLinks.Remove(recordToFree);
            }
        }

        /// <summary>Выделяет память с помощью Marshal.AllocHGlobal</summary>
        public class AllocHGlobal_AllocatorForUnsafeMemory : AllocatorForUnsafeMemoryInterface
        {
            protected volatile nint _memAllocated = 0;
            public nint memAllocated { get => _memAllocated; }

            #if RECORD_DEBUG
                public List<Record> allocatedRecords = new List<Record>(1024);
            #endif

            /// <summary>Аналог Interlocked.Increment для nint (в классе Interlocked его нет). Выполняет приращение val на единицу</summary>
            /// <returns>Оригинальное (не изменённое) значение переменной</returns>
            protected nint InterlockedIncrement_memAllocated()
            {
                nint a, b;

                do
                {
                    a = _memAllocated;
                    b = a + 1;                    
                }
                while (Interlocked.CompareExchange(ref _memAllocated, b, a) != a);

                return a;
            }

            /// <summary>Аналог Interlocked.Increment для nint (в классе Interlocked его нет). Выполняет приращение val на единицу</summary>
            /// <returns>Оригинальное (не изменённое) значение переменной</returns>
            protected nint InterlockedDecrement_memAllocated()
            {
                nint a, b;

                do
                {
                    a = _memAllocated;
                    b = a - 1;                    
                }
                while (Interlocked.CompareExchange(ref _memAllocated, b, a) != a);

                return a;
            }
                                                                                                    /// <summary>Показатель степени значения выравнивания памяти</summary>
            public byte alignmentDegree = 0;


            public AllocHGlobal_AllocatorForUnsafeMemory(byte alignmentDegree = 0)
            {
                this.alignmentDegree = alignmentDegree;
            }

            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Длина выделяемого участка памяти</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public virtual Record AllocMemory(nint len)
            {
                nint alignmentSize = 1 << alignmentDegree;
                nint alignmentAnd  = alignmentSize - 1;

                if (len < 1)
                    throw new ArgumentOutOfRangeException("len", "AllocHGlobal_AllocatorForUnsafeMemory.ArgumentOutOfRangeException: len must be > 0");

                // ptr никогда не null, если не хватает памяти, то будет OutOfMemoryException
                // alignmentSize домножаем на два, чтобы при невыравненной памяти захватить как память в начале (для выравнивания),
                // так и память в конце - чтобы исключить попадание туда каких-либо других массивов и их конкуренцию за линию кеша
                var ptr = Marshal.AllocHGlobal(len + alignmentSize*2);
                var rec = new Record() { len = len, array = (byte *) ptr.ToPointer(), ptr = ptr, allocator = this };

                var bmod = (nint) rec.array & alignmentAnd;
                if (bmod > 0)
                {
                    // Выравниваем array. Сохранять старое значение не надо, т.к. для удаления используется ptr
                    rec.array += alignmentSize - bmod;
                }

                InterlockedIncrement_memAllocated();

                #if RECORD_DEBUG
                lock (allocatedRecords)
                    allocatedRecords.Add(rec);
                #endif

                return rec;
            }

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями)</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public virtual void FreeMemory(Record recordToFree)
            {
                Marshal.FreeHGlobal(recordToFree.ptr);
                InterlockedDecrement_memAllocated();

                #if RECORD_DEBUG
                lock (allocatedRecords)
                    allocatedRecords.Remove(recordToFree);
                #endif
            }

            /// <summary>Не реализовано</summary>
            public Record FixMemory(byte[] array)
            {
                throw new NotImplementedException();
            }

            /// <summary>Не реализовано</summary>
            public Record FixMemory(object array, nint length)
            {
                throw new NotImplementedException();
            }

            ~AllocHGlobal_AllocatorForUnsafeMemory()
            {
                if (_memAllocated > 0)
                    throw new Exception("~AllocHGlobal_AllocatorForUnsafeMemory: Allocator have memory this not been freed");
            }
        }

        public class AllocHGlobal_AllocatorForUnsafeMemory_debug : AllocHGlobal_AllocatorForUnsafeMemory
        {
            public ConcurrentDictionary<Record, string> allocatedRecords_Debug = new ConcurrentDictionary<Record, string>(Environment.ProcessorCount, 1024);

            public override Record AllocMemory(nint len)
            {
                var result = base.AllocMemory(len);

                allocatedRecords_Debug.TryAdd(  result, new System.Diagnostics.StackTrace(true).ToString()  );

                return result;
            }

            public override void FreeMemory(Record recordToFree)
            {
                base.FreeMemory(recordToFree);

                while (!allocatedRecords_Debug.TryRemove(recordToFree, out string? _))
                {}
            }
        }

        /// <summary>Выделяет память для массива с помощью его фиксации: то есть используется обычный сборщик мусора и GCHandle.Alloc</summary>
        public class Fixed_AllocatorForUnsafeMemory : AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память с помощью сборщика мусора, а потом фиксирует её. Это работает медленнее раза в 3, чем AllocHGlobal_AllocatorForUnsafeMemory</summary>
            public Record AllocMemory(nint len)
            {
                var b = new byte[len];
                return FixMemory(b);
            }

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями)</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public void FreeMemory(Record recordToFree)
            {
                recordToFree.handle.Free();
            }

            /// <summary>Производит фиксацию в памяти массива</summary>
            /// <param name="array">Исходный массив</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(byte[] array)
            {
                return FixMemory(array, checked((nint) array.LongLength));
            }

            /// <summary>Производит фиксацию в памяти массива</summary>
            /// <param name="array">Исходный массив</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(ushort[] array)
            {
                nint l = checked(  (nint) array.LongLength * sizeof(ushort)  );
                return FixMemory(array, l);
            }

            /// <summary>Производит фиксацию в памяти массива</summary>
            /// <param name="array">Исходный массив</param>
            /// <param name="length">Длина массива</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(object array, nint length)
            {
                var h = GCHandle.Alloc(array, GCHandleType.Pinned);
                var p = h.AddrOfPinnedObject();

                return new Record()
                {
                    len       = length,
                    ptr       = p,
                    array     = (byte *) p.ToPointer(),
                    handle    = h,
                    allocator = this
                };
            }
        }
    }
}
#pragma warning restore CA1034 // Nested types should not be visible
