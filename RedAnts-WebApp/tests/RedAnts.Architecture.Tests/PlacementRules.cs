using Xunit;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RedAnts.Architecture.Tests;

public class PlacementRules
{
    private static readonly Regex CapabilityNamespace =
        new(@"^RedAnts\.(?<module>Ticketing|Show)\.Features\.(?<capability>[A-Za-z]+)(\.|$)");

    private static readonly Regex InfrastructureNamespace = new(@"\.Infrastructure(\.|$)");
    private static readonly Regex DomainNamespace = new(@"^RedAnts\.((Ticketing|Show)\.)?Domain(\.|$)");
    private static readonly Regex RepositoryName = new(@"^I[A-Z][A-Za-z]*Repository$");
    private static readonly Regex LoadMethod = new(@"^(Get|Load|Find)[A-Za-z]*Async$");
    private static readonly Regex NamespaceDeclaration = new(@"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);

    private static readonly string[] ModuleProjects = ["RedAnts.Ticketing", "RedAnts.Show"];

    private static readonly Dictionary<string, string> AdapterBaseline = RedAntsArchitecture.ReadBaseline("adapter-placement.txt");

    private static IEnumerable<System.Type> ModuleTypes =>
        new[] { RedAntsArchitecture.Ticketing, RedAntsArchitecture.Show }.SelectMany(RedAntsArchitecture.ReflectedTypes);

    private static string? CapabilityOf(string? fullNamespace)
    {
        var match = CapabilityNamespace.Match(fullNamespace ?? "");
        return match.Success ? $"{match.Groups["module"].Value}/{match.Groups["capability"].Value}" : null;
    }

    private static HashSet<string> AggregateRoots => ModuleTypes
        .Where(t => t.IsInterface && RepositoryName.IsMatch(t.Name))
        .SelectMany(t => t.GetMethods().Where(m => LoadMethod.IsMatch(m.Name)).Select(m => Unwrap(m.ReturnType)))
        .Where(t => t.Namespace?.StartsWith("RedAnts.") == true && !t.IsEnum)
        .Select(t => t.FullName!)
        .ToHashSet();

    private static IEnumerable<string> AdapterPlacements => ModuleTypes
        .Where(t => t is { IsClass: true, IsAbstract: false } && InfrastructureNamespace.IsMatch(t.Namespace ?? ""))
        .SelectMany(t => t.GetInterfaces()
            .Where(port => CapabilityOf(port.Namespace) is not null)
            .Where(port => CapabilityOf(port.Namespace) != CapabilityOf(t.Namespace))
            .Select(port => $"{t.FullName}->{port.FullName}"));

    [Fact]
    public void Adapters_live_in_the_capability_of_the_port_they_implement()
    {
        var offenders = AdapterPlacements.Where(a => !AdapterBaseline.ContainsKey(a)).Distinct().Order().ToList();
        Assert.True(offenders.Count == 0,
            "An adapter lives in the Infrastructure folder of the capability whose port it implements, so one capability is one folder. "
            + "The top-level Infrastructure keeps boot, migrations, seeding and mappers only. "
            + "A deliberate exception goes to adapter-placement.txt as <class>-><port> with its reason:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Adapter_placement_baseline_is_current()
    {
        var current = AdapterPlacements.ToHashSet();
        var stale = AdapterBaseline.Keys.Where(a => !current.Contains(a)).Order().ToList();
        Assert.True(stale.Count == 0, "Remove moved or renamed adapters from adapter-placement.txt:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void Aggregates_live_in_a_domain_namespace()
    {
        var offenders = AggregateRoots.Where(name => !DomainNamespace.IsMatch(Namespace(name))).Order().ToList();
        Assert.True(offenders.Count == 0,
            "What a repository loads and saves is an aggregate and belongs in Domain, never in Features or Infrastructure:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Readers_never_return_an_aggregate()
    {
        var aggregates = AggregateRoots;
        var offenders = ModuleTypes
            .Where(t => t.IsInterface && t.Name.EndsWith("Reader", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods()
                .Where(m => aggregates.Contains(Unwrap(m.ReturnType).FullName ?? ""))
                .Select(m => $"{t.FullName}.{m.Name} -> {Unwrap(m.ReturnType).FullName}"))
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "A reader answers one screen or export with a read model; handing out an aggregate would let a screen write through it:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Namespaces_follow_the_folder_path()
    {
        var offenders = new List<string>();
        foreach (var (project, rootNamespace) in new[]
                 {
                     ("RedAnts.Ticketing", "RedAnts.Ticketing"),
                     ("RedAnts.Show", "RedAnts.Show"),
                     ("RedAnts.Host", "RedAnts")
                 })
        {
            var projectDirectory = Path.Combine(RedAntsArchitecture.RepoRoot(), "src", project);
            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (RedAntsArchitecture.IsBuildOutput(file)) continue;
                var declared = NamespaceDeclaration.Match(File.ReadAllText(file));
                if (!declared.Success) continue;
                var folder = Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, file))!;
                var expected = folder.Length == 0
                    ? rootNamespace
                    : $"{rootNamespace}.{folder.Replace(Path.DirectorySeparatorChar, '.')}";
                if (declared.Groups[1].Value != expected)
                    offenders.Add($"{project}{Path.DirectorySeparatorChar}{Path.GetRelativePath(projectDirectory, file)}: {declared.Groups[1].Value}, expected {expected}");
            }
        }
        Assert.True(offenders.Count == 0,
            "Every layer, module and capability rule keys on the namespace, so a namespace must spell its folder path; "
            + "moving a file without renaming its namespace would silently switch its layer:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Feature_views_live_in_the_views_folder_of_their_capability()
    {
        var offenders = ModuleProjects
            .SelectMany(project => FeatureViews(project).Select(view => (project, view)))
            .Where(x => x.view != Path.Combine("Features", "_ViewImports.cshtml"))
            .Where(x => !Regex.IsMatch(x.view.Replace(Path.DirectorySeparatorChar, '/'), @"^Features/[A-Za-z]+/Views/"))
            .Select(x => $"{x.project}/{x.view.Replace(Path.DirectorySeparatorChar, '/')}")
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "FeatureViewLocationExpander resolves /Features/<Capability>/Views/{controller}/{view}.cshtml, "
            + "/Features/<Capability>/Views/{view}.cshtml and /Features/Shared/Views/{view}.cshtml; a view anywhere else is never found:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void The_libraries_compile_no_view_on_the_same_path()
    {
        var shared = ModuleProjects
            .SelectMany(project => FeatureViews(project).Select(view => (project, view)))
            .GroupBy(x => x.view)
            .Where(g => g.Count() > 1)
            .Where(g => g.Key != Path.Combine("Features", "_ViewImports.cshtml"))
            .Select(g => $"{g.Key.Replace(Path.DirectorySeparatorChar, '/')} in {string.Join(", ", g.Select(x => x.project))}")
            .Order()
            .ToList();
        Assert.True(shared.Count == 0,
            "Precompiled views are keyed by their relative path, so two libraries on the same path collide in the view engine "
            + "and in these tests; the import file is the one shared path every library needs:\n" + string.Join("\n", shared));
    }

    [Fact]
    public void A_port_file_declares_a_single_port()
    {
        var offenders = new List<string>();
        foreach (var project in ModuleProjects)
        {
            var projectDirectory = Path.Combine(RedAntsArchitecture.RepoRoot(), "src", project);
            foreach (var file in Directory.EnumerateFiles(projectDirectory, "I*.cs", SearchOption.AllDirectories))
            {
                if (RedAntsArchitecture.IsBuildOutput(file)) continue;
                var interfaces = Regex.Matches(File.ReadAllText(file), @"^public\s+interface\s+(\w+)", RegexOptions.Multiline);
                if (interfaces.Count > 1)
                    offenders.Add($"{project}/{Path.GetRelativePath(projectDirectory, file).Replace(Path.DirectorySeparatorChar, '/')}: "
                                  + string.Join(", ", interfaces.Select(m => m.Groups[1].Value)));
            }
        }
        Assert.True(offenders.Count == 0,
            "One interface per file, named after the file, so a port is found where its name says:\n" + string.Join("\n", offenders));
    }

    private static IEnumerable<string> FeatureViews(string project)
    {
        var features = Path.Combine(RedAntsArchitecture.RepoRoot(), "src", project, "Features");
        if (!Directory.Exists(features)) return [];
        var projectDirectory = Path.Combine(RedAntsArchitecture.RepoRoot(), "src", project);
        return Directory.EnumerateFiles(features, "*.cshtml", SearchOption.AllDirectories)
            .Where(f => !RedAntsArchitecture.IsBuildOutput(f))
            .Select(f => Path.GetRelativePath(projectDirectory, f));
    }

    private static string Namespace(string fullName) => fullName.Contains('.') ? fullName[..fullName.LastIndexOf('.')] : "";

    private static System.Type Unwrap(System.Type type)
    {
        if (type.IsByRef || type.IsArray) return Unwrap(type.GetElementType()!);
        if (type.IsGenericType && type.GetGenericArguments().Length == 1) return Unwrap(type.GetGenericArguments()[0]);
        return type;
    }
}
