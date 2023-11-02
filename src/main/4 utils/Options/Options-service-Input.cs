// TODO: tests
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text;
// TODO: сделать здесь локализацию
namespace VinKekFish_Utils.ProgramOptions;

public partial class Options_Service
{
    public class Input: Element
    {
        public Input(Root parent, List<Options.Block> blocks, Options.Block thisBlock): base(parent, blocks, thisBlock)
        {}

        public Entropy? entropy;

        public override void SelectBlock(Options.Block block, string canonicalName)
        {
            switch(canonicalName)
            {
                case "entropy": entropy = new Entropy(this, block.blocks, block); break;
                default:        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'entropy'");
            }
        }

        public override void Check()
        {
            if (entropy == null)
                throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have 'entropy' element. Have no 'entropy' element");

            base.Check();
        }

        public class Entropy : Element
        {
            public Entropy(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
            {}

            public OS?       os;
            public Standard? standard;

            public override void SelectBlock(Options.Block block, string canonicalName)
            {
                switch(canonicalName)
                {
                    case "os"      : os       = new OS      (this, block.blocks, block); break;
                    case "standard": standard = new Standard(this, block.blocks, block); break;
                    default:        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'OS' and 'standard'");
                }
            }

            public override void Check()
            {
                if (os == null)
                    this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service options was not found 'OS' element");
                if (standard == null)
                    this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service options was not found 'standard' element");

                if (elements.Count <= 0)
                    throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have an one or more element. Have no one element. Acceptable: 'OS' and 'standard'");

                base.Check();
            }

            public class OS : Element
            {
                public OS(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public readonly List<InputElement> randoms = new List<InputElement>();

                public FileInfo? File;

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    randoms.Add(  InputElement.getInputElemement(this, block, canonicalName)  );
                }

                public override void Check()
                {
                    if (randoms.Count <= 0)
                        this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service options was found no one element");

                    base.Check();
                }
            }

            public class Standard : OS
            {
                public Standard(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}
            }

            /// <summary>Представляет элементы типа 'file' и 'cmd', содержащие настройки для получения энтропии</summary>
            public abstract class InputElement: Element
            {
                public string?         PathString {get; protected set;}
                public Intervals?      intervals;

                public InputElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    if (PathString == null)
                    {
                        PathString = block.Name;
                    }
                    else
                    {
                        AdditionalBlock(block, canonicalName);
                    }

                    if (block.blocks.Count > 0)
                    {
                        if (intervals != null)
                            throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option found the excess element '{block.Name}' at line {1+block.startLine}. Perhaps the new command ('file', 'cmd' or another) is forgotten?");

                        intervals = new Intervals(parent, block.blocks, block);
                    }
                }

                public abstract void AdditionalBlock(Options.Block block, string canonicalName);

                public override void Check()
                {
                    if (intervals == null)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have one interval element. Have no interval element");

                    if (PathString == null)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have a path string. Have no the path string");

                    base.Check();
                }

                public static InputElement getInputElemement(Element parent, Options.Block block, string canonicalName)
                {
                    switch (canonicalName)
                    {
                        case "file":
                            var rnd = new InputFileElement(parent, block.blocks, block);

                            if (string.IsNullOrEmpty(rnd.PathString))
                                throw new Options_Service_Exception($"The '{parent.getFullElementName()}' element (at line {1+parent.thisBlock.startLine}) of the service option must represent the existing file path. Have no path value (example: '/dev/random')");

                            var file = new FileInfo(rnd.PathString); file.Refresh();
                            if (!file.Exists)
                                throw new Options_Service_Exception($"The '{parent.getFullElementName()}' element (at line {1+parent.thisBlock.startLine}) of the service option must represent the existing file path. The '{file.FullName}' string represents non existent file");

                            return rnd;
                        
                        case "cmd":
                            var cmd = new InputCmdElement(parent, block.blocks, block);

                            if (string.IsNullOrEmpty(cmd.PathString))
                                throw new Options_Service_Exception($"The '{parent.getFullElementName()}' element (at line {1+parent.thisBlock.startLine}) of the service option must represent the existing file path. Have no path value (example: '/dev/random')");

                            return cmd;
                        
                        case "dir":
                        case "directory":
                            var dir = new InputDirElement(parent, block.blocks, block);

                            if (string.IsNullOrEmpty(dir.PathString))
                                throw new Options_Service_Exception($"The '{parent.getFullElementName()}' element (at line {1+parent.thisBlock.startLine}) of the service option must represent the existing file path. Have no path value (example: '/dev/random')");

                            return dir;

                        default:
                            throw new Options_Service_Exception($"The '{parent.getFullElementName()}' element (at line {1+parent.thisBlock.startLine}) of the service option have unknown value '{canonicalName}'. Acceptable is 'dir' ('directory'), 'cmd', 'file'");
                    }
                }
            }

            /// <summary>Представляет источник энтропии, являющийся файлом (или совместимым с ним устройством)</summary>
            public class InputFileElement: InputElement
            {
                public FileInfo? fileInfo { get; protected set; }

                public InputFileElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    base.SelectBlock(block, canonicalName);

                    if (string.IsNullOrEmpty(this.PathString))
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the null path for the file command. Must have a not empty value");

                    fileInfo = new FileInfo(this.PathString); fileInfo.Refresh();
                    if (!fileInfo.Exists)
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the not exists file '{fileInfo.FullName}'. The file must exists");
                }

                public override void Check()
                {
                    if (string.IsNullOrEmpty(PathString))
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have 'PathString' element. Have no 'PathString' element");

                    base.Check();
                }

                public override void AdditionalBlock(Options.Block block, string canonicalName)
                {
                    throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option found the excess element '{block.Name}' at line {1+block.startLine}. Perhaps the new command ('file', 'cmd' or another) is forgotten?");
                }
            }

            /// <summary>Представляет источник энтропии, являющийся файлом (или совместимым с ним устройством)</summary>
            public class InputCmdElement: InputElement
            {
                public string  parameters = "";
                public string? workingDir;
                public string? userName;
                public InputCmdElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void Check()
                {
                    base.Check();
                }

                public override void AdditionalBlock(Options.Block block, string canonicalName)
                {
                    if (canonicalName.StartsWith("working dir:"))
                    {
                        workingDir = block.Name.Substring("working dir:".Length).Trim();
                        return;
                    }

                    if (canonicalName.StartsWith("user:"))
                    {
                        userName = block.Name.Substring("user:".Length).Trim();
                        return;
                    }

                    if (parameters.Length > 0)
                        parameters += " ";

                    parameters += block.Name;
                }
            }

            /// <summary>Представляет источник энтропии, являющийся директорией с использованием FileSystemWatcher для наблюдения за вновь появляющимися файлами</summary>
            public class InputDirElement: InputElement
            {
                public DirectoryInfo? dirInfo { get; protected set; }
                public InputDirElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    base.SelectBlock(block, canonicalName);

                    if (string.IsNullOrEmpty(this.PathString))
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the null path for the directory command. Must have a not empty value");

                    dirInfo = new DirectoryInfo(this.PathString); dirInfo.Refresh();
                    if (!dirInfo.Exists)
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the not exists directory '{dirInfo.FullName}'. The directory must exists");
                }

                public override void Check()
                {
                    base.Check();
                }

                public override void AdditionalBlock(Options.Block block, string canonicalName)
                {}
            }

            public class EntropyValues
            {
                /// <summary>max >= min, EME >= max, avg >= min. EME и max всегда могут быть равны нулю, даже если min или max не равно нулю. Все значения могут быть равны нулю. Нулевые значения означают, что источник энтропии вообще может не дать энтропии.
                /// <para>min - оценка минимального количества битов, которое нужно получить из этого источника для того, чтобы получить хотя бы один бит энтропии.</para>
                /// <para>max - оценка максимального количества битов, которое нужно получить из этого источника для того, чтобы получить хотя бы один бит энтропии.</para>
                /// <para>EME - оценка максимального количества битов в благоприятных условиях, которое нужно получить из этого источника для того, чтобы получить хотя бы один бит энтропии, который затруднительно перехватить с помощью прослушивания электромагнитных излучений, прослушивания сетевого траффика и других атак по побочным каналам</para>
                /// <para>avg - оценка максимального количества битов в благоприятных условиях, которое нужно получить из этого источника для того, чтобы получить хотя бы один бит энтропии.</para>
                /// </summary>
                public long min = -1, max = -1, EME = -1, avg = -1;

                public bool isCorrect()
                {
                    if (min < 0)
                        return false;
                    if (max < 0)
                        return false;
                    if (avg < 0)
                        return false;

                    if (max != 0)
                    if (max < min)
                        return false;

                    if (avg != 0)
                    {
                        if (avg < min)
                            return false;

                        if (avg > max && max != 0)
                            return false;
                    }

                    if (EME < 0)
                        return false;

                    if (EME != 0)
                    if (EME < max)
                        return false;

                    if (min == 0)
                    if (EME > 0 || max > 0 || avg > 0)
                        return false;

                    return true;
                }
            }

            public class Intervals: Element
            {
                public Interval? interval { get; protected set; }

                public readonly EntropyValues entropy = new EntropyValues();

                public Intervals(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    switch (canonicalName)
                    {
                        case "no entropy evaluation":
                            entropy.min = 0;
                            entropy.max = 0;
                            entropy.avg = 0;
                            entropy.EME = 0;
                            break;

                        case "min":
                            setMin(block); break;
                        case "max":
                            setMax(block); break;
                        case "eme":
                            setEME(block); break;
                        case "avg":
                            setAvg(block); break;
                        case "interval":

                            if (interval == null)
                                interval = new Interval(this, block.blocks, block);
                            else
                                throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found twiced element interval. Can only one an 'interval' element");

                            break;

                        default:
                            throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the value '{block.Name}'. Acceptable is values 'min', 'max', 'EME', 'interval'");
                    }
                }

                protected void setMin(Options.Block block)
                {
                    try
                    {
                        entropy.min = long.Parse(block.blocks[0].Name);
                        if (block.blocks.Count > 1)
                            throw new Exception();
                        if (entropy.min < 0)
                            throw new Exception();
                    }
                    catch
                    {
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element 'min' found. Acceptable value is nonnegative integer (example: 0, 1, 2, 3)");
                    }
                }

                protected void setMax(Options.Block block)
                {
                    try
                    {
                        entropy.max = long.Parse(block.blocks[0].Name);
                        if (block.blocks.Count > 1)
                            throw new Exception();
                        if (entropy.max < 0)
                            throw new Exception();
                    }
                    catch
                    {
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element 'max' found. Acceptable value is nonnegative integer (example: 0, 1, 2, 3)");
                    }
                }

                protected void setEME(Options.Block block)
                {
                    try
                    {
                        entropy.EME = long.Parse(block.blocks[0].Name);
                        if (block.blocks.Count > 1)
                            throw new Exception();
                        if (entropy.EME < 0)
                            throw new Exception();
                    }
                    catch
                    {
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element 'EME' found. Acceptable value is nonnegative integer (example: 0, 1, 2, 3)");
                    }
                }

                protected void setAvg(Options.Block block)
                {
                    try
                    {
                        entropy.avg = long.Parse(block.blocks[0].Name);
                        if (block.blocks.Count > 1)
                            throw new Exception();
                        if (entropy.avg < 0)
                            throw new Exception();
                    }
                    catch
                    {
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element 'EME' found. Acceptable value is nonnegative integer (example: 0, 1, 2, 3)");
                    }
                }

                public override void Check()
                {
                    if (!entropy.isCorrect())
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have 'min', 'avg', 'max' and 'EME' elements. Must 'min' >= 0, 'avg' >= 0, 'max' >= 0, 'EME' >= 0 and min <= avg <= max <= EME (exclude 0 values)");
                    
                    if (interval == null)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have one 'interval' element. Have no one 'interval' element");

                    base.Check();
                }
            }

            public class Interval: Element
            {
                public enum IntervalTypeEnum { none = 0, time = 1, once = 2, continuously = 3, fast = 4 };

                public Interval.Flags? flags;

                public Interval(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public readonly List<InnerIntervalElement> inner = new List<InnerIntervalElement>();

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    IntervalTypeEnum IntervalType = IntervalTypeEnum.none;
                    long time = -2;
                    if (canonicalName == "once")
                    {
                        time = -1;
                        IntervalType = IntervalTypeEnum.once;
                    }
                    else
                    if (canonicalName == "fast" || canonicalName == "often")
                    {
                        time = 0;
                        IntervalType = IntervalTypeEnum.fast;
                    }
                    else
                    if (canonicalName == "continuously" || canonicalName == "0")
                    {
                        time = 0;
                        IntervalType = IntervalTypeEnum.continuously;
                    }
                    else
                    {
                        var timev = getTime(canonicalName);
                        if (timev <= -1)
                            throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the value '{block.Name}'. Acceptable is value similary 'once', '--' (once), '0' (continuesly), '1ms', '1s', '1' (seconds), '1m', '1h'");

                        time = timev;
                        IntervalType = IntervalTypeEnum.time;
                    }

                    inner.Add(new InnerIntervalElement(this, block.blocks, block, time, IntervalType));
                }

                protected SortedList<string, long> TimeFactors = new SortedList<string, long>(4) { {"ms", 1}, {"s", 1000}, {"m", 60*1000}, {"h", 60*60*1000} };
                /// <summary>Распарсить строку вида "1s"</summary>
                /// <param name="timeString">Строка для парсинга</param>
                /// <returns>-1 - если строку не удалось распарсить. Иначе - время в миллисекундах.</returns>
                public long getTime(string timeString)
                {
                    long result;
                    foreach (var factor in TimeFactors)
                    {
                        if (timeString.EndsWith(factor.Key))
                        {
                            if (getTime(timeString, factor, out result))
                                return result;
                        }
                    }

                    if (getTime(timeString, new KeyValuePair<string, long>("", 1000), out result))
                        return result;

                    return -1;
                }

                /// <summary>Распарсить строку типа "1s", получив из неё время</summary>
                /// <param name="timeString">Строка для парсинга</param>
                /// <param name="factor">Модификатор единицы времени из списка TimeFactors</param>
                /// <param name="time">Возвращаемый результат: время в миллисекундах</param>
                /// <returns>true - если время получено успешно. false - если время не было распознано</returns>
                public static bool getTime(string timeString, KeyValuePair<string, long> factor, out long time)
                {
                    time = -1;
                    try
                    {
                        timeString = timeString[0..^factor.Key.Length].Trim();
                        time       = factor.Value * long.Parse(timeString);

                        return true;
                    }
                    catch
                    {}

                    return false;
                }

                public override void Check()
                {
                    if (inner.Count <= 0)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have one 'interval' element. Have no one element");

                    base.Check();
                }

                public class InnerIntervalElement: Element
                {
                    public InnerIntervalElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock, long time, IntervalTypeEnum IntervalType) : base(parent, blocks, thisBlock)
                    {
                        this.time = time;
                        this.IntervalType = IntervalType;
                    }

                    public long             time       { get; protected set; } = -2;
                    public LengthElement?   Length;
                    public Flags?           flags;
                    public Difference?      Difference;
                    public IntervalTypeEnum IntervalType = IntervalTypeEnum.none;
                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        switch (canonicalName)
                        {
                            case "length":
                                Length = new LengthElement(this, block.blocks, block); break;

                            case "flags":
                                flags = new Flags(this, block.blocks, block);
                                (parent as Interval)!.flags = flags;
                                break;

                            case "difference":
                                Difference = new Difference(this, block.blocks, block); break;

                            default:
                                throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the element '{block.Name}'. Acceptable is 'length', 'flags', 'difference'");
                        }
                    }

                    public override void Check()
                    {
                        if (time == -2)
                            throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have element this represents time (for example: '1s')");

                        if (Length == null || Length.Length == -2)
                            throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option must have element this represents length of data to input (for example: '32', '--', 'full')");

                        if (flags == null)
                            this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+thisBlock.startLine}) of the service options was not found a 'flags' element");

                        if (IntervalType != IntervalTypeEnum.continuously)
                        if (IntervalType != IntervalTypeEnum.once)
                        if (IntervalType != IntervalTypeEnum.fast)
                        if (Difference == null || Difference.differenceValue == Difference.DifferenceValue.undefined)
                            this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+thisBlock.startLine}) of the service options was not found a 'difference' element or value of the element");

                        if (IntervalType == IntervalTypeEnum.none)
                            throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option occured unknown interval type. Acceptable is 'once', 'fast' ('often'), 'continuously'");

                        if (flags != null)
                        if (IntervalType == IntervalTypeEnum.fast)
                        {
                            if (flags.date == Flags.FlagValue.dateOnly)
                                throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of the service option occured 'date only' flag. This flag is not acceptable.");
                        }

                        base.Check();
                    }
                }

                public class Flags : Element
                {
                    public Flags(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {
                    }

                    public enum FlagValue { undefined = 0, yes = 1, no = 2, dateOnly = 3 };           /// <summary>Добавлять при чтении из источника энтропии точное время чтения (как дополнительный источник энтропии)</summary>
                    public FlagValue date       = FlagValue.no;                         /// <summary>Информация не поступает в губку</summary>
                    public FlagValue ignored    = FlagValue.no;                         /// <summary>Выводить ли всю информацию, полученную из источника энтропии, в лог-файл</summary>
                    public FlagValue log        = FlagValue.no;
                    public FlagValue broker     = FlagValue.no;                         /// <summary>Показывать ли время от времени в логах количество полученных из источника байтов</summary>
                    public FlagValue watchInLog = FlagValue.no;

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        switch (canonicalName)
                        {
                            case "date":
                                date    = FlagValue.yes; break;

                            case "date only":
                                date    = FlagValue.dateOnly; break;

                            case "log":
                                log     = FlagValue.yes;
                                break;

                            case "ignored:log":
                                log     = FlagValue.yes;
                                goto case "ignored";

                            case "ignored":
                                ignored = FlagValue.yes; break;

                            case "broker":
                                broker  = FlagValue.yes; break;

                            case "watch counter in log":
                                watchInLog = FlagValue.yes; break;

                            default:
                                throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the value '{block.Name}'. Acceptable is 'date', 'date only', 'ignored', 'log', 'ignored:log', 'watch counter in log'");
                        }
                    }

                    public override void Check()
                    {
                        if (log == FlagValue.yes)
                            this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+thisBlock.startLine}) of the service options found 'log' flag");
                        if (ignored == FlagValue.yes)
                            this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+thisBlock.startLine}) of the service options found 'ignored' flag");

                        base.Check();
                    }
                }

                public class LengthElement : Element
                {
                    protected int  i = 0;
                    public    long Length = -2;
                    public LengthElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {}

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        if (i > 0)
                            throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the second value of the length block: '{block.Name}'. Must be one and only one value");

                        i++;

                        if (canonicalName == "full" || canonicalName == "--")
                        {
                            Length = 0;
                            return;
                        }

                        try
                        {
                            Length = long.Parse(canonicalName);
                        }
                        catch
                        {
                            throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the value '{block.Name}'. Acceptable is value similary '1', '32', etc. (in bytes)");
                        }
                    }
                }

                public class Difference : Element
                {
                    public enum    DifferenceValue { undefined = 0, yes = 1, no = 2, complex = 4 };
                    public DifferenceValue differenceValue;
                    public bool    processDifference = false;
                    public string? differenceCommand;
                    public Difference(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {
                    }

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        if (canonicalName == "no")
                        {
                            processDifference = false;
                            differenceValue   = DifferenceValue.no;
                            return;
                        }

                        if (canonicalName == "yes")
                        {
                            processDifference = false;
                            differenceValue   = DifferenceValue.yes;
                            return;
                        }

                        processDifference = true;
                        differenceValue   = DifferenceValue.complex;
                        differenceCommand = block.Name;
                    }
                }
            }
        }
    }
}
