using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace CSharply;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        bool version = args.Contains("--version");
        if (version)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version? versionNumber = assembly.GetName().Version;
            WriteLine($"CSharply v{versionNumber}");

            return 0;
        }

        string? verb = null;
        if (args.Length > 0)
        {
            string iverb = args[0].ToLowerInvariant();
            verb = !iverb.StartsWith('-') ? iverb : null;
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
        else if (verb == "server")
        {
            return await ServerAsync(verbArgs);
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

            Commands:
              organize <directoryOrFile>    Organize C# files.
              server                        Start web server to organize code via HTTP.

            Options:
              --version          Show version information
              -?, -h, --help     Show help and usage information
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

        string plural = result.SuccessFiles.Count == 1 ? string.Empty : "s";
        string successContent =
            $"Organized {result.SuccessFiles.Count:N0} file{plural} in {durationContent}";

        plural = result.IgnoreFiles.Count == 1 ? string.Empty : "s";
        string ignoreContent = $" {result.IgnoreFiles.Count:N0} file{plural} ignored";

        plural = result.FailFiles.Count == 1 ? string.Empty : "s";
        string failContent = $" {result.FailFiles.Count:N0} file{plural} failed.";

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
              -v --verbose             Display outcome for each file.
              -?, -h, --help           Show help and usage information.
            """;

        WriteLine(help);
    }

    private static int ParseIntArg(string[] args, string argName, int defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == argName && int.TryParse(args[i + 1], out int parsedValue))
            {
                return parsedValue;
            }
        }

        return defaultValue;
    }

    private static void ServeHelp()
    {
        string help = """
            Start a web server for organizing C# code via HTTP API.

            Usage:
              CSharply server [options]

            Options:
              --port <port>            The port to listen on (default: 8149)
              -?, -h, --help           Show help and usage information.

            Examples:
              CSharply server
              CSharply server --port 8080

            API Endpoints:
              GET  /health             Health check
              POST /organize           Organize C# code (plain text body)
            """;

        WriteLine(help);
    }

    private static async Task<int> ServerAsync(string[] args)
    {
        bool help = args.Contains("--help") || args.Contains("-h") || args.Contains("-?");

        if (help)
        {
            ServeHelp();
            return 0;
        }

        try
        {
            int port = ParseIntArg(args, "--port", 8149);

            WriteLine("Starting CSharply Server...", ConsoleColor.Green);

            ServerService webServer = new(port);

            WriteLine("Web Server started successfully!", ConsoleColor.Green);
            WriteLine($"Available at: http://localhost:{port}", ConsoleColor.Cyan);
            WriteLine("Press Ctrl+C to stop the server.", ConsoleColor.Yellow);

            await webServer.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            WriteLine($"Error starting web server: {ex.Message}", ConsoleColor.Red);
            return 1;
        }
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
