using System.Runtime.InteropServices;
using static cryptoprime.KeccakPrime;
using cryptoprime;

using Memory = VinKekFish_Utils.Memory;
using static cryptoprime.BytesBuilderForPointers;

// В этой сборке специально не включаются оптимизации: хрен знает, что они тут сделают
// С оптимизациями могут быть проблемы с очисткой памяти. Сами функции очистки вынесены в другую библиотеку, с оптимизациями
namespace maincrypto.keccak;

/// <summary>
/// <para>Этот класс является предком остальных
/// </para><para>Классы не предназначены для изменений
/// </para><para>Наследники этого класса: Keccak_base_*
/// </para><para>Чтобы их изменять, по хорошему, надо создать новый класс с другой датой создания и добавить его в тесты
/// </para><para>То есть все уже написанные и протестированные классы не должны быть изменены, чтобы не привнести туда никаких изменений и ошибок
/// </para>
/// </summary>
public unsafe abstract class Keccak_abstract: IDisposable
{
    public readonly static AllocHGlobal_AllocatorForUnsafeMemory allocator = new();
    /// <summary>Создаёт объект для использования с примитивом keccak</summary>
    /// <param name="noInit">Если true, то не будет делать инициализацию полей нулями</param>
    public Keccak_abstract(bool noInit = false)
    {
        cryptoprime.BytesBuilderForPointers.Record.DoRegisterDestructor(this);

        StatePtr = allocator.AllocMemory(StateLen); //Marshal.AllocHGlobal(StateLen);
        State    = StatePtr.array;
        GetStatesArray();

        if (!noInit)
            Init();
    }

    public byte  * S, B, C;                 // Размеры в элементах ulong: S_len2, S_len2, S_len
    public ulong * Slong, Blong, Clong;
    protected void GetStatesArray()
    {
        B     = State;
        C     = B + (S_len2 << 3);
        S     = C + (S_len  << 3);

        Slong = (ulong *) S;
        Blong = (ulong *) B;
        Clong = (ulong *) C;
    }

    // Это внутреннее состояние keccak, а также вспомогательные переменные, не являющиеся состоянием
    // Здесь сначала идёт B, потом C, потом S.
    // При перезаписи после конца массивов B или C с высокой вероятностью пострадает S, что даст возможность тестам сделать своё дело
    /// <summary> Не нужно конечному пользователю. Внутреннее состояние keccak. Используйте Slong, Blong, Clong для того, чтобы разбить его на указатели</summary>
    protected byte*   State    = null;
    protected Record? StatePtr;                     //  Константы объявлены в cryptoprime.KeccakPrime
                                                    /// <summary>Общая длина состояния keccak, включая вспомогательные матрицы. Это значение используется для выделения памяти.</summary>
    protected int    StateLen = (S_len2 + S_len + S_len2) << 3;

    public abstract Keccak_abstract Clone();
    /// <summary>Дополнительно очищает всё состояние объекта после вычислений. После этого объект будет неинициализирован (инициализирован нулями), но пригоден к дальнейшему использованию с другой инициализацией</summary>
    /// <param name="GCCollect">Если true, то override реализации должны дополнительно попытаться перезаписать всю память программы. <see langword="abstract"/> реализация ничего не делает</param>
    public virtual void Clear(bool GCCollect = false)
    {
        spongeState = SpongeState.DataNotInputed;
        ClearState();
    }

    /// <summary>Очищает состояние объекта. После этого объект будет неинициализирован (инициализирован нулями), но пригоден к дальнейшему использованию с другой инициализацией</summary>
    public virtual void ClearState()
    {
        spongeState = SpongeState.DataNotInputed;
        if (State != null)
        {
            BytesBuilder.ToNull(StateLen, State);
            ClearStateWithoutStateField();
        }
    }

    /// <summary>Очищает вспомогательные поля объекта, но оставляет объект проинициализированным. В том числе, очищает вспомогательные массивы B и C. Эта функция ничего не меняет и её вызов не требуется,</summary>
    public virtual void ClearStateWithoutStateField()
    {
        ClearOnly_C_and_B();
    }

    /// <summary>Описывает состояние губки.</summary>
    public enum SpongeState
    {                                        /// <summary>Губка не проинициализирована (в том числе, нулями).</summary>
        Uninitiated        = 0,              /// <summary>Данные не введены (в губку не введён режим шифрования и данные либо факт их отсутствия; необходимо ввести данные перед шагом).</summary>
        DataNotInputed     = 1,              /// <summary>В губку введены данные и можно её шифровать.</summary>
        DataInputed        = 2,              /// <summary>Сделан шаг губки, можно брать из неё данные.</summary>
        DataReadyForOutput = 3
    };

    public SpongeState spongeState = SpongeState.Uninitiated;

    /// <summary>Инициализирует состояние нулями</summary>
    public virtual void Init()
    {
        BytesBuilder.ToNull(StateLen, State);
        spongeState = SpongeState.DataNotInputed;
    }

    /// <summary>Эту функцию можно вызывать после keccak, если нужно состояние S, но хочется очистить B и C. В целом, вызов этой функции не требуется и ничего не меняет.</summary>
    public void ClearOnly_C_and_B()
    {
        Clear5x5(Blong);
        Clear5  (Clong);
    }

    /// <summary>Этот метод может использоваться для очистки матриц S и B после вычисления последнего шага хеша</summary>
    /// <param name="S">Очищаемая матрица размера 5x5 *ulong</param>
    public unsafe static void Clear5x5(ulong * S)
    {
        var len = S_len2;
        var se  = S + len;
        for (; S < se; S++)
            *S = 0;
    }

    /// <summary>Этот метод может использоваться для очистки вспомогательного массива C</summary>
    /// <param name="C">Очищаемый массив размера 5*ulong</param>
    public unsafe static void Clear5(ulong * C)
    {
        var se  = C + S_len;
        for (; C < se; C++)
            *C = 0;
    }

    public virtual void Dispose(bool disposing)
    {
        if (State == null)
            throw new ObjectDisposedException("Keccak_abstract");

        Clear(false);

        // Marshal.FreeHGlobal(StatePtr);
        StatePtr?.Free();
        StatePtr = null;
        State    = null;
        B        = null;
        C        = null;
        S        = null;
        Blong    = null;
        Clong    = null;
        S        = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Keccak_abstract()
    {
        if (State != null)
        {
            Dispose(false);
            throw new Exception("~Keccak_abstract: State != null");
        }
    }

    /// <summary>Производит шаг криптографического преобразования губки. Данные (или факт их отсутствия) и режим уже должны быть введены в губку.</summary>
    public void CalcStep()
    {
        if (spongeState == SpongeState.Uninitiated || spongeState == SpongeState.DataNotInputed)
            throw new Exception("Keccak_abstract.CalcStep: spongeState == SpongeState.Uninitiated || spongeState == SpongeState.DataNotInputed");

        Keccackf(a: Slong, c: Clong, b: Blong);
        spongeState = SpongeState.DataReadyForOutput;
    }
}
