// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using cryptoprime;
using maincrypto.keccak;
using VinKekFish_Utils.ProgramOptions;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public unsafe partial class AutoCrypt
{
    /// <summary>Класс представляет основную команду для парсинга, отдаваемую через auto-режим. Например, команды enc, dec.</summary>
    public abstract partial class Command
    {
        protected BytesBuilderForPointers bbp = new BytesBuilderForPointers();
        public void Connect()
        {
            try
            {
                byte* b  = stackalloc byte[Regime_Service.MinBlockSize];
                var   bb = new Span<byte>(b, Regime_Service.MinBlockSize);
                lock (autoCrypt.RandomSocket)
                {
                    autoCrypt.RandomSocket.Connect(autoCrypt.RandomSocketPoint);
                    autoCrypt.RandomSocket.Receive(bb);
                }

                lock (bbp)
                bbp.addWithCopy(b, Regime_Service.MinBlockSize, Keccak_abstract.allocator);
            }
            catch (Exception ex)
            {
                formatException(ex);
                Terminated = true;
            }
        }
    }
}
