// TODO: tests
using static VinKekFish_console.Program;

namespace VinKekFish_console;

using System;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using VinKekFish_EXE;
using VinKekFish_Utils.console;
using static VinKekFish_Utils.Language;

public partial class Program
{
    public static ProgramErrorCode DoCommand_install(string[] args)
    {
        _ = new Command_install();

        return ProgramErrorCode.success;
    }

    public static bool Is_command_install(string[] args)
    {
        if (args[0].ToLowerInvariant().Trim() == "install")
            return true;

        return false;
    }

    protected class Command_install
    {
        public readonly DirectoryInfo dir;
        public readonly DirectoryInfo dirExe;
        public readonly DirectoryInfo dirData;
        public readonly DirectoryInfo dirOpts;

        public Command_install()
        {
            dir     = new DirectoryInfo(  Directory.GetCurrentDirectory()  );
            dirExe  = new DirectoryInfo("exe");
            dirData = new DirectoryInfo("data");
            dirOpts = new DirectoryInfo("options");

            ProcessOptionsFile();
            ProcessServiceFile();
        }

        protected void ProcessServiceFile()
        {
            var files = dirExe.GetFiles("*.service", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fi = new FileInfo(Path.Combine(dirOpts.FullName, file.Name)); fi.Refresh();
                if (fi.Exists)
                {
                    using (var console = new YellowTextConsole())
                        Console.Write($"{L("The .service file")} '{fi.FullName}' {L("is exists. Will not replaced. Check this is correct manually")}.");

                    continue;
                }

                using (var fs = file.OpenText())
                {
                    var text = fs.ReadToEnd();
                        text = text.Replace("$$$VkfDir", dir.FullName);

                    using (var fws = fi.OpenWrite())
                    {
                        var txt = new UTF8Encoding().GetBytes(text);
                        fws.Write(txt);
                    }
                }
            }

            var vkfo = new FileInfo(Path.Combine(dirOpts.FullName, "vkf.service")); vkfo.Refresh();
            var vkft = new FileInfo($"/lib/systemd/system/{vkfo.Name}"); vkft.Refresh();

            if (vkft.Exists)
                vkft.Delete();

            // vkfo.CopyTo(vkft.FullName);
            vkft.CreateAsSymbolicLink(vkfo.FullName);

            var proc = Process.Start("systemctl", $"daemon-reload");
            proc.WaitForExit();
            proc.Dispose();

                proc = Process.Start("systemctl", $"enable {vkft.Name}");
            proc.WaitForExit();
            proc.Dispose();

            Console.WriteLine(L("'daemon-reload' and 'systemctl enable vkf' executed"));
        }

        protected void ProcessOptionsFile()
        {
            var files = dirExe.GetFiles("*.options", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fi = new FileInfo(Path.Combine(dirOpts.FullName, file.Name)); fi.Refresh();
                if (fi.Exists)
                {
                    /*
                    fi.MoveTo(fi.FullName + $".copy.{fi.LastWriteTime.Ticks}");
                    using (var console = new RedTextConsole())
                        Console.Write($"The option file '{fi.FullName}' will replaced");

                    fi.Refresh();
                    */
                    using (var console = new YellowTextConsole())
                        Console.Write($"{L("The option file")} '{fi.FullName}' {L("is exists. Will not replaced. Check this is correct manually")}.");

                    continue;
                }

                // file.CopyTo(fi.FullName);
                using (var fs = file.OpenText())
                {
                    var text = fs.ReadToEnd();
                        text = text.Replace("$$$VkfDir", dir.FullName);

                    using (var fws = fi.OpenWrite())
                    {
                        var txt = new UTF8Encoding().GetBytes(text);
                        fws.Write(txt);
                    }
                }
            }
        }
    }
}
