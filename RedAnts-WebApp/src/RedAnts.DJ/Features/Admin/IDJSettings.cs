namespace RedAnts.DJ.Features.Admin;

public interface IDJSettings
{
    string? Get(string key);
    Task SetAsync(string key, string? value);
    Task LoadAsync();
}
