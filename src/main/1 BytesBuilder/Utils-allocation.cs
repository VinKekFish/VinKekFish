namespace VinKekFish_Utils;

using System.Runtime.InteropServices;
using cryptoprime;
using Record = cryptoprime.BytesBuilderForPointers.Record;
using System.Runtime.CompilerServices;

// ::warn:onlylinux:sOq1JvFKRxQyw7FQ:

// find /usr/include -iname "mman.h"
// cd /usr/include
// egrep -ri 'MAP_ANONYMOUS' .

/// <summary>
/// Не использовать напрямую. Использовать AllocHGlobal_AllocatorForUnsafeMemory
/// Класс описывает аллокаторы для выделения неуправляемой памяти.
/// Memory.alloc является текущим установленным аллокатором
/// Memory.free  - освобождает память
/// </summary>
public unsafe static class Memory
{
    /// <summary>Размер страницы оперативной памяти</summary>
    public static readonly int PAGE_SIZE = Environment.SystemPageSize;

    [Flags]
    public enum MMAPS_Flags
    {
        /*
        The mapping is not backed by any file; its contents are initial‐
            ized to zero.  The fd argument is ignored; however, some  imple‐
            mentations require fd to be -1 if MAP_ANONYMOUS (or MAP_ANON) is
            specified, and portable applications should  ensure  this.   The
            offset  argument  should  be  zero.  The use of MAP_ANONYMOUS in
            conjunction with MAP_SHARED is supported  on  Linux  only  since
            kernel 2.4.
        */
        // ./asm-generic/mman-common.h
        MAP_ANONYMOUS = 0x20,

        /*Mark the mapped region to be locked in the same way as mlock(2).
            This  implementation  will  try to populate (prefault) the whole
            range but the mmap() call  doesn't  fail  with  ENOMEM  if  this
            fails.   Therefore  major  faults might happen later on.  So the
            semantic is not as strong as mlock(2).  One  should  use  mmap()
            plus  mlock(2)  when  major  faults are not acceptable after the
            initialization of the mapping.  The MAP_LOCKED flag  is  ignored
            in older kernels.
            */
        // ./asm-generic/mman.h
        MAP_LOCKED  = 0x2000,
        /// <summary>Этот флаг указывается всегда, если это не shared-память</summary>
        MAP_PRIVATE = 0x0002
    }

    [Flags]
    public enum MemoryProtectionType
    {
        none      = 0,
        read      = 0x0001,
        write     = 0x0002,
        execute   = 0x0004,
        rw        = read | write
    };

    /// <summary>
    /// Этот класс описывает, какой аллокатор используется сейчас для выделения памяти
    /// </summary>
    [Flags]
    public enum MemoryLockType
    {                                                    /// <summary>Непроинициализированный экземпляр класса</summary>
        UNINIT    = 0,                                   /// <summary>Неопределённый тип аллокатора. Вызвать Memory.Init()</summary>
        unknown   = 0x4000,                              /// <summary>Ошибка при определении аллокатора: выделение памяти запрещено</summary>
        errore    = 0x8000,                              /// <summary>Некорректный аллокатор, но к работе разрешён</summary>
        incorrect = 0x0001,                              /// <summary>Корректный аллокатор. Должен фиксировать в оперативной памяти страницы и выделять выравненные значения (хотя значения выравненные, аллокатор внутри Record может вставлять дополнительные контрольные поля, сбивающие выравнивание)</summary>
        correct   = 0x0002,                              /// <summary>Используется аллокатор из libc (mmap)</summary>
        linux     = 0x0200
    };
    public static MemoryLockType memoryLockType = MemoryLockType.unknown;

    public static bool IsError(this MemoryLockType type)
    {
        if (type == MemoryLockType.UNINIT)
            return true;
        if (type.HasFlag(MemoryLockType.errore))
            return true;
        if (type.HasFlag(MemoryLockType.unknown))
            return true;

        return false;
    }

    public static bool isCorrect(this MemoryLockType type)
        => type.HasFlag(MemoryLockType.correct);


    public static readonly object sync = new Object();
    public static void Init()
    {
        if (memoryLockType != MemoryLockType.unknown)
            return;

        lock (sync)
        {
            if (memoryLockType != MemoryLockType.unknown)
                return;

            var addr = mmap(
                            0,
                            PAGE_SIZE,
                            (int)MemoryProtectionType.rw,
                            (int)MMAPS_Flags.MAP_ANONYMOUS | (int)MMAPS_Flags.MAP_LOCKED | (int)MMAPS_Flags.MAP_PRIVATE,
                            fd:    -1,
                            offset: 0
                            );

            if (addr == -1)
            {
                Console.Error.WriteLine("Error in mmap call. addr == -1");
                SetHGlobalAllocator();
                return;
            }
            var bt = (byte *) addr.ToPointer();
            bt[0]  = 0;

            if (munmap(addr, PAGE_SIZE) != 0)
            {
                Console.Error.WriteLine("Error in munmap call. addr != 0");
                SetHGlobalAllocator();
                return;
            }

            SetLinuxAllocator();
        }
    }

    private static void SetHGlobalAllocator()
    {
        _alloc = AllocHGlobal;
        _free = FreeHGlobal;

        memoryLockType = MemoryLockType.incorrect;
    }

    private static void SetLinuxAllocator()
    {
        _alloc = AllocMMap;
        _free  = FreeMMap;

        memoryLockType = MemoryLockType.correct | MemoryLockType.linux;
    }

    public  delegate nint allocDelegate(nint len);
    public  delegate void freeDelegate (nint addr, nint len);

    private static allocDelegate? _alloc =  null;
    private static freeDelegate?  _free  =  null;
                                                                                                                                                            /// <summary>Выделяет память. Это текущий абстрагированный аллокатор. Если выделение памяти не прошло успешно, вызывает OutOfMemoryException</summary>
    public  static allocDelegate   alloc => _alloc ?? throw new Exception("Utils.Memory: alloc == null; see Memory.Init()");                                /// <summary>Освобождает память, выделенную до этого alloc</summary>
    public  static freeDelegate    free  => _free  ?? throw new Exception("Utils.Memory:  free == null; see Memory.Init()");

    public static nint AllocHGlobal(nint len)
    {
        return Marshal.AllocHGlobal(len);
    }

    public static void FreeHGlobal(nint addr, nint len)
    {
        Marshal.FreeHGlobal(addr);
    }

    public static nint getPadSizeForMMap(nint len)
    {
        if (len % PAGE_SIZE == 0)
            return PAGE_SIZE*2;

        return PAGE_SIZE*3;
    }

    private static volatile nint _allocatedMemory = 0;
    public  static          nint  allocatedMemory => _allocatedMemory;

    /// <summary>
    /// Выделяет память через mmap (импорт из libc.so.6; Linux). Память ограничивается дополнительными защищёнными от чтения и записи страницами слева и справа. Память помечается как невыгружаемая в файл подкачки
    /// </summary>
    /// <param name="len">Размер выделяемого пространства памяти</param>
    /// <returns>Указатель на выделенное пространство. Если неуспех, то OutOfMemoryException. Если mprotect не сработал, то Exception</returns>
    public static nint AllocMMap(nint len)
    {
        var pad = getPadSizeForMMap(len);
        // Выделяем больше памяти, чтобы последнюю страницу заблокировать
        var result = mmap
        (
            0,
            len + pad,      // При изменении этого, исправить и FreeMMap, а также расчёт в этой функции ниже
            (int)MemoryProtectionType.rw,
            (int)MMAPS_Flags.MAP_ANONYMOUS | (int)MMAPS_Flags.MAP_LOCKED | (int)MMAPS_Flags.MAP_PRIVATE,
            fd:    -1,
            offset: 0
        );

        if (result == -1)
            throw new OutOfMemoryException();

        // Добавляем последнюю страницу в качестве недоступной ни для чего
        var size = len + pad;
        var R    = result + size - 1;       // Это ещё последний байт будет
        R       -= R % PAGE_SIZE;           // Вычисляем начало страницы, где будет последний байт - это наша последняя страница
        var en   = mprotect(R, PAGE_SIZE, (int)MemoryProtectionType.none);
        if (en != 0)
            throw new Exception($"AllocMMap.mprotect != 0 (last page) ({en})");
            en   = mprotect(result, PAGE_SIZE, (int)MemoryProtectionType.none);
        if (en != 0)
            throw new Exception($"AllocMMap.mprotect != 0 (first page) ({en})");

        lock (sync)
            _allocatedMemory += size;

        return result + PAGE_SIZE;
    }

    public static void FreeMMap(nint addr, nint len)
    {
        var size   = len + getPadSizeForMMap(len);
        var result = munmap(addr - PAGE_SIZE, size);
        if (result != 0)
            throw new Exception("FreeMMap: result != 0");

        lock (sync)
            _allocatedMemory -= size;
    }

    /// <summary>
    /// Выделяет оперативную память, как описано в man mmap
    /// </summary>
    /// <param name="addr">Запрашиваемый адрес. 0 - если адрес будет выделять операционная система</param>
    /// <param name="length">Длина запрашиваемого пространства (строго больше нуля, выравнивание не требуется)</param>
    /// <param name="protection">Что можно делать с памятью. MemoryProtectionType.rw означает, что данную память можно читать и писать</param>
    /// <param name="flags">Флаги. MMAPS_Flags</param>
    /// <param name="fd"></param>
    /// <param name="offset"></param>
    /// <returns>В случае неудачи возвращает -1. В случае удачи возвращает адрес выделенного места в памяти</returns>
    [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint mmap(nint addr, nint length, nint protection,
                                        nint flags, nint fd, nint offset);

    /// <summary>
    /// Освобождает выделенные страницы оперативной памяти
    /// </summary>
    /// <param name="addr">Адрес, полученный из mmap</param>
    /// <param name="lenght">Длина участка памяти</param>
    /// <returns>В случае успеха возвращает 0</returns>
    [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint munmap(nint addr, nint lenght);

    [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint mprotect(nint addr, nint lenght, nint protection);
}

