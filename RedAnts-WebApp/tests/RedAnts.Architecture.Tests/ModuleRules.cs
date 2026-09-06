using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace RedAnts.Architecture.Tests;

public class ModuleRules
{
    private static readonly IObjectProvider<IType> TicketingInternals =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.(Domain|Infrastructure)\.Ticketing(\..*)?$")
            .And().DoNotHaveNameEndingWith("Extensions")
            .And().DoNotHaveNameEndingWith("Composer")
            .As("Ticketing internals");

    private static readonly IObjectProvider<IType> Ticketing =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.(Domain|Features|Infrastructure)\.Ticketing(\..*)?$")
            .As("Ticketing");

    private static readonly IObjectProvider<IType> ShowInternals =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.(Domain|Features|Infrastructure)\.Show(\..*)?$")
            .And().DoNotHaveNameEndingWith("Extensions")
            .As("Show internals");

    private static readonly IObjectProvider<IType> WebsiteInternals =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.(Domain|Features|Infrastructure)\.Website(\..*)?$")
            .As("Website internals");

    [Fact]
    public void Host_uses_ticketing_only_through_its_extension_methods_and_ports() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Host).Should().NotDependOnAny(TicketingInternals)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Show_does_not_use_ticketing() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Show).Should().NotDependOnAny(Ticketing)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Ticketing_does_not_use_show_or_website() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Ticketing)
            .Should().NotDependOnAny(ShowInternals).AndShould().NotDependOnAny(WebsiteInternals)
            .Check(RedAntsArchitecture.Loaded);
}
