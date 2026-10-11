using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Agents;

public static class RecordAgentReport
{
    public sealed record Command(string HallId, string Kind, string Json);

    public sealed class Handler(IAgentReports reports, IHallRepository halls, ISoundPlayer sounds)
    {
        public async Task HandleAsync(Command command)
        {
            ValidationException.ThrowIfBlank(command.Kind, nameof(command.Kind), "Art der Meldung");
            var kind = command.Kind.Trim().ToLowerInvariant();
            await reports.RecordAsync(command.HallId, new AgentReport(kind, command.Json, SwissTime.Timestamp));
            if (kind == AgentReportKinds.Sound) await ForwardSoundAsync(command.HallId, command.Json);
        }

        private async Task ForwardSoundAsync(string hallId, string json)
        {
            if (SoundRequest.Parse(json) is not { } request) return;
            var hall = await halls.GetByIdAsync(hallId);
            var room = hall?.Devices.DjRoom is { Length: > 0 } configured ? configured : null;
            switch (request.Action)
            {
                case SceneActions.SoundPlay when request.TileId is { Length: > 0 } tile:
                    await sounds.PlayAsync(room, tile, request.ProfileId);
                    break;
                case SceneActions.SoundStop:
                    await sounds.StopAsync(room);
                    break;
                case SceneActions.SoundFade:
                    await sounds.FadeAsync(room);
                    break;
            }
        }
    }
}
