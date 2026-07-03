using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Renames a Flexticket bundle's reference in <c>FlexTicketBundles</c>. Reuses the bundle
/// port's per-season uniqueness check, then updates the single column directly. Independent of the
/// bundle repository so it does not widen S2's creation port.</summary>
public sealed class FlexBundleEditorAdapter(IScopeProvider scopeProvider, IFlexTicketBundles bundles)
    : IFlexBundleEditor
{
    public async Task RenameAsync(int bundleId, int seasonId, string newReference)
    {
        var reference = (newReference ?? "").Trim();
        if (reference.Length == 0)
            throw new DomainException("Bitte eine Referenz angeben.");
        if (reference.Length > FlexTicketBundle.ReferenceMaxLength)
            throw new DomainException($"Die Referenz darf höchstens {FlexTicketBundle.ReferenceMaxLength} Zeichen lang sein.");
        if (await bundles.ReferenceExistsAsync(seasonId, reference))
            throw new DomainException($"Die Referenz „{reference}“ ist in dieser Saison bereits vergeben.");

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE FlexTicketBundles SET Reference = @0 WHERE Id = @1",
            new object[] { reference, bundleId });
    }
}

/// <summary>Registers the Flextickets bundle rename adapter (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class FlexBundleEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IFlexBundleEditor, FlexBundleEditorAdapter>();
}
