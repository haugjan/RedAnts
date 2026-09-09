namespace RedAnts.Show.Features.Admin;

public interface IShowSettings
{
    string? Get(string key);
    Task SetAsync(string key, string? value);
    Task LoadAsync();
}
