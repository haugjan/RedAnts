using System.Net;
using System.Text.Json;

namespace RedAnts.Show.Agent.Web;

public sealed record HallChoice(string Id, string Name, bool AgentOnline);

public sealed record HallAccess(string Id, string Name, string Key);

public enum HallListOutcome { Listed, WrongPassword, Unreachable }

public sealed record HallList(HallListOutcome Outcome, IReadOnlyList<HallChoice> Halls, string Message = "");

public sealed class HallDirectory : IDisposable
{
    readonly HttpClient _http;

    public HallDirectory()
    {
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("RedAnts-Board");
    }

    public async Task<HallList> ListAsync(AgentConfig config, CancellationToken stop)
    {
        try
        {
            using var answer = await _http.GetAsync($"{config.Cloud}/bord/hallen?key={Uri.EscapeDataString(config.CloudPassword)}", stop);
            if (Refused(answer.StatusCode)) return new HallList(HallListOutcome.WrongPassword, []);
            if (!answer.IsSuccessStatusCode) return new HallList(HallListOutcome.Unreachable, [], $"{(int)answer.StatusCode} {answer.ReasonPhrase}");
            var halls = ParseHalls(await answer.Content.ReadAsStringAsync(stop));
            return halls is null ? new HallList(HallListOutcome.WrongPassword, []) : new HallList(HallListOutcome.Listed, halls);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return new HallList(HallListOutcome.Unreachable, [], e.Message);
        }
    }

    public async Task<HallAccess?> AccessAsync(AgentConfig config, string hallId, CancellationToken stop)
    {
        try
        {
            using var answer = await _http.GetAsync(
                $"{config.Cloud}/bord/hallen/{Uri.EscapeDataString(hallId)}/agent?key={Uri.EscapeDataString(config.CloudPassword)}", stop);
            return answer.IsSuccessStatusCode ? ParseAccess(await answer.Content.ReadAsStringAsync(stop)) : null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    public static IReadOnlyList<HallChoice>? ParseHalls(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return null;
            return document.RootElement.EnumerateArray()
                .Select(hall => new HallChoice(Text(hall, "id"), Text(hall, "name"), hall.TryGetProperty("agent", out var agent) && agent.ValueKind == JsonValueKind.True))
                .Where(hall => hall.Id.Length > 0)
                .Select(hall => hall with { Name = hall.Name.Length > 0 ? hall.Name : hall.Id })
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static HallAccess? ParseAccess(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            var access = new HallAccess(Text(document.RootElement, "id"), Text(document.RootElement, "name"), Text(document.RootElement, "key"));
            return access.Id.Length > 0 && access.Key.Length > 0 ? access : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static bool Refused(HttpStatusCode status) =>
        status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden || ((int)status >= 300 && (int)status < 400);

    static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    public void Dispose() => _http.Dispose();
}
