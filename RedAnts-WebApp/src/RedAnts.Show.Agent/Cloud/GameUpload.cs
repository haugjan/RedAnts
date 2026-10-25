using System.Text.Json;

namespace RedAnts.Show.Agent.Cloud;

public sealed class GameUpload(GameOutbox outbox, CloudLink cloud, ILogger<GameUpload> log) : BackgroundService
{
    static readonly TimeSpan Retry = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            if (outbox.Pending is { } game && cloud.Connected)
            {
                if (await cloud.SendAsync("Game", stop, JsonSerializer.Serialize(game, PayloadJson.Options)))
                {
                    outbox.Sent(game);
                    log.LogInformation("Spiel {Game} in die Cloud geschrieben", game.Id);
                }
            }
            try { await Task.Delay(Retry, stop); }
            catch (OperationCanceledException) { break; }
        }
    }
}
