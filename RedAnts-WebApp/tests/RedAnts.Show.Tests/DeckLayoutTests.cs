using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class DeckLayoutTests
{
    [Fact]
    public void The_hall_deck_keeps_back_and_panic_on_their_fixed_keys()
    {
        var deck = DefaultDecks.Field();

        Assert.Equal(32, deck.Count);
        Assert.Equal(DeckKeyKinds.Back, deck.KeyAt(DefaultDecks.BackKey).Kind);
        Assert.Equal("panic", deck.KeyAt(DefaultDecks.PanicKey).SceneId);
        Assert.Same(deck, deck.Validated());
    }

    [Fact]
    public void A_state_key_carries_one_row_per_value()
    {
        var overlay = DefaultDecks.Field().KeyAt(32);
        var state = DefaultStates.All.Single(s => s.Id == DefaultStates.Overlay);

        Assert.Equal(DeckKeyKinds.State, overlay.Kind);
        Assert.Equal(DefaultStates.Overlay, overlay.StateId);
        Assert.Equal(state.Values, overlay.Rows!.Select(r => r.Value));
        Assert.All(overlay.Rows!, row => Assert.Equal(DeckStateRow.RunScene, row.Behaviour));
    }

    [Fact]
    public void Every_scene_a_default_key_runs_exists()
    {
        var scenes = DefaultScenes.WithSystem(DefaultScenes.All).Select(s => s.Id).ToHashSet();
        var referenced = DefaultDecks.All
            .SelectMany(d => d.Keys)
            .SelectMany(k => new[] { k.SceneId }.Concat((k.Rows ?? []).Select(r => r.SceneId)))
            .OfType<string>()
            .Distinct();

        Assert.All(referenced, id => Assert.Contains(id, scenes));
    }

    [Fact]
    public void A_key_outside_the_grid_is_rejected()
    {
        var deck = new DeckLayout("mk2", "Mk2", 5, 3, [new DeckKey(16, DeckKeyKinds.Empty)]);

        Assert.Throws<ValidationException>(() => deck.Validated());
    }

    [Fact]
    public void Replacing_a_key_keeps_the_others_in_order()
    {
        var deck = DefaultDecks.Field().With(new DeckKey(1, DeckKeyKinds.Sound, "Einlauf", SoundId: "einlauf"));

        Assert.Equal("einlauf", deck.KeyAt(1).SoundId);
        Assert.Equal(deck.Keys.Select(k => k.Number).Order(), deck.Keys.Select(k => k.Number));
    }

    [Fact]
    public void A_hall_without_decks_falls_back_to_the_default_layouts()
    {
        var hall = Hall.Create("win4", "Win4");

        Assert.Equal(DefaultDecks.All.Count, hall.DeckLayouts.Count);
        Assert.Empty(hall.Rules);
    }
}
