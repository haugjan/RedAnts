using System.Text.Json;

namespace RedAnts.Show.Domain;

public static class DeckSharing
{
    public sealed record Library(IReadOnlyList<Deck> Decks, IReadOnlyList<PageSet> Sets);

    public static Library Share(IReadOnlyList<Deck> decks, IReadOnlyList<PageSet>? sets = null, bool tagNames = false)
    {
        var groups = new List<Group>();
        foreach (var set in sets ?? []) Of(groups, set.Columns, set.Rows).Seed(set.Pages);

        var moved = decks.Select(deck => Of(groups, deck.Columns, deck.Rows).Take(deck)).ToList();
        var shared = groups.Where(group => group.Any).Select(group => group.Set(tagNames)).ToList();
        return new Library(moved, shared);
    }

    private static Group Of(List<Group> groups, int columns, int rows)
    {
        if (groups.FirstOrDefault(group => group.Columns == columns && group.Rows == rows) is { } known) return known;
        var fresh = new Group(columns, rows);
        groups.Add(fresh);
        return fresh;
    }

    private sealed class Group(int columns, int rows)
    {
        private readonly List<DeckPage> _pages = [];
        private readonly Dictionary<string, string> _origin = new(StringComparer.OrdinalIgnoreCase);

        public int Columns => columns;

        public int Rows => rows;

        public bool Any => _pages.Count > 0;

        public void Seed(IReadOnlyList<DeckPage> pages)
        {
            foreach (var page in pages)
                if (!page.IsHome && Known(page.Id) is null) _pages.Add(page);
        }

        public Deck Take(Deck deck)
        {
            var own = deck.Pages.Where(page => !page.IsHome).ToList();
            var moved = Mapping(deck, own);
            foreach (var page in own)
            {
                var id = moved[page.Id];
                if (Known(id) is not null) continue;
                _pages.Add(Rewritten(page, moved) with { Id = id });
                _origin[id] = deck.Name;
            }
            var home = deck.Pages.FirstOrDefault(page => page.IsHome) ?? Deck.Empty(DeckPage.HomeId, "Startseite");
            return deck with { Pages = [Rewritten(home, moved)] };
        }

        public PageSet Set(bool tagNames) =>
            new(columns, rows, tagNames ? _pages.Select(Tagged).ToList() : _pages.ToList());

        private Dictionary<string, string> Mapping(Deck deck, IReadOnlyList<DeckPage> own)
        {
            var moved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var page in own) moved[page.Id] = page.Id;
            var apart = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                var split = own.FirstOrDefault(page =>
                    !apart.Contains(page.Id)
                    && Known(page.Id) is { } known
                    && !Alike(Rewritten(page, moved), known));
                if (split is null) return moved;
                apart.Add(split.Id);
                moved[split.Id] = Free($"{split.Id}-{deck.Id}", moved, own);
            }
        }

        private string Free(string wanted, Dictionary<string, string> moved, IReadOnlyList<DeckPage> own)
        {
            var id = wanted;
            while (Known(id) is not null
                   || moved.Values.Any(taken => taken.Equals(id, StringComparison.OrdinalIgnoreCase))
                   || own.Any(page => page.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                id += "x";
            return id;
        }

        private DeckPage Tagged(DeckPage page)
        {
            var name = page.Name.Trim();
            if (_pages.Count(other => other.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) < 2) return page;
            if (!_origin.TryGetValue(page.Id, out var deck) || deck.Trim().Length == 0) return page;
            var tag = deck.Trim();
            return name.EndsWith(tag, StringComparison.OrdinalIgnoreCase) ? page : page with { Name = $"{name} · {tag}" };
        }

        private DeckPage? Known(string id) =>
            _pages.FirstOrDefault(page => page.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        private static DeckPage Rewritten(DeckPage page, IReadOnlyDictionary<string, string> moved) =>
            page with
            {
                ParentId = page.ParentId is { } parent && moved.TryGetValue(parent, out var above) ? above : page.ParentId,
                Keys = page.Keys
                    .Select(key => key.PageId is { } target && moved.TryGetValue(target, out var opens)
                        ? key with { PageId = opens }
                        : key)
                    .ToList(),
            };

        private static bool Alike(DeckPage page, DeckPage other) => Shape(page) == Shape(other);

        private static string Shape(DeckPage page) =>
            JsonSerializer.Serialize(
                page.Keys.Where(key => !key.IsEmpty).OrderBy(key => key.Number).ToList(),
                ShowJson.Options);
    }
}
