using Microsoft.Extensions.Logging;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Setup;
using RedAnts.Show.Features.TabBoards;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace RedAnts.Show.Infrastructure.Content;

public sealed class TabBoardSeeder(
    ITabBoardRepository boards,
    IHallRepository halls,
    IShowSetup setup,
    ILogger<TabBoardSeeder> log) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken stop)
    {
        try
        {
            if (!(await boards.GetAsync()).IsEmpty) return;
            var known = new Dictionary<string, Hall>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in TabBoardStart.VenueSources.SelectMany(source => source.Halls))
                if (await halls.GetByIdAsync(id) is { } hall) known[id] = hall;
            var board = TabBoardStart.Build(await setup.GetAsync(), known).Validated();
            await boards.SaveAsync(board);
            log.LogInformation("Tab-Bord angelegt: {Venues} Hallen, {Leagues} Ligen, {Buttons} Buttons",
                board.Venues.Count, board.Leagues.Count, board.Buttons.Count);
        }
        catch (Exception e)
        {
            log.LogWarning("Tab-Bord konnte nicht angelegt werden: {Message}", e.Message);
        }
    }
}
