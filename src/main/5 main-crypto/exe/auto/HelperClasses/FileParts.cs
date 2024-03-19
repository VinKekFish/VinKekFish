// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

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
    /// <summary>Представляет минимальную и максимальную величины чего-либо, например, оценки размера некоторого файла или поля.</summary>
    public class Approximation
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

        public static readonly Approximation Null = new Approximation(0);

        /// <summary>Если min == max, то value возвращает это знаение. Иначе выкидывает исключение InvalidOperationException</summary>
        public nint value
        {
            get
            {
                if (!isFine)
                    throw new InvalidOperationException("FileParts.Approximation.value: min != max (!isFine)");

                return min;
            }
        }
                                                    /// <summary>Если оценка min и max совпадает, то у объекта имеется одно строго определённое значение типа nint (свойство value)</summary>
        public bool isFine => min == max;

        public static explicit operator nint(Approximation a)
        {
            return a.value;
        }
    }

                                                                                /// <summary>Содержимое части</summary>
    public Record? content   = null;                                            /// <summary>Содержимое части</summary>
    public byte[]? btContent = null;

    public required string Name {get; init;}
                                                                                                    /// <summary>Части файла, отсортированные по порядку их вхождения в файл.</summary>
    public readonly List<FileParts>               innerParts = new List<FileParts>();

                                                                                /// <summary>Адрес, по которому начинается данный блок</summary>
    protected Approximation _startAddress        =  Approximation.Null;         /// <summary>Адрес первого байта, который уже не входит в блок. То есть этот адрес уже за границей данного блока. (StartAddress + fullLen)</summary>
    public    Approximation FirstAfterEndAddress => _startAddress + fullLen;

    public virtual Approximation fullLen
    {
        get
        {
            Approximation result = Approximation.Null;
            foreach (var part in innerParts)
            {
                result += part.fullLen;
            }

            return result;
        }
    }
                                                                /// <summary>Поле, используемое свойством size. Не рекомендуется использовать, т.к. size автоматически производит перерасчёт адресов при изменении размера.</summary>
    public Approximation _size = Approximation.Null;            /// <summary>Оценка размера. Если это не лист, то может иметь нулевой размер. Ненулевой размер у узла сдвигает подчинённые узлы на этот размер (то есть этот блок идёт перед подчинёнными узлами).</summary>
    public virtual Approximation size
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
        var result = new FileParts() {Name = Name};
        innerParts.Add(result);

        result.size = new Approximation(min, max);

        return (innerParts.Count - 1, result);
    }

    /// <summary>Добавляет в конец файла новую часть</summary>
    /// <param name="Name">Имя добавляемой части файла</param>
    /// <param name="btContent">Содержимое части</param>
    /// <returns>(Индекс добавленной части в списке innerParts. Сама добавленная часть файла)</returns>
    public (int, FileParts) AddFilePart(string Name, byte[] btContent)
    {
        var result = new FileParts() {Name = Name};
        innerParts.Add(result);

        result.size      = new Approximation(btContent.Length, btContent.Length);
        result.btContent = btContent;

        return (innerParts.Count - 1, result);
    }

    /// <summary>Добавляет в конец файла новую часть</summary>
    /// <param name="Name">Имя добавляемой части файла</param>
    /// <param name="content">Содержимое части</param>
    /// <returns>(Индекс добавленной части в списке innerParts. Сама добавленная часть файла)</returns>
    public (int, FileParts) AddFilePart(string Name, Record content)
    {
        var result = new FileParts() {Name = Name};
        innerParts.Add(result);

        result.size    = new Approximation(content.len, content.len);
        result.content = content;

        return (innerParts.Count - 1, result);
    }

    /// <summary>Находит первую часть файла, имеющую имя Name.</summary>
    /// <param name="Name">Имя искомой части.</param>
    /// <param name="startIndex">Начальный индекс в списке частей, с которого мы будем искать.</param>
    /// <returns>Индекс найденной части в списке innerParts. -1 - если ничего не найдено. Найденная часть файла или null, если ничего не найдено.</returns>
    public (int, FileParts?) FindFirstPart(string Name, int startIndex = 0)
    {
        for (int i = startIndex; i < innerParts.Count; i++)
        {
            if (innerParts[i].Name == Name)
                return (i, innerParts[i]);
        }

        return (-1, null);
    }
}
