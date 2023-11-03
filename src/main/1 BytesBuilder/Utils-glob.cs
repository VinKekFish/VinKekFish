namespace VinKekFish_Utils;

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

// ::warn:onlylinux:sOq1JvFKRxQyw7FQ:

/// <summary>
/// Этот класс помогает найти файлы, соответствующие шаблону поиска файла.
/// Пример шаблона "/dev/*/*/*-event-*".
/// </summary>
public unsafe static class Glob
{
    public static List<string> getGlobFileNames(string pattern)
    {
        var result = new List<string>(16);

        var pGlob = stackalloc glob_t[1];
        var str8  = Utf8StringMarshaller.ConvertToUnmanaged(pattern);

        glob(str8, 0, null, pGlob);

        try
        {
            for (int i = 0; i < pGlob->pathCount; i++)
            {
                if (pGlob->pathes[i] == null)
                    continue;

                var u = Utf8StringMarshaller.ConvertToManaged(pGlob->pathes[i]);

                if (u != null)
                    result.Add(u);
            }
        }
        finally
        {
            globfree(pGlob);
            Utf8StringMarshaller.Free(str8);
        }

        return result;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct glob_t
    {
        public glob_t()
        {}

        public nint   pathCount = 0;
        public byte** pathes    = null;   // char **
        public nint   offset    = 0;
    }

    /// <summary>Выдаёт имена файлов, соответствующие шаблону</summary>
    /// <param name="pattern">Шаблон для поиска</param>
    /// <param name="flags">Флаги, см. man glob</param>
    /// <param name="func">Функция обратного вызова. В данной реализации всегда null.</param>
    /// <param name="pGlob">Ссылка на структуру, которая будет заполнена функцией glob.</param>
    [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
    public static extern void glob(byte * pattern, nint flags, void * func, glob_t * pGlob);

    [DllImport("libc.so.6", CallingConvention = CallingConvention.Cdecl)]
    public static extern void globfree(glob_t * pGlob);
}

