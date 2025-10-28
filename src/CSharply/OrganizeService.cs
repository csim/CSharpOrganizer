using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharply;

public partial class OrganizeService(IgnoreFileService ignoreService, OrganizeOptions options)
{
    private readonly List<string> _failFiles = [];
    private readonly List<string> _ignoreFiles = [];
    private readonly List<string> _successFiles = [];

    public OrganizeService()
        : this(new IgnoreFileService(), new OrganizeOptions()) { }

    public OrganizeService(OrganizeOptions options)
        : this(new IgnoreFileService(), options) { }

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
            SuccessFiles: _successFiles,
            FailFiles: _failFiles,
            IgnoreFiles: _ignoreFiles,
            Duration: watch.Elapsed
        );
    }

    private void AddFailFile(FileInfo file)
    {
        lock (_failFiles)
        {
            _failFiles.Add(file.FullName);
        }
    }

    private void AddIgnoreFile(FileInfo file)
    {
        lock (_ignoreFiles)
        {
            _ignoreFiles.Add(file.FullName);
        }
    }

    private void AddSuccessFile(FileInfo file)
    {
        lock (_successFiles)
        {
            _successFiles.Add(file.FullName);
        }
    }

    private void Process(DirectoryInfo directory)
    {
        List<FileInfo> files = directory
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .ToList();

        if (options.Debug || files.Count < 10)
        {
            Process(files);

            return;
        }

        Parallel.ForEach(
            files,
            new ParallelOptions { MaxDegreeOfParallelism = options.Threads },
            Process
        );
    }

    private void Process(IReadOnlyList<FileInfo> files)
    {
        foreach (FileInfo file in files)
        {
            Process(file);
        }
    }

    private void Process(FileInfo file)
    {
        if (ignoreService.Ignore(file))
        {
            AddIgnoreFile(file);

            return;
        }

        if (!file.Extension.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            AddIgnoreFile(file);

            return;
        }

        try
        {
            string fileContent = File.ReadAllText(file.FullName);
            string organizedContent = OrganizeCode(fileContent);

            if (!options.Debug)
            {
                if (!fileContent.Equals(organizedContent, StringComparison.Ordinal))
                {
                    File.WriteAllText(file.FullName, organizedContent, Encoding.UTF8);
                }
            }

            if (options.Debug)
            {
                Console.WriteLine("====================");
                Console.WriteLine($"{file}:");
                Console.WriteLine(organizedContent);
                Console.Error.WriteLine("-----");
            }

            AddSuccessFile(file);
        }
        catch (Exception ex)
        {
            AddFailFile(file);

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

public record OrganizeOptions(int Threads = 1, bool Verbose = false, bool Debug = false);

public record OrganizeResult(
    IReadOnlyList<string> SuccessFiles,
    IReadOnlyList<string> FailFiles,
    IReadOnlyList<string> IgnoreFiles,
    TimeSpan Duration
);
