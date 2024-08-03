// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.IO;
using System.Reflection;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;
using static DoNotDisposeEnumHelper;
using static FileParts;
using System.Runtime.CompilerServices;

public static class DoNotDisposeEnumHelper
{
    /// <summary>Предназначено для определения, будет ли производится освобождение содержимого в FileParts. Возвращает результирующий doNotDispose = doNotDispose1 || doNotDispose2</summary>
    /// <param name="doNotDispose1">Первый doNotDispose (например, текущего объекта FileParts).</param>
    /// <param name="doNotDispose2">Второй doNotDispose (например, родительского объекта FileParts).</param>
    /// <returns>Если true, освобождение не должно производится.</returns>
    public static bool GlueDoNotDispose(bool doNotDispose1, bool doNotDispose2)
    {
        return doNotDispose1 || doNotDispose2;
    }
/*
    public static bool GlueDoNotDispose(this DoNotDisposeEnum doNotDisposeOption, bool doNotDispose)
    {
        // Это логическое or - аналог GlueDoNotDispose
        return doNotDisposeOption switch
        {
            DoNotDisposeEnum.unknown => doNotDispose,
            DoNotDisposeEnum.yes     => true,
            DoNotDisposeEnum.no      => doNotDispose,
            _ => throw new InvalidDataException(),
        };
    }
*/
    /// <summary>Возвращает результат переопределения текущей настройки doNotDispose новой настройкой doNotDisposeOption.</summary>
    /// <param name="doNotDisposeOption">Новая настройка.</param>
    /// <param name="doNotDispose">Настройка текущей записи FileParts, которая может быть унаследована новой записью. Эта настройка учитывается только при doNotDisposeOption == DoNotDisposeEnum.unknown.</param>
    public static bool ResetDoNotDispose(this DoNotDisposeEnum doNotDisposeOption, bool doNotDispose)
    {
        // Это операция перезаписи значения doNotDispose значением doNotDisposeOption
        return doNotDisposeOption switch
        {
            DoNotDisposeEnum.unknown => doNotDispose,
            DoNotDisposeEnum.yes     => true,
            DoNotDisposeEnum.no      => false,
            _ => throw new InvalidDataException(),
        };
    }
}

/// <summary>Представляет описатель части файла, которая может содержать другие части файла. Используется для того, чтобы рассчитывать размеры и адреса файлов. Потоконебезопасный, требует внешних блокировок.</summary>
public unsafe partial class FileParts : IDisposable
{
    public enum DoNotDisposeEnum { unknown = 0, yes = 1, no = 2 };

    /// <summary>Если true, то Dispose не будет освобождать никакие данные внутри объекта, однако вызовет деструкторы подчинённых частей рекурсивно. Настройка doNotDispose наследуется: если doNotDispose установлен, то он будет передан ниже и Dispose ниже по иерархии ничего не будут делать. willDispose = !doNotDispose && !parentDoNotDispose.</summary>
    public readonly bool doNotDispose = false;
    public void Dispose(bool parentDoNotDispose)
    {
        var willDispose = !GlueDoNotDispose(doNotDispose, parentDoNotDispose);
        if (willDispose)
        {
            if (btContent is not null)
                BytesBuilder.ToNull(btContent);

            TryToDispose(this.content);
        }

        this.content = null;
        btContent    = null;

        Size = Approximation.Null;
        _startAddress = Approximation.Null;

        foreach (var part in innerParts)
        try
        {
            part.Dispose(!willDispose);
        }
        catch (Exception ex)
        {
            DoFormatException(ex);
        }

        innerParts.Clear();
    }

    public void Dispose()
    {
        Dispose(doNotDispose);
        GC.SuppressFinalize(this);
    }

    ~FileParts()
    {
        if (content != null || btContent != null)
        {
            try
            {
                Dispose();
            }
            catch (Exception e)
            {
                DoFormatException(e);
            }

            var msg = $"Destructor for {this.Name} of FileParts executed with a not disposed state.";
            if (parent is not null)
                msg += $" Parent {this.parent.Name}.";

            if (BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor)
                throw new Exception(msg);
            else
                Console.Error.WriteLine(msg);
        }
    }
}
