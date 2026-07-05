using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class UmbracoSeasonStatusEditor(IContentService contentService) : ISeasonStatusEditor
{
    private const int SuperUser = Constants.Security.SuperUserId;

    public Task SetStatusAsync(int seasonId, SeasonStatus status) => Task.Run(() =>
    {
        var node = contentService.GetById(seasonId)
            ?? throw new InvalidOperationException($"Saison {seasonId} wurde nicht gefunden.");
        if (node.ContentType.Alias != A.SeasonType)
            throw new InvalidOperationException($"Inhalt {seasonId} ist keine Saison.");

        node.SetValue(A.SeasonStatus, System.Text.Json.JsonSerializer.Serialize(new[] { status.ToString() }));
        contentService.Save(node, SuperUser);
        contentService.Publish(node, new[] { "*" }, SuperUser);
    });
}

public sealed class SeasonStatusEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonStatusEditor, UmbracoSeasonStatusEditor>();
}
