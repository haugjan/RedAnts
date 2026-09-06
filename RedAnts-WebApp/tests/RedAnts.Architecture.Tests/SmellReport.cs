using Xunit;
using Xunit.Abstractions;

namespace RedAnts.Architecture.Tests;

public class SmellReport(ITestOutputHelper output)
{
    private const int LongFile = 300;
    private const int ManyUsings = 14;
    private const int ManyInjections = 7;

    [Fact]
    public void Report_large_files_many_usings_and_wide_dependencies()
    {
        var root = RepoRoot();
        var src = Path.Combine(root, "src");
        var findings = Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".razor") || f.EndsWith(".cshtml"))
            .Where(f => !IsBuildOutput(f))
            .Select(f => Inspect(root, f))
            .Where(s => s.Smells.Count > 0)
            .OrderByDescending(s => s.Lines)
            .ToList();

        output.WriteLine($"{findings.Count} files with smells (informational, never fails):");
        foreach (var s in findings)
            output.WriteLine($"  {s.Path}: {string.Join(", ", s.Smells)}");
    }

    private static bool IsBuildOutput(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}obj{sep}") || path.Contains($"{sep}bin{sep}");
    }

    private static FileSmells Inspect(string root, string path)
    {
        var lines = File.ReadAllLines(path);
        var usings = lines.Count(l => l.StartsWith("using ") || l.StartsWith("@using "));
        var injections = lines.Count(l => l.TrimStart().StartsWith("@inject "));
        var smells = new List<string>();
        if (lines.Length > LongFile) smells.Add($"{lines.Length} lines");
        if (usings > ManyUsings) smells.Add($"{usings} usings");
        if (injections > ManyInjections) smells.Add($"{injections} injections");
        return new FileSmells(Path.GetRelativePath(root, path), lines.Length, smells);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RedAnts.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("RedAnts.slnx not found above " + AppContext.BaseDirectory);
    }

    private sealed record FileSmells(string Path, int Lines, List<string> Smells);
}
