// TODO: tests
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
                throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'entropy' element. Have no 'entropy' element");

            base.Check();
        }

        public class Entropy : Element
        {
            public Entropy(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
            {}

            public OS? os;

            public override void SelectBlock(Options.Block block, string canonicalName)
            {
                switch(canonicalName)
                {
                    case "os": os = new OS(this, block.blocks, block); break;
                    default:        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'OS'");
                }
            }

            public override void Check()
            {
                if (os == null)
                    this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options was not found 'OS' element");

                if (elements.Count <= 0)
                    throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have an one or more element. Have no one element");

                base.Check();
            }

            public class OS : Element
            {
                public OS(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public readonly List<InputFileElement> random = new List<InputFileElement>();

                public FileInfo? File;

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    var rnd = new InputFileElement(this, block.blocks, block);
                    random.Add(rnd);

                    if (string.IsNullOrEmpty(rnd.PathString))
                        throw new Options_Service_Exception($"The '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must represent the existing file path. Have no path value (example: '/dev/random')");

                    File = new FileInfo(rnd.PathString); File.Refresh();
                    if (!File.Exists)
                        throw new Options_Service_Exception($"The '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must represent the existing file path. The '{rnd.PathString}' string represents non existent file");
                }

                public override void Check()
                {
                    if (random.Count <= 0)
                        this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options was found no one element");

                    base.Check();
                }
            }

            /// <summary>Представляет элементы типа 'file' и 'cmd', содержащие настройки для получения энтропии</summary>
            public abstract class InputElement: Element
            {
                public string?    PathString {get; protected set;}
                public Intervals? intervals;

                public InputElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    PathString = block.Name;
                    intervals  = new Intervals(parent, block.blocks, block);
                }

                public override void Check()
                {
                    if (intervals == null)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have interval elements. Have no interval elements");

                    base.Check();
                }

                public static InputElement getInputElemement(Element parent, Options.Block block, string canonicalName)
                {
                    switch(canonicalName)
                    {
                        case "file":    return new InputFileElement(parent, block.blocks, block);
                        case "cmd" :    return new InputCmdElement (parent, block.blocks, block);
                        default:        throw  new Options_Service_Exception($"At line {1+block.startLine} in the '{parent.getFullElementName()}' element found the unknown element '{block.Name}'. Acceptable is 'file', 'cmd'");
                    }
                }
            }

            /// <summary>Представляет источник энтропии, являющийся файлом (или совместимым с ним устройством)</summary>
            public class InputFileElement: InputElement
            {
                public InputFileElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void Check()
                {
                    if (string.IsNullOrEmpty(PathString))
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'PathString' element. Have no 'PathString' element");

                    base.Check();
                }
            }

            /// <summary>Представляет источник энтропии, являющийся файлом (или совместимым с ним устройством)</summary>
            public class InputCmdElement: InputElement
            {
                public InputCmdElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void Check()
                {
                    base.Check();
                }
            }

            public class EntropyValues
            {
                public long min = -1, max = -1, EME = -1;

                public bool isCorrect()
                {
                    if (min < 0)
                        return false;
                    if (max < 0)
                        return false;
                    if (EME < 0)
                        return false;

                    return true;
                }
            }

            public class Intervals: Element
            {
                public readonly List<Interval> intervals = new List<Interval>();

                public readonly EntropyValues entropy = new EntropyValues();

                public Intervals(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    switch (canonicalName)
                    {
                        case "min":
                            setMin(block); break;
                        case "max":
                            setMax(block); break;
                        case "eme":
                            setEME(block); break;
                        case "interval":
                            intervals.Add( new Interval(this, block.blocks, block) ); break;

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

                public override void Check()
                {
                    if (intervals.Count <= 0)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have at least one 'interval' element. Have no one element");
                    if (!entropy.isCorrect())
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'min', 'max' and 'EME' elements. Must 'min' >= 0, 'max' >= 0, 'EME' >= 0");

                    base.Check();
                }
            }

            public class Interval: Element
            {
                public Interval(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public long        time       { get; protected set; } = -2;
                public long        Length     { get; protected set; } = -2;
                public Date?       date       { get; protected set; }
                public Difference? difference { get; protected set; }

                protected InnerIntervalElement? inner;

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    if (canonicalName == "once" || canonicalName == "--")
                    {
                        time = -1;
                    }
                    else
                    {
                        var timev = getTime(canonicalName);
                        if (timev <= -1)
                            throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the value '{block.Name}'. Acceptable is value similary 'once', '--' (once), '0' (continuesly), '1ms', '1s', '1' (seconds), '1m', '1h'");

                        time = timev;
                    }

                    inner  = new InnerIntervalElement(this, block.blocks, block);
                    if (inner.Length != null)
                        Length = inner.Length.Length;

                    date = inner.Date;
                    if (date == null || date.date == Date.DateValue.undefined)
                        this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options was not found a 'date' element or value of the element");

                    difference = inner.Difference;
                    if (difference == null || difference.differenceValue == Difference.DifferenceValue.undefined)
                        this.getRoot()!.warns.addWarning($"Warning: In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service options was not found a 'difference' element or value of the element");
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
                    if (time   == -2)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have element this represents time (for example: '1s')");
                    if (Length == -2)
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have element this represents length of data to input (for example: '32', '--', 'full')");

                    base.Check();
                }

                public class InnerIntervalElement: Element
                {
                    public InnerIntervalElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {}

                    public LengthElement? Length;
                    public Date?          Date;
                    public Difference?    Difference;
                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        switch (canonicalName)
                        {
                            case "length":
                                Length = new LengthElement(this, block.blocks, block); break;

                            case "date":
                                Date = new Date(this, block.blocks, block); break;

                            case "difference":
                                Difference = new Difference(this, block.blocks, block); break;

                            default:
                                throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the element '{block.Name}'. Acceptable is 'length', 'date', 'difference'");
                        }
                    }
                }

                public class Date : Element
                {
                    public Date(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {
                    }

                    public enum DateValue { undefined = 0, yes = 1, no = 2 };
                    public DateValue date;
                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        switch (canonicalName)
                        {
                            case "no":
                            case "-":
                                date = DateValue.no; break;

                            case "yes":
                            case "+":
                                date = DateValue.yes; break;

                            default:
                                throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the value '{block.Name}'. Acceptable is 'yes', '+', 'no', '-'");
                        }
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
