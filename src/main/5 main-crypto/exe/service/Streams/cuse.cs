// TODO: tests
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using cryptoprime;

namespace VinKekFish_EXE;
using static VinKekFish_Utils.Language;

/// <summary>
/// НИЧЕГО НЕ ДЕЛАЕТ!!!
/// Класс для режима работы как службы
/// Представляет собой символьное устройство для вывода энтропии, альтернативное unix-сокетам
/// Принимает входящие запросы и высылает ответы
/// </summary>
public class CuseStream: IDisposable
{
    public readonly FileInfo path;
    public          bool     doTerminate = false;

    public readonly Regime_Service service;

    // Для проверки можно использовать nc -UN path_to_socket
    public CuseStream(string path, Regime_Service service)
    {
        this.service = service;

        this.path = new FileInfo(path);
        this.path.Refresh();
        if (this.path.Exists)
            this.path.Delete();
        if (!this.path.Directory!.Exists)
            this.path.Directory.Create();



        Process.Start("chmod", $"a+r \"{path}\"");
    }

    ~CuseStream()
    {
        if (!isDisposed)
            Close(true);
    }

    public void Dispose()
    {
        Close(false);
    }
// TODO: проверку на то, что не вызван повторно, но вызван вообще
    public bool isDisposed = false;
    public virtual void Close(bool fromDestructor = false)
    {
        // Блокируем объект на случай повторных вызовов
        // Блокируем connections, т.к. мы его сейчас очищать будем
        lock (this)
        {
            isDisposed = true;
        }
    }
}
