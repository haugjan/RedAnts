using Xunit;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RedAnts.Architecture.Tests;

public class ProjectRules
{
    private static readonly Dictionary<string, string[]> AllowedReferences = new()
    {
        ["RedAnts.Kernel"] = [],
        ["RedAnts.Ticketing"] = ["RedAnts.Kernel"],
        ["RedAnts.Show"] = ["RedAnts.Kernel"],
        ["RedAnts.Host"] = ["RedAnts.Kernel", "RedAnts.Ticketing", "RedAnts.Show"],
        ["RedAnts.Show.Agent"] = ["RedAnts.Kernel"],
        ["RedAnts.Show.Simulator"] = ["RedAnts.Kernel"]
    };

    private static string Source => Path.Combine(RedAntsArchitecture.RepoRoot(), "src");

    private static IEnumerable<string> ProjectFiles => Directory.EnumerateFiles(Source, "*.csproj", SearchOption.AllDirectories)
        .Where(f => !RedAntsArchitecture.IsBuildOutput(f));

    private static IEnumerable<string> ReferencesOf(string projectFile) =>
        XDocument.Load(projectFile).Descendants("ProjectReference")
            .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar)));

    [Fact]
    public void Every_project_keeps_to_the_dependencies_its_role_allows()
    {
        var offenders = new List<string>();
        foreach (var file in ProjectFiles)
        {
            var project = Path.GetFileNameWithoutExtension(file);
            if (!AllowedReferences.TryGetValue(project, out var allowed))
            {
                offenders.Add($"{project} is not listed in the architecture test; add it with the references its role allows");
                continue;
            }
            foreach (var reference in ReferencesOf(file).Where(r => !allowed.Contains(r)))
                offenders.Add($"{project} -> {reference}");
        }
        Assert.True(offenders.Count == 0,
            "The modules share the Kernel and nothing else, the Host composes them through their extension methods, "
            + "and the hall agent and the simulator stay free of the web modules so they run without the cloud:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void The_kernel_references_no_package()
    {
        var kernel = Path.Combine(Source, "RedAnts.Kernel", "RedAnts.Kernel.csproj");
        var packages = XDocument.Load(kernel).Descendants("PackageReference")
            .Select(r => r.Attribute("Include")!.Value)
            .Order()
            .ToList();
        Assert.True(packages.Count == 0,
            "The Kernel carries what every module needs and therefore no dependency of its own; "
            + "a package here would reach every module and the Host:\n" + string.Join("\n", packages));
    }

    [Fact]
    public void Every_project_is_part_of_the_solution()
    {
        var solution = File.ReadAllText(Path.Combine(RedAntsArchitecture.RepoRoot(), "RedAnts.slnx"));
        var listed = Regex.Matches(solution, @"Path=""([^""]+)""")
            .Select(m => Path.GetFileNameWithoutExtension(m.Groups[1].Value))
            .ToHashSet();
        var root = RedAntsArchitecture.RepoRoot();
        var missing = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !RedAntsArchitecture.IsBuildOutput(f))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(p => !listed.Contains(p))
            .Order()
            .ToList();
        Assert.True(missing.Count == 0,
            "A project outside RedAnts.slnx is neither built nor tested by the pipeline:\n" + string.Join("\n", missing));
    }
}
