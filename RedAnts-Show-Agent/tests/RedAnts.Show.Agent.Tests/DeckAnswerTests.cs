using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Scenes;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class DeckAnswerTests
{
    private static (Deck.Deck Deck, LiveGame Live) Build()
    {
        var payload = new PayloadStore(NullLogger<PayloadStore>.Instance);
        payload.Apply(new AgentPayload(Hall.Offline with { Id = "win4" }, [], null, DateTimeOffset.Now, []));
        var live = new LiveGame();
        var states = new ShowStates(payload, NullLogger<ShowStates>.Instance);
        var vmix = new VmixLink(payload, live, NullLogger<VmixLink>.Instance);
        var led = new SednaLink(payload, NullLogger<SednaLink>.Instance);
        var runner = new SceneRunner(payload, live, states, vmix, led, NullLogger<SceneRunner>.Instance);
        return (new Deck.Deck(payload, live, states, runner, vmix, led, NullLogger<Deck.Deck>.Instance), live);
    }

    private static JsonElement Json(object view) =>
        JsonDocument.Parse(JsonSerializer.Serialize(view, PayloadJson.Options)).RootElement;

    [Fact]
    public void A_first_request_carries_the_keys()
    {
        var (deck, _) = Build();

        var view = Json(deck.View(DeckLayout.FieldDeck, 0));

        Assert.Equal(32, view.GetProperty("slots").GetArrayLength());
        Assert.True(view.TryGetProperty("live", out _));
        Assert.True(view.TryGetProperty("states", out _));
    }

    [Fact]
    public void A_request_with_the_known_version_leaves_the_keys_out()
    {
        var (deck, _) = Build();
        var version = deck.Version;

        var view = Json(deck.View(DeckLayout.FieldDeck, version));

        Assert.False(view.TryGetProperty("slots", out _));
        Assert.Equal(version, view.GetProperty("version").GetInt64());
        Assert.True(view.TryGetProperty("live", out _));
    }

    [Fact]
    public void A_moved_clock_brings_the_keys_back()
    {
        var (deck, live) = Build();
        var version = deck.Version;

        live.Update(ICastProtocol.Parse("12:34;3;1;2;;;;;;;RA;GEG;GAME TIME")!);

        var view = Json(deck.View(DeckLayout.FieldDeck, version));
        Assert.True(view.TryGetProperty("slots", out _));
        Assert.Equal("12:34", view.GetProperty("live").GetProperty("time").GetString());
    }
}
