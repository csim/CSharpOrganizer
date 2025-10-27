using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace CSharpOrganizer;

public static class Program
{
    private static bool _debug;
    private static int _errorCount;
    private static int _successCount;

    public static int Main(string[] args)
    {
        _debug = args.Contains("--debug");
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
        //args = [@"C:\src\CSharpOrganizer\.scratch\test.cs"];
        //args = [@"C:\src\CSharpOrganizer\.scratch\test.cs"];
        //args = [@"C:\src\CSharpOrganizer\.scratch\test2.cs"];
        //_debug = true;

        if (args.Length == 0)
        {
            WriteLine("Usage: csharp-organizer <file-path-or-directory>");
            WriteLine("Organizes C# code in the specified file or all .cs files in a directory.");
            WriteLine();
            WriteLine("Examples:");
            WriteLine("  csharp-organizer MyClass.cs");
            WriteLine("  csharp-organizer src/Services/UserService.cs");
            WriteLine("  csharp-organizer src/");
            WriteLine("  csharp-organizer .");

            return 1;
        }

        string path = args[0];

        try
        {
            Stopwatch watch = Stopwatch.StartNew();
            int ret = 0;

            DirectoryInfo directory = new(path);

            if (directory.Exists)
            {
                ret = Process(directory);
            }

            FileInfo file = new(path);
            if (file.Exists)
            {
                ret = Process(file);
            }

            if (ret == 0)
            {
                string duration =
                    watch.ElapsedMilliseconds < 1_000 ? $"{watch.ElapsedMilliseconds:N0}ms"
                    : watch.Elapsed.TotalSeconds < 60 ? $"{watch.Elapsed.TotalSeconds:N1}s"
                    : $"{watch.Elapsed.TotalSeconds / 60d:N1} minutes";

                WriteLine($"organized {_successCount:N0} files, {duration}");

                return 0;
            }

            WriteLine($"NotFound: '{path}'", ConsoleColor.Red);

            return ret;
        }
        catch (Exception ex)
        {
            //Console.Error.WriteLine($"Error processing path: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static int Process(DirectoryInfo directory)
    {
        FileInfo[] files = directory.EnumerateFiles("*.cs", SearchOption.AllDirectories).ToArray();

        if (files.Length == 0)
        {
            WriteLine("No C# files found in the specified directory.");
            return 0;
        }

        foreach (FileInfo file in files)
        {
            int ret = Process(file);
        }

        return _errorCount > 0 ? 1 : 0;
    }

    private static int Process(FileInfo file)
    {
        if (!file.Extension.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        try
        {
            string fileContent = File.ReadAllText(file.FullName);
            string organizedContent = OrganizeService.OrganizeFile(fileContent);

            if (!_debug)
            {
                File.WriteAllText(file.FullName, organizedContent, Encoding.UTF8);
            }

            if (_debug)
            {
                WriteLine("====================");
                WriteLine($"{file}:");
                WriteLine("---");
                WriteLine(organizedContent);
                WriteLine("---");

                // Write("✓", ConsoleColor.Green);
                // WriteLine($" {filePath}");
            }

            _successCount++;

            return 0;
        }
        catch (Exception ex)
        {
            _errorCount++;

            Console.Error.WriteLine($"Error processing file: {file}");
            Console.Error.WriteLine(ex.ToString());

            return 1;
        }
    }

    // private static void Write(string content)
    // {
    //     Write(content, foregroundColor: null, backgroundColor: null);
    // }

    // private static void Write(string content, ConsoleColor? foregroundColor)
    // {
    //     Write(content, foregroundColor: foregroundColor, backgroundColor: null);
    // }

    // private static void Write(
    //     string? content,
    //     ConsoleColor? foregroundColor,
    //     ConsoleColor? backgroundColor
    // )
    // {
    //     if (foregroundColor != null)
    //         Console.ForegroundColor = foregroundColor.Value;
    //     if (backgroundColor != null)
    //         Console.BackgroundColor = backgroundColor.Value;

    //     Console.Write(content);

    //     if (foregroundColor != null || backgroundColor != null)
    //         Console.ResetColor();
    // }

    private static void WriteLine(string? content = null)
    {
        WriteLine(content, foregroundColor: null, backgroundColor: null);
    }

    private static void WriteLine(string? content, ConsoleColor? foregroundColor)
    {
        WriteLine(content, foregroundColor: foregroundColor, backgroundColor: null);
    }

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
