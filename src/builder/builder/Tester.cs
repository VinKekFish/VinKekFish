using System.Diagnostics;

namespace builder;

partial class Program
{
    public static ErrorCode MainTests(string testsTags)
    {
        var cd  = Directory.GetCurrentDirectory();

        var args = $"\"{testsTags}\"";
        var psi  = new ProcessStartInfo("dotnet",  $"./build/tests.dll {args}");
        psi.WorkingDirectory = cd;

        // Console.WriteLine(args);

        var ps  = Process.Start(psi);
        if (ps == null)
            return ErrorCode.DotnetError;

        ps.WaitForExit();
        if (ps.ExitCode != 0)
            return ErrorCode.TestError;

        return ErrorCode.Success;
    }
}
