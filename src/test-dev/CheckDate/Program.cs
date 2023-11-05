// dotnet publish --output ./build.dev -c Release --self-contained false --use-current-runtime true /p:PublishSingleFile=true
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.IO;

namespace ConsoleTest;
unsafe class Program
{
    static void Main(string[] args)
    {
        var fi = new FileInfo("/inRamA/log.log"); fi.Refresh();
        if (!fi.Exists)
        {
            Console.Error.WriteLine("File not found: " + fi.FullName);
            return;
        }

        var bt = new byte[8];
        using (var fs = fi.OpenRead())
        {
            while (true)
            {
                var readed = fs.Read(bt, 0, bt.Length);
                if (readed <= 0)
                    break;

                long ticks = 0;
                for (int i = 7; i >= 0; i--)
                {
                    ticks <<= 8;
                    ticks  |= bt[i];
                }

                var dt = new DateTime(ticks);

                Console.WriteLine(dt.ToLongTimeString() + "." + dt.Millisecond + "." + dt.Microsecond);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Нажмите любую клавишу...");
        Console.ReadLine();
    }
}
