// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Net.Sockets;
using cryptoprime;
using maincrypto.keccak;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет основную команду для парсинга, отдаваемую через auto-режим. Например, команды enc, dec.</summary>
    public abstract partial class Command: IDisposable
    {
        protected BytesBuilderForPointers bbp = new() {debugNameForRecords = "AutoCrypt.Command.bbp"};
        public void Connect()
        {
            lock (this)
            try
            {
                RandomSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

                byte* b  = stackalloc byte[Regime_Service.MinBlockSize];
                var   bb = new Span<byte>(b, Regime_Service.MinBlockSize);
                lock (this)
                {
                    RandomSocket.Connect(autoCrypt.RandomSocketPoint);
                    RandomSocket.Receive(bb);
                }

                lock (bbp)
                bbp.AddWithCopy(b, Regime_Service.MinBlockSize, Keccak_abstract.allocator);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(L("Failed to connect to the socket") + $" {autoCrypt.RandomSocketPoint}");
                Console.Error.WriteLine(ex.Message);
                Terminated = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(L("Failed to connect to the socket") + $" {autoCrypt.RandomSocketPoint}");
                DoFormatException(ex);
                Terminated = true;
            }
            finally
            {
                TryToDispose(RandomSocket);
                RandomSocket = null;

                Monitor.PulseAll(this);
            }
        }

        protected bool isDisposed = false;
        public virtual void Dispose(bool fromDestructor = false)
        {
            var id = isDisposed;
            if (!isDisposed)
            {
                TryToDispose(bbp);
                isDisposed = true;
            }

            if (!id)
            if (fromDestructor)
            {
                var msg = $"AutoCrypt.Command.Dispose AutoCrypt.~Command() executed with a not disposed state.";
                if (BytesBuilderForPointers.Record.doExceptionOnDisposeInDestructor)
                    throw new Exception(msg);
                else
                    Console.Error.WriteLine(msg);
            }
        }

        ~Command()
        {
            Dispose(fromDestructor: true);
        }

        void IDisposable.Dispose()
        {
            Dispose(fromDestructor: false);
            GC.SuppressFinalize(this);
        }
    }
}
