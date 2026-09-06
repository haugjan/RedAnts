using Assembly = System.Reflection.Assembly;
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
}
