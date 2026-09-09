using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace RedAnts.Ticketing.Infrastructure;

public sealed class FeatureViewLocationExpander : IViewLocationExpander
{
    private const string FeatureKey = "feature";
    private const string FeaturesSegment = ".Features.";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        if (context.ActionContext.ActionDescriptor is ControllerActionDescriptor descriptor
            && FeatureOf(descriptor.ControllerTypeInfo.Namespace) is { } feature)
        {
            context.Values[FeatureKey] = feature;
        }
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(FeatureKey, out var feature))
        {
            yield return $"/Features/{feature}/Views/{{1}}/{{0}}.cshtml";
            yield return $"/Features/{feature}/Views/{{0}}.cshtml";
        }
        yield return "/Features/Shared/Views/{0}.cshtml";
        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }

    public static string? FeatureOf(string? controllerNamespace)
    {
        if (controllerNamespace is null) return null;
        var index = controllerNamespace.IndexOf(FeaturesSegment, StringComparison.Ordinal);
        if (index < 0) return null;
        var rest = controllerNamespace[(index + FeaturesSegment.Length)..];
        var end = rest.IndexOf('.');
        return end < 0 ? rest : rest[..end];
    }
}
