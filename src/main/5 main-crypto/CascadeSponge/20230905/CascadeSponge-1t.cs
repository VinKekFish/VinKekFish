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
    public readonly nint   maxDataLen;                          /// <summary>Минимальная ширина губки (4). Ширина губки всегда должна быть чётной. Минимальная ширина зависит от высоты губки, см. CalcMinWide</summary>
    public const    nint   MinWide = 4;                         /// <summary>Минимальная высота каскадной губки (4)</summary>
    public const    nint   MinTall = 4;                         /// <summary>Максимальное количество байтов, которое можно ввести/вывести в губку вне каскада. Эта константа никогда не изменяется и зависит от построения алгоритма keccak. Реальное количество байтов, доступное для пользовательского ввода/вывода из каждой губки - Wn. Общее количество байтов, доступных для ввода/вывода из всей губки - maxDataLen.</summary>
    public const    byte   MaxInputForKeccak = 64;
                                                                /// <summary>Не нужно пользователю. Значение для инкремента счётчика обратной связи. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(62, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  CounterIncrement   = 3148241843069173559; /// <summary>Не нужно пользователю. Значение для инкремента tweak при пустой инициализации. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(62, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  TweakInitIncrement = 2743445726634853529; /// <summary>Не нужно пользователю. Значение для инкремента key при пустой инициализации. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(63, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  KeyInitIncrement   = 7825219255190903851;
                                                                /// <summary>Выходные данные для пользователя. Заполняется при вызове step (точнее, после вызова outputAllData)</summary>
    public    readonly Record lastOutput;                       /// <summary>Полный вывод всех губок. Массив используется для формирования пользовательского вывода lastOutput, для вычисления ThreeFish на месте для пользовательского выхода.</summary>
    protected readonly Record fullOutput;                       /// <summary>Полный вывод всех губок. Используется для формирования и вычисления обратной связи чере ThreeFish</summary>
    protected readonly Record   rcOutput;                       /// <summary>Если true, значит в lastOutput есть данные после шага. false говорит о том, что кто-то сбросил этот флаг и данные из lastOutput использовать уже нельзя.</summary>
    public             bool   haveOutput = false;
                                                                /// <summary>Стойкость шифрования в байтах. Это tall*MaxInputForKeccak</summary>
    public    readonly nint   strenghtInBytes = 0;              /// <summary>Количество ключей, которые нужны для шифрования обратной связи. Реально ключей в два раза больше</summary>
    public    readonly nint   countOfThreeFish_RC;              /// <summary>Количество ключей, которые нужны для шифрования обратной связи и для шифрования выхода</summary>
    public    readonly nint   countOfThreeFish;
                                                                /// <summary>Количество шагов губки, которое пропускается (делается расчёт вхолостую) после ввода/вывода информации в шаге в генерации ключей</summary>
    public    readonly nint   countStepsForKeyGeneration;       /// <summary>Количество шагов губки, которое пропускается после ввода/вывода информации в режиме повышенной стойкости (это меньше, чем countStepsForKeyGeneration)</summary>
    public    readonly nint   countStepsForHardening;

    protected nint _countOfProcessedSteps = 0;                          /// <summary>Общее количество шагов, которые провела каскадная губка за всё время шифрования, включая поглощение синхропосылки и ключа.</summary>
    public    nint  countOfProcessedSteps => _countOfProcessedSteps;

    /// <summary>Создаёт каскадную губку (каскад) по заданной целевой стойкости и длине блока</summary>
    /// <param name="_strenghtInBytes">Стойкость в байтах (512 байтов = 4096 битов)</param>
    /// <param name="targetBlockLen">Длина выходного блока в байтах</param>
    public static CascadeSponge_1t_20230905 getCascade(nint _strenghtInBytes, nint targetBlockLen)
    {
        nint _wide = 0;
        CalcCascadeParameters(_strenghtInBytes, targetBlockLen, _tall: out nint _tall, _wide: ref _wide);

        return new CascadeSponge_1t_20230905(_tall: _tall, _wide: _wide);
    }

    // CascadeSponge_1t_20230905.CalcCascadeParameters(192, 404, _tall: out nint _tall, _wide: out nint _wide);
    /// <summary>Вычисляет требуемые параметры каскада по заданной целевой стойкости и длине блока</summary>
    /// <param name="_strenghtInBytes">Стойкость в байтах (512 байтов = 4096 битов)</param>
    /// <param name="targetBlockLen">Длина выходного блока в байтах</param>
    /// <param name="_wide">Требуемая ширина</param>
    /// <param name="_tall"></param>
    public static void CalcCascadeParameters(nint _strenghtInBytes, nint targetBlockLen, out nint _tall, ref nint _wide)
    {
        _tall = CalcTallAndWideByStrenght(_strenghtInBytes, ref _wide);
        (var W, var Wn) = CalcW(_tall);
        nint Mx = Wn * _wide;

        if (Mx < targetBlockLen)
        {
            _wide = targetBlockLen / Wn;
            if ((_wide & 1) > 0)
                _wide++;

            Mx = Wn * _wide;
            if (Mx < targetBlockLen)
                _wide += 2;
        }

        // Если _wide слишком широкий, получаем всё заново, но с запасом по стойкости
        if (_wide > _tall)
        {
            _tall = _wide;
            CalcCascadeParameters(MaxInputForKeccak*_tall, targetBlockLen, _tall: out _tall, _wide: ref _wide);
        }

        // Для проверки вычисляем новую длину выходного блока
        (W, Wn) = CalcW(_tall);
        Mx = Wn * _wide;

        if (Mx < targetBlockLen)
        {
            _wide += 2;
            CalcCascadeParameters(MaxInputForKeccak*_tall, targetBlockLen, _tall: out _tall, _wide: ref _wide);
        }

        if (Mx < targetBlockLen || (_wide & 1) > 0)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.getCascade: fatal algorithmic error: Mx < targetBlockLen || (_wide & 1) > 0");
    }

    /// <summary>Вычисляет коэффициент запаса W и максимальный вход/выход одной губки Wn</summary>
    /// <param name="_tall">Высота губки</param>
    /// <returns>Возвращает кортеж: коэффициент запаса W, максимальная длина ввода/вывода из одной губки (подгубки) Wn</returns>
    public static (double W, nint Wn) CalcW(nint _tall)
    {
        double W  = Math.Log2(_tall) + 1.0;
        nint   Wn = (nint) Math.Floor((double) MaxInputForKeccak / (double) W);

        if (Wn <= 0)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.CalcW: Wn <= 0. tall >= 2^63 ???");

        return (W, Wn);
    }

    /// <summary>Создаёт каскадную губку с заданными параметрами</summary>
    /// <param name="_wide">Ширина каскадной губки, не менее MinWide и не менее CalcMinWide. Всегда должна быть чётной. Чем больше ширина, тем больше выход данных губки за один шаг.</param>
    /// <param name="_tall">Высота каскадной губки, не менее MinTall</param>
    /// <param name="_strenghtInBytes">Потребная стойкость губки в байтах (4096 битов стойкости - 512 байтов)</param>
    public CascadeSponge_1t_20230905(nint _strenghtInBytes = 192, nint _wide = 0, nint _tall = 0)
    {
        if ((_wide & 1) > 0)
            _wide++;

        // Если параметры заданы путём стойкости, то рассчитываем необходимые параметры
        if (_strenghtInBytes > 0)
        {
            nint wd = 0;
            nint tl = CalcTallAndWideByStrenght(_strenghtInBytes, ref wd);

            if (tl > _tall)
                _tall = tl;
            if (wd > _wide)
                _wide = wd;
        }

        this.wide = _wide;
        this.tall = _tall;

        if (wide == 0)
            wide = MinWide;
        if (tall == 0)
            tall = MinTall;
        if (wide < MinWide)
            throw new CascadeSpongeException($"CascadeSponge_1t_20230905: wide < MinWide ({wide} < {MinWide})");
        if (tall < MinTall)
            throw new CascadeSpongeException($"CascadeSponge_1t_20230905: tall < MinTall ({tall} < {MinTall})");
        if (wide > tall)
            throw new CascadeSpongeException($"CascadeSponge_1t_20230905: wide > tall ({wide} > {tall})");
        if (wide < CalcMinWide(this.tall))
            throw new CascadeSpongeException($"CascadeSponge_1t_20230905: wide < CalcMinWide ({wide} < {CalcMinWide(tall)})");
        if ((_wide & 1) > 0)
            throw new CascadeSpongeException($"CascadeSponge_1t_20230905: (_wide & 1) > 0 ({wide})");

        CascadeKeccak = new Keccak_20200918[tall, wide];
        forAllKeccaks
        (
            (nint i, nint j) =>
                CascadeKeccak[i, j] = new Keccak_20200918()
        );


        (W, Wn) = CalcW(tall);

        maxDataLen                 = Wn*wide;
        ReserveConnectionLen       = MaxInputForKeccak*wide;
        ReserveConnectionFullLen   = ReserveConnectionLen + 8;
        strenghtInBytes            = tall*MaxInputForKeccak;

        // countStepsForKeyGeneration = (nint) Math.Ceiling(  2*tall*Math.Log2(tall) + 1  );        // Это очень долго
        countStepsForKeyGeneration = (nint) Math.Ceiling(  Math.Log2(tall)+1  );
        countStepsForHardening     = (nint) 2;

        lastOutput = Keccak_abstract.allocator.AllocMemory(maxDataLen, "CascadeSponge_1t_20230905.lastOutput");
        fullOutput = Keccak_abstract.allocator.AllocMemory(ReserveConnectionFullLen, "CascadeSponge_1t_20230905.fullOutput");
          rcOutput = Keccak_abstract.allocator.AllocMemory(ReserveConnectionFullLen, "CascadeSponge_1t_20230905.rcOutput");
        BytesBuilder.ToNull(maxDataLen,               lastOutput);
        BytesBuilder.ToNull(ReserveConnectionFullLen, fullOutput);
        BytesBuilder.ULongToBytes(MagicNumber_ReverseConnectionLink_forInput, fullOutput, ReserveConnectionFullLen, ReserveConnectionLen);        // Устанавливаем магическое число
        BytesBuilder.ULongToBytes(MagicNumber_ReverseConnectionLink_forInput,   rcOutput, ReserveConnectionFullLen, ReserveConnectionLen);        // Устанавливаем магическое число

        // Для выравнивания, считаем, что на один ThreeFish приходится 256 байтов. Это используется в setThreeFishKeysAndTweak
        countOfThreeFish_RC = wide >> 1;
        countOfThreeFish    = wide;
        threefishCrypto     = Keccak_abstract.allocator.AllocMemory(256*countOfThreeFish, "CascadeSponge_1t_20230905.reverseCrypto");

        // На всякий случай, сразу же инициализируем ключи и твики ThreeFish, чтобы их можно было дальше использовать
        InitEmptyThreeFish();

        // Console.WriteLine(this);
    }

    public static nint CalcTallAndWideByStrenght(nint _strenghtInBytes, ref nint _wide)
    {
        nint _tall = (nint)Math.Ceiling((double)_strenghtInBytes / (double)MaxInputForKeccak);
        if (_tall < MinTall)
            _tall = MinTall;

        if (_wide == 0)
        {
            // _wide = CalcMinWide(_tall);
            _wide = _tall - 1;
            if ((_wide & 1) > 0)
                _wide++;
        }

        return _tall;
    }

    protected bool isThreeFishInitialized = false;

    /// <summary>Позволяет установить ключи и твики для ThreeFish обратной связи</summary>
    /// <param name="keys">Ключи, каждый ключ по 128-мь байтов. Количество ключей - countOfThreeFish (countOfThreeFish_RC для обратной связи и столько же для шифрования выхода). Первыми идут ключи для обратной связи, потом - для заключительного преобразования. В конце массива должно быть магическое число MagicNumber_ReverseConnectionLink_forInput</param>
    /// <param name="tweaks">Твики, каждый твик по 16-ть байтов (по одному твику на ключ). В конце массива должно быть магическое число MagicNumber_ReverseConnectionLink_forInput</param>
    /// <param name="countOfKeys">Общее количество ключей и твиков</param>
    public void setThreeFishKeysAndTweak(byte * keys, byte * tweaks, int countOfKeys)
    {
        CheckMagicNumber(keys,   "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: keys",   countOfKeys * threefish_slowly.keyLen);
        CheckMagicNumber(tweaks, "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: tweaks", countOfKeys * threefish_slowly.twLen);

        if (countOfKeys < countOfThreeFish)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: countOfKeys < countOfThreeFish");

        var rc = threefishCrypto!.array;
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

        // Console.WriteLine("keys"); Console.WriteLine(VinKekFish_Utils.Utils.ArrayToHex(threefishCrypto, countOfThreeFish*256));
    }

    // code::docs:Wt74dfPfEIcGzPN5Jrxe:
    /// <summary>Инициализирует твики ThreeFish константами. Не портит ключи, если пользователь их проинициализировал.</summary>
    /// <param name="emptyTweakInitValue">Простое значение для инициализации начального твика. Дальше движение по твикам идёт с инкрементом на TweakInitIncrement</param>
    public void InitEmptyThreeFishTweaks(ulong emptyTweakInitValue = TweakInitIncrement)
    {
        var   rc  = threefishCrypto!.array + 192;    // Сразу выполняем переход на твики
        ulong tw0 = emptyTweakInitValue;
        ulong tw1 = 0;
        for (ulong i = 0; i < (ulong) countOfThreeFish; i++)
        {
            var tw = (ulong*)rc;
            rc += 256;

            tw[0] = tw0;
            tw[1] = tw1;

            tw0  += TweakInitIncrement;
            // Если произошло переполнение
            if (tw0 < tw[0])
                tw1 += 1;
        }

        ExpandThreeFish();
    }

    /// <summary>Инициализирует ключи ThreeFish константами, если пользователь не хочет их проинициализировать. Не портит твики, если пользователь их проинициализировал.</summary>
    /// <param name="emptyKeyInitValue">Простое значение для инициализации ключей</param>
    public void InitEmptyThreeFishKeys(ulong emptyKeyInitValue = KeyInitIncrement)
    {
        var   rc  = threefishCrypto!.array + 0;    // Сразу выполняем переход на ключи
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
        rc = threefishCrypto!.array;
        for (int i = 0; i < countOfThreeFish; i++)
        {
            Threefish1024.genExpandedKey((ulong*)rc); rc += 192;

            rcu = (ulong*)rc;
            rcu[2] = rcu[0] ^ rcu[1];
            rc += 64;
        }

        isThreeFishInitialized = true;
    }

    // code::cp:num:iaV4PVmCjh9eUIuXIy34:
    public static nint CalcMinWide(nint tall)
    {
        var wide = (nint) Math.Ceiling(  Math.Log2(tall)+1  );
        if ((wide & 1) > 0)
            wide++;

        return wide;
    }

    /// <summary>Каскад губок keccak. Первый индекс - высота, второй - ширина</summary>
    protected Keccak_20200918?[,] CascadeKeccak;
    protected Keccak_20200918 getInputLayer (nint i) => CascadeKeccak[0,      i]!;
    protected Keccak_20200918 getOutputLayer(nint i) => CascadeKeccak[tall-1, i]!;

    /// <summary>Массив ключей и твиков ThreeFish. Первыми идут ключи обратной связи, потом ключи заключительного преобразования.</summary>
    protected Record?  threefishCrypto;


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
        if (  rcOutput.array is not null)
            rcOutput.Dispose();

        forAllKeccaks
        (
            (nint i, nint j) =>
            {
                CascadeKeccak[i, j]?.Dispose();
                CascadeKeccak[i, j] = null;
            }
        );

        threefishCrypto?.Dispose();
        threefishCrypto = null;

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
