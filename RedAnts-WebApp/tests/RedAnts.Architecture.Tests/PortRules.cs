using Xunit;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace RedAnts.Architecture.Tests;

public class PortRules
{
    private static readonly Regex ModuleNamespace = new(@"^RedAnts\.(Ticketing|Show)\.");
    private static readonly Regex CapabilityNamespace = new(@"^RedAnts\.(Ticketing|Show)\.Features\.[A-Za-z]+$");
    private static readonly Regex InfrastructureNamespace = new(@"\.Infrastructure(\.|$)");
    private static readonly string[] AllowedToUseInfrastructure = ["Composer", "Extensions", "Features"];

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
            .Where(t => !InfrastructureNamespace.IsMatch(t.Namespace.FullName))
            .Where(t => !AllowedToUseInfrastructure.Any(s => t.Name.EndsWith(s, StringComparison.Ordinal)))
            .SelectMany(t => t.Dependencies
                .Select(d => d.Target)
                .Where(target => target.Namespace.FullName.StartsWith("RedAnts.") && InfrastructureNamespace.IsMatch(target.Namespace.FullName))
                .Select(target => $"{t.FullName} -> {target.FullName}"))
            .Distinct()
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "Only types named *Composer, *Extensions or *Features may depend on an Infrastructure namespace from outside Infrastructure:\n" + string.Join("\n", offenders));
    }
}
