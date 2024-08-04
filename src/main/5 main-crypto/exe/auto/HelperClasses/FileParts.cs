using System.Runtime;

// TODO: tests. Тесты сделаны, но очень слабые. Нужно ещё.
// test:6mN7tkWO7uSf70KW9M3I:

namespace VinKekFish_EXE;

using System.IO;
using System.Reflection;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

/// <summary>Представляет описатель части файла, которая может содержать другие части файла. Используется для того, чтобы рассчитывать размеры и адреса файлов. Потоконебезопасный, требует внешних блокировок.</summary>
public unsafe partial class FileParts
{
    /// <summary>Это нужно задать при создании. Мнемоническое имя части, используется для облегчения ориентирования программиста в файле.</summary>
    public readonly string Name;
    public readonly FileParts? parent = null;

    public FileParts(string Name, bool doNotDispose = false, FileParts? parent = null)
    {
        GC.ReRegisterForFinalize(this);

        this.Name         = Name;
        this.doNotDispose = doNotDispose;
        this.parent       = parent;
    }

    /// <summary>Представляет минимальную и максимальную величины чего-либо, например, оценки размера некоторого файла или поля.</summary>
    public readonly struct Approximation
    {
        public readonly nint min = 0;
        public readonly nint max = 0;

        public Approximation(nint min, nint max)
        {
            this.min = min;
            this.max = max;
        }

        public Approximation(nint value): this(value, value)
        {}

        public static Approximation operator +(Approximation a, nint size)
        {
            return new Approximation
            (
                a.min + size,
                a.max + size
            );
        }

        public static Approximation operator +(Approximation a, Approximation size)
        {
            return new Approximation
            (
                min: a.min + size.min,
                max: a.max + size.max
            );
        }

        public static readonly Approximation Null = new(0);

        /// <summary>Если min == max, то value возвращает это знаение. Иначе выкидывает исключение InvalidOperationException</summary>
        public nint Value
        {
            get
            {
                if (!IsFine)
                    throw new InvalidOperationException("FileParts.Approximation.value: min != max (!isFine)");

                return min;
            }
        }
                                                    /// <summary>Если оценка min и max совпадает, то у объекта имеется одно строго определённое значение типа nint (свойство value)</summary>
        public bool IsFine => min == max;

        public static explicit operator nint(Approximation a)
        {
            return a.Value;
        }
    }

                                                                                /// <summary>Содержимое части. Если это поле записывается с помощью setArrayToRecord, то переопределяется и поле size.</summary>
    public Record? content   = null;                                            /// <summary>Вспомогательное содержимое части. Если есть и content, и btContent, сначала идёт btContent. btContent, по умолчанию, содержит длину content.</summary>
    public byte[]? btContent = null;
                                                                                                    /// <summary>Части файла, отсортированные по порядку их вхождения в файл.</summary>
    public readonly List<FileParts> innerParts = new();

                                                                                /// <summary>Адрес, по которому начинается данный блок</summary>
    protected Approximation _startAddress        =  Approximation.Null;         /// <summary>Адрес первого байта, который уже не входит в блок. То есть этот адрес уже за границей данного блока. (StartAddress + fullLen)</summary>
    public    Approximation FirstAfterEndAddress => _startAddress + FullLen;

    public virtual Approximation FullLen
    {
        get
        {
            Approximation result = Approximation.Null;

            result += Size;
            foreach (var part in innerParts)
            {
                result += part.FullLen;
            }

            return result;
        }
    }
                                                                /// <summary>Поле, используемое свойством size. Не рекомендуется использовать, т.к. size автоматически производит перерасчёт адресов при изменении размера.</summary>
    public Approximation _size = Approximation.Null;            /// <summary>Оценка размера. Если это не лист, то может иметь нулевой размер. Ненулевой размер у узла сдвигает подчинённые узлы на этот размер (то есть этот блок идёт перед подчинёнными узлами). Если записывается содержимое с помощью setArrayToRecord, то размер переопределяется.</summary>
    public virtual Approximation Size
    {
        get => _size;
        set
        {
            _size = value;
            CalcAddresses();
        }
    }
                                                                            /// <summary>Адрес, по которому начинается данный блок</summary>
    public Approximation StartAddress
    {
        get => _startAddress;
        set
        {
            _startAddress = value;
            CalcAddresses();
        }
    }
                                                                    /// <summary>Метод рассчитывает адреса входящих в него </summary>
    public void CalcAddresses()
    {
        var start = _startAddress + _size;
        foreach (var part in innerParts)
        {
            part.StartAddress = start;
            start = part.FirstAfterEndAddress;
        }
    }

    /// <summary>Добавляет в конец файла новую часть</summary>
    /// <param name="Name">Имя добавляемой части файла</param>
    /// <param name="min">Минимальная оценка собственной длины.</param>
    /// <param name="max">Максимальная оценка собственный длины.</param>
    /// <returns>(Индекс добавленной части в списке innerParts. Сама добавленная часть файла)</returns>
    public (int, FileParts) AddFilePart(string Name, nint min = 0, nint max = 0)
    {
        var result = new FileParts(Name, doNotDispose, parent: this);
        innerParts.Add(result);

        result.Size = new Approximation(min, max);

        return (innerParts.Count - 1, result);
    }

    /// <summary>Добавляет в конец секции файла новую часть и пересчитывает size.</summary>
    /// <param name="Name">Имя добавляемой части файла.</param>
    /// <param name="content">Содержимое части. Копируется в новый Record. Массив content может быть (и, возможно, должен быть) перезаписан сразу после возврата функции.</param>
    /// <returns>(Индекс добавленной части в списке innerParts. Сама добавленная часть файла)</returns>
    /// <param name="createLengthArray">Если true, то btContent (должен быть null) будет содержать массив с длиной записи content.</param>
    public (int Index, FileParts newFilePart) AddFilePart(string Name, byte[] content, bool createLengthArray = true)
    {
        return AddFilePart(Name, Record.GetRecordFromBytesArray(content), createLengthArray);
    }

    /// <summary>Добавляет в конец файла новую часть и пересчитывает size.</summary>
    /// <param name="Name">Имя добавляемой части файла</param>
    /// <param name="content">Содержимое части. Запоминается по ссылке. Уничтожается автоматически при Dispose объекта FileParts.</param>
    /// <param name="createLengthArray">Вставить перед записью длину записи.</param>
    /// <param name="doNotDisposeOption">Если doNotDispose == true (yes), то новая запись будет создана с doNotDispose == true. Если "no", то запись будет создана с doNotDispose == false. Иначе (unknown) будет унаследована от текущей записи. Значение "yes" означает, что параметр content не будет автоматически освобождён и пользователь должен освободить его сам после использования в этой секции.</param>
    /// <returns>(Индекс добавленной части в списке innerParts. Сама добавленная часть файла)</returns>
    /// <param name="createLengthArray">Если true, то btContent (должен быть null) будет содержать массив с длиной записи content.</param>
    public (int Index, FileParts newFilePart) AddFilePart(string Name, Record content, bool createLengthArray = true, DoNotDisposeEnum doNotDisposeOption = DoNotDisposeEnum.unknown)
    {
        var result = new FileParts(Name, doNotDisposeOption.ResetDoNotDispose(doNotDispose), parent: this);
        innerParts.Add(result);

        result.SetArrayToRecord(content, createLengthArray);

        return (innerParts.Count - 1, result);
    }

    /// <summary>Находит первую часть файла, имеющую имя Name.</summary>
    /// <param name="Name">Имя искомой части.</param>
    /// <param name="startIndex">Начальный индекс в списке частей, с которого мы будем искать.</param>
    /// <returns>Индекс найденной части в списке innerParts. -1 - если ничего не найдено. Найденная часть файла или null, если ничего не найдено.</returns>
    public (int Index, FileParts? FoundFilePart) FindFirstPart(string Name, int startIndex = 0)
    {
        for (int i = startIndex; i < innerParts.Count; i++)
        {
            if (innerParts[i].Name == Name)
                return (i, innerParts[i]);
        }

        return (-1, null);
    }

    /// <summary>Задать значение поля content и свойства size.</summary>
    /// <param name="content">Содержимое части файла.</param>
    /// <param name="createLengthArray">Если true, то btContent (должен быть null) будет содержать массив с длиной записи content.</param>
    public void SetArrayToRecord(Record content, bool createLengthArray = false)
    {
        if (createLengthArray)
        {
            if (btContent != null)
                throw new InvalidOperationException("FileParts.setArrayToRecord: btContent != null");

            BytesBuilder.VariableULongToBytes((ulong) content.len, ref btContent);
        }

        this.content = content;

        var btLen = btContent is null ? 0 : btContent.Length;
        this.Size = new Approximation(content.len + btLen);
    }

    /// <summary>Записывает данные в поток ввода-вывода.</summary>
    /// <param name="fs">Поток ввода вывода. FileStream, MemoryStream (если нужно, с использованием далее StreamReader) и др.</param>
    public void WriteToFile(Stream fs)
    {
        if (btContent is not null)
        if (btContent.LongLength > 0)
            fs.Write(btContent);

        if (content is not null)
        if (content.len > 0)
            fs.Write(content);

        foreach (var f in innerParts)
        {
            f.WriteToFile(fs);
        }
    }

    /// <summary>Записывает данные в файл.</summary>
    /// <param name="outKeyFile">Описатель файла. Файл должен не существовать.</param>
    /// <param name="fileMode">Режим открытия файла. По-умолчанию файл должен не существовать (FileMode.CreateNew).</param>
    public void WriteToFile(FileInfo outKeyFile, FileMode fileMode = FileMode.CreateNew)
    {
        var fs = new FileStream(outKeyFile.FullName, fileMode, FileAccess.Write, FileShare.None);
        try
        {
            WriteToFile(fs);
        }
        finally
        {
            fs.Flush();
            fs.Close();
        }
    }

    /// <summary>Записывает данные из объекта в Record.</summary>
    /// <param name="rec">Объект Record, в который происходит запись данных. Может быть null. В таком случае, объект создаётся в методе и возвращается как результат метода. Объект не удаляется.</param>
    /// <returns>Возвращает объект rec, в который была произведена запись. Если rec был не равен нулю, то это тот же объект, что был передан как параметр rec, в противнос случае это вновь созданный объект.</returns>
    /// <exception cref="InvalidDataException">Если размер данного объекта не определён.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Если rec слишком мал.</exception>
    public Record WriteToRecord(Record? rec = null)
    {
        nint cur = 0;
        return WriteToRecord(ref cur, rec);
    }

    /// <summary>Записывает данные из объекта в Record.</summary>
    /// <param name="current">Вероятнее всего 0, если вызывается из пользовательского кода. Это - начальное смещение относительно начала записи rec, с которого начинается запись в rec.</param>
    /// <param name="rec">Объект Record, в который происходит запись данных. Может быть null. В таком случае, объект создаётся в методе и возвращается как результат метода. Объект не удаляется.</param>
    /// <returns>Возвращает объект rec, в который была произведена запись. Если rec был не равен нулю, то это тот же объект, что был передан как параметр rec, в противнос случае это вновь созданный объект.</returns>
    /// <exception cref="InvalidDataException">Если размер данного объекта не определён.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Если rec слишком мал.</exception>
    public Record WriteToRecord(ref nint current, Record? rec = null)
    {
        var fSize = this.FullLen;
        if (fSize.min != fSize.max)
            throw new InvalidDataException("FileParts.WriteToRecord: fullLen.min != fullLen.max. Data has not been initialized.");

        rec ??= Keccak_abstract.allocator.AllocMemory(fSize.max, "FileParts.WriteToRecord");

        if (rec.len < fSize.max)
            throw new ArgumentOutOfRangeException(nameof(rec), "FileParts.WriteToRecord: rec.len < fullLen.max");

        if (btContent is not null)
        if (btContent.LongLength > 0)
        fixed (byte * bt = btContent)
        {
            current += BytesBuilder.CopyTo((nint) btContent.LongLength, rec.len, bt, rec, current);
        }

        if (content is not null)
        if (content.len > 0)
            current += BytesBuilder.CopyTo(content.len, rec.len, content, rec, current);

        foreach (var f in innerParts)
        {
            f.WriteToRecord(ref current, rec);
        }

        return rec;
    }
}
