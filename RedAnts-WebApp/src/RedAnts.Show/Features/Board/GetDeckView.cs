using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Features.Board;

public static class GetDeckView
{
    public sealed record Query(string HallId, string DeckId);

    public sealed class Handler(IDeckMirror mirror, IHallRepository halls, IMusicLibrary music)
    {
        public async Task<DeckView?> HandleAsync(Query query)
        {
            var hall = await halls.GetByIdAsync(query.HallId);
            if (hall is null) return null;
            var deck = hall.DeckLayouts.FirstOrDefault(d => d.Id == query.DeckId) ?? hall.DeckLayouts[0];
            if (mirror.IsConnected(query.HallId))
            {
                if (mirror.Known(query.HallId, deck.Id) is { } known) return known;
                if (await mirror.LiveAsync(query.HallId, deck) is { } live) return live;
            }
            return Local(deck, await music.AllAsync());
        }

        public static DeckView Local(Deck deck, IReadOnlyList<MusicItem> music)
        {
            var page = deck.Home;
            var keys = new List<DeckKeyRow>();
            for (var number = 1; number <= deck.Count; number++)
            {
                if (number == deck.Panic)
                {
                    keys.Add(new DeckKeyRow(number, "PANIC", null, "#d01212", "#ffffff", true));
                    continue;
                }
                if (number == deck.Back)
                {
                    keys.Add(new DeckKeyRow(number, "", null, "#000000", "#ffffff", false));
                    continue;
                }
                var key = page.KeyAt(number);
                var item = key.Kind == KeyKinds.Music ? music.FirstOrDefault(m => m.Id == key.MusicId) : null;
                var label = key.Label.Length > 0 ? key.Label : item?.Name ?? "";
                keys.Add(new DeckKeyRow(
                    number,
                    label,
                    Picture(key.Icon),
                    key.IsEmpty ? "#000000" : key.Color,
                    "#ffffff",
                    key.Kind == KeyKinds.Music ? item is not null : !key.IsEmpty,
                    item?.Id,
                    false,
                    Symbol(key.Icon),
                    item?.Subtitle ?? ""));
            }
            return new DeckView(deck.Id, page.Name, deck.Columns, deck.Rows, keys, false);
        }

        private static string? Picture(string? icon) =>
            icon is { Length: > 0 } && DeckIcons.All.Contains(icon) ? icon : null;

        private static string Symbol(string? icon) =>
            icon is { Length: > 0 } && !DeckIcons.All.Contains(icon) ? icon : "";
    }
}
