using static VinKekFish_console.Program;

namespace VinKekFish_console;

public partial class Program
{
    /// <summary>true - режим работы программы, где клиент - другая вызывающая программа</summary>
    public static bool isAutomaticProgram = false;
    public static void command_auto(string[] args)
    {
        isAutomaticProgram = true;
/*
        var argsList = new List<string>(args);
        argsList.RemoveAt(0);

        var command = new gen_command(argsList);

        return command.exec();*/
    }

    public static bool is_command_auto(string[] args)
    {
        if (args[0] == "auto")
            return true;

        return false;
    }
}

public class gen_command
{
    public readonly List<string> argsList;
    public readonly bool         quilet = false;
    public gen_command(List<string> argsList)
    {
        this.argsList = argsList;

        var cnt = int.MaxValue;
        while (argsList.Count != cnt && argsList.Count > 0)
        {
            cnt = argsList.Count;

            var f = argsList.Count == 0 ? "" : argsList[0];
            if (f == "help" || f == "h" || f == "hlp" || f == "?" || f == "??")
            {
                PrintHelp();
                argsList.RemoveAt(0);
                continue;
            }

            if (f == "quilet" || f == "q" || f == "silent")
            {
                quilet = true;
                argsList.RemoveAt(0);
                continue;
            }
        }

        if (argsList.Count > 0)
            throw new ArgumentException("Incorrect args for the 'gen' command");

        argsList.Clear();
    }

    public void PrintHelp()
    {
        Console.WriteLine("quilet mode:");
        Console.WriteLine("vkf gen quilet");
        Console.WriteLine("vkf gen q");
        Console.WriteLine("vkf gen silent");
        Console.WriteLine();
    }

    public void Help_0()
    {
        if (quilet)
            return;

        Console.WriteLine("0 level commands");
        PrintHelp(commands0);
    }

    public void PrintHelp(List<HelpMessages> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            Console.WriteLine($"{i}. {cmd.Name}");
            Console.WriteLine($"{cmd.BaseHelp}");
            Console.WriteLine();
        }
    }

    public class HelpMessages
    {
        public string Name     = "Error. CommandName empty - send message for developer, please";
        public string BaseHelp = "";
    }

    public static List<HelpMessages> commands0 = new List<HelpMessages>
    {
        // 0
        new HelpMessages()
        {
            Name     = "exit",
            BaseHelp = "Will exit from program"
        },
        // 1
        new HelpMessages()
        {
            Name     = "set file",
            BaseHelp = "Will do set a randomization commands file.\nThis is an option for the randomization."
        },
    };

    private void PrintExitedByClosed()
    {
        if (quilet)
            return;

        Console.WriteLine("exited by the input stream closed");
    }

    private void PrintEnter()
    {
        if (quilet)
            return;

        Console.WriteLine();
        Console.WriteLine("Enter the command:");
    }

    public ProgramErrorCode exec()
    {
        Help_0();

        string? line;
        do
        {
            PrintEnter();

            line = Console.ReadLine();
            if (line == null)
            {
                PrintExitedByClosed();
                break;
            }

            if (line.Trim().Length <= 0)
            {
                Console.Clear();
                Help_0();
            }
        }
        while (line.ToLowerInvariant().Trim() != "exit" && line != "0");

        return ProgramErrorCode.success;
    }
}
