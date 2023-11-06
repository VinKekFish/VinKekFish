// dotnet publish --output ./build.dev -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.IO;

// Программа подсчитывает количество повторений каждого конкретного значения байта,
// а также количество повторений конкретной частоты
// Для дальнейшего построения диаграммы в OpenOffice Calc (или LibreOffice Calc)
namespace ConsoleTest;
unsafe class Program
{
    static void Main(string[] args)
    {
        var fileName = "/inRamA/stat.bin";
        if (args.Length > 0)
            fileName = args[0];

        var fi = new FileInfo(fileName); fi.Refresh();
        if (!fi.Exists)
        {
            Console.Error.WriteLine("File not found: " + fi.FullName);
            return;
        }

        var bt = new byte[fi.Length];
        using (var fs = fi.OpenRead())
        {
            fs.Read(bt, 0, bt.Length);
        }

        var stats  = new nint[256];
        var stats2 = new nint[bt.Length];
        foreach (var value in bt)
        {
            stats[value]++;
        }

        // Если значение value было повторено k раз
        // В массиве stats2[k] будет указано, какое количество разных значений value повторено k раз или менее
        bool isFound = false;
        for (int k = 0; k < stats2.Length; k++)
        {
            var cnt = 0;
            for (int val = 0; val < 256; val++)
            {
                if (stats[val] == k)
                {
                    stats2[k]++;
                    cnt++;
                }
            }

            if (cnt == 0)
            {
                if (isFound)
                    break;
            }
            else
            if (cnt > fi.Length >> 9)
                isFound = true;
        }

        var sb = new StringBuilder();
        for (int k = 0; k < 256; k++)
        {
            sb.AppendLine(stats[k].ToString() + "\t\t" + stats2[k].ToString());
        }

        nint max = 0, min = nint.MaxValue;
        foreach (var value in stats)
        {
            if (max < value)
                max = value;
            if (min > value)
                min = value;
        }

        File.WriteAllText("/inRamA/stat.csv", sb.ToString());
        Console.WriteLine($"min {min}, max {max}; ideal: {bt.Length >> 8}");
    }
}
