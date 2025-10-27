using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharply;

public partial class OrganizeService(Options options)
{
    private int _failCount;
    private readonly object _lock = new();
    private int _successCount;

    public OrganizeResult Process(string path)
    {
        Stopwatch watch = Stopwatch.StartNew();
        DirectoryInfo directory = new(path);

        if (directory.Exists)
        {
            Process(directory);
        }
        else
        {
            FileInfo file = new(path);
            if (file.Exists)
            {
                Process(file);
            }
        }

        watch.Stop();

        return new OrganizeResult(
            SuccessCount: _successCount,
            FailCount: _failCount,
            Duration: watch.Elapsed
        );
    }

    private void Process(DirectoryInfo directory)
    {
        FileInfo[] files = directory.EnumerateFiles("*.cs", SearchOption.AllDirectories).ToArray();

        foreach (FileInfo file in files)
        {
            Process(file);
        }
    }

    private void Process(FileInfo file)
    {
        if (!file.Extension.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            string fileContent = File.ReadAllText(file.FullName);
            string organizedContent = OrganizeCode(fileContent);

            if (!options.Debug)
            {
                File.WriteAllText(file.FullName, organizedContent, Encoding.UTF8);
            }

            if (options.Debug)
            {
                Console.WriteLine("====================");
                Console.WriteLine($"{file}:");
                Console.WriteLine(organizedContent);
                Console.Error.WriteLine("-----");
            }

            lock (_lock)
            {
                _successCount++;
            }
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _failCount++;
            }

            if (options.Verbose)
            {
                Console.Error.WriteLine("-----");
                Console.Error.WriteLine(ex.ToString());
                Console.Error.WriteLine("-----");

                throw;
            }
        }
    }
}

public record Options(bool Verbose, bool Debug);

public record OrganizeResult(int SuccessCount, int FailCount, TimeSpan Duration);
