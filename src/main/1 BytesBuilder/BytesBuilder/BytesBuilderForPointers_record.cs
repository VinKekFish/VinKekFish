// #define RECORD_DEBUG
#pragma warning disable CA2211
#pragma warning disable CA1816

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using VinKekFish_Utils;
using static VinKekFish_Utils.Utils;
using static VinKekFish_Utils.Memory;
using System.Diagnostics;
using System.Reflection;

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
            public IAllocatorForUnsafeMemoryInterface? allocator = null; /// <summary>Имя записи. Используется в отладочных целях.</summary>
            public string?  Name   = null;

            // Отладочный код
            #if RECORD_DEBUG
            /// <summary>Имя записи для отладки</summary>
            public        string? StackTraceString = null;
                                                                /// <summary>Номер записи для отладки</summary>
            public        nint   DebugNum  = 0;                 /// <summary>Следующий номер записи для отладки</summary>
            public static nint   CurrentDebugNum = 0;
            #endif

            // Конструктор. Не вызывается напрямую
            /// <summary>Этот метод вызывать не надо (если только вы не хотите сделать обёртку под уже выделенную память). Используйте AllocatorForUnsafeMemoryInterface.AllocMemory.<para>При вызове этого метода для создания копии, нужно учитывать, что Dispose обнуляет память. Чтобы Dispose не обнулил память, необходимо обнулить array до вызова Dispose с учётом возможного возникновения исключений.</para></summary>
            public Record(string? Name = null)
            {
                this.Name = Name;
                DoRegisterDestructor(this);

                #if RECORD_DEBUG
                DebugNum = CurrentDebugNum++;
                // if (DebugNum == 7)
                StackTraceString = new System.Diagnostics.StackTrace(true).ToString();
                #endif
            }

            // cryptoprime.BytesBuilderForPointers.Record.doRegisterDestructor
            /// <summary>Регистрирует деструктор для вызова с помощью GC.ReRegisterForFinalize. Иначе деструктор может быть не вызван.</summary>
            /// <param name="obj">Объект, деструктор которого должен быть зарегистрирован для вызова.</param>
            public static void DoRegisterDestructor(object obj)
            {
                try
                {
                    GC.ReRegisterForFinalize(obj);     // Без этого деструктор, обычно, не вызывается
                }
                catch (Exception ex)
                {
                    DoFormatException(ex);
                }
            }

            /// <summary>Создать запись и скопировать туда содержимое массива байтов</summary>
            /// <param name="allocator">Аллокатор памяти, который предоставит выделение памяти посредством вызова AllocMemory</param>
            /// <param name="sourceArray"></param>
            /// <param name="RecordDebugName">Идентификатор записи, для отладки удаления памяти</param>
            public static Record GetRecordFromBytesArray(byte[] sourceArray, IAllocatorForUnsafeMemoryInterface? allocator = null, string? RecordDebugName = null)
            {
                allocator ??= new AllocHGlobal_AllocatorForUnsafeMemory();

                var r = allocator.AllocMemory((nint) sourceArray.LongLength, RecordName: RecordDebugName);

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

            /// <summary>Клонирует запись. Данные внутри записи копируются</summary>
            /// <param name="RecordName">Имя записи, для отладки</param>
            /// <returns>Возвращает полностью скопированный массив, независимый от исходного</returns>
            public object Clone(string? RecordName = null)
            {
                return CloneBytes(this, RecordName: RecordName);
            }

            /// <summary>Клонирует запись. Данные внутри записи копируются из диапазона [start .. PostEnd - 1]</summary>
            /// <param name="start">Начальный элемент для копирования</param>
            /// <param name="PostEnd">Первый элемент, который не надо копировать</param>
            /// <param name="allocator">Аллокатор для выделения памяти, может быть <see langword="null"/>, если у this установлен аллокатор</param>
            /// <param name="destroyRecord">Удалить запись this после того, как она будет склонирована</param>
            /// <param name="RecordName">Имя записи, для отладки</param>
            /// <returns>Возвращает новую запись, являющуюся независимой копией старой записи</returns>
            public Record Clone(nint start = 0, nint PostEnd = -1, IAllocatorForUnsafeMemoryInterface? allocator = null, bool destroyRecord = false, string? RecordName = null)
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
            /// <param name="RecordName">Имя записи, для отладки</param>
            /// <returns>Новая запись, указывающая на тот же самый массив</returns>
            public Record NoCopyClone(nint len = 0, nint shift = 0, string? RecordName = null)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("Record.NoCopyClone");
                if (shift < 0 || shift >= this.len)
                    throw new ArgumentOutOfRangeException(nameof(shift));

                if (len <= 0)
                {
                    len = this.len - shift + len;
                }

                if (len + shift > this.len || len == 0)
                    throw new ArgumentOutOfRangeException(nameof(len));

                var r = new Record()
                {
                    len       = len,
                    array     = this.array + shift,
                    Name      = RecordName ?? this.Name
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
                    throw new ArgumentOutOfRangeException(nameof(PostEnd), "BytesBuilderForPointers.Record.CloneToSafeBytes: PostEnd out of range");

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
            public readonly List<Record> inLinks = new(0);

            /// <summary>Очищает и освобождает выделенную область памяти</summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>Вызывает Dispose(true)</summary>
            public void Free()
            {
                Dispose(true);
            }

            public readonly static List<string> errorsInDispose_List = new();
            protected static volatile bool _errorsInDispose = false;/// <summary>Если true, то была ошибка либо в деструкторе Record, либо Record.Dispose, либо в других классах, которые используют флаги "doException...". Это может быть только установлено, но не сброшено. Данный флаг используется и в других классах для того, чтобы показать аналогичные ошибки в Dispose</summary>
            public    static          bool  ErrorsInDispose
            {
                get => _errorsInDispose;
                set
                {
                    _errorsInDispose = true;

                    lock (errorsInDispose_List)
                    errorsInDispose_List.Add(  new StackTrace(true).ToString()  );

                    if (!value)
                        throw new ArgumentOutOfRangeException("Record: errorsInDispose can be set only to true");
                }
            }

            /// <summary>Очищает массив и освобождает выделенную под него память</summary>
            /// <param name="disposing">true, если вызов происходит из-вне деструктора</param>
            protected virtual void Dispose(bool disposing)
            {
                if (isDisposed)
                {
                    if (disposing == false)
                        return;

                    ErrorsInDispose = true;
                    var msg = $"BytesBuilderForPointers.Record Dispose() executed twice. For Record with Name: {Name}";
                    if (doExceptionOnDisposeTwiced)
                        throw new Exception(msg);
                    else
                        Console.Error.WriteLine(msg);

                    return;
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
                            // if (!inLinks[i].isDisposed)
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
                {
                    ErrorsInDispose = true;

                    var msg = $"BytesBuilderForPointers.Record ~Record() executed with a not disposed Record. For Record with Name: {Name}";
                    if (doExceptionOnDisposeInDestructor)
                        throw new Exception(msg);
                    else
                        Console.Error.WriteLine(msg);
                }
            }

                                                                                        /// <summary>Если true, то в деструкторе могут быть сгенерированны исключения, если объект не был освобождён ранее. В противном случае, будет только установлен флаг errorsInDispose</summary>
            public static bool doExceptionOnDisposeInDestructor = true;                 /// <summary>Если true, то может быть вызвано исключение при повторном вызове Dispose. В противном случае, будет только установлен флаг errorsInDispose</summary>
            public static bool doExceptionOnDisposeTwiced       = true;

            /// <summary>Деструктор. Выполняет очистку памяти, если она ещё не была вызвана (с исключением)</summary>
            ~Record()
            {
                Dispose(false);
            }
                                                                                    /// <summary>Возвращает ссылку на Span, которая представляет содержимое данной записи</summary>
            public static implicit operator Span<byte> (Record? t)
            {
                if (t == null)
                    return null;
                if (t.len > int.MaxValue)
                    throw new ArgumentOutOfRangeException($"BytesBuilderForPointers.Record.implicit operator Span<byte> (Record? t): record.len > int.MaxValue ({t.len})");

                return new Span<byte>(t.array, (int) t.len);
            }
                                                                                    /// <summary>Возвращает ссылку на ReadOnlySpan, которая представляет содержимое данной записи</summary>
            public static implicit operator ReadOnlySpan<byte> (Record? t)
            {
                if (t == null)
                    return null;
                if (t.len > int.MaxValue)
                    throw new ArgumentOutOfRangeException($"BytesBuilderForPointers.Record.implicit operator ReadOnlySpan<byte> (Record? t): record.len > int.MaxValue ({t.len})");

                return new ReadOnlySpan<byte>(t.array, (int) t.len);
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
                    throw new ArgumentOutOfRangeException(nameof(len), "in '>>' operator");

                var r = new Record()
                {
                    array     = a.array + len,
                    len       = a.len   - len
                };

                r.allocator = new AllocHGlobal_NoCopy(a, r);
                return r;
            }

                                                                                    /// <summary>Уменьшает длину записи на subtracted. Например, r &lt;&lt; 128 возвращает запись res: res.array=r.array, res.len=r.len-128</summary>
            public static Record operator <<(Record a, nint subtracted)
            {
                if (subtracted < 0)
                    throw new ArgumentOutOfRangeException(nameof(subtracted), "subtracted < 0");
                if (a.len <= subtracted)
                    throw new ArgumentOutOfRangeException(nameof(subtracted), "in Record.'<<' operator: a.len <= subtracted");

                var r = new Record()
                {
                    array     = a.array,
                    len       = a.len - subtracted
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
                                                                            /// <summary>Конкатенация двух массивов</summary>
            public static Record operator |(Record a, Record b)
            {
                var allocator = a.allocator ?? b.allocator ?? throw new InvalidOperationException("Record.| : a.allocator ?? b.allocator == null. " + a.Name + " | " + b.Name);
                var result    = allocator.AllocMemory(a.len + b.len, a.Name + " | " + b.Name);

                Concat(result, a, b);
// TODO: tests
                return result;
            }
                                                                            /// <summary>Конкатенация двух массивов a и b в массив result</summary>
            public static void Concat(Record result, Record a, Record b)
            {
                if (result.len < a.len + b.len)
                    throw new ArgumentOutOfRangeException(nameof(result), "result.len < a.len + b.len");

                var cur = BytesBuilder.CopyTo(a.len, result.len, a, result);
                          BytesBuilder.CopyTo(b.len, result.len, b, result, cur);
            }

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
                    throw new ArgumentOutOfRangeException(nameof(start), "postEnd - start > b.len");

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
            public void CheckRange(nint start, nint end)
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
                        throw new ArgumentOutOfRangeException($"index >= len ({index} >= {len})");
                    if (index < 0)
                        throw new ArgumentOutOfRangeException($"index < 0 ({index} < 0)");

                    return this.array[index];
                }
                set
                {
                    if (index >= len)
                        throw new ArgumentOutOfRangeException($"index >= len ({index} >= {len})");
                    if (index < 0)
                        throw new ArgumentOutOfRangeException($"index < 0 ({index} < 0)");

                    this.array[index] = value;
                }
            }
        }


        /// <summary>Интерфейс описывает способ выделения памяти. Реализация: AllocHGlobal_AllocatorForUnsafeMemory</summary>
        public interface IAllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Размер выделяемого блока памяти</param>
            /// <param name="RecordName">Имя записи: для отладочных целей</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public Record AllocMemory(nint len, string? RecordName = null);

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

        public class AllocHGlobal_NoCopy : IAllocatorForUnsafeMemoryInterface
        {
            public readonly Record sourceRecord;
            public AllocHGlobal_NoCopy(Record sourceRecord, Record newRecord)
            {
                this.sourceRecord = sourceRecord;
                lock (sourceRecord.inLinks)
                    sourceRecord.inLinks.Add(newRecord);
            }

            public Record AllocMemory(nint len, string? RecordName = null)
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
        public class AllocHGlobal_AllocatorForUnsafeMemory : IAllocatorForUnsafeMemoryInterface
        {
            protected volatile nint _memAllocated = 0;
            public nint MemAllocated { get => _memAllocated; }

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
            // Эти значения должны быть readonly, иначе mmap будет получать неверный length
                                                                                                    /// <summary>Показатель степени значения выравнивания памяти</summary>
            public readonly byte alignmentDegree = 0;
            public readonly nint alignmentSize, alignmentAnd;

            public AllocHGlobal_AllocatorForUnsafeMemory()
            {
                Memory.Init();

                this.alignmentDegree = 0;
                if (Memory.memoryLockType.HasFlag(MemoryLockType.incorrect))
                    this.alignmentDegree = 6;

                alignmentSize = 1 << alignmentDegree;
                alignmentAnd = alignmentSize - 1;

                // Обнуляем alignmentSize, чтобы не выделять дополнительные байты памяти, если нет выравнивания
                if (alignmentAnd == 0)
                    alignmentSize = 0;

                if (alignmentSize < 0)
                    throw new ArgumentOutOfRangeException("alignmentSize", "AllocHGlobal_AllocatorForUnsafeMemory: alignmentSize < 0");
            }

            /// <summary>Размер отступов контрольных значений. И левый, и правый отступы имеют одни и те же значения</summary>
            public const nint ControlPaddingSize = 128;

            public nint GetFullSizeToAllocate(nint len)
            {
                return checked(  len + alignmentSize * 2 + ControlPaddingSize * 2  );
            }

            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Длина выделяемого участка памяти</param>
            /// <param name="RecordName">Имя структуры. Для отладки, может быть null.</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public virtual Record AllocMemory(nint len, string? RecordName = null)
            {
                if (len < 1)
                    throw new ArgumentOutOfRangeException(nameof(len), "AllocHGlobal_AllocatorForUnsafeMemory: len must be > 0");

                // ptr никогда не null, если не хватает памяти, то будет OutOfMemoryException
                // alignmentSize домножаем на два, чтобы при невыравненной памяти захватить как память в начале (для выравнивания),
                // так и память в конце - чтобы исключить попадание туда каких-либо других массивов и их конкуренцию за линию кеша
                // ControlPaddingSize - дополнительные отступы для контрольных значений
                var ptr = Alloc(GetFullSizeToAllocate(len));
                var rec = new Record() { len = len, array = (byte*)ptr.ToPointer(), ptr = ptr, allocator = this, Name = RecordName };

                var bmod = ((nint) rec.array) & alignmentAnd;
                if (bmod > 0)
                {
                    // Выравниваем array. Сохранять старое значение не надо, т.к. для удаления используется ptr
                    rec.array += alignmentSize - bmod;
                    if (bmod >= alignmentSize || alignmentSize <= 0)
                        throw new Exception("Record.AllocHGlobal_AllocatorForUnsafeMemory.AllocMemory: fatal error: bmod >= alignmentSize || alignmentSize <= 0");
                }

                // Делаем отступ для того, чтобы записать туда контрольные значения
                rec.array += ControlPaddingSize;
                SetControlValues(len, rec);

                InterlockedIncrement_memAllocated();

#if RECORD_DEBUG
                lock (allocatedRecords)
                    allocatedRecords.Add(rec);
#endif

                return rec;
            }

            public static void SetControlValues(nint len, Record rec)
            {
                var controlVal = (byte)(len >> 8);
                if (controlVal == 0)
                    controlVal = 1;

                var start = rec.array - 1;
                for (nint i = 0; i < ControlPaddingSize; i++, controlVal += 7)
                {
                    *(start - i) = controlVal;
                }

                var end = rec.array + len;
                if (controlVal == 0)
                    controlVal = 129;

                for (nint i = 0; i < ControlPaddingSize; i++, controlVal += 7)
                {
                    *(end + i) = controlVal;
                }
            }

            public enum CheckControlValuesResult { success = 0x55AA36FE, none = 0, ErrorAtStart = 1, ErrorAtEnd = 2 };
            public static CheckControlValuesResult CheckControlValues(Record rec)
            {
                nint len = rec.len;
                var controlVal = (byte)(len >> 8);
                if (controlVal == 0)
                    controlVal = 1;

                var start = rec.array - 1;
                for (nint i = 0; i < ControlPaddingSize; i++, controlVal += 7)
                {
                    if (*(start - i) != controlVal)
                        return CheckControlValuesResult.ErrorAtStart;
                }

                var end = rec.array + len;
                if (controlVal == 0)
                    controlVal = 129;

                for (nint i = 0; i < ControlPaddingSize; i++, controlVal += 7)
                {
                    if (*(end + i) != controlVal)
                    return CheckControlValuesResult.ErrorAtEnd;
                }

                return CheckControlValuesResult.success;
            }

            public class RecordControlValuesException: Exception
            {
                public RecordControlValuesException(CheckControlValuesResult result, Record rec, string? message = null):
                    base($"Record.RecordControlValuesException for the rec with len={rec.len} and the check result={result} with a message:\n{message}")
                {}
            }

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями)</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public virtual void FreeMemory(Record recordToFree)
            {
                var checkResult = CheckControlValues(recordToFree);

                // Marshal.FreeHGlobal(recordToFree.ptr);
                Free(recordToFree.ptr, GetFullSizeToAllocate(recordToFree.len));
                InterlockedDecrement_memAllocated();

                if (checkResult != CheckControlValuesResult.success)
                    throw new RecordControlValuesException(checkResult, recordToFree, $"Record.FreeMemory: CheckControlValues return error {checkResult}. For Record with Name: {recordToFree.Name}");

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
                {
                    Record.ErrorsInDispose = true;
                    var msg = "~AllocHGlobal_AllocatorForUnsafeMemory: Allocator have memory this not been freed";

                    if (Record.doExceptionOnDisposeInDestructor)
                        throw new Exception(msg);
                    else
                        Console.Error.WriteLine(msg);
                }
            }
        }

        public class AllocHGlobal_AllocatorForUnsafeMemory_debug : AllocHGlobal_AllocatorForUnsafeMemory
        {
            public ConcurrentDictionary<Record, string> allocatedRecords_Debug = new(Environment.ProcessorCount, 1024);

            public override Record AllocMemory(nint len, string? RecordName = null)
            {
                var result = base.AllocMemory(len, RecordName);

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
        public class Fixed_AllocatorForUnsafeMemory : IAllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память с помощью сборщика мусора, а потом фиксирует её. Это работает медленнее раза в 3, чем AllocHGlobal_AllocatorForUnsafeMemory</summary>
            public Record AllocMemory(nint len, string? RecordName = null)
            {
                var b  = new byte[len];
                var r  = FixMemory(b);
                r.Name = RecordName;
                return r;
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
