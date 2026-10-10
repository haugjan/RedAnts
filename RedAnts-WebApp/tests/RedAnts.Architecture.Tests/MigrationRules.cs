using Xunit;
using System.Text.RegularExpressions;

namespace RedAnts.Architecture.Tests;

public class MigrationRules
{
    private static readonly Regex Destructive = new(
        @"\bDROP\s+(TABLE|COLUMN|CONSTRAINT)\b|\bALTER\s+COLUMN\b|\bsp_rename\b|\bTRUNCATE\s+TABLE\b|\bDelete\.(Column|Table)\(|\bAlter\.Column\(|\bRename\.(Column|Table)\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ClassDeclaration = new(@"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> Allowed = RedAntsArchitecture.ReadBaseline("destructive-migrations.txt");

    private static IEnumerable<Finding> AllFindings => SchemaSources().SelectMany(FindingsIn);

    [Fact]
    public void Schema_migrations_only_add()
    {
        var offenders = AllFindings
            .Where(f => !Allowed.ContainsKey(f.Key))
            .Select(f => f.Describe())
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "Schema changes ship additively: migrations run before the deploy and the old code serves against the new schema until the slot swap. " +
            "Add the new column or table first, drop or change the old one in a later deploy. " +
            "A deliberate exception goes to destructive-migrations.txt as <File>:<MigrationClass> with the reason and the maintenance window:\n" +
            string.Join("\n", offenders));
    }

    [Fact]
    public void Destructive_migration_baseline_is_current()
    {
        var keys = AllFindings.Select(f => f.Key).ToHashSet();
        var stale = Allowed.Keys.Where(k => !keys.Contains(k)).Order().ToList();
        Assert.True(stale.Count == 0,
            "Remove steps that no longer contain a destructive statement from destructive-migrations.txt:\n" + string.Join("\n", stale));
    }

    private static IEnumerable<string> SchemaSources()
    {
        var src = Path.Combine(RedAntsArchitecture.RepoRoot(), "src");
        var infrastructure = $"{Path.DirectorySeparatorChar}Infrastructure{Path.DirectorySeparatorChar}";
        return Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !RedAntsArchitecture.IsBuildOutput(f))
            .Where(f => f.Contains(infrastructure))
            .Where(f => Path.GetFileName(f).Contains("Migration") || Path.GetFileName(f).Contains("Schema"))
            .Order();
    }

    private static IEnumerable<Finding> FindingsIn(string path)
    {
        var lines = File.ReadAllLines(path);
        var currentClass = "(top level)";
        for (var i = 0; i < lines.Length; i++)
        {
            var declaration = ClassDeclaration.Match(lines[i]);
            if (declaration.Success) currentClass = declaration.Groups[1].Value;
            if (Destructive.IsMatch(lines[i]))
                yield return new Finding(Path.GetFileName(path), currentClass, i + 1, lines[i].Trim());
        }
    }

    private sealed record Finding(string File, string Class, int Line, string Statement)
    {
        public string Key => $"{File}:{Class}";
        public string Describe() => $"{File}:{Line} ({Class}): {Statement}";
    }
}
