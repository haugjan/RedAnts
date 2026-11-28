using System.Text.Json;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;

namespace RedAnts.Show.Features.TabBoards.Infrastructure;

public sealed class BoardSessionRepository(IDJSettings settings) : IBoardSessionRepository
{
    public static string KeyOf(string hallId) => $"Show:TabBoard:Session:{hallId.ToLowerInvariant()}";

    public async Task<BoardSession?> GetAsync(string hallId)
    {
        await settings.LoadAsync();
        if (settings.Get(KeyOf(hallId)) is not { Length: > 0 } json) return null;
        try { return JsonSerializer.Deserialize<BoardSession>(json, ShowJson.Options); }
        catch (JsonException) { return null; }
    }

    public Task SaveAsync(string hallId, BoardSession session) =>
        settings.SetAsync(KeyOf(hallId), JsonSerializer.Serialize(session, ShowJson.Options));
}
