using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Deck;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Scenes;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class DeckTests
{
    static readonly IReadOnlyList<Scene> Scenes =
    [
        new("tor-heim", "Tor Heim", "home", [new(SceneActions.DeckContext, "GoalHome"), new(SceneActions.PlayerPick), new(SceneActions.DeckContext, "Main")]),
        new("strafe-heim", "Strafe Heim", "home", [new(SceneActions.DeckContext, "PlayerHome"), new(SceneActions.PlayerPick), new(SceneActions.DeckContext, "Main")]),
        new("mitteilung", "Mitteilung", "", []),
        new("panic", "PANIC", "", [new(SceneActions.DeckContext, "Main")]),
    ];

    static readonly Game Game = Game.None with
    {
        Home = GameTeam.Empty with
        {
            LongName = "Red Ants Winterthur",
            ShortName = "RAW",
            Players = [new Player(41, "Livia Werz"), new Player(7, "Nora Stark")],
        },
    };

    static (Deck.Deck Deck, SceneRunner Runner, PayloadStore Payload) Build()
    {
        var payload = new PayloadStore(NullLogger<PayloadStore>.Instance);
        payload.Apply(new AgentPayload(Hall.Offline with { Id = "test", Shortcuts = ["Technische Störung", "Highlights"] }, Scenes, Game, DateTimeOffset.Now));
        var live = new LiveGame();
        var vmix = new VmixLink(payload, live, NullLogger<VmixLink>.Instance);
        var led = new SednaLink(payload, NullLogger<SednaLink>.Instance);
        var states = new ShowStates(payload, NullLogger<ShowStates>.Instance);
        var runner = new SceneRunner(payload, live, states, vmix, led, NullLogger<SceneRunner>.Instance);
        var deck = new Deck.Deck(payload, live, states, runner, vmix, led, NullLogger<Deck.Deck>.Instance);
        return (deck, runner, payload);
    }

    static JsonElement View(Deck.Deck deck) => JsonSerializer.SerializeToElement(deck.View());

    static JsonElement Slot(JsonElement view, int number) =>
        view.GetProperty("slots").EnumerateArray().Single(s => s.GetProperty("number").GetInt32() == number);

    [Fact]
    public void Main_page_has_32_slots_with_fixed_panic_key()
    {
        var (deck, _, _) = Build();
        var view = View(deck);

        Assert.Equal(32, view.GetProperty("slots").GetArrayLength());
        Assert.Equal("main", view.GetProperty("context").GetString());
        Assert.Contains("PANIC", Slot(view, 25).GetProperty("label").GetString());
        Assert.Equal("Tor", Slot(view, 1).GetProperty("label").GetString());
        Assert.True(Slot(view, 1).GetProperty("pressable").GetBoolean());
        Assert.StartsWith("ball|Tor|", Slot(view, 1).GetProperty("image").GetString());
    }

    [Fact]
    public void Keys_without_a_configured_scene_are_not_pressable()
    {
        var (deck, _, _) = Build();
        var coach = Slot(View(deck), 2);

        Assert.False(coach.GetProperty("pressable").GetBoolean());
        Assert.Equal("Coach", coach.GetProperty("label").GetString());
    }

    [Fact]
    public async Task Goal_opens_the_player_page_and_a_pick_returns_to_main()
    {
        var (deck, runner, _) = Build();

        await deck.PressAsync(1);
        await WaitUntil(() => runner.WantsPick);
        var players = View(deck);
        Assert.Equal("goalhome", players.GetProperty("context").GetString());
        Assert.Equal("41\nLivia Werz", Slot(players, 1).GetProperty("label").GetString());
        Assert.Equal("Eigentor", Slot(players, 27).GetProperty("label").GetString());
        Assert.Equal("◀ Zurück", Slot(players, 17).GetProperty("label").GetString());
        Assert.Equal("Tor Heim", Slot(players, 26).GetProperty("label").GetString());

        await deck.PressAsync(1);
        await WaitUntil(() => runner.Running is null);

        Assert.Equal("main", View(deck).GetProperty("context").GetString());
    }

    [Fact]
    public async Task Penalty_page_offers_the_four_floorball_penalties()
    {
        var (deck, runner, _) = Build();
        await deck.PressAsync(9);
        var view = View(deck);

        Assert.Equal("penaltyhome", view.GetProperty("context").GetString());
        Assert.Equal(["2'", "2'+2'", "10'", "Match"], Enumerable.Range(1, 4).Select(n => Slot(view, n).GetProperty("label").GetString()));

        await deck.PressAsync(2);
        await WaitUntil(() => runner.WantsPick);
        Assert.Equal("playerhome", View(deck).GetProperty("context").GetString());

        await deck.PressAsync(25);
        await WaitUntil(() => runner.Running is null && !runner.WantsPick);
        Assert.Equal("main", View(deck).GetProperty("context").GetString());
    }

    [Fact]
    public async Task Message_page_lists_the_hall_shortcuts()
    {
        var (deck, _, _) = Build();
        await deck.PressAsync(18);
        var view = View(deck);

        Assert.Equal("message", view.GetProperty("context").GetString());
        Assert.Equal("Technische\nStörung", Slot(view, 1).GetProperty("label").GetString());
        Assert.Equal("Highlights", Slot(view, 2).GetProperty("label").GetString());
        Assert.False(Slot(view, 3).GetProperty("pressable").GetBoolean());
    }

    [Fact]
    public async Task Long_poll_returns_once_the_version_moves()
    {
        var (deck, _, _) = Build();
        var before = deck.Version;
        var wait = deck.WaitAsync(before, TimeSpan.FromSeconds(5), CancellationToken.None);

        await deck.PressAsync(9);

        await wait;
        Assert.True(deck.Version > before);
    }

    [Fact]
    public void Icons_compose_a_png_with_the_label()
    {
        var image = DeckIcons.Get(DeckIcons.Tor, "Tor", 0x1A1A1A);

        Assert.NotNull(image);
        var bytes = Convert.FromBase64String(image);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], bytes[..4]);
    }

    static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition());
    }
}
