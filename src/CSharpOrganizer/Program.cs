namespace CSharpOrganizer;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: csharp-organizer <file-path-or-directory>");
            Console.WriteLine(
                "Organizes C# code in the specified file or all .cs files in a directory."
            );
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  csharp-organizer MyClass.cs");
            Console.WriteLine("  csharp-organizer src/Services/UserService.cs");
            Console.WriteLine("  csharp-organizer src/");
            Console.WriteLine("  csharp-organizer .");

            return 1;
        }

        string path = args[0];

        try
        {
            // Check if the path is a directory
            if (Directory.Exists(path))
            {
                return ProcessDirectory(path);
            }

            if (File.Exists(path))
            {
                return ProcessFile(path);
            }

            Console.Error.WriteLine($"Error: Path '{path}' not found.");
            return 1;
        }
        catch (Exception ex)
        {
            throw;
            //Console.Error.WriteLine($"Error processing path: {ex.Message}");
            //return 1;
        }
    }

    private static int ProcessFile(string filePath)
    {
        // Only process .cs files
        if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        try
        {
            string fileContent = File.ReadAllText(filePath);
            string organizedContent = OrganizerService.OrganizeFile(fileContent);

            // Write the organized content back to the file
            //File.WriteAllText(filePath, organizedContent);

            Console.WriteLine($"{filePath}:");
            Console.WriteLine(organizedContent);

            Console.WriteLine($"✓ Organized: {filePath}");

            return 0;
        }
        catch (Exception ex)
        {
            throw;
            //Console.Error.WriteLine($"Error processing file '{filePath}': {ex.Message}");
            //return 1;
        }
    }

    private static int ProcessDirectory(string directoryPath)
    {
        Console.WriteLine($"Processing directory: {directoryPath}");

        // Get all .cs files in the directory and subdirectories
        string[] csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

        if (csFiles.Length == 0)
        {
            Console.WriteLine("No C# files found in the specified directory.");
            return 0;
        }

        int successCount = 0;
        int errorCount = 0;

        foreach (string filePath in csFiles)
        {
            ProcessFile(filePath);
        }

        Console.WriteLine();
        Console.WriteLine(
            $"Processing complete: {successCount} files organized, {errorCount} errors."
        );

        return errorCount > 0 ? 1 : 0;
    }
}
