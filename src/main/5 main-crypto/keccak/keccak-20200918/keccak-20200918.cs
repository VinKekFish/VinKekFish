using cryptoprime;

// ::test:N9vgPiXnOHBJrgMjXM6o:

namespace maincrypto.keccak;

public unsafe class Keccak_20200918: Keccak_base_20200918
{
    public Keccak_20200918(bool noInit = false): base(noInit)
    {}

    public override Keccak_abstract Clone()
    {
        var result = new Keccak_20200918(true);
        CloneState(result);

        result.isInitiated = this.isInitiated;
        return result;
    }

    public void CloneState(Keccak_20200918 result)
    {
        // Очищаем C и B, чтобы не копировать какие-то значения, которые не стоит копировать, да и хранить тоже
        ClearOnly_C_and_B();

        // Копировать всё состояние не обязательно. Но здесь, для надёжности, копируется всё
        BytesBuilder.CopyTo(StateLen, StateLen, State, result.State);
    }

    /// <summary>Инициализирует губку keccak в режиме Keccak_InputOverwrite64_512 ключом переменной длины.</summary>
    /// <param name="key">Ключ для инициализации. После использования удаляется в этом же методе.</param>
    public void DoInitFromKey(BytesBuilderForPointers.Record key, byte regime)
    {
        try
        {
            for (int l = KeccakPrime.b_size; l > 0;)
            {
                var L = l > 64 ? 64 : l;
                KeccakPrime.Keccak_InputOverwrite64_512(key, (byte) L, this.S, regime);
                l -= L;
            }
        }
        finally
        {
            key.Dispose();
        }
    }
}
