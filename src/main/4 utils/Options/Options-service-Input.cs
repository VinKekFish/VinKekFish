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

            public abstract class InputElement: Element
            {
                public string? PathString {get; protected set;}

                public InputElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    throw new Options_Service_Exception($"Fatal error: InputElemement must not be called");
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

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    PathString = block.Name;
                }

                public override void Check()
                {
                    if (PathString == "")
                        throw new Options_Service_Exception($"In the '{getFullElementName()}' element (at line {1+this.thisBlock.startLine}) of service option must have 'PathString' element. Have no 'PathString' element");

                    base.Check();
                }
            }

            /// <summary>Представляет источник энтропии, являющийся файлом (или совместимым с ним устройством)</summary>
            public class InputCmdElement: InputElement
            {
                public InputCmdElement(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    PathString = block.Name;
                }

                public override void Check()
                {
                    base.Check();
                }
            }

            public class EntropyValues
            {
                public long min, max, EME;
            }

            public class Intervals: Element
            {
                protected List<Interval> intervals = new List<Interval>();

                public readonly EntropyValues entropy = new EntropyValues();

                public Intervals(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {}

                protected SortedList<string, long> TimeFactors = new SortedList<string, long>(4) { {"ms", 1}, {"s", 1000}, {"m", 60*1000}, {"h", 60*60*1000} };
                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    if (canonicalName == "min")
                        setMin(block);
                    if (canonicalName == "max")
                        setMax(block);
                    if (canonicalName == "EME")
                        setEME(block);

                    if (canonicalName == "once" || canonicalName == "--")
                    {
                        intervals.Add( new Interval(this, block.blocks, block) {time = -1} );
                        return;
                    }

                    var time = getTime(canonicalName);
                    if (time <= 0)
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element found the value '{block.Name}'. Acceptable is value similary 'once', '--' (once), '0' (continuesly), '1ms', '1s', '1' (seconds), '1m', '1h'");

                    intervals.Add( new Interval(this, block.blocks, block) {time = time} );
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
                        if (entropy.max <= 0)
                            throw new Exception();
                    }
                    catch
                    {
                        throw new Options_Service_Exception($"At line {1+block.startLine} in the '{getFullElementName()}' element 'max' found. Acceptable value is positive integer (example: 1, 2, 3)");
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
            }

            public class Interval: Element
            {
                public Interval(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                {
                    Length = -2;
                    time   = -2;
                }

                public required long        time       { get; init; }
                public          long        Length     { get; protected set; }
                public          Date?       date       { get; protected set; }
                public          Difference? difference { get; protected set; }

                public override void SelectBlock(Options.Block block, string canonicalName)
                {
                    throw new NotImplementedException();
                }

                public class Date : Element
                {
                    public Date(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {
                    }

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        throw new NotImplementedException();
                    }
                }

                public class Difference : Element
                {
                    public Difference(Element? parent, List<Options.Block> blocks, Options.Block thisBlock) : base(parent, blocks, thisBlock)
                    {
                    }

                    public override void SelectBlock(Options.Block block, string canonicalName)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
