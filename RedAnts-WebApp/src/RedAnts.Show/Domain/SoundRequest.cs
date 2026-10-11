using System.Text.Json;

namespace RedAnts.Show.Domain;

public sealed record SoundRequest(string Action, string? TileId, string? ProfileId)
{
    private const string TilePrefix = "tile:";

    public static SoundRequest? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var action = Text(root, "action");
            if (!action.StartsWith("sound.", StringComparison.Ordinal) || !SceneActions.All.Contains(action)) return null;
            var (profileId, tileId) = SplitTarget(Text(root, "target"));
            return new SoundRequest(action, tileId, profileId);
        }
        catch (JsonException) { return null; }
    }

    public static (string? ProfileId, string? TileId) SplitTarget(string target)
    {
        if (!target.StartsWith(TilePrefix, StringComparison.OrdinalIgnoreCase)) return (null, null);
        var rest = target[TilePrefix.Length..].Trim();
        if (rest.Length == 0) return (null, null);
        var slash = rest.IndexOf('/');
        return slash > 0 ? (rest[..slash], rest[(slash + 1)..]) : (null, rest);
    }

    private static string Text(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}
