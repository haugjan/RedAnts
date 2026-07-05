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

    public Task SetStatusAsync(int seasonId, SeasonStatus status) => Apply(seasonId, node =>
        node.SetValue(A.SeasonStatus, System.Text.Json.JsonSerializer.Serialize(new[] { status.ToString() })));

    public Task SetNameAsync(int seasonId, string name) => Apply(seasonId, node =>
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Die Saisonbezeichnung darf nicht leer sein.");
        node.Name = name.Trim();
    });

    public Task SetPeriodAsync(int seasonId, DateOnly startDate, DateOnly endDate) => Apply(seasonId, node =>
    {
        if (endDate < startDate)
            throw new InvalidOperationException("Das Enddatum darf nicht vor dem Startdatum liegen.");
        node.SetValue(A.SeasonStartDate, startDate.ToDateTime(TimeOnly.MinValue));
        node.SetValue(A.SeasonEndDate, endDate.ToDateTime(TimeOnly.MinValue));
    });

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

public sealed class SeasonStatusEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonStatusEditor, UmbracoSeasonStatusEditor>();
}
