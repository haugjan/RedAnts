using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace RedAnts.Architecture.Tests;

public class LayerRules
{
    private static readonly IObjectProvider<IType> Domain =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Domain(\..*)?$").As("Domain");

    private static readonly IObjectProvider<IType> TicketingAndWebsiteFeatures =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Features\.(Ticketing|Website)(\..*)?$").As("Features");

    private static readonly IObjectProvider<IType> ShowFeatures =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Features\.Show(\..*)?$").As("Show features");

    private static readonly IObjectProvider<IType> Infrastructure =
        Types().That().ResideInNamespaceMatching(@"^RedAnts\.Infrastructure(\..*)?$").As("Infrastructure");

    [Fact]
    public void Domain_depends_on_nothing_above_it() =>
        Types().That().Are(Domain).Should().NotDependOnAny(TicketingAndWebsiteFeatures)
            .AndShould().NotDependOnAny(ShowFeatures)
            .AndShould().NotDependOnAny(Infrastructure)
            .Check(RedAntsArchitecture.Loaded);

    [Theory]
    [InlineData(@"^Umbraco(\..*)?$")]
    [InlineData(@"^NPoco(\..*)?$")]
    [InlineData(@"^Microsoft\.AspNetCore(\..*)?$")]
    [InlineData(@"^Microsoft\.Data(\..*)?$")]
    public void Domain_uses_no_framework(string namespacePattern) =>
        Types().That().Are(Domain).Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(namespacePattern)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Features_do_not_depend_on_infrastructure() =>
        Types().That().Are(TicketingAndWebsiteFeatures).Should().NotDependOnAny(Infrastructure)
            .Check(RedAntsArchitecture.Loaded);

    [Fact]
    public void Show_features_do_not_depend_on_infrastructure() =>
        Types().That().Are(ShowFeatures).Should().NotDependOnAny(Infrastructure)
            .Check(RedAntsArchitecture.Loaded);

    [Theory]
    [InlineData(@"^RedAnts\.(Domain|Features|Infrastructure)\.(Ticketing|Show|Website|Shared)(\..*)?$")]
    [InlineData(@"^(Umbraco|NPoco|Microsoft\.AspNetCore|Microsoft\.Extensions)(\..*)?$")]
    public void Kernel_depends_only_on_the_base_class_library(string namespacePattern) =>
        Types().That().ResideInAssembly(RedAntsArchitecture.Kernel)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(namespacePattern)
            .Check(RedAntsArchitecture.Loaded);
}
