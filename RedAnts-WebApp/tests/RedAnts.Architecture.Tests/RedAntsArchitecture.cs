using Assembly = System.Reflection.Assembly;
using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace RedAnts.Architecture.Tests;

public static class RedAntsArchitecture
{
    public static readonly Assembly Kernel = Assembly.Load("RedAnts.Kernel");
    public static readonly Assembly Ticketing = Assembly.Load("RedAnts.Ticketing");
    public static readonly Assembly Show = Assembly.Load("RedAnts.Show");
    public static readonly Assembly Host = Assembly.Load("RedAnts");

    public static readonly ArchUnitNET.Domain.Architecture Loaded = new ArchLoader()
        .LoadAssemblies(Kernel, Ticketing, Show, Host)
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

    public static Dictionary<string, string> ReadBaseline(string fileName) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, fileName))
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => l.Split(' ', 2, StringSplitOptions.TrimEntries))
            .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : "");
}
