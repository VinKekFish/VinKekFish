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
    public readonly nint   ReserveConnectionFullLen;            /// <summary>Максимальная длина данных, вводимая из-вне за один раз или выводимая во-вне (пользователю) за один раз (за один шаг). Это длина данных уже со всей каскадной губки</summary>
    public readonly nint   maxDataLen;                          /// <summary>Минимальная ширина губки (2). Минимальная ширина зависит от высоты губки, см. CalcMinWide</summary>
    public const    nint   MinWide = 2;                         /// <summary>Минимальная высота каскадной губки (3)</summary>
    public const    nint   MinTall = 3;                         /// <summary>Максимальное количество байтов, которое можно ввести/вывести в губку вне каскада. Эта константа никогда не изменяется и зависит от построения алгоритма keccak. Реальное количество байтов, доступное для пользовательского ввода/вывода из каждой губки - Wn. Общее количество байтов, доступных для ввода/вывода из всей губки - maxDataLen.</summary>
    public const    byte   MaxInputForKeccak = 64;
                                                                /// <summary>Не нужно пользователю. Значение для инкремента счётчика обратной связи. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(62, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  CounterIncrement   = 3148241843069173559; /// <summary>Не нужно пользователю. Значение для инкремента tweak при пустой инициализации. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(62, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  TweakInitIncrement = 2743445726634853529; /// <summary>Не нужно пользователю. Значение для инкремента key при пустой инициализации. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(63, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  KeyInitIncrement   = 7825219255190903851;
                                                                /// <summary>Выходные данные для пользователя. Заполняется при вызове step (точнее, после вызова outputAllData)</summary>
    public    readonly Record lastOutput;                       /// <summary>Полный вывод всех губок. Массив используется для формирования пользовательского вывода lastOutput, для вычисления ThreeFish на месте, ввода обратной связи.</summary>
    protected readonly Record fullOutput;                       /// <summary>Если true, значит в lastOutput есть данные после шага. false говорит о том, что кто-то сбросил этот флаг и данные из lastOutput использовать уже нельзя.</summary>
    public             bool   haveOutput = false;
                                                                /// <summary>Стойкость шифрования в байтах. Это tall*MaxInputForKeccak</summary>
    public    readonly nint   strenghtInBytes = 0;              /// <summary>Количество ключей, которые нужны для шифрования обратной связи</summary>
    public    readonly nint   countOfThreeFish;
                                                                /// <summary>Количество шагов губки, которое пропускается (делается расчёт вхолостую) после ввода/вывода информации в шаге в генерации ключей</summary>
    public    readonly nint   countStepsForKeyGeneration;       /// <summary>Количество шагов губки, которое пропускается после ввода/вывода информации в режиме повышенной стойкости (это меньше, чем countStepsForKeyGeneration)</summary>
    public    readonly nint   countStepsForHardening;

    protected nint _countOfProcessedSteps = 0;                          /// <summary>Общее количество шагов, которые провела каскадная губка за всё время шифрования, включая поглощение синхропосылки и ключа.</summary>
    public    nint  countOfProcessedSteps => _countOfProcessedSteps;

    /// <summary>Создаёт каскадную губку с заданными параметрами</summary>
    /// <param name="wide">Ширина каскадной губки, не менее MinWide</param>
    /// <param name="tall">Высота каскадной губки, не менее MinTall</param>
    /// <param name="_strenghtInBytes">Потребная стойкость губки в байтах (4096 битов стойкости - 512 байтов). _tall должен быть равен нулю, если этот параметр используется</param>
    public CascadeSponge_1t_20230905(nint _strenghtInBytes = 192, nint _wide = 0, nint _tall = 0)
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


        W  = Math.Log2(tall)+1;
        Wn = (nint) Math.Floor((double) MaxInputForKeccak / (double) W);
        if (Wn <= 0)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905: Wn <= 0. tall > 2^63 ???");

        maxDataLen                 = Wn*wide;
        ReserveConnectionLen       = MaxInputForKeccak*wide;
        ReserveConnectionFullLen   = ReserveConnectionLen + 8;
        strenghtInBytes            = tall*MaxInputForKeccak;

        countStepsForKeyGeneration = (nint) Math.Ceiling(  2*tall*Math.Log2(tall) + 1  );
        countStepsForHardening     = (nint) Math.Ceiling(  Math.Log2(tall)+1  );

        lastOutput = Keccak_abstract.allocator.AllocMemory(maxDataLen, "CascadeSponge_1t_20230905.lastOutput");
        fullOutput = Keccak_abstract.allocator.AllocMemory(ReserveConnectionFullLen, "CascadeSponge_1t_20230905.fullOutput");
        BytesBuilder.ToNull(maxDataLen,               lastOutput);
        BytesBuilder.ToNull(ReserveConnectionFullLen, fullOutput);
        BytesBuilder.ULongToBytes(MagicNumber_ReverseConnectionLink_forInput, fullOutput, ReserveConnectionFullLen, ReserveConnectionLen);        // Устанавливаем магическое число

        // Для выравнивания, считаем, что на один ThreeFish приходится 256 байтов. Это используется в setThreeFishKeysAndTweak
        countOfThreeFish = wide >> 1;
        reverseCrypto    = Keccak_abstract.allocator.AllocMemory(256*countOfThreeFish, "CascadeSponge_1t_20230905.reverseCrypto");

        // На всякий случай, сразу же инициализируем ключи и твики ThreeFish, чтобы их можно было дальше использовать
        InitEmptyThreeFish();

        // Console.WriteLine(this);
    }

    protected bool isThreeFishInitialized = false;

    /// <summary>Позволяет установить ключи и твики для ThreeFish обратной связи</summary>
    /// <param name="keys">Ключи, каждый ключ по 128-мь байтов. В конце массива должно быть магическое число MagicNumber_ReverseConnectionLink_forInput</param>
    /// <param name="tweaks">Твики, каждый твик по 16-ть байтов. В конце массива должно быть магическое число MagicNumber_ReverseConnectionLink_forInput</param>
    /// <param name="countOfKeys">Общее количество ключей и твиков</param>
    public void setThreeFishKeysAndTweak(byte * keys, byte * tweaks, int countOfKeys)
    {
        CheckMagicNumber(keys,   "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: keys",   countOfKeys * threefish_slowly.keyLen);
        CheckMagicNumber(tweaks, "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: tweaks", countOfKeys * threefish_slowly.twLen);

        if (countOfKeys < countOfThreeFish)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: countOfKeys < countOfThreeFish");

        var rc = reverseCrypto!.array;
        for (int i = 0; i < countOfThreeFish; i++)
        {
            BytesBuilder.CopyTo(threefish_slowly.keyLen, threefish_slowly.keyLen, keys,   rc); rc += 192; keys   += threefish_slowly.keyLen;
            BytesBuilder.CopyTo(threefish_slowly.twLen,  threefish_slowly.twLen,  tweaks, rc); rc += 64;  tweaks += threefish_slowly.twLen;
        }

        ExpandThreeFish();
    }

    /// <summary>Инициализирует ThreeFish пустым (нулевым) ключом и твиками (вызывает InitEmptyThreeFishTweaks). Это уменьшает стойкость алгоритма. В данном случае, tweak инициализируется константами с помощью вызова InitEmptyThreeFishTweaks, ключи с помощью InitEmptyThreeFishKeys. Лучше вместо этого использовать InitThreeFishByCascade.</summary>
    public void InitEmptyThreeFish(ulong emptyKeyInitValue = KeyInitIncrement, ulong emptyTweakInitValue = TweakInitIncrement)
    {
        InitEmptyThreeFishTweaks(emptyTweakInitValue);
        InitEmptyThreeFishKeys  (emptyKeyInitValue);
    }

    // code::docs:Wt74dfPfEIcGzPN5Jrxe:
    /// <summary>Инициализирует твики ThreeFish константами. Не портит ключи, если пользователь их проинициализировал.</summary>
    /// <param name="emptyTweakInitValue">Простое значение для инициализации начального твика</param>
    public void InitEmptyThreeFishTweaks(ulong emptyTweakInitValue = TweakInitIncrement)
    {
        var   rc  = reverseCrypto!.array + 192;    // Сразу выполняем переход на твики
        ulong tw0 = emptyTweakInitValue;
        for (ulong i = 0; i < (ulong) countOfThreeFish; i++)
        {
            var tw = (ulong*)rc;
            rc += 256;

            tw[0] = tw0;
            tw0  += TweakInitIncrement;
            // Если произошло переполнение
            if (tw0 < tw[0])
                tw[1] += 1;
        }

        ExpandThreeFish();
    }

    /// <summary>Инициализирует ключи ThreeFish константами, если пользователь не хочет их проинициализировать. Не портит твики, если пользователь их проинициализировал.</summary>
    /// <param name="emptyKeyInitValue">Простое значение для инициализации ключей</param>
    public void InitEmptyThreeFishKeys(ulong emptyKeyInitValue = KeyInitIncrement)
    {
        var   rc  = reverseCrypto!.array + 0;    // Сразу выполняем переход на ключи
        ulong key = emptyKeyInitValue;
        for (ulong i = 0; i < (ulong) countOfThreeFish; i++)
        {
            var rck = (ulong*)rc;
            rc += 256;

            for (int j = 0; j < threefish_slowly.Nw; j++)
            {
                rck[j] = key;
                key   += KeyInitIncrement;
            }
        }

        ExpandThreeFish();
    }

    /// <summary>Проводит расширение ключей и твиков ThreeFish обратной связи. При изменении только ключей или только твиков не повредит неизменённые ключи или твики (они будут перевычисленны в те же значения)</summary>
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
        return (nint) Math.Ceiling(  Math.Log2(tall)+1  );
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
