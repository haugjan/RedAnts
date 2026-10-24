using Xunit;
using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;

namespace RedAnts.Architecture.Tests;

public class SliceRules
{
    private static readonly string[] OpenToEveryCapability = ["Admin", "Shared"];

    private static readonly Regex SliceNamespace = new(@"^RedAnts\.(Ticketing|Show)\.Features(\.|$)");

    private static readonly Regex CapabilityNamespace =
        new(@"^RedAnts\.(?<module>Ticketing|Show)\.Features\.(?<capability>[A-Za-z]+)(\.|$)");

    private static readonly string[] MessageNames = ["Command", "Query", "Check"];

    private static readonly Dictionary<string, string> UiExceptions = RedAntsArchitecture.ReadBaseline("capability-ui-exceptions.txt");

    private static IEnumerable<System.Type> ModuleTypes =>
        new[] { RedAntsArchitecture.Ticketing, RedAntsArchitecture.Show }.SelectMany(RedAntsArchitecture.ReflectedTypes);

    private static IEnumerable<System.Type> Slices => ModuleTypes
        .Where(t => t.IsClass && t.Name == "Handler" && SliceNamespace.IsMatch(t.Namespace ?? ""))
        .Where(t => !t.GetInterfaces().Any(i => i.Name.StartsWith("INotification")))
        .Select(t => t.DeclaringType)
        .OfType<System.Type>()
        .Distinct();

    private static string? CapabilityOf(string? fullNamespace)
    {
        var match = CapabilityNamespace.Match(fullNamespace ?? "");
        return match.Success ? $"{match.Groups["module"].Value}/{match.Groups["capability"].Value}" : null;
    }

    private static bool IsUi(System.Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        (typeof(ComponentBase).IsAssignableFrom(type) || typeof(ControllerBase).IsAssignableFrom(type) || typeof(IRazorPage).IsAssignableFrom(type));

    [Fact]
    public void Slices_carry_exactly_one_message_record()
    {
        var offenders = Slices
            .Select(slice => new
            {
                Slice = slice,
                Messages = MessageNames.Where(name => slice.GetNestedType(name) is not null).ToList()
            })
            .Where(x => x.Messages.Count != 1)
            .Select(x => $"{x.Slice.FullName} has {(x.Messages.Count == 0 ? "no" : string.Join("+", x.Messages))} message record")
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "A slice is one command, one query or one check: its message is a nested record named Command, Query or Check, and there is exactly one:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Slice_classes_are_static()
    {
        var offenders = Slices
            .Where(slice => !(slice.IsAbstract && slice.IsSealed))
            .Select(slice => slice.FullName!)
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "A slice is a static class that only hosts its message and its handler; state belongs to the handler or the aggregate:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Message_records_are_immutable()
    {
        var offenders = Slices
            .SelectMany(slice => MessageNames.Select(slice.GetNestedType).OfType<System.Type>())
            .SelectMany(message => message.GetProperties()
                .Where(p => p.SetMethod is { IsPublic: true } setter && !setter.ReturnParameter.GetRequiredCustomModifiers()
                    .Any(m => m.Name == "IsExternalInit"))
                .Select(p => $"{message.DeclaringType!.FullName}.{message.Name}.{p.Name}"))
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "A command, query or check is an immutable record: a handler must not be able to rewrite the message it was given:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void A_capability_does_not_render_another_capabilitys_user_interface()
    {
        var ui = ModuleTypes.Where(IsUi).Select(t => t.FullName!).ToHashSet();
        var offenders = RedAntsArchitecture.OwnTypes
            .Where(type => CapabilityOf(type.Namespace.FullName) is not null)
            .SelectMany(type => type.Dependencies
                .Select(d => d.Target)
                .Where(target => ui.Contains(target.FullName))
                .Where(target => CapabilityOf(target.Namespace?.FullName) is { } to
                                 && CapabilityOf(type.Namespace.FullName) is { } from
                                 && to != from
                                 && !OpenToEveryCapability.Any(open => from.EndsWith('/' + open))
                                 && !OpenToEveryCapability.Any(open => to.EndsWith('/' + open)))
                .Select(target => $"{type.FullName}->{target.FullName}"))
            .Distinct()
            .Where(edge => !UiExceptions.ContainsKey(edge))
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "Components, dialogs and controllers belong to their own capability. The Admin shell composes the capabilities, "
            + "Shared and the shell hold the widgets every capability may render, and everything else talks to another capability through its handlers. "
            + "A deliberate exception goes to capability-ui-exceptions.txt as <from>-><to> with its reason:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Capability_ui_exceptions_only_list_edges_that_still_exist()
    {
        var ui = ModuleTypes.Where(IsUi).Select(t => t.FullName!).ToHashSet();
        var edges = RedAntsArchitecture.OwnTypes
            .SelectMany(type => type.Dependencies.Select(d => $"{type.FullName}->{d.Target.FullName}"))
            .ToHashSet();
        var stale = UiExceptions.Keys.Where(e => !edges.Contains(e)).Order().ToList();
        Assert.True(stale.Count == 0, "Remove edges that no longer exist from capability-ui-exceptions.txt:\n" + string.Join("\n", stale));
    }
}
