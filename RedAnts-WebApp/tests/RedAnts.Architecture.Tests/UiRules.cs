using Xunit;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;

namespace RedAnts.Architecture.Tests;

public class UiRules
{
    private static readonly Regex PortNamespace =
        new(@"^RedAnts\.(Ticketing|Show)\.Features\.(?!Shared\b)[A-Za-z]+(\.(?!Admin\b|Infrastructure\b|Views\b|Shared\b)[A-Za-z]+)?$");

    private static readonly Dictionary<string, string> Exceptions = RedAntsArchitecture.ReadBaseline("ui-port-exceptions.txt");

    private static readonly HashSet<string> UiTypeNames = new[] { RedAntsArchitecture.Ticketing, RedAntsArchitecture.Show, RedAntsArchitecture.Host }
        .SelectMany(RedAntsArchitecture.ReflectedTypes)
        .Where(IsUi)
        .Select(t => t.FullName!)
        .ToHashSet();

    private static bool IsUi(System.Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        (typeof(ComponentBase).IsAssignableFrom(type) || typeof(ControllerBase).IsAssignableFrom(type) || typeof(IRazorPage).IsAssignableFrom(type));

    private static bool IsPort(IType type) =>
        type is Interface && !type.FullName.Contains('+') && PortNamespace.IsMatch(type.Namespace?.FullName ?? "");

    [Fact]
    public void Ui_depends_on_handlers_only()
    {
        var offenders = RedAntsArchitecture.OwnTypes
            .Where(t => UiTypeNames.Contains(t.FullName) && !Exceptions.ContainsKey(t.FullName))
            .SelectMany(t => t.Dependencies
                .Select(d => d.Target)
                .Where(IsPort)
                .Select(port => $"{t.FullName} -> {port.FullName}"))
            .Distinct()
            .Order()
            .ToList();
        Assert.True(offenders.Count == 0,
            "Razor components, controllers and compiled views inject handlers only; ports (repositories, readers, mailers, tokens) belong to handlers and adapters:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void Ui_port_exceptions_only_list_types_that_still_exist()
    {
        var stale = Exceptions.Keys.Where(n => !UiTypeNames.Contains(n)).Order().ToList();
        Assert.True(stale.Count == 0, "Remove renamed or deleted types from ui-port-exceptions.txt:\n" + string.Join("\n", stale));
    }
}
