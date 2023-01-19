using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CA1034 // Nested types should not be visible
namespace cryptoprime
{
    public unsafe partial class BytesBuilderForPointers
    {
        // Документация по состояниям ./Documentation/BytesBuilderForPointers.Record.md
        /// <summary>Класс-обёртка для массивов, доступных по указателю</summary>
        public unsafe class Record: IDisposable, ICloneable
        {                                                               /// <summary>Массив с данными</summary>
            public          byte *   array = null;                      /// <summary>Длина массива с данными</summary>
            public          long     len   = 0;
                                                                        /// <summary>Данные для удаления, если этот массив выделен с помощью Fixed_AllocatorForUnsafeMemory</summary>
            public GCHandle handle = default;                           /// <summary>Данные для удаления, если этот массив выделен с помощью AllocHGlobal_AllocatorForUnsafeMemory</summary>
            public IntPtr   ptr    = default;
                                                                        /// <summary>Аллокатор, используемый для освобождения памяти в Dispose</summary>
            public AllocatorForUnsafeMemoryInterface? allocator = null;

            #if RECORD_DEBUG
            /// <summary>Имя записи для отладки</summary>
            public        string DebugName = null;
                                                                /// <summary>Номер записи для отладки</summary>
            public        long   DebugNum  = 0;                 /// <summary>Следующий номер записи для отладки</summary>
            public static long   CurrentDebugNum = 0;
            #endif

            /// <summary>Этот метод вызывать не надо. Используйте AllocatorForUnsafeMemoryInterface.AllocMemory</summary>
            public Record()
            {
                #if RECORD_DEBUG
                DebugNum = CurrentDebugNum++;
                // if (DebugNum == 7)
                DebugName = new System.Diagnostics.StackTrace(true).ToString();
                #endif
            }

            /// <summary>Выводит строковое представление для отладки в формате "{длина}; элемент элемент элемент"</summary>
            public override string ToString()
            {
                var sb = new StringBuilder();

                sb.AppendLine($"length = {len}; ");
                if (array != null)
                {
                    for (int i = 0; i < len; i++)
                        sb.Append(array[i].ToString("D3") + "  ");
                }
                else
                {
                    sb.Append("array == null");
                }

                return sb.ToString();
            }

            /// <summary>Выводит строковое представление для отладки в формате "{длина}; элемент элемент элемент"</summary>
            /// <param name="maxLen">Максимальное количество элементов массива для вывода в строку</param>
            /// <param name="maxStrLen">Максимальная длина строки для вывода результата</param>
            public string ToString(int maxLen = int.MaxValue, int maxStrLen = int.MaxValue)
            {
                var sb = new StringBuilder();

                sb.AppendLine($"length = {len}");
                if (array != null)
                {
                    string? tmp = null;
                    for (int i = 0; i < len && i < maxLen; i++)
                    {
                        tmp = array[i].ToString("D3") + "  ";
                        if (sb.Length + tmp.Length > maxStrLen)
                            break;

                        sb.Append(tmp);
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
            /// <returns></returns>
            public Record Clone(long start = 0, long PostEnd = -1, AllocatorForUnsafeMemoryInterface? allocator = null)
            {
                if (allocator == null && this.allocator == null)
                    throw new ArgumentNullException("BytesBuilderForPointers.Record.Clone: allocator == null && this.allocator == null");

                // allocator будет взят из this, если он null
                return CloneBytes(this, allocator, start, PostEnd);
            }

            /// <summary>Копирует запись, но без копированя массива и без возможности его освободить. Массив должен быть освобождён в копируемой записи только после того, как будет закончено использование копии</summary>
            /// <param name="len">Длина массива либо -1, если длина массива такая же, как копируемой записи</param>
            /// <returns>Новая запись, указывающая на тот же самый массив</returns>
            public Record NoCopyClone(long len = -1)
            {
                if (len < 0)
                    len = this.len;

                return new Record()
                {
                    len       = len,
                    array     = this.array,
                    allocator = null
                };
            }

            /// <summary>Очищает выделенную область памяти (пригодно для последующего использования). Для освобождения памяти используйте Dispose()</summary>
            public void Clear()
            {
                if (array != null)
                    BytesBuilder.ToNull(len, array);
            }

            /// <summary>Если true, то объект уже уничтожен</summary>
            public bool isDisposed = false;     // Оставлено public, чтобы обеспечить возможность повторного использования того же объекта

            /// <summary>Очищает и освобождает выделенную область памяти</summary>
            // TODO: протестировать на двойной вызов (должно всё работать)
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
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

                    throw new Exception("BytesBuilderForPointers.Record ~Record() executed twice");
                }

                bool allocatorExists = allocator != null || array != null;

                Clear();
                allocator?.FreeMemory(this);

                len        = 0;
                array     = null;
                ptr       = default;
                handle    = default;
                allocator = null;

                isDisposed = true;

                // TODO: Проверить, что это исключение действительно работает, то есть оно будет залогировано при окончании программы
                // Если аллокатора нет, то и вызывать Dispose не обязательно
                if (!disposing && allocatorExists)
                    throw new Exception("BytesBuilderForPointers.Record ~Record() executed");
            }

            /// <summary></summary>
            ~Record()
            {
                Dispose(false);
            }
                                                                                    /// <summary>Возвращает ссылку на массив</summary>
            public static implicit operator byte * (Record t)
            {
                if (t == null)
                    return null;

                return t.array;
            }
                                                                                    /// <summary>Возвращает ссылку на массив, преобразованную в тип ushort * </summary>
            public static implicit operator ushort * (Record t)
            {
                if (t == null)
                    return null;

                return (ushort *) t.array;
            }
                                                                                    /// <summary>Возвращает ссылку на массив, преобразованную в тип ulong * </summary>
            public static implicit operator ulong * (Record t)
            {
                if (t == null)
                    return null;

                return (ulong *) t.array;
            }
                                                                                    /// <summary>var r = a + Len возвратит запись r, длиной Len, начинающуюся после конца записи r. То есть r.array = a.array + a.len, r.len = Len</summary>
            public static Record operator +(Record a, long len)
            {
                return new Record
                {
                    allocator = null,
                    array     = a.array + a.len,
                    len       = len
                };
            }
            /*                                                                    /// <summary>Возвращает длину данных</summary>
            public static implicit operator long (Record t)
            {
                if (t == null)
                    return 0;

                return t.len;
            }*/
        }

        /// <summary>Интерфейс описывает способ выделения памяти. Реализация: AllocHGlobal_AllocatorForUnsafeMemory</summary>
        public interface AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Размер выделяемого блока памяти</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public Record AllocMemory(long len);

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями). Должен вызываться автоматически в Record</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public void   FreeMemory (Record recordToFree);

            /// <summary>Производит фиксацию в памяти массива (интерфейс должен реализовывать либо AllocMemory(long), либо этот метод, либо оба)</summary>
            /// <param name="array">Исходный массив</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(byte[] array);

            /// <summary>Производит фиксацию в памяти объекта, длиной length байтов</summary>
            /// <param name="array">Закрепляемый объект</param>
            /// <param name="length">Длина объекта в байтах. Длины массивов необходимо домножать на размер элемента массива</param>
            /// <returns></returns>
            public Record FixMemory(object array, long length);
        }

        /// <summary>Выделяет память с помощью Marshal.AllocHGlobal</summary>
        public class AllocHGlobal_AllocatorForUnsafeMemory : AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Длина выделяемого участка памяти</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public Record AllocMemory(long len)
            {
                if (len > int.MaxValue)
                    throw new ArgumentOutOfRangeException("BytesBuilderForPointers.AllocatorForUnsafeMemory.AllocatorForUnsafeMemory: len > int.MaxValue");

                // ptr никогда не null, если не хватает памяти, то будет OutOfMemoryException
                var ptr = Marshal.AllocHGlobal(  (int) len  );
                return new Record() { len = len, array = (byte *) ptr.ToPointer(), ptr = ptr, allocator = this };
            }

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями)</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public void FreeMemory(Record recordToFree)
            {
                Marshal.FreeHGlobal(recordToFree.ptr);
            }

            /// <summary>Не реализовано</summary>
            public Record FixMemory(byte[] array)
            {
                throw new NotImplementedException();
            }

            /// <summary>Не реализовано</summary>
            public Record FixMemory(object array, long length)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Выделяет память для массива с помощью его фиксации: то есть используется обычный сборщик мусора и GCHandle.Alloc</summary>
        public class Fixed_AllocatorForUnsafeMemory : AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память с помощью сборщика мусора, а потом фиксирует её. Это работает медленнее раза в 3, чем AllocHGlobal_AllocatorForUnsafeMemory</summary>
            public Record AllocMemory(long len)
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
                return FixMemory(array, array.LongLength);
            }

            /// <summary>Производит фиксацию в памяти массива</summary>
            /// <param name="array">Исходный массив</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(ushort[] array)
            {
                return FixMemory(array, array.LongLength * sizeof(ushort));
            }

            /// <summary>Производит фиксацию в памяти массива</summary>
            /// <param name="array">Исходный массив</param>
            /// <param name="length">Длина массива</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(object array, long length)
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
