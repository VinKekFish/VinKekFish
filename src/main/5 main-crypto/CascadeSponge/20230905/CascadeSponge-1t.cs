// TODO: tests
namespace vinkekfish;

using System.Collections;
using System.Diagnostics.Tracing;
using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;

// code::docs:rQN6ZzeeepyOpOnTPKAT:  Это главный файл реализации

// ::cp:alg:a7L6XjXsuwWGVxwJSN1x.main:20230930


/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_1t_20230905: IDisposable
{
                                                                /// <summary>Ширина каскадной губки</summary>
    public readonly nint   wide;                                /// <summary>Высота каскадной губки</summary>
    public readonly nint   tall;                                /// <summary>Параметр W - коэффициент, уменьшающий количество внешних данных для ввода и вывода из каждой губки</summary>
    public readonly double W;                                   /// <summary>Параметр Wn - максимальное количество внешних (пользовательских) байтов, которое можно за один шаг ввести или вывести из каждой внешней губки keccak с учётом ограничений каскада</summary>
    public readonly nint   Wn;                                  /// <summary>Количество данных, которые нужно вводить в обратной связи. Это же полный размер данных, передаваемых между разными уровнями каскада keccak.</summary>
    public readonly nint   ReserveConnectionLen;                /// <summary>Размер массива, который необходимо выделить для данных обратной связи с учётом магического числа</summary>
    public readonly nint   ReserveConnectionFullLen;            /// <summary>Максимальная длина данных, вводимая из-вне за один раз или выводимая во-вне (пользователю) за один раз (за один шаг). Это длина данных уже со всей каскадной губки</summary>
    public readonly nint   maxDataLen;                          /// <summary>Минимальная ширина губки (4). Ширина губки всегда должна быть чётной. Минимальная ширина зависит от высоты губки, см. CalcMinWide</summary>
    public const    nint   MinWide = 4;                         /// <summary>Минимальная высота каскадной губки (4)</summary>
    public const    nint   MinTall = 4;                         /// <summary>Максимальное количество байтов, которое можно ввести/вывести в губку вне каскада. Эта константа никогда не изменяется и зависит от построения алгоритма keccak. Реальное количество байтов, доступное для пользовательского ввода/вывода из каждой губки - Wn. Общее количество байтов, доступных для ввода/вывода из всей губки - maxDataLen.</summary>
    public const    byte   MaxInputForKeccak = 64;
                                                                /// <summary>Не нужно пользователю. Значение для инкремента счётчика обратной связи. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(62, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  CounterIncrement   = 3148241843069173559; /// <summary>Не нужно пользователю. Значение для инкремента tweak при пустой инициализации. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(62, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  TweakInitIncrement = 2743445726634853529; /// <summary>Не нужно пользователю. Значение для инкремента key при пустой инициализации. Это значение, вероятно, простое число. Найдено с помощью libnum.generate_prime(63, 1024*80) [ https://github.com/hellman/libnum ]</summary>
    public const    ulong  KeyInitIncrement   = 7825219255190903851; /// <summary>Параметр по-умолчанию для простой инициализации таблицы подстановок.</summary>
    public const    ushort SubstituteInit     = 44381;
                                                                /// <summary>Выходные данные для пользователя. Заполняется при вызове step (точнее, после вызова outputAllData). Если пользователь хочет увеличить стойкость, он может взять только половину этого массива.</summary>
    public    readonly Record lastOutput;                       /// <summary>Полный вывод всех губок. Массив используется для формирования пользовательского вывода lastOutput, для вычисления ThreeFish на месте для пользовательского выхода.</summary>
    protected readonly Record fullOutput;                       /// <summary>Полный вывод всех губок. Используется для формирования и вычисления обратной связи чере ThreeFish</summary>
    protected readonly Record   rcOutput;                       /// <summary>Если true, значит в lastOutput есть данные после шага. false говорит о том, что кто-то сбросил этот флаг и данные из lastOutput использовать уже нельзя.</summary>
    public             bool   haveOutput = false;
                                                                /// <summary>Таблица подстановок. Инициализируется в InitThreeFishByCascade вызовом InitSubstitutionTable. При использовании не забывать приводить к ushort *.</summary>
    protected readonly Record SubstitutionTable;
    protected readonly int    SubstitutionTableLen_inBytes  = 1 << 17;
    protected readonly int    SubstitutionTableLen_inUShort = 1 << 16;
                                                                /// <summary>Стойкость шифрования в байтах. Это tall*MaxInputForKeccak</summary>
    public    readonly nint   strenghtInBytes = 0;              /// <summary>Количество ключей, которые нужны для шифрования обратной связи. Реально ключей в два раза больше</summary>
    public    readonly nint   countOfThreeFish_RC;              /// <summary>Количество ключей, которые нужны для шифрования обратной связи и для шифрования выхода</summary>
    public    readonly nint   countOfThreeFish;                 /// <summary>Полная длина всех ключей ThreeFish в байтах. Равна countOfThreeFish * threefish_slowly.keyLen</summary>
    public    readonly nint   fullLengthOfThreeFishKeys;
                                                                /// <summary>Количество шагов губки, которое пропускается (делается расчёт вхолостую) после ввода/вывода информации в шаге в генерации ключей. Пользователь может вводить это как параметр основного количества шагов в функции step (или как параметр ArmoringSteps; там можно вычесть 1) при генерации важных данных, таких как ключи шифрования.</summary>
    public    readonly nint   countStepsForKeyGeneration;       /// <summary>Количество шагов губки, которое пропускается после ввода/вывода информации в режиме повышенной стойкости (это меньше, чем countStepsForKeyGeneration). Пользователь может вводить это как параметр ArmoringSteps в функции step для усиления криптостойкости сгенерированных данных, например, при шифровании важных данных, или один раз как основное количество холостых шагов перед вычислением имитовставки после шифрования</summary>
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

    /// <summary>Создаёт каскадную губку с заданными параметрами. Однопоточная реализация. Стоит использовать, если не хочется нагружать много процессорных ядер, особенно, на малых wide.</summary>
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
        {
            tall = MinTall;
            if (tall < wide)
                tall = wide;
        }
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

        try
        {

            // Выделяем под губки память сразу для всех губок, иначе, с учётом защитный полей, получается очень много памяти выделяется
            keccakStateLen = (KeccakPrime.S_len2 + KeccakPrime.S_len + KeccakPrime.S_len2) << 3;
            keccakStateLen = VinKekFish_Utils.Utils.calcAlignment(keccakStateLen, 128);  // На всякий случай, берём выравнивание 128-мь байтов, хотя 64-ре вполне достаточно (здесь достаточно выравнивания на границу линии кеша)
            keccakStatesLayerLen = keccakStateLen * wide;
            keccakStatesFullLen = keccakStateLen * tall * wide;
            keccaks = Keccak_abstract.allocator.AllocMemory(keccakStatesFullLen, "CascadeSponge_1t_20230905.keccaks");
            keccaks.Clear();

            // Проверяем, что расчёт длин верный
            if (keccakStatesLayerLen * tall != keccakStatesFullLen)
                throw new CascadeSpongeException($"CascadeSponge_1t_20230905: fatal algorithmic error: keccakStatesLayerLen*tall != keccakStatesFullLen");


            SubstitutionTable = Keccak_abstract.allocator.AllocMemory(SubstitutionTableLen_inBytes, "CascadeSponge_1t_20230905.SubstitutionTable");
            InitEmptySubstitutionTable();

            (W, Wn) = CalcW(tall);

            maxDataLen = Wn * wide;
            ReserveConnectionLen = MaxInputForKeccak * wide;
            ReserveConnectionFullLen = ReserveConnectionLen + 8;
            strenghtInBytes = tall * MaxInputForKeccak;

            // countStepsForKeyGeneration = (nint) Math.Ceiling(  2*tall*Math.Log2(tall) + 1  );        // Это очень долго
            // countStepsForKeyGeneration = (nint) Math.Ceiling(  Math.Log2(tall)+1  );
            // countStepsForHardening     = (nint) 1;
            countStepsForKeyGeneration = (nint)Math.Ceiling(2 * Math.Log2(3 * tall) + 1);
            countStepsForHardening     = (nint)Math.Ceiling(Math.Log2(tall));

            lastOutput = Keccak_abstract.allocator.AllocMemory(maxDataLen, "CascadeSponge_1t_20230905.lastOutput");
            fullOutput = Keccak_abstract.allocator.AllocMemory(ReserveConnectionFullLen, "CascadeSponge_1t_20230905.fullOutput");
            rcOutput = Keccak_abstract.allocator.AllocMemory(ReserveConnectionFullLen, "CascadeSponge_1t_20230905.rcOutput");
            BytesBuilder.ToNull(maxDataLen, lastOutput);
            BytesBuilder.ToNull(ReserveConnectionFullLen, fullOutput);
            BytesBuilder.ULongToBytes(MagicNumber_ReverseConnectionLink_forInput, fullOutput, ReserveConnectionFullLen, ReserveConnectionLen);        // Устанавливаем магическое число
            BytesBuilder.ULongToBytes(MagicNumber_ReverseConnectionLink_forInput, rcOutput, ReserveConnectionFullLen, ReserveConnectionLen);        // Устанавливаем магическое число

            // Для выравнивания, считаем, что на один ThreeFish приходится 256 байтов. Это используется в setThreeFishKeysAndTweak
            countOfThreeFish_RC = wide >> 1;
            countOfThreeFish = wide;
            threefishCrypto = Keccak_abstract.allocator.AllocMemory(256 * countOfThreeFish, "CascadeSponge_1t_20230905.reverseCrypto");

            fullLengthOfThreeFishKeys = countOfThreeFish * threefish_slowly.keyLen;

            // На всякий случай, сразу же инициализируем ключи и твики ThreeFish, чтобы их можно было дальше использовать
            InitEmptyThreeFish();

            // Console.WriteLine(this);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Выполняет простую инициализацию таблицы подстановок</summary>
    /// <param name="value">Число 0 до 65535 для инициализации</param>
    public void InitEmptySubstitutionTable(ushort value = SubstituteInit)
    {
        var sb = (ushort*) SubstitutionTable;
        for (int i = 0; i <= ushort.MaxValue; i++)
        {
            sb[i] = (ushort) (i ^ value);
        }

        if (!SubstitutionTable_IsCorrect())
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.InitEmptySubstitutionTable: !SubstitutionTable_IsCorrect");

    }

    public bool SubstitutionTable_IsCorrect()
    {
        var len = SubstitutionTableLen_inUShort;
        var b   = stackalloc byte[len >> 3];

        for (int i = 0; i < len; i++)
            BitToBytes.resetBit(b, i);

        var sb = (ushort*) SubstitutionTable;
        for (int i = 0; i <= ushort.MaxValue; i++)
        {
            BitToBytes.setBit(b, sb[i]);
        }

        for (int i = 0; i < len; i++)
            if (!BitToBytes.getBit(b, i))
                return false;

        return true;
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
    /// <param name="tweaks">Может быть null. Твики, каждый твик по 16-ть байтов (по одному твику на ключ). В конце массива должно быть магическое число MagicNumber_ReverseConnectionLink_forInput</param>
    /// <param name="countOfKeys">Общее количество ключей и твиков. Не менее countOfThreeFish. Полная длина массива ключей - fullLengthOfThreeFishKeys</param>
    public void setThreeFishKeysAndTweak(byte * keys, byte * tweaks, nint countOfKeys)
    {
        ObjectDisposedCheck("CascadeSponge_1t_20230905.setThreeFishKeysAndTweak");

        if (tweaks != null)
        CheckMagicNumber(tweaks, "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: tweaks", countOfKeys * threefish_slowly.twLen);
        CheckMagicNumber(keys,   "CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: keys",   countOfKeys * threefish_slowly.keyLen);

        if (countOfKeys < countOfThreeFish)
            throw new CascadeSpongeException("CascadeSponge_1t_20230905.setThreeFishKeysAndTweak: countOfKeys < countOfThreeFish");

        var rc = threefishCrypto!.array;
        for (nint i = 0; i < countOfThreeFish; i++)
        {
            BytesBuilder.CopyTo(threefish_slowly.keyLen, threefish_slowly.keyLen, keys,   rc); rc += 192; keys   += threefish_slowly.keyLen;

            if (tweaks != null)
            {
                BytesBuilder.CopyTo(threefish_slowly.twLen,  threefish_slowly.twLen,  tweaks, rc); rc += 64;  tweaks += threefish_slowly.twLen;
            }
            else
                rc += 64;
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
        ObjectDisposedCheck("CascadeSponge_1t_20230905.InitEmptyThreeFishTweaks");

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
        ObjectDisposedCheck("CascadeSponge_1t_20230905.InitEmptyThreeFishKeys");

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

    /// <summary>Получает матрицу S состояния губки keccak на входном слое</summary>
    /// <param name="i">Номер матрицы на входном слое</param>
    protected byte * getInputLayerS (nint i)
    {
        getKeccakS(0, i, S: out byte * S, C: out byte * C, B: out byte * B);

        return S;
    }

    /// <summary>Получает матрицу S состояния губки keccak на выходном слое</summary>
    /// <param name="i">Номер матрицы на входном слое</param>
    protected byte * getOutputLayerS(nint i)
    {
        getKeccakS(tall-1, i, S: out byte * S, C: out byte * C, B: out byte * B);

        return S;
    }
                                                      /// <summary>Каскад губок keccak. Первый индекс - высота, второй - ширина</summary>
    protected Record keccaks;                         /// <summary>Выравненный размер одиночной губки каскада</summary>
    protected nint   keccakStateLen       = 0;        /// <summary>Размер состояний одиночных губок одного слоя</summary>
    protected nint   keccakStatesLayerLen = 0;        /// <summary>Полный размер состояний всех одиночных губок</summary>
    protected nint   keccakStatesFullLen  = 0;

    /// <summary>Получает матрицы S, C, B состояния keccak.</summary>
    /// <param name="h">Номер слоя каскада (высота)</param>
    /// <param name="w">Номер столбца каскада</param>
    /// <param name="S">Матрица S</param>
    /// <param name="C">Вектор C</param>
    /// <param name="B">Матрица B</param>
    public void getKeccakS(nint h, nint w, out byte * S, out byte * C, out byte * B)
    {
        var State = keccaks.array + h*keccakStatesLayerLen + w*keccakStateLen;
            B     = State;
            C     = B + (KeccakPrime.S_len2 << 3);
            S     = C + (KeccakPrime.S_len  << 3);
    }

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
        {
            var msg = "CascadeSponge_1t_20230905: Dispose executed twiced";
            if (!fromDestructor)
            {
                Record.errorsInDispose = true;
                if (Record.doExceptionOnDisposeTwiced)
                {
                    throw new CascadeSpongeException(msg);
                }
                else
                {
                    Console.Error.WriteLine(msg);
                }
            }

            return;
        }

        if (lastOutput is not null && lastOutput.array is not null)
            VinKekFish_Utils.Utils.TryToDispose(lastOutput);
        if (fullOutput is not null && fullOutput.array is not null)
            VinKekFish_Utils.Utils.TryToDispose(fullOutput);
        if (rcOutput is not null && rcOutput.array is not null)
            VinKekFish_Utils.Utils.TryToDispose(rcOutput);
        if (keccaks is not null && keccaks.array is not null)
            VinKekFish_Utils.Utils.TryToDispose(keccaks);
        if (SubstitutionTable is not null && SubstitutionTable.array is not null)
            VinKekFish_Utils.Utils.TryToDispose(SubstitutionTable);

        VinKekFish_Utils.Utils.TryToDispose(threefishCrypto);
        threefishCrypto = null;

        isDisposed = true;
        if (fromDestructor && !d)
        {
            Record.errorsInDispose = true;

            var emsg = "CascadeSponge_1t_20230905: Object not disposed correctly (Dispose from destructor)";
            if (Record.doExceptionOnDisposeInDestructor)
                throw new CascadeSpongeException(emsg);
            else
                Console.Error.WriteLine(emsg);
        }
    }
}

/*
/// <summary>
/// Класс для тестирования, открывающий некоторые защищённые члены
/// </summary>
public class CascadeSponge_1t_public_for_test: CascadeSponge_1t_20230905
{
    // public Keccak_20200918?[,] CascadeKeccak_public => CascadeKeccak;
}
*/
