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
        CloneStateTo(result);

        result.spongeState = this.spongeState;
        return result;
    }

    public void CloneStateTo(Keccak_20200918 result)
    {
        // Очищаем C и B, чтобы не копировать какие-то значения, которые не стоит копировать, да и хранить тоже
        ClearOnly_C_and_B();

        // Копировать всё состояние не обязательно. Но здесь, для надёжности, копируется всё
        BytesBuilder.CopyTo(StateLen, StateLen, State, result.State);
        result.spongeState = this.spongeState;
    }

    /// <summary>Инициализирует губку keccak в режиме Keccak_InputOverwrite64_512 ключом переменной длины. Эта инициализация может быть как вызвана на пустой губке, так и быть наложена на уже проинициализированное состояние. Состояние не перезатирается.</summary>
    /// <param name="key">Ключ для инициализации. После использования удаляется в этом же методе, если установлен doDelete.</param>
    public void DoInitFromKey(BytesBuilderForPointers.Record key, byte regime, bool doDelete = false)
    {
        try
        {
            DoInputAndStep(key.array, key.len, regime);
        }
        finally
        {
            if (doDelete)
                key.Dispose();
        }
    }

    public void DoInputAndStep(byte* key, nint len, byte regime)
    {
        for (nint l = len; l > 0;)
        {
            var L = l > 64 ? 64 : l;
            KeccakPrime.Keccak_InputOverwrite64_512(key, (byte) L, this.S, regime);

            spongeState = SpongeState.DataInputed;
            CalcStep();

            l   -= L;
            key += L;
        }
    }

    /// <summary>Производит шаг губки с вводом пустых данных.</summary>
    /// <param name="regime">Логический режим шифрования</param>
    public void DoEmptyStep(byte regime)
    {
        KeccakPrime.Keccak_InputOverwrite64_512(null, 0, this.S, regime);
        spongeState = SpongeState.DataInputed;
        CalcStep();
    }

    /// <summary>Наложить гамму после выполнения шага (сам шаг здесь не производится).</summary>
    /// <param name="bytesFromFile">Текст, на который надо наложить гамму.</param>
    /// <param name="len">Длина текста, не более 64-х байтов.</param>
    /// <param name="offest">Начальный индекс, с которого начинается xor в bytesFromFile.</param>
    public void DoXor(BytesBuilderForPointers.Record bytesFromFile, byte len, nint offest = 0)
    {
        if (len > KeccakPrime.BlockLen)
            throw new ArgumentOutOfRangeException("DoXor: len > KeccakPrime.BlockLen");
        if (len > bytesFromFile.len - offest)
            throw new ArgumentOutOfRangeException("DoXor: len > bytesFromFile.len");
        if (spongeState != SpongeState.DataReadyForOutput)
            throw new InvalidOperationException("DoXor: spongeState != SpongeState.DataReadyForOutput");

        var st = stackalloc byte[len];
        KeccakPrime.Keccak_Output_512(st, len, this.S);
        BytesBuilder.Xor(len, bytesFromFile.array + offest, st);

        BytesBuilder.ToNull(len, st);
        spongeState = SpongeState.DataNotInputed;
    }

    /// <summary>Вывести блок данных из губки (не более 64-х байтов)</summary>
    /// <param name="forData">Приёмник данных. Должен быть выделен и быть надлежащего размера.</param>
    /// <param name="len">Длина запрашиваемых данных. Не более 64.</param>
    public void DoOutput(BytesBuilderForPointers.Record forData, byte len)
    {
        if (len > KeccakPrime.BlockLen)
            throw new ArgumentOutOfRangeException("DoOutput: len > KeccakPrime.BlockLen");
        if (len > forData.len)
            throw new ArgumentOutOfRangeException("DoOutput: len > forData.len");
        if (spongeState != SpongeState.DataReadyForOutput)
            throw new InvalidOperationException("DoOutput: spongeState != SpongeState.DataReadyForOutput");

        KeccakPrime.Keccak_Output_512(forData, len, this.S);
        spongeState = SpongeState.DataNotInputed;
    }
}
