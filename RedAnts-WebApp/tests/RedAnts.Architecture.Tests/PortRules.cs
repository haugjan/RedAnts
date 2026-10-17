using Xunit;
using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace RedAnts.Architecture.Tests;

public class PortRules
{
    private static readonly Regex ModuleNamespace = new(@"^RedAnts\.(Ticketing|Show)\.");
    private static readonly Regex CapabilityNamespace = new(@"^RedAnts\.(Ticketing|Show)\.Features\.[A-Za-z]+$");
    private static readonly Regex InfrastructureNamespace = new(@"\.Infrastructure(\.|$)");
    private static readonly Regex RepositoryName = new(@"^I[A-Z][A-Za-z]*Repository$");
    private static readonly string[] AllowedToUseInfrastructure = ["Composer", "Extensions", "Features"];

    private static readonly System.Type[] CollectionShapes =
        [typeof(IReadOnlyList<>), typeof(IEnumerable<>), typeof(IList<>), typeof(List<>), typeof(ICollection<>), typeof(IReadOnlyCollection<>)];

    private static readonly Dictionary<string, string> CollectionBaseline = RedAntsArchitecture.ReadBaseline("repository-collection-methods.txt");

    private static IEnumerable<string> RepositoryCollectionMethods => new[] { RedAntsArchitecture.Ticketing, RedAntsArchitecture.Show }
        .SelectMany(RedAntsArchitecture.ReflectedTypes)
        .Where(t => t.IsInterface && RepositoryName.IsMatch(t.Name) && ModuleNamespace.IsMatch(t.Namespace ?? ""))
        .SelectMany(t => t.GetMethods()
            .Where(m => ReturnsCollection(m.ReturnType))
            .Select(m => $"{t.FullName}.{m.Name}"));

    [Fact]
    public void Ports_live_directly_in_their_capability()
    {
        var offenders = RedAntsArchitecture.OwnTypes
            .Where(t => t is Interface && ModuleNamespace.IsMatch(t.Namespace.FullName) && !t.FullName.Contains('+'))
            .Where(t => !CapabilityNamespace.IsMatch(t.Namespace.FullName))
            .Select(t => t.FullName)
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "Ports are interfaces directly in RedAnts.<Module>.Features.<Capability>, never in Admin, Infrastructure or Shared:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Only_composers_and_registrations_reach_into_infrastructure()
    {
        var offenders = RedAntsArchitecture.OwnTypes
            .Where(t => !InfrastructureNamespace.IsMatch(t.Namespace?.FullName ?? ""))
            .Where(t => !AllowedToUseInfrastructure.Any(s => t.Name.EndsWith(s, StringComparison.Ordinal)))
            .SelectMany(t => t.Dependencies
                .Select(d => d.Target)
                .Where(target => (target.Namespace?.FullName ?? "").StartsWith("RedAnts.") && InfrastructureNamespace.IsMatch(target.Namespace?.FullName ?? ""))
                .Select(target => $"{t.FullName} -> {target.FullName}"))
            .Distinct()
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "Only types named *Composer, *Extensions or *Features may depend on an Infrastructure namespace from outside Infrastructure:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Repositories_expose_no_collections()
    {
        var offenders = RepositoryCollectionMethods
            .Where(m => !CollectionBaseline.ContainsKey(m))
            .Distinct()
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "I…Repository ports load, save and delete one aggregate; lists, searches and reports belong to an I…Reader:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Repository_collection_baseline_only_lists_methods_that_still_exist()
    {
        var current = RepositoryCollectionMethods.ToHashSet();
        var stale = CollectionBaseline.Keys.Where(m => !current.Contains(m)).Order().ToList();
        Assert.True(stale.Count == 0, "Remove moved or deleted methods from repository-collection-methods.txt:\n" + string.Join("\n", stale));
    }

    private static bool ReturnsCollection(System.Type returnType)
    {
        var unwrapped = returnType.IsGenericType && returnType.GetGenericTypeDefinition() is var definition
                        && (definition == typeof(Task<>) || definition == typeof(ValueTask<>))
            ? returnType.GetGenericArguments()[0]
            : returnType;
        return unwrapped.IsArray
            || (unwrapped.IsGenericType && CollectionShapes.Contains(unwrapped.GetGenericTypeDefinition()));
    }
}
