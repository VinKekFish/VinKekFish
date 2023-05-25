using cryptoprime;

using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;

namespace maincrypto.keccak;

public unsafe class Keccak_20200918: Keccak_base_20200918
{
    public override Keccak_abstract Clone()
    {
        var result = new Keccak_20200918();

        // Очищаем C и B, чтобы не копировать какие-то значения, которые не стоит копировать, да и хранить тоже
        clearOnly_C_and_B();

        // Копировать всё состояние не обязательно. Но здесь, для надёжности, копируется всё
        BytesBuilder.CopyTo(StateLen, StateLen, State, result.State);

        return result;
    }
}
