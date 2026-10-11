namespace RedAnts.Show.Features.Agents;

public interface ISoundPlayer
{
    Task PlayAsync(string? room, string tileId, string? profileId);
    Task StopAsync(string? room);
    Task FadeAsync(string? room);
}
