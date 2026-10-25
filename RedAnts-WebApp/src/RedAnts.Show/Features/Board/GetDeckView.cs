using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Setup;

namespace RedAnts.Show.Features.Board;

public static class GetDeckView
{
    public sealed record Query(string HallId, string DeckId, string PageId = "");

    public sealed class Handler(IDeckMirror mirror, IHallRepository halls, IMusicLibrary music, IShowSetup setup)
    {
        public async Task<DeckView?> HandleAsync(Query query)
        {
            var hall = await halls.GetByIdAsync(query.HallId);
            if (hall is null) return null;
            var decks = (await setup.GetAsync()).DecksOf(hall);
            if ((decks.FirstOrDefault(d => d.Id == query.DeckId) ?? decks.FirstOrDefault()) is not { } deck) return null;
            if (mirror.IsConnected(query.HallId))
            {
                if (mirror.Known(query.HallId, deck.Id) is { } known) return known;
                if (await mirror.LiveAsync(query.HallId, deck) is { } live) return live;
            }
            return Local(deck, await music.AllAsync(), query.PageId);
        }

        public static DeckView Local(Deck deck, IReadOnlyList<MusicItem> music, string pageId = "")
        {
            var page = deck.Page(pageId) ?? deck.Home;
            var keys = new List<DeckKeyRow>();
            for (var number = 1; number <= deck.Count; number++)
            {
                if (number == deck.Panic)
                {
                    keys.Add(new DeckKeyRow(number, "PANIC", null, "#d01212", "#ffffff", true));
                    continue;
                }
                var key = page.KeyAt(number);
                if (number == deck.Back && (!page.IsHome || key.IsEmpty))
                {
                    var deep = !page.IsHome;
                    keys.Add(new DeckKeyRow(number, deep ? "◀ Zurück" : "", null, deep ? "#8ed973" : "#000000", "#111111", deep));
                    continue;
                }
                var item = key.Kind == KeyKinds.Music ? music.FirstOrDefault(m => m.Id == key.MusicId) : null;
                var label = key.Label.Length > 0 ? key.Label : item?.Name ?? "";
                keys.Add(new DeckKeyRow(
                    number,
                    label,
                    Picture(key.Icon),
                    key.IsEmpty ? "#000000" : key.Color,
                    "#ffffff",
                    key.Kind switch
                    {
                        KeyKinds.Music => item is not null,
                        KeyKinds.Live or KeyKinds.Empty => false,
                        _ => true,
                    },
                    item?.Id,
                    false,
                    Symbol(key.Icon),
                    item?.Subtitle ?? ""));
            }
            return new DeckView(deck.Id, page.Name, deck.Columns, deck.Rows, keys, false, null, null, page.Id);
        }

        private static string? Picture(string? icon) => DeckIcons.Known(icon) ? icon : null;

        private static string Symbol(string? icon) =>
            icon is { Length: > 0 } && !DeckIcons.Known(icon) ? icon : "";
    }
}
