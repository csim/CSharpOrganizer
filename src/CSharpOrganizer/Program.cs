namespace CSharpOrganizer;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: csharp-organizer <file-path>");
            Console.WriteLine("Organizes C# code in the specified file.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  csharp-organizer MyClass.cs");
            Console.WriteLine("  csharp-organizer src/Services/UserService.cs");
            return 1;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File '{filePath}' not found.");
            return 1;
        }

        try
        {
            string fileContent = File.ReadAllText(filePath);
            string organizedContent = OrganizerService.OrganizeFile(fileContent);

            // Write the organized content back to the file
            File.WriteAllText(filePath, organizedContent);
            Console.WriteLine($"Successfully organized '{filePath}'");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing file: {ex.Message}");
            return 1;
        }
    }
}
