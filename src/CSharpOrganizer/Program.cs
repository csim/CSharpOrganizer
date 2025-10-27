using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace CSharpOrganizer;

public static class Program
{
    private static bool _debug;

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

            if (Directory.Exists(path))
            {
                ret = ProcessDirectory(path);
            }

            if (File.Exists(path))
            {
                ret = ProcessFile(path);
            }

            if (ret == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                WriteLine($"{watch.ElapsedMilliseconds:N0}ms");
                Console.ResetColor();

                return 0;
            }

            Console.Error.WriteLine($"Error: Path '{path}' not found.");

            return ret;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing path: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static int ProcessDirectory(string directoryPath)
    {
        WriteLine($"Processing directory: {directoryPath}");

        string[] csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

        if (csFiles.Length == 0)
        {
            WriteLine("No C# files found in the specified directory.");
            return 0;
        }

        int successCount = 0;
        int errorCount = 0;

        foreach (string filePath in csFiles)
        {
            int ret = ProcessFile(filePath);
            if (ret == 0)
                successCount++;
            else
                errorCount++;
        }

        WriteLine();
        WriteLine($"Processing complete: {successCount} files organized, {errorCount} errors.");

        return errorCount > 0 ? 1 : 0;
    }

    private static int ProcessFile(string filePath)
    {
        // Only process .cs files
        if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        try
        {
            string fileContent = File.ReadAllText(filePath);
            string organizedContent = OrganizeService.OrganizeFile(fileContent);

            if (!_debug)
            {
                File.WriteAllText(filePath, organizedContent, Encoding.UTF8);
            }

            if (_debug)
            {
                WriteLine("====================");
                WriteLine($"{filePath}:");
                WriteLine("---");
                WriteLine(organizedContent);
                WriteLine("---");

                // Write("✓", ConsoleColor.Green);
                // WriteLine($" {filePath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing file '{filePath}': {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static void Write(string content)
    {
        Write(content, foregroundColor: null, backgroundColor: null);
    }

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
