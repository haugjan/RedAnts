using Assembly = System.Reflection.Assembly;
using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace RedAnts.Architecture.Tests;

public static class RedAntsArchitecture
{
    public static readonly Assembly Kernel = Assembly.Load("RedAnts.Kernel");
    public static readonly Assembly Ticketing = Assembly.Load("RedAnts.Ticketing");
    public static readonly Assembly DJ = Assembly.Load("RedAnts.DJ");
    public static readonly Assembly Host = Assembly.Load("RedAnts");

    public static readonly ArchUnitNET.Domain.Architecture Loaded = new ArchLoader()
        .LoadAssemblies(Kernel, Ticketing, DJ, Host)
        .Build();

    public static IEnumerable<IType> OwnTypes => Loaded.Types.Where(t =>
        t.Namespace.FullName.StartsWith("RedAnts.") && !t.Name.StartsWith('<'));

    public static IEnumerable<System.Type> ReflectedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.OfType<System.Type>();
        }
    }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RedAnts.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("RedAnts.slnx not found above " + AppContext.BaseDirectory);
    }

    public static bool IsBuildOutput(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}obj{sep}") || path.Contains($"{sep}bin{sep}");
    }

    public static Dictionary<string, string> ReadBaseline(string fileName) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, fileName))
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => l.Split(' ', 2, StringSplitOptions.TrimEntries))
            .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : "");
}
