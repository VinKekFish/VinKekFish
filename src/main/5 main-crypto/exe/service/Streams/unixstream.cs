// TODO: tests
using System.Net.Sockets;

namespace VinKekFish_EXE;

/// <summary>
/// Класс для режима работы как службы
/// Представляет собой прослушиватель unix-сокета
/// Принимает входящие запросы и высылает ответы
/// Пока не реализован полностью
/// </summary>
public class UnixSocketListener: IDisposable
{
    public readonly FileInfo path;
    public          bool     doTerminate = false;
    public          Socket   listenSocket;

    // Для проверки можно использовать nc -UN path_to_socket
    public UnixSocketListener(string path, int backlog = 64)
    {
        this.path = new FileInfo(path);
        this.path.Refresh();
        if (this.path.Exists)
            this.path.Delete();

        listenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        var un = new UnixDomainSocketEndPoint(path);
        listenSocket.Bind(un);
        listenSocket.Listen(backlog);

        listenSocket.BeginAccept(AcceptConnection, null);
    }

    ~UnixSocketListener()
    {
        if (!isDisposed)
            Close(true);
    }

    public List<Connection> connections = new List<Connection>(4);
    public void AcceptConnection(IAsyncResult ar)
    {
        if (!doTerminate)
            ThreadPool.QueueUserWorkItem
            (
                (obj) => listenSocket.BeginAccept(AcceptConnection, null)
            );

        try
        {
            var newConnectionSocket = listenSocket.EndAccept(ar);
            var newConnection       = new Connection(this, newConnectionSocket);
            lock (connections)
            connections.Add(newConnection);
        }
        // Если сокет уже закрыт
        catch (System.ObjectDisposedException)
        {}
    }

    public void Dispose()
    {
        Close(false);
    }

    public bool isDisposed = false;
    public virtual void Close(bool fromDestructor = false)
    {
        doTerminate = true;
        listenSocket.Close();

        Parallel.ForEach
        (
            connections,
            delegate(Connection connection, ParallelLoopState state, long i)
            {
                connection.Close();
            }
        );
        connections.Clear();

        if (fromDestructor)
            throw new Exception("UnixSocketListener: Close.fromDestructor == true");

        isDisposed = true;
    }

    public int ConnectionsCount => connections?.Count ?? 0;

    public class Connection: IDisposable
    {
        public readonly UnixSocketListener listenSocket;
        public readonly Socket             connection;
        public          bool               closed = false;
        public Connection(UnixSocketListener listenSocket, Socket connection)
        {
            this.listenSocket = listenSocket;
            this.connection   = connection;

            StartReceive();
        }

        ~Connection()
        {
            if (!closed)
            {
                Dispose();
                throw new Exception("UnixSocketListener.Connection: not closed connection in ~Connection()");
            }
        }

        public void Dispose()
        {
            Close();
        }

        public virtual void Close(bool deleteFromConnections = true)
        {
            try
            {
                connection.Close();

                if (deleteFromConnections)
                lock (listenSocket.connections)
                    listenSocket.connections.Remove(this);
            }
            catch
            {}

            closed = true;
        }

        public readonly byte[] receiveBuffer = new byte[64*1024];
        protected virtual void StartReceive()
        {
            // TODO: SocketFlags
            connection.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, EndReceive, this);
        }
// System.Runtime.InteropServices.UnmanagedFunctionPointer
// https://github.com/libfuse/libfuse/blob/master/example/hello.c#L176
// https://github.com/vzabavnov/dotnetcore.fuse
// https://github.com/vzabavnov/dotnetcore.fuse/blob/master/src/fuse.interop/FuseOpt/Functions.cs
        public void EndReceive(IAsyncResult ar)
        {
            if (listenSocket.doTerminate)
                return;

            try
            {
                var received = connection.EndReceive(ar, out SocketError errorCode);
                if (received == 0 || errorCode != SocketError.Success || !connection.Connected)
                {
                    Close();
                    return;
                }

                if (errorCode == SocketError.Success)
                if (connection.Connected)
                    connection.Send(receiveBuffer, 0, received, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in UnixSocketListener.Connection.EndReceive: " + ex.Message + "\n" + ex.StackTrace);
                Close();
                return;
            }

            ThreadPool.QueueUserWorkItem
            (
                (obj) => connection.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, EndReceive, this)
            );            
        }
    }
}
