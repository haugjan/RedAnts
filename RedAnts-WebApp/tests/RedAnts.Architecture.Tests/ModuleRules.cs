using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace RedAnts.Architecture.Tests;

public class ModuleRules
{
    private static readonly IObjectProvider<IType> TicketingInternals =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Ticketing\.(Domain(\..*)?|(\w+\.)*Infrastructure(\..*)?)$")
            .And().DoNotHaveNameEndingWith("Extensions")
            .And().DoNotHaveNameEndingWith("Composer")
            .As("Ticketing internals");

    private static readonly IObjectProvider<IType> Ticketing =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Ticketing(\..*)?$").As("Ticketing");

    private static readonly IObjectProvider<IType> DJ =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.DJ(\..*)?$").As("DJ");

    private static readonly IObjectProvider<IType> DJInternals =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.DJ\.(Domain(\..*)?|(\w+\.)*Infrastructure(\..*)?)$")
            .And().DoNotHaveNameEndingWith("Extensions")
            .And().DoNotHaveNameEndingWith("Composer")
            .As("DJ internals");

    private static readonly IObjectProvider<IType> Show =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Show(\..*)?$").As("Hallensteuerung");

    private static readonly IObjectProvider<IType> ShowInternals =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Show\.(Domain(\..*)?|(\w+\.)*Infrastructure(\..*)?)$")
            .And().DoNotHaveNameEndingWith("Extensions")
            .And().DoNotHaveNameEndingWith("Composer")
            .As("Hallensteuerung internals");

    private static readonly IObjectProvider<IType> ShowOutsideInfrastructure =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Show(\..*)?$")
            .And().DoNotResideInNamespaceMatching(@"^RedAnts\.Show\.(\w+\.)*Infrastructure(\..*)?$")
            .As("Hallensteuerung ausserhalb der Infrastruktur");

    private static readonly IObjectProvider<IType> WebsiteInternals =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.(Domain|Features|Infrastructure)\.Website(\..*)?$")
            .As("Website internals");

    [Fact]
    public void Host_uses_ticketing_only_through_its_extension_methods_and_ports() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Host).Should().NotDependOnAny(TicketingInternals)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Host_uses_show_only_through_its_extension_methods_and_ports() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Host).Should().NotDependOnAny(DJInternals)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Host_uses_the_hall_control_only_through_its_extension_methods_and_ports() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Host).Should().NotDependOnAny(ShowInternals)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void The_hall_control_does_not_use_ticketing() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Show).Should().NotDependOnAny(Ticketing)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void The_soundboard_does_not_use_the_hall_control() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.DJ).Should().NotDependOnAny(Show)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Only_the_infrastructure_of_the_hall_control_reaches_into_the_soundboard() =>
        Types().That().Are(ShowOutsideInfrastructure).Should().NotDependOnAny(DJ)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void DJ_does_not_use_ticketing() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.DJ).Should().NotDependOnAny(Ticketing)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Ticketing_does_not_use_show_or_website() =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Ticketing)
            .Should().NotDependOnAny(DJ).AndShould().NotDependOnAny(WebsiteInternals)
            .Check(RedAntsArchitecture.Loaded);
}
