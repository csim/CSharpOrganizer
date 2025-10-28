using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace CSharply;

public static class Program
{
    public static int Main(string[] args)
    {
        //args = [@"C:\src\koalas\src\Koalas\Text\TextFieldSetItemBuilder.cs"];
        //args = [@"C:\src\koalas\src\Koalas\Text\"];
        //args = [@"C:\prose_wip\tformula\Transformation.Formula\"];
        // args =
        // [
        //     @"C:\prose_wip\tformula\Transformation.Formula\Semantics\Learning\Conditionals\PredicateFirst\Models\Cluster.cs",
        // ];

        // args =
        // [
        //     @"C:\prose_wip\tformula\Transformation.Formula\Semantics\Extensions\IProgramNodeBuilder.Extension.cs",
        // ];
        //args = [@"C:\src\CSharply\.scratch\test.cs"];
        //args = [@"C:\src\CSharply\.scratch\test.cs"];
        //args = [@"C:\src\CSharply\.scratch\test2.cs"];
        //_debug = true;

        string? verb = null;
        if (args.Length > 0)
        {
            string iverb = args[0].ToLowerInvariant();
            verb = !iverb.StartsWith('-') ? iverb : null;
        }

        bool version = args.Contains("--version");
        if (version)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version? versionNumber = assembly.GetName().Version;
            WriteLine($"CSharply v{versionNumber}");

            return 0;
        }

        if (verb == null)
        {
            DisplayHelp();

            return 0;
        }

        string[] verbArgs = args.Skip(1).ToArray();

        if (verb == "organize")
        {
            return Organize(verbArgs);
        }
        else
        {
            WriteLine($"Invalid verb: {verb}", ConsoleColor.Red);
            WriteLine();
            DisplayHelp();

            return 1;
        }
    }

    private static void DisplayHelp()
    {
        string help = """
            Usage:
              CSharply [command] [options]

            Options:
              --version          Show version information
              -?, -h, --help     Show help and usage information

            Commands:
              organize <directoryOrFile>   Organize C# files.
            """;

        WriteLine(help);
    }

    private static int Organize(string[] args)
    {
        bool help = args.Contains("--help") || args.Contains("-h") || args.Contains("-?");
        bool simulate = args.Contains("--simulate") || args.Contains("-s");
        bool verbose = args.Contains("--verbose") || args.Contains("-v");

        if (help)
        {
            OrganizeHelp();

            return 0;
        }

        string? path = args.ElementAtOrDefault(0);
        if (path == null)
        {
            WriteLine($"No path given.", ConsoleColor.Red);
            WriteLine();
            OrganizeHelp();

            return 1;
        }

        DirectoryInfo directory = new(path);
        FileInfo file = new(path);
        if (!directory.Exists && !file.Exists)
        {
            WriteLine($"Invalid path: {path}", ConsoleColor.Red);
            WriteLine();
            OrganizeHelp();

            return 1;
        }

        OrganizeOptions options = new(Threads: 3, Debug: simulate, Verbose: verbose);
        OrganizeService service = new(options);

        OrganizeResult result = service.Process(args[0]);
        TimeSpan duration = result.Duration;

        string durationContent =
            duration.TotalMilliseconds < 1_000 ? $"{duration.TotalMilliseconds:N0}ms"
            : duration.TotalSeconds < 60 ? $"{duration.TotalSeconds:N1}s"
            : $"{duration.TotalSeconds / 60d:N1} minutes";

        string successContent =
            $"Organized {result.SuccessFiles.Count:N0} files in {durationContent}.";
        string ignoreContent = $" {result.IgnoreFiles.Count:N0} files ignored";
        string failContent = $" {result.FailFiles.Count:N0} files failed.";

        Write(successContent);

        if (result.IgnoreFiles.Count > 0)
        {
            Write(ignoreContent);
        }

        if (result.FailFiles.Count > 0)
        {
            Write(failContent);
        }

        WriteLine();

        if (options.Verbose)
        {
            foreach (string filePath in result.SuccessFiles)
            {
                WriteLine($"organized : {filePath}");
            }

            foreach (string filePath in result.IgnoreFiles)
            {
                WriteLine($"ignored   : {filePath}");
            }

            foreach (string filePath in result.FailFiles)
            {
                WriteLine($"failed    : {filePath}");
            }
        }

        return 0;
    }

    private static void OrganizeHelp()
    {
        string help = """
            Description:
              Organize C# files

            Usage:
              CSharply organize <DirectoryOrFile> [options]

            Arguments:
              <DirectoryOrFile>        A directory or file to be organized. Only .cs files are considered.

            Options:
              -s --simulate            Display organized file content without saving files.
              -v --verbose             Display each file outcome.
              -?, -h, --help           Show help and usage information.
            """;

        WriteLine(help);
    }

    private static void Write(string content)
    {
        Console.Write(content);
    }

    private static void WriteLine(string? content = null, ConsoleColor? color = null)
    {
        if (color != null)
            Console.ForegroundColor = color.Value;

        Console.WriteLine(content);

        if (color != null)
            Console.ResetColor();
    }
}
