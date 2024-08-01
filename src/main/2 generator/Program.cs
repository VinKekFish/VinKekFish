#pragma warning disable CA1806 // Main создает экземпляр ThreeFish_Gen, который не используется. Передайте этот экземпляр в качестве аргумента другому методу, присвойте экземпляр переменной, или удалите создание объекта, если он не нужен. [generator]
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
