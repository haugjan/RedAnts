using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

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

public sealed class FlexBundleEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IFlexBundleEditor, FlexBundleEditorAdapter>();
}
