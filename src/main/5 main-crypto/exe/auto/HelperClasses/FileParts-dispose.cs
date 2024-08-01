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

/// <summary>Представляет описатель части файла, которая может содержать другие части файла. Используется для того, чтобы рассчитывать размеры и адреса файлов. Потоконебезопасный, требует внешних блокировок.</summary>
public unsafe partial class FileParts : IDisposable
{
    /// <summary>Если true, то Dispose не будет освобождать никакие данные внутри объекта, однако вызовет деструкторы подчинённых частей рекурсивно. Настройка doNotDispose наследуется: если doNotDispose установлен, то он будет передан ниже и Dispose ниже по иерархии ничего не будут делать. willDispose = !doNotDispose && !parentDoNotDispose.</summary>
    public readonly bool doNotDispose = false;
    public void Dispose(bool parentDoNotDispose)
    {
        var willDispose = !doNotDispose && !parentDoNotDispose;
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
            FormatException(ex);
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
                FormatException(e);
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
