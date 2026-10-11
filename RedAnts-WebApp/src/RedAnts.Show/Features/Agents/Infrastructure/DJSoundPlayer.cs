using RedAnts.DJ.Features.Remote;

namespace RedAnts.Show.Features.Agents.Infrastructure;

public sealed class DJSoundPlayer(SendDJCommand.Handler dj) : ISoundPlayer
{
    public async Task PlayAsync(string? room, string tileId, string? profileId)
    {
        if (profileId is { Length: > 0 })
            await dj.HandleAsync(new SendDJCommand.Command("profile", ProfileId: profileId, Room: room));
        await dj.HandleAsync(new SendDJCommand.Command("play", tileId, profileId, Room: room));
    }

    public Task StopAsync(string? room) => dj.HandleAsync(new SendDJCommand.Command("stop", Room: room));

    public Task FadeAsync(string? room) => dj.HandleAsync(new SendDJCommand.Command("fade", Room: room));
}
