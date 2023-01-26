namespace CodeGenerator;

public class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
            new ThreeFish_Gen(args[0]);
        else
            new ThreeFish_Gen();

        if (args.Length > 1)
            new ThreeFish_Gen2(args[1]);
    }
}
