using System.Text.Json;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Features.Agents;

public sealed record DeckKeyRow(int Number, string Label, string? Icon, string Color, string TextColor, bool Pressable, string? SoundId);

public sealed record DeckView(string DeckId, string Title, int Columns, IReadOnlyList<DeckKeyRow> Keys, bool FromAgent);

public static class GetDeckView
{
    public sealed record Query(string HallId, string DeckId);

    public sealed class Handler(IAgentConnections connections, IAgentChannel channel, IHallRepository halls, ISounds sounds)
    {
        public async Task<DeckView?> HandleAsync(Query query)
        {
            var hall = await halls.GetByIdAsync(query.HallId);
            if (hall is null) return null;
            var layout = hall.DeckLayouts.FirstOrDefault(d => d.Id == query.DeckId) ?? hall.DeckLayouts[0];
            if (connections.IsConnected(query.HallId) && await Remote(query, layout) is { } live) return live;
            return Local(layout, await sounds.GetAllAsync());
        }

        private async Task<DeckView?> Remote(Query query, DeckLayout layout)
        {
            var json = await channel.ViewAsync(query.HallId, query.DeckId);
            if (json is not { } view || view.ValueKind != JsonValueKind.Object) return null;
            if (!view.TryGetProperty("slots", out var slots) || slots.ValueKind != JsonValueKind.Array) return null;
            var keys = slots.EnumerateArray().Select(slot => new DeckKeyRow(
                slot.GetProperty("number").GetInt32(),
                slot.TryGetProperty("label", out var label) ? label.GetString() ?? "" : "",
                slot.TryGetProperty("image", out var image) ? IconOf(image.GetString()) : null,
                Hex(slot, "bgcolor"),
                Hex(slot, "color"),
                slot.TryGetProperty("pressable", out var pressable) && pressable.GetBoolean(),
                null)).ToList();
            var title = view.TryGetProperty("title", out var t) ? t.GetString() ?? layout.Name : layout.Name;
            return new DeckView(query.DeckId, title, layout.Columns, keys, true);
        }

        private static DeckView Local(DeckLayout layout, IReadOnlyList<SoundCommand> sounds)
        {
            var keys = new List<DeckKeyRow>();
            for (var number = 1; number <= layout.Count; number++)
            {
                var key = layout.KeyAt(number);
                if (key is null || key.Kind == DeckKeyKinds.Empty)
                {
                    keys.Add(new DeckKeyRow(number, "", null, "#000000", "#ffffff", false, null));
                    continue;
                }
                var sound = key.Kind == DeckKeyKinds.Sound ? sounds.FirstOrDefault(s => s.Id == key.SoundId) : null;
                keys.Add(new DeckKeyRow(
                    number,
                    key.Label.Length > 0 ? key.Label : sound?.Label ?? "",
                    key.Icon,
                    key.Color.Length > 0 ? key.Color : "#333333",
                    "#ffffff",
                    sound is not null,
                    sound?.Id));
            }
            return new DeckView(layout.Id, layout.Name, layout.Columns, keys, false);
        }

        private static string? IconOf(string? imageKey) =>
            imageKey is { Length: > 0 } key && key.Split('|') is { Length: 3 } parts && parts[0].Length > 0 ? parts[0] : null;

        private static string Hex(JsonElement slot, string property) =>
            slot.TryGetProperty(property, out var value) && value.TryGetInt32(out var color)
                ? "#" + color.ToString("X6")
                : "#000000";
    }
}
