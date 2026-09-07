// Uses Umbraco 17 APIs deprecated for removal in Umbraco 18 (content/data-type Save, DataType GetAll,
// FileService templates, Constants.Security.SuperUserId, IPublishedContent.Parent, SpecialDbTypes.NTEXT).
// Still functional; migrate to the async management services at the Umbraco 18 upgrade.
#pragma warning disable CS0618
using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class UmbracoSeasonStatusPublisher(IContentService contentService) : ISeasonStatusPublisher
{
    private const int SuperUser = Constants.Security.SuperUserId;

    public Task SetStatusAsync(int seasonId, SeasonStatus status) => Apply(seasonId, node =>
        node.SetValue(A.SeasonStatus, System.Text.Json.JsonSerializer.Serialize(new[] { status.ToString() })));

    private Task Apply(int seasonId, Action<Umbraco.Cms.Core.Models.IContent> mutate) => Task.Run(() =>
    {
        var node = contentService.GetById(seasonId)
            ?? throw new InvalidOperationException($"Saison {seasonId} wurde nicht gefunden.");
        if (node.ContentType.Alias != A.SeasonType)
            throw new InvalidOperationException($"Inhalt {seasonId} ist keine Saison.");

        mutate(node);
        contentService.Save(node, SuperUser);
        contentService.Publish(node, new[] { "*" }, SuperUser);
    });
}

public sealed class SeasonStatusPublisherComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonStatusPublisher, UmbracoSeasonStatusPublisher>();
}
