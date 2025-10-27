using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace CSharply;

public static class Program
{
    public static int Main(string[] args)
    {
        bool debug = args.Contains("--debug") || args.Contains("-d");
        bool verbose = args.Contains("--verbose") || args.Contains("-v");
        bool help = args.Contains("--help") || args.Contains("-h");
        string path = args[0];

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

        if (help || args.Length == 0)
        {
            DisplayHelp();

            return help ? 0 : 1;
        }

        try
        {
            Options options = new(Debug: debug, Verbose: verbose);
            OrganizeService service = new(options);

            OrganizeResult result = service.Process(args[0]);
            TimeSpan duration = result.Duration;

            string successPlural = result.SuccessCount == 1 ? string.Empty : "s";
            string successContent = $"organized {result.SuccessCount:N0} file{successPlural},";

            string failPlural = result.FailCount == 1 ? string.Empty : "s";
            string failContent = $"{result.FailCount:N0} failure{failPlural},";

            string durationContent =
                duration.TotalMilliseconds < 1_000 ? $"{duration.TotalMilliseconds:N0}ms"
                : duration.TotalSeconds < 60 ? $"{duration.TotalSeconds:N1}s"
                : $" {duration.TotalSeconds / 60d:N1} minutes";

            Write(successContent, foregroundColor: ConsoleColor.Green);

            if (result.FailCount > 0)
            {
                Write(failContent, foregroundColor: ConsoleColor.Red);
            }

            WriteLine($" {durationContent}");

            return 0;
        }
        catch (Exception ex)
        {
            //Console.Error.WriteLine($"Error processing path: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static void DisplayHelp()
    {
        WriteLine("Usage: csharply <file-path-or-directory>");
        WriteLine("Organizes C# code in the specified file or all .cs files in a directory.");
        WriteLine();
        WriteLine("Examples:");
        WriteLine("  csharply MyClass.cs");
        WriteLine("  csharply src/Services/UserService.cs");
        WriteLine("  csharply src/");
        WriteLine("  csharply .");
    }

    // private static void Write(string content)
    // {
    //     Write(content, foregroundColor: null, backgroundColor: null);
    // }

    private static void Write(string content, ConsoleColor? foregroundColor)
    {
        Write(content, foregroundColor: foregroundColor, backgroundColor: null);
    }

    private static void Write(
        string? content,
        ConsoleColor? foregroundColor,
        ConsoleColor? backgroundColor
    )
    {
        if (foregroundColor != null)
            Console.ForegroundColor = foregroundColor.Value;
        if (backgroundColor != null)
            Console.BackgroundColor = backgroundColor.Value;

        Console.Write(content);

        if (foregroundColor != null || backgroundColor != null)
            Console.ResetColor();
    }

    private static void WriteLine(string? content = null)
    {
        WriteLine(content, foregroundColor: null, backgroundColor: null);
    }

    // private static void WriteLine(string? content, ConsoleColor? foregroundColor)
    // {
    //     WriteLine(content, foregroundColor: foregroundColor, backgroundColor: null);
    // }

    private static void WriteLine(
        string? content,
        ConsoleColor? foregroundColor,
        ConsoleColor? backgroundColor
    )
    {
        if (foregroundColor != null)
            Console.ForegroundColor = foregroundColor.Value;
        if (backgroundColor != null)
            Console.BackgroundColor = backgroundColor.Value;

        Console.WriteLine(content);

        if (foregroundColor != null || backgroundColor != null)
            Console.ResetColor();
    }
}
