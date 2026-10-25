using System.Text.Json;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Games;

public static class SaveGameFromAgent
{
    public sealed record Command(string HallId, string Json);

    public sealed class Handler(IHallRepository halls, IGameRepository games, AgentSync sync, ILogger<Handler> log)
    {
        public async Task HandleAsync(Command command)
        {
            if (await halls.GetByIdAsync(command.HallId) is not { ActiveGameId: { Length: > 0 } active }) return;
            if (Read(command.Json) is not { } sent || sent.Id != active) return;
            try
            {
                await games.SaveAsync(sent.Validated() with { LoadedAt = SwissTime.Timestamp });
            }
            catch (ValidationException e)
            {
                log.LogWarning("Spiel {Game} aus der Halle {Hall} abgelehnt: {Message}", sent.Id, command.HallId, e.Message);
                return;
            }
            await sync.PushGameAsync(active);
            log.LogInformation("Spiel {Game} aus der Halle {Hall} übernommen", sent.Id, command.HallId);
        }

        private Game? Read(string json)
        {
            try { return JsonSerializer.Deserialize<Game>(json, ShowJson.Options); }
            catch (JsonException e)
            {
                log.LogWarning("Spiel aus der Halle unlesbar: {Message}", e.Message);
                return null;
            }
        }
    }
}
