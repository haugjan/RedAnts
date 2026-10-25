using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Setup;

namespace RedAnts.Show.Features.Board;

public static class GetDeckView
{
    public sealed record Query(string HallId, string DeckId, string PageId = "", string Title = "");

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
            return Local(deck, await music.AllAsync(), query.PageId, hall.Offers, query.Title);
        }

        public static DeckView Local(Deck deck, IReadOnlyList<MusicItem> music, string pageId = "", HallCapabilities? offered = null, string title = "")
        {
            var offers = offered ?? HallCapabilities.Default;
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
                    keys.Add(deep
                        ? new DeckKeyRow(number, "◀ Zurück", null, "#1e7b34", "#ffffff", true, Top: title.Length > 0 ? title : page.Name)
                        : new DeckKeyRow(number, "", null, "#000000", "#111111", false));
                    continue;
                }
                if (!Offered(key, offers))
                {
                    keys.Add(new DeckKeyRow(number, "", null, "#000000", "#ffffff", false));
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
            return new DeckView(deck.Id, title.Length > 0 ? title : page.Name, deck.Columns, deck.Rows, keys, false, null, null, page.Id);
        }

        public static bool Offered(DeckKey key, HallCapabilities offers) => key.Kind switch
        {
            KeyKinds.Music => offers.Sound,
            KeyKinds.Special => SpecialKey.Offered(key.Special ?? "", offers.Sound, offers.Clock, offers.Vmix),
            KeyKinds.Steps => key.Sequence.Count == 0 || key.Sequence.Any(step => Runs(step, offers)),
            KeyKinds.Live => key.Live switch
            {
                LiveValues.Time or LiveValues.Period or LiveValues.HomeScore or LiveValues.AwayScore => offers.Clock,
                LiveValues.Overlay => offers.Vmix,
                _ => true,
            },
            _ => true,
        };

        private static bool Runs(KeyStep step, HallCapabilities offers) => step.Action switch
        {
            KeyActions.MusicPlay or KeyActions.MusicStop or KeyActions.MusicFade => offers.Sound,
            KeyActions.VmixShow or KeyActions.VmixText or KeyActions.VmixHide => offers.Vmix,
            KeyActions.LedStart or KeyActions.LedStop => offers.Led,
            _ => false,
        };

        private static string? Picture(string? icon) => DeckIcons.Known(icon) ? icon : null;

        private static string Symbol(string? icon) =>
            icon is { Length: > 0 } && !DeckIcons.Known(icon) ? icon : "";
    }
}
