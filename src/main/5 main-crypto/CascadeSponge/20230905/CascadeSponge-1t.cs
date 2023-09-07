// TODO: tests
namespace vinkekfish;

using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;

// code::docs:rQN6ZzeeepyOpOnTPKAT:  Это главный файл реализации

/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_1t_20230905: IDisposable
{
                                                                /// <summary>Ширина каскадной губки</summary>
    public readonly nint   wide;                                /// <summary>Высота каскадной губки</summary>
    public readonly nint   tall;                                /// <summary>Параметр W - коэффициент, уменьшающий количество внешних данных для ввода и вывода из каждой губки</summary>
    public readonly double W;                                   /// <summary>Параметр Wn - максимальное количество внешних (пользовательских) байтов, которое можно за один шаг ввести или вывести из каждой внешней губки keccak с учётом ограничений каскада</summary>
    public readonly nint   Wn;                                  /// <summary>Количество данных, которые нужно вводить в обратной связи</summary>
    public readonly nint   ReserveConnectionLen;                /// <summary>Размер массива, который необходимо выделить для данных обратной связи с учётом магического числа</summary>
    public readonly nint   ReserveConnectionFullLen;            /// <summary>Максимальная длина данных, вводимая за один раз</summary>
    public readonly nint   maxDataLen;                          /// <summary>Минимальная ширина губки (2). Минимальная ширина зависит от высоты губки, см. CalcMinWide</summary>
    public const nint MinWide = 2;                              /// <summary>Минимальная высота каскадной губки (3)</summary>
    public const nint MinTall = 3;                              /// <summary>Максимальное количество байтов, которое можно ввести/вывести в губку вне каскада. Реальное количество байтов, доступное для пользовательского ввода - Wn</summary>
    public const byte MaxInputForKeccak = 64;
                                                                /// <summary>Значение для инкремента счётчика обратной связи. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(62, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const long CounterIncrement = 3148241843069173559;
                                                                /// <summary>Вывод для пользователя. Заполняется при вызове step (точнее, после вызова outputAllData)</summary>
    public    readonly Record lastOutput;                       /// <summary>Полный вывод всех губок (транспонированный для обратной связи и вывода)</summary>
    protected readonly Record fullOutput;
                                                                /// <summary>Стойкость шифрования в байтах. Это tall*MaxInputForKeccak</summary>
    public    readonly nint   strenghtInBytes = 0;              /// <summary>Количество ключей, которые нужны для шифрования обратной связи</summary>
    public    readonly nint   countOfThreeFish;
                                                                /// <summary>Количество шагов губки, которое пропускается (делается расчёт вхолостую) после вывода информации в шаге в режиме повышенной стойкости</summary>
    public    readonly nint   countStepsForKeyGeneration;

    /// <summary>Создаёт каскадную губку с заданными параметрами</summary>
    /// <param name="wide">Ширина каскадной губки, не менее MinWide</param>
    /// <param name="tall">Высота каскадной губки, не менее MinTall</param>
    /// <param name="_strenghtInBytes">Потребная стойкость губки в байтах (4096 битов стойкости - 512 байтов). _tall должен быть равен нулю, если этот параметр используется</param>
    public CascadeSponge_1t_20230905(nint _wide = 0, nint _tall = 0, nint _strenghtInBytes = -1)
    {
        // Если параметры заданы путём стойкости, то рассчитываем необходимые параметры
        if (_strenghtInBytes > 0)
        {
            if (_tall > 0)
                throw new CascadeSpongeException("CascadeSponge_1t_20230905: _strenghtInBytes > 0 && _tall > 0");

            _tall = (nint) Math.Ceiling(  (double) _strenghtInBytes / (double) MaxInputForKeccak  );
            if (_wide == 0)
                _wide = CalcMinWide(_tall);
        }

        this.wide = _wide;
        this.tall = _tall;

        if (wide == 0)
            wide = MinWide;
        if (tall == 0)
            tall = MinTall;
        if (wide < MinWide)
            throw new CascadeSpongeException($"wide < MinWide ({wide} < {MinWide})");
        if (tall < MinTall)
            throw new CascadeSpongeException($"tall < MinTall ({tall} < {MinTall})");
        if (wide > tall)
            throw new CascadeSpongeException($"wide > tall ({wide} > {tall})");
        if (wide < CalcMinWide(this.tall))
            throw new CascadeSpongeException($"wide < CalcMinWide ({wide} < {CalcMinWide(tall)})");

        CascadeKeccak = new Keccak_20200918[tall, wide];
        forAllKeccaks
        (
            (nint i, nint j) =>
                CascadeKeccak[i, j] = new Keccak_20200918()
        );


        W  = Math.Log2(tall+1);
        Wn = (nint) Math.Floor((double) MaxInputForKeccak / (double) W);
        countStepsForKeyGeneration = (nint) Math.Ceiling(  2*tall*Math.Log2(tall)+tall  );
        maxDataLen                 = Wn*wide;
        ReserveConnectionLen       = MaxInputForKeccak*wide;
        ReserveConnectionFullLen   = ReserveConnectionLen + 8;
        strenghtInBytes            = tall*MaxInputForKeccak;

        lastOutput = Keccak_abstract.allocator.AllocMemory(maxDataLen);
        fullOutput = Keccak_abstract.allocator.AllocMemory(ReserveConnectionFullLen);
        BytesBuilder.ToNull(maxDataLen,               lastOutput);
        BytesBuilder.ToNull(ReserveConnectionFullLen, fullOutput);
        BytesBuilder.ULongToBytes(MagicNumber_ReverseConnectionLink_forInput, fullOutput, ReserveConnectionFullLen, ReserveConnectionLen);        // Устанавливаем магическое число

        // Для выравнивания, считаем, что на один ThreeFish приходится 256 байтов. Это используется в setThreeFishKeysAndTweak
        countOfThreeFish = wide >> 1;
        reverseCrypto    = Keccak_abstract.allocator.AllocMemory(256*countOfThreeFish);

        // Console.WriteLine(this);
    }

    protected bool isThreeFishInitialized = false;

    /// <summary>Позволяет установить ключи и твики для ThreeFish обратной связи</summary>
    /// <param name="keys">Ключи, каждый ключ по 128-мь байтов. В конце массива должно быть магическое число MagicNumber_ReverseConnectionLink_forInput</param>
    /// <param name="tweaks">Твики, каждый твик по 16-ть байтов. В конце массива должно быть магическое число MagicNumber_ReverseConnectionLink_forInput</param>
    /// <param name="countOfKeys">Общее количество ключей и твиков</param>
    public void setThreeFishKeysAndTweak(byte * keys, byte * tweaks, int countOfKeys)
    {
        CheckMagicNumber(keys, "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: keys", countOfKeys * 128);
        CheckMagicNumber(tweaks, "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: tweaks", countOfKeys * 16);

        if (countOfKeys < countOfThreeFish)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: countOfKeys < countOfThreeFish");

        var rc = reverseCrypto!.array;
        for (int i = 0; i < countOfThreeFish; i++)
        {
            BytesBuilder.CopyTo(128, 128, keys, rc); rc += 192; keys += 128;
            BytesBuilder.CopyTo(16, 16, tweaks, rc); rc += 64; tweaks += 16;
        }

        ExpandThreeFish();
    }

    /// <summary>Инициализирует ThreeFish пустым (нулевым) ключом. Это уменьшает стойкость алгоритма</summary>
    public void InitEmptyThreeFish()
    {
        ExpandThreeFish();
    }

    protected void ExpandThreeFish()
    {
        byte* rc;
        ulong* rcu;
        rc = reverseCrypto!.array;
        for (int i = 0; i < countOfThreeFish; i++)
        {
            Threefish1024.genExpandedKey((ulong*)rc); rc += 192;

            rcu = (ulong*)rc;
            rcu[2] = rcu[0] ^ rcu[1];
            rc += 64;
        }

        isThreeFishInitialized = true;
    }

    // TODO: Если ThreeFish не проинициализирован, хотя бы нулями, то нужно выдавать исключение
    public nint CalcMinWide(nint tall)
    {
        return (nint) Math.Ceiling(  Math.Log2(tall+1)  );
    }

    /// <summary>Каскад губок keccak. Первый индекс - высота, второй - ширина</summary>
    protected Keccak_20200918?[,] CascadeKeccak;
    protected Keccak_20200918 getInputLayer (nint i) => CascadeKeccak[0,      i]!;
    protected Keccak_20200918 getOutputLayer(nint i) => CascadeKeccak[tall-1, i]!;

    /// <summary>Массив ключей и твиков ThreeFish</summary>
    protected Record?  reverseCrypto;


    void IDisposable.Dispose()
    {
        Dispose(false);
    }

    ~CascadeSponge_1t_20230905()
    {
        Dispose(true);
    }

    public bool isDisposed = false;
    public virtual void Dispose(bool fromDestructor = false)
    {
        var d = isDisposed;
        if (isDisposed)
            return;

        if (lastOutput.array is not null)
            lastOutput.Dispose();
        if (fullOutput.array is not null)
            fullOutput.Dispose();

        forAllKeccaks
        (
            (nint i, nint j) =>
            {
                CascadeKeccak[i, j]?.Dispose();
                CascadeKeccak[i, j] = null;
            }
        );

        reverseCrypto?.Dispose();
        reverseCrypto = null;

        isDisposed = true;
        if (fromDestructor && !d)
        {
            throw new CascadeSpongeException("Object not disposed correctly (Dispose from destructor)");
        }
    }
}
/// <summary>
/// Класс для тестирования, открывающий некоторые защищённые члены
/// </summary>
public class CascadeSponge_1t_public_for_test: CascadeSponge_1t_20230905
{
    public Keccak_20200918?[,] CascadeKeccak_public => CascadeKeccak;
}
