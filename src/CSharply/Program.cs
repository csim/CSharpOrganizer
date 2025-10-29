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
        else if (verb == "serve")
        {
            return await ServeAsync(verbArgs);
        }
        else if (verb == "daemon")
        {
            return await DaemonAsync(verbArgs);
        }
        else
        {
            WriteLine($"Invalid verb: {verb}", ConsoleColor.Red);
            WriteLine();
            DisplayHelp();

            return 1;
        }
    }

    private static async Task<int> DaemonAsync(string[] args)
    {
        bool help = args.Contains("--help") || args.Contains("-h") || args.Contains("-?");

        if (help)
        {
            DaemonHelp();
            return 0;
        }

        try
        {
            // string pipeName =
            //     args.FirstOrDefault(arg => arg.StartsWith("--pipe="))?[7..] ?? "CSharply";

            string pipeName = "csharply";
            WriteLine("Starting CSharply Daemon Service...", ConsoleColor.Green);

            using DaemonService daemonService = new(pipeName);

            // Handle Ctrl+C gracefully
            using CancellationTokenSource cts = new();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Start the daemon service in a background task
            Task daemonTask = daemonService.StartAsync();

            WriteLine("Daemon Service started successfully!", ConsoleColor.Green);
            WriteLine($"Listening on named pipe: {pipeName}", ConsoleColor.Cyan);
            WriteLine("Press Ctrl+C to stop the daemon.", ConsoleColor.Yellow);

            // Wait for cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                WriteLine("\nShutting down daemon service...", ConsoleColor.Yellow);
            }

            daemonService.Stop();
            WriteLine("Daemon service stopped.", ConsoleColor.Green);

            return 0;
        }
        catch (Exception ex)
        {
            WriteLine($"Error starting daemon service: {ex.Message}", ConsoleColor.Red);
            return 1;
        }
    }

    private static void DaemonHelp()
    {
        string help = """
            Start a daemon service for organizing C# code via named pipes.

            Usage:
              CSharply daemon [options]

            Options:
              --pipe=<name>            The named pipe name (default: CSharply)
              -?, -h, --help           Show help and usage information.

            Examples:
              CSharply daemon
              CSharply daemon --pipe=MyCSharplyPipe

            Usage with named pipes:
              echo "using System; class Test { }" | csharply-pipe CSharply
            """;

        WriteLine(help);
    }

    private static void DisplayHelp()
    {
        string help = """
            Usage:
              CSharply [command] [options]

            Commands:
              organize <directoryOrFile>    Organize C# files.
              serve                         Start web server to organize code via HTTP.
              daemon                        Start daemon service to organize code via named pipes.

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

    private static async Task<int> ServeAsync(string[] args)
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

            WriteLine("Starting CSharply Web Server...", ConsoleColor.Green);

            ServeService webServer = new(port);

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

    private static void ServeHelp()
    {
        string help = """
            Start a web server for organizing C# code via HTTP API.

            Usage:
              CSharply serve [options]

            Options:
              --port <port>            The port to listen on (default: 8147)
              -?, -h, --help           Show help and usage information.

            Examples:
              CSharply serve
              CSharply serve --port 8080

            API Endpoints:
              GET  /health             Health check
              POST /organize           Organize C# code (plain text body)
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
