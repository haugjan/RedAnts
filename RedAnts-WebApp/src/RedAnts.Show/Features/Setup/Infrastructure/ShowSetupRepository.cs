using RedAnts.Show.Domain;
using System.Text.Json;
using RedAnts.Show.Features.Admin;

namespace RedAnts.Show.Features.Setup.Infrastructure;

public sealed class ShowSetupRepository(IDJSettings settings) : IShowSetup
{
    public const string TitlesKey = "Show:Titles";
    public const string LedKey = "Show:Led";

    public Task<ShowSetup> GetAsync() =>
        Task.FromResult(new ShowSetup(
            Read<OverlayTitle>(TitlesKey),
            Read<LedTemplate>(LedKey)));

    public async Task SaveAsync(ShowSetup setup)
    {
        await settings.SetAsync(TitlesKey, JsonSerializer.Serialize(setup.Titles, ShowJson.Options));
        await settings.SetAsync(LedKey, JsonSerializer.Serialize(setup.Led, ShowJson.Options));
    }

    private IReadOnlyList<T> Read<T>(string key)
    {
        if (settings.Get(key) is not { Length: > 0 } json) return [];
        try { return JsonSerializer.Deserialize<List<T>>(json, ShowJson.Options) ?? []; }
        catch (JsonException) { return []; }
    }
}
