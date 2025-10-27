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

public partial class OrganizeService(Options options)
{
    private readonly List<string> _failFiles = [];
    private readonly List<string> _skipFiles = [];
    private readonly List<string> _successFiles = [];

    public OrganizeService()
        : this(new Options()) { }

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
            SkipFiles: _skipFiles,
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

    private void AddSkipFile(FileInfo file)
    {
        lock (_skipFiles)
        {
            _skipFiles.Add(file.FullName);
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
        if (!file.Extension.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            AddSkipFile(file);

            return;
        }

        if (
            file.Name.Contains("Utils.cs")
            || file.Name == "DateTimeFormat.cs"
            || file.Name == "Heuristics.cs"
            || file.Name == "TypeResolver.cs"
            || file.Name == "BenchmarkCase.cs"
            || file.Name == "SupportedExcelFeatures.cs"
            || file.Name == "TableFuzzer.cs"
        )
        {
            AddSkipFile(file);

            return;
        }

        try
        {
            string fileContent = File.ReadAllText(file.FullName);

            if (
                fileContent.Contains("#if")
                || fileContent.Contains("#endif")
                || fileContent.Contains("#nullable")
            )
            {
                AddSkipFile(file);

                return;
            }

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

public record Options(int Threads = 1, bool Verbose = false, bool Debug = false);

public record OrganizeResult(
    IReadOnlyList<string> SuccessFiles,
    IReadOnlyList<string> FailFiles,
    IReadOnlyList<string> SkipFiles,
    TimeSpan Duration
);
