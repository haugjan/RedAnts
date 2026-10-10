namespace RedAnts.Game.Features.Settings;

public interface IGameSettings
{
    string? Get(string key);

    int Budget { get; }

    int Round { get; }

    Task LoadAsync();

    Task SetAsync(string key, string? value);
}
