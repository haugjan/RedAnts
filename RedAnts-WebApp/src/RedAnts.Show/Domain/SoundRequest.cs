using System.Text.Json;

namespace RedAnts.Show.Domain;

public sealed record SoundRequest(string Action, string? SoundId, string Scene)
{
    public const string Prefix = "sound:";

    public bool IsPlay => Action == SceneActions.SoundPlay;

    public static SoundRequest? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var action = Text(root, "action");
            if (!action.StartsWith("sound.", StringComparison.Ordinal) || !SceneActions.All.Contains(action)) return null;
            return new SoundRequest(action, SoundOf(Text(root, "target")), Text(root, "scene"));
        }
        catch (JsonException) { return null; }
    }

    public static string? SoundOf(string target) =>
        target.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) && target.Length > Prefix.Length
            ? target[Prefix.Length..].Trim()
            : null;

    private static string Text(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}
