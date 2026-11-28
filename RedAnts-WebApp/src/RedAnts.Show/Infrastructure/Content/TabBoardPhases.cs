using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class TabBoardPhases
{
    public const string Open = "#3a3f47";
    public const string Home = "#c8102e";
    public const string Away = "#1f4e79";

    public const string Warmup = "aufwaermmusik";
    public const string Intro = "intro";
    public const string BreakLoop = "led-pausenloop";
    public const string LidlWelcome = "lidl-welcome";
    public const string Gastro = "gastro-werbung";
    public const string Raffle = "gewinnspiel-werbung";
    public const string Entrance = "einlaufen";
    public const string Interruption = "spielunterbruch";
    public const string InterruptionAuto = "spielunterbruch-automatisch";
    public const string BreakGame = "werbung-pausenspiel";
    public const string LidlBreak = "lidl-pause";
    public const string BreakMusic = "musik-waehrend-pause";
    public const string BestHome = "best-player-red-ants";
    public const string BestAway = "best-player-gegner";
    public const string LidlEnd = "lidl-ende";
    public const string AfterMusic = "musik-nach-dem-spiel";

    public static string GoalNow(bool home) => home ? "tor-sofort-red-ants" : "tor-sofort-gegner";
    public static string GoalSpeaker(bool home) => home ? "tor-speaker-red-ants" : "tor-speaker-gegner";
    public static string PenaltyNow(bool home) => home ? "strafe-sofort-red-ants" : "strafe-sofort-gegner";
    public static string PenaltySpeaker(bool home) => home ? "strafe-speaker-red-ants" : "strafe-speaker-gegner";
    public static string Timeout(bool home) => home ? "timeout-red-ants" : "timeout-gegner";
    public static string EmptyNet(bool home) => home ? "empty-net-red-ants" : "empty-net-gegner";
    public static string StartingSix(bool home) => home ? "starting-six-red-ants" : "starting-six-gegner";

    public static IReadOnlyList<string> EntranceOrder() =>
        ["mobiliar-unihockey", "wartesong-bis-teams-bereit", LidlWelcome, "einlaufmusik", "torbeschwoerung", StartingSix(false), StartingSix(true), "bully"];

    public static TabBoard Arrange(TabBoard board, IReadOnlyList<(string Id, string Name)> music)
    {
        var buttons = board.Buttons.ToList();

        string? Music(string name) =>
            music.FirstOrDefault(m => Last(m.Name).Equals(name, StringComparison.OrdinalIgnoreCase)).Id
            ?? music.FirstOrDefault(m => Last(m.Name).Contains(name, StringComparison.OrdinalIgnoreCase)).Id;

        int IndexOf(string id) => buttons.FindIndex(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        void Rework(string id, Func<BoardButton, BoardButton> change)
        {
            var index = IndexOf(id);
            if (index >= 0) buttons[index] = change(buttons[index]);
        }

        void Rename(string from, string to, string label, string? category = null)
        {
            var free = IndexOf(to) < 0 || from.Equals(to, StringComparison.OrdinalIgnoreCase);
            Rework(from, b => b with { Id = free ? to : b.Id, Label = label, Category = category ?? b.Category });
        }

        void Add(BoardButton button)
        {
            if (IndexOf(button.Id) < 0) buttons.Add(button);
        }

        ButtonAction Steps(params KeyStep[] steps) => new(KeyKinds.Steps, Steps: steps);

        KeyStep? Play(string name) => Music(name) is { } id ? new KeyStep(KeyActions.MusicPlay, id) : null;

        KeyStep[] Some(params KeyStep?[] steps) => steps.OfType<KeyStep>().ToArray();

        Rename("aufwaermmusik-vor-spiel", Warmup, "Aufwärmmusik", Categories.Before);
        Rename("led-pause", BreakLoop, "LED Pausenloop", TabBoardStart.Categories.Led);
        Rename("lidl", LidlWelcome, "LIDL Welcome");
        Rework("werbung-gastro", b => b with
        {
            Label = "Gastro Werbung",
            Action = Music("Gastro Werbung") is { } gastro ? b.Action with { MusicId = gastro } : b.Action,
            Leagues = null,
            Category = Categories.Break,
        });
        Rename("werbung-gastro", Gastro, "Gastro Werbung");
        Rename("lidl-pausendurchsage", LidlBreak, "LIDL Pause");
        Rename("lidl-nach-spiel", LidlEnd, "LIDL Ende");
        Rework("pausenmusik", b => b with { Leagues = null });
        Rename("pausenmusik", BreakMusic, "Musik während Pause");
        Rename("nach-spiel-sieg-red-ants", AfterMusic, "Musik nach dem Spiel");
        Rename("timeout-heim", Timeout(true), "Timeout Red Ants", Categories.During);
        Rename("timeout-gast", Timeout(false), "Timeout Gegner", Categories.During);
        Rename("tor-heim", GoalSpeaker(true), "Tor Speaker Red Ants", Categories.During);
        Rename("tor-gast", GoalSpeaker(false), "Tor Speaker Gegner", Categories.During);
        Rename("strafe-heim", PenaltySpeaker(true), "Strafe Speaker Red Ants", Categories.During);
        Rename("strafe-gast", PenaltySpeaker(false), "Strafe Speaker Gegner", Categories.During);
        Rename("best-player-heim", BestHome, "Best Player Red Ants", Categories.After);
        Rename("best-player-gast", BestAway, "Best Player Gegner", Categories.After);

        foreach (var id in new[] { GoalSpeaker(true), GoalSpeaker(false), PenaltySpeaker(true), PenaltySpeaker(false) })
            Rework(id, b => b with { Action = Silent(b.Action), PerLeague = b.PerLeague?.ToDictionary(v => v.Key, v => Silent(v.Value)) });

        foreach (var home in new[] { true, false })
        {
            var special = home ? SpecialKey.Starting6Home : SpecialKey.Starting6Away;
            Rework(StartingSix(home), b => b with
            {
                Action = WithSpecial(b.Action, special),
                PerLeague = b.PerLeague?.ToDictionary(v => v.Key, v => WithSpecial(v.Value, special)),
            });
        }

        Add(new BoardButton(Intro, "Intro", Open, Steps(new KeyStep(KeyActions.LedStart, "led:intro")), Category: Categories.Before));
        Add(new BoardButton(Raffle, "Gewinnspiel Werbung", Open, Steps(), Category: Categories.Before));
        Add(new BoardButton(BreakGame, "Werbung Pausenspiel", Open, Steps(), Category: Categories.Break));
        foreach (var home in new[] { true, false })
        {
            var side = home ? "Red Ants" : "Gegner";
            Add(new BoardButton(GoalNow(home), $"Tor sofort {side}", home ? Home : Away,
                Steps(Some(Play($"Tor {side}"), new KeyStep(KeyActions.LedStart, home ? "led:tor-heim" : "led:tor-gast"))),
                Category: Categories.During));
            Add(new BoardButton(PenaltyNow(home), $"Strafe sofort {side}", home ? Home : Away,
                Steps(Some(Play($"Strafe {side}"))), Category: Categories.During));
            Add(new BoardButton(EmptyNet(home), $"Empty Net {side}", Open, Steps(), Category: Categories.During));
        }

        var arranged = board with { Buttons = buttons };
        return arranged with { Layouts = Layouts(arranged) };
    }

    private static ButtonAction WithSpecial(ButtonAction action, string special) =>
        action.Kind == KeyKinds.Music && action.MusicId is { } id
            ? new ButtonAction(KeyKinds.Special, Special: special, Steps: [new KeyStep(KeyActions.MusicPlay, id)])
            : action;

    private static ButtonAction Silent(ButtonAction action) =>
        action with { Steps = action.Sequence.Where(s => s.Action != KeyActions.MusicPlay).ToList() };

    private static string Last(string name) => name.Split(" · ").Last().Trim();

    private static IReadOnlyList<TabLayout> Layouts(TabBoard board)
    {
        var deck = board.Deck(TabBoardStart.XlDeck);
        bool Free(int number) => deck is null || !deck.Reserved(number);

        BoardLayout Keys(params (int Number, string Button)[] keys) =>
            new(Keys: keys.Where(k => Free(k.Number) && board.Button(k.Button) is not null).Select(k => new Placement(k.Number, k.Button)).ToList());

        var entrance = EntranceOrder().Where(id => board.Button(id) is not null).ToList();
        IReadOnlyList<NumberedRun> runs = entrance.Count > 0 && Enumerable.Range(9, entrance.Count).All(Free)
            ? [new NumberedRun(Entrance, "Einlaufen", 9, entrance)]
            : [];

        var before = Keys((1, Warmup), (2, Intro), (3, BreakLoop), (4, LidlWelcome), (5, Gastro), (6, Raffle)) with { Runs = runs };
        var during = Keys(
            (1, Interruption), (2, InterruptionAuto),
            (9, GoalNow(true)), (10, GoalSpeaker(true)), (11, PenaltyNow(true)), (12, PenaltySpeaker(true)),
            (18, Timeout(true)), (19, EmptyNet(true)),
            (13, GoalNow(false)), (14, GoalSpeaker(false)), (15, PenaltyNow(false)), (16, PenaltySpeaker(false)),
            (21, Timeout(false)), (22, EmptyNet(false)));
        var pause = Keys((1, BreakGame), (2, Gastro), (3, LidlBreak), (4, BreakLoop), (5, BreakMusic));
        var after = Keys((1, BestHome), (2, BestAway), (3, LidlEnd), (4, AfterMusic));

        return
        [
            new TabLayout(BoardTabs.Before, 8, 4, before),
            new TabLayout(BoardTabs.During, 8, 4, during),
            new TabLayout(BoardTabs.Break, 8, 4, pause),
            new TabLayout(BoardTabs.After, 8, 4, after),
        ];
    }

    public static class Categories
    {
        public const string Before = TabBoardStart.Categories.Before;
        public const string During = "Während dem Spiel";
        public const string Break = TabBoardStart.Categories.Break;
        public const string After = TabBoardStart.Categories.After;
    }
}
