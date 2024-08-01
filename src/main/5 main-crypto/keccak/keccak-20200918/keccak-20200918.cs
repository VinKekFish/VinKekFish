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
}
