using RedAnts.DJ.Features.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace RedAnts.DJ.Infrastructure;

public sealed class DJSpotifyWarmupComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DJSpotifyWarmup>();
}

public sealed class DJSpotifyWarmup(IDJSpotifyAccount account, ILogger<DJSpotifyWarmup> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (!account.Connected) return;
        var token = await account.AccessTokenAsync();
        if (token is null)
            logger.LogWarning("Spotify warm-up got no access token; the first DJ action will fetch it instead.");
        else
            logger.LogInformation("Spotify access token warmed for {Account}.", account.AccountName);
    }
}
