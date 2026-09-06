using Xunit;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace RedAnts.Architecture.Tests;

public class HandlerRules
{
    private static IEnumerable<IType> Handlers => RedAntsArchitecture.OwnTypes.Where(IsHandler);

    private static bool IsHandler(IType type) =>
        type.Name == "Handler" && type.Namespace.FullName.StartsWith("RedAnts.Features.");

    [Fact]
    public void Handlers_are_nested_in_a_slice_and_sealed()
    {
        var offenders = Handlers
            .Where(h => h is not Class { IsSealed: true } || h.FullName.Count(c => c == '+') != 1)
            .Select(h => h.FullName)
            .ToList();
        Assert.True(offenders.Count == 0, "Handlers must be sealed and nested once in their slice class:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Handlers_never_call_other_handlers()
    {
        var offenders = Handlers
            .SelectMany(h => h.Dependencies
                .Select(d => d.Target)
                .Where(t => IsHandler(t) && t.FullName != h.FullName)
                .Select(t => $"{h.FullName} -> {t.FullName}"))
            .Distinct()
            .ToList();
        Assert.True(offenders.Count == 0, "Handlers must not depend on other handlers:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Slices_live_directly_in_a_module_or_a_workflow_folder()
    {
        var offenders = Handlers
            .Select(h => h.Namespace.FullName)
            .Where(ns => !Regex.IsMatch(ns, @"^RedAnts\.Features\.[A-Za-z]+(\.[A-Za-z]+Workflow)?$"))
            .Distinct()
            .ToList();
        Assert.True(offenders.Count == 0, "Slice namespaces must be RedAnts.Features.<Module>[.<Name>Workflow]:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Slices_are_registered_exactly_once()
    {
        var expected = RedAntsArchitecture.Ticketing.GetTypes()
            .Where(t => t.IsClass && t.Name == "Handler" && t.Namespace is { } ns && ns.StartsWith("RedAnts.Features."))
            .Where(t => !t.GetInterfaces().Any(i => i.Name.StartsWith("INotification")))
            .ToHashSet();
        var registered = RedAnts.Features.Ticketing.TicketingFeatures.Handlers.ToList();

        var missing = expected.Except(registered).Select(t => t.FullName).ToList();
        var duplicates = registered.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key.FullName).ToList();
        var unknown = registered.Except(expected).Select(t => t.FullName).ToList();

        Assert.True(missing.Count == 0, "Handlers missing in TicketingFeatures.Handlers:\n" + string.Join("\n", missing));
        Assert.True(duplicates.Count == 0, "Handlers registered twice:\n" + string.Join("\n", duplicates));
        Assert.True(unknown.Count == 0, "Registered types that are no slice handlers:\n" + string.Join("\n", unknown));
    }
}
