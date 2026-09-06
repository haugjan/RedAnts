using Xunit;
using ArchUnitNET.Domain;

namespace RedAnts.Architecture.Tests;

public class NamingRules
{
    private static readonly string[] ForbiddenSuffixes =
        ["Service", "Manager", "Adapter", "Port", "Editor", "Store", "Impl", "Helper", "Util", "Utils"];

    private static readonly HashSet<string> Legacy = File.ReadAllLines(
            Path.Combine(AppContext.BaseDirectory, "legacy-names.txt"))
        .Select(l => l.Trim())
        .Where(l => l.Length > 0 && !l.StartsWith('#'))
        .ToHashSet();

    private static IEnumerable<IType> Candidates => RedAntsArchitecture.OwnTypes
        .Where(t => !t.Namespace.FullName.StartsWith("RedAnts.Domain"))
        .Where(t => ForbiddenSuffixes.Any(s => TrimInterfacePrefix(t.Name).EndsWith(s, StringComparison.Ordinal)));

    [Fact]
    public void New_types_say_what_they_do_not_what_they_are()
    {
        var offenders = Candidates.Select(t => t.FullName).Where(n => !Legacy.Contains(n)).Order().ToList();
        Assert.True(offenders.Count == 0,
            "Types outside Domain must not end with " + string.Join("/", ForbiddenSuffixes) +
            " (name the activity, e.g. PasswordGenerator, TierOfferResolver):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Legacy_list_only_contains_types_that_still_exist()
    {
        var current = Candidates.Select(t => t.FullName).ToHashSet();
        var stale = Legacy.Where(n => !current.Contains(n)).Order().ToList();
        Assert.True(stale.Count == 0, "Remove renamed or deleted types from legacy-names.txt:\n" + string.Join("\n", stale));
    }

    private static string TrimInterfacePrefix(string name) =>
        name.Length > 2 && name[0] == 'I' && char.IsUpper(name[1]) ? name[1..] : name;
}
