namespace RedAnts.Show.Domain;

public enum GameSource { SwissUnihockey, File, Manual }

public sealed record Player(int Number, string Name, string Position = "", string? Photo = null, string? Sponsor = null);

public sealed record GameTeam(
    int? TeamId,
    string LongName,
    string ShortName,
    string Color,
    string? LogoUrl,
    string Coach,
    IReadOnlyList<Player> Players,
    IReadOnlyList<int> Starting6,
    IReadOnlyList<IReadOnlyList<int>> Lines,
    IReadOnlyList<int> Goalies,
    int? TopScorer = null)
{
    public const string HomeColor = "#D02D38";
    public const string AwayColor = "#2A6EBB";

    public static GameTeam Empty(string color) => new(null, "", "", color, null, "", [], [], [], []);

    public Player? Find(int number) => Players.FirstOrDefault(p => p.Number == number);

    public Player? TopScorerPlayer => TopScorer is { } number ? Find(number) : null;

    public bool HasStarting6 => Starting6.Count == 6 && Starting6.All(n => Find(n) is not null);

    public static string ShortNameOf(string longName)
    {
        var words = longName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var letters = string.Concat(words.Select(w => char.ToUpperInvariant(w[0])));
        if (letters.Length >= 3) return letters[..3];
        return longName.Length >= 3 ? longName[..3].ToUpperInvariant() : longName.ToUpperInvariant();
    }
}

public sealed record Game(
    string Id,
    GameSource Source,
    string Title,
    string League,
    DateTime? Date,
    string Location,
    GameTeam Home,
    GameTeam Away,
    string Referee1,
    string Referee2,
    string Commentator1,
    string Commentator2,
    DateTimeOffset LoadedAt)
{
    public static Game Manual(string id, string title)
    {
        ValidationException.ThrowIfBlank(id, nameof(Id), "Spielnummer");
        return new Game(id.Trim(), GameSource.Manual, title.Trim(), "", null, "",
            GameTeam.Empty(GameTeam.HomeColor), GameTeam.Empty(GameTeam.AwayColor), "", "", "", "", SwissTime.Timestamp);
    }

    public Game Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Spielnummer");
        foreach (var (team, field) in new[] { (Home, nameof(Home)), (Away, nameof(Away)) })
        {
            var duplicate = team.Players.GroupBy(p => p.Number).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
                throw new ValidationException(field, $"Nummer {duplicate.Key} ist im Kader doppelt.");
        }
        return this;
    }

    public GameTeam Side(string side) =>
        side.Equals("away", StringComparison.OrdinalIgnoreCase) ? Away : Home;

    public Game WithSide(string side, GameTeam team) =>
        side.Equals("away", StringComparison.OrdinalIgnoreCase) ? this with { Away = team } : this with { Home = team };

    public Game Merge(Game fresh) =>
        fresh with
        {
            Commentator1 = Commentator1.Length > 0 ? Commentator1 : fresh.Commentator1,
            Commentator2 = Commentator2.Length > 0 ? Commentator2 : fresh.Commentator2,
            Home = MergeTeam(Home, fresh.Home),
            Away = MergeTeam(Away, fresh.Away),
        };

    private static GameTeam MergeTeam(GameTeam kept, GameTeam fresh) =>
        fresh with
        {
            Coach = kept.Coach.Length > 0 ? kept.Coach : fresh.Coach,
            Starting6 = kept.Starting6.Count > 0 ? kept.Starting6 : fresh.Starting6,
            Lines = kept.Lines.Count > 0 ? kept.Lines : fresh.Lines,
            Goalies = kept.Goalies.Count > 0 ? kept.Goalies : fresh.Goalies,
            Players = fresh.Players.Select(p =>
                kept.Find(p.Number) is { } old && old.Name == p.Name ? p with { Photo = old.Photo, Sponsor = old.Sponsor } : p).ToList(),
        };
}
