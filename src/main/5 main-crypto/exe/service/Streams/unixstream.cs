// TODO: tests
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using cryptoprime;

namespace VinKekFish_EXE;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

/// <summary>
/// Класс для режима работы как службы
/// Представляет собой прослушиватель unix-сокета
/// Принимает входящие запросы и высылает ответы
/// </summary>
public class UnixSocketListener: IDisposable
{
    public readonly FileInfo path;
    public          bool     doTerminate = false;
    public          Socket   listenSocket;

    public readonly Regime_Service service;

    public enum SocketinformationType { error = 0, entropy = 1, entropyParams = 2 };
    public SocketinformationType typeOfInformation;

    // Для проверки можно использовать nc -UN path_to_socket
    public UnixSocketListener(string path, Regime_Service service, SocketinformationType typeOfInformation , int backlog = 64)
    {
        cryptoprime.BytesBuilderForPointers.Record.DoRegisterDestructor(this);

        this.typeOfInformation = typeOfInformation;

        this.path = new FileInfo(path);
        this.path.Refresh();
        if (this.path.Exists)
            this.path.Delete();
        if (!this.path.Directory!.Exists)
            this.path.Directory.Create();

        this.service = service;
        listenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        var un = new UnixDomainSocketEndPoint(path);
        listenSocket.Bind(un);
        listenSocket.Listen(backlog);

        listenSocket.BeginAccept(AcceptConnection, null);

        Process.Start("chmod", $"a+rw \"{path}\"").Dispose();
    }

    ~UnixSocketListener()
    {
        if (!isDisposed)
            Close(true);
    }

    public List<Connection> connections = new(4);
    public void AcceptConnection(IAsyncResult ar)
    {
        if (doTerminate)
            return;

        ThreadPool.QueueUserWorkItem
        (
            (obj) => listenSocket.BeginAccept(AcceptConnection, null)
        );

        try
        {
            var newConnectionSocket = listenSocket.EndAccept(ar);
            var newConnection       = new Connection(this, newConnectionSocket, typeOfInformation);
            lock (connections)
            connections.Add(newConnection);
        }
        // Если сокет уже закрыт
        catch (System.ObjectDisposedException)
        {}
        catch
        {
            // Если завершаем работу, то просто игнорируем исключения
            if (!doTerminate)
                throw;
        }
    }

    public void Dispose()
    {
        Close(false);
        GC.SuppressFinalize(this);
    }

    public bool isDisposed = false;
    public virtual void Close(bool fromDestructor = false)
    {
        // Блокируем объект на случай повторных вызовов
        // Блокируем connections, т.к. мы его сейчас очищать будем
        lock (this)
        lock (connections)
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
    }

    public int ConnectionsCount => connections?.Count ?? 0;

    public class Connection: IDisposable
    {
        public readonly UnixSocketListener    listenSocket;
        public readonly Socket                connection;
        public readonly SocketinformationType sendEntropyParameters;
        public          bool                  closed = false;

        public Connection(UnixSocketListener listenSocket, Socket connection, SocketinformationType sendEntropyParameters)
        {
            this.listenSocket = listenSocket;
            this.connection   = connection;

            this.sendEntropyParameters = sendEntropyParameters;

            StartReceive();
        }

        ~Connection()
        {
            if (!closed)
            {
                Dispose();
                BytesBuilderForPointers.Record.ErrorsInDispose = true;
                Console.Error.WriteLine("UnixSocketListener.Connection: not closed connection in ~Connection()");
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
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

        // Здесь ничего не принимаем. Выдаём 404 байта на выход молча - и всё.
        // Если губка не проинициализированна, то getEntropyForOut заблокирует поток.
        // Если программа завершается, getEntropyForOut выдаст исключение
        protected unsafe virtual void StartReceive()
        {
            // connection.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, EndReceive, this);

            try
            {
                switch (sendEntropyParameters)
                {
                    case SocketinformationType.entropy:
                        SendEntropyToUser();
                    break;
                    case SocketinformationType.entropyParams:
                        SendEntropyParamsToUser();
                    break;

                    default:
                        throw new NotImplementedException("UnixSocketListener.Connection.StartReceive: sendEntropyParameters.default");
                }
            }
            catch (Exception ex)
            {
                DoFormatException(ex);
            }
            finally
            {
                Close();
            }
        }

        protected unsafe void SendEntropyToUser()
        {
            nint blockSize = Regime_Service.GetMinBlockSize();

            using (var buff = listenSocket.service.GetEntropyForOut(blockSize))
            {
                var span = new ReadOnlySpan<byte>(buff, (int)blockSize);
                connection.Send(span);
            }
        }

        protected unsafe void SendEntropyParamsToUser()
        {
            listenSocket.service.ConditionalInputEntropyToMainSponges(nint.MaxValue);
            var paramString = listenSocket.service.CountOfBytesCounterTotal.ToString() + "\n" + listenSocket.service.CountOfBytesCounterCorr.ToString();

            var @params = new UTF8Encoding().GetBytes(paramString);
            connection.Send(@params);
        }
        /*
       protected byte[] receiveBuffer = new byte[1];
       public void EndReceive(IAsyncResult ar)
       {
           if (listenSocket.doTerminate)
               return;

           try
           {
               var received = connection.EndReceive(ar, out SocketError errorCode);
               if (errorCode != SocketError.Success || !connection.Connected)
               {
                   Close();
                   return;
               }

               // Это эхо.
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
       }*/
    }
}
