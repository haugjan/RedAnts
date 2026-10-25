using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Music;
using Xunit;

namespace RedAnts.Show.Tests;

public class HallOfferTests
{
    private static readonly MusicItem Piece = new("tor", "Tor Red Ants", "", 1, false, "[]");

    private static readonly HallCapabilities SoundOnly = new(Sound: true, Clock: false, Vmix: false, Led: false);

    private static Deck Deck(params DeckKey[] keys) =>
        new("mk2", "Mk2", 5, 3, [new DeckPage(DeckPage.HomeId, "Startseite", null, keys)], PanicKey: 11, BackKey: 1);

    private static DeckKeyRow KeyOf(DeckView view, int number) => view.Keys.First(k => k.Number == number);

    [Fact]
    public void The_break_music_switch_needs_a_match_clock()
    {
        Assert.True(SpecialKey.Offered(SpecialKey.AutoBreakMusic, sound: true, clock: true, vmix: false));
        Assert.False(SpecialKey.Offered(SpecialKey.AutoBreakMusic, sound: true, clock: false, vmix: false));
        Assert.False(SpecialKey.Offered(SpecialKey.AutoBreakMusic, sound: false, clock: true, vmix: false));
    }

    [Fact]
    public void An_insert_needs_vmix_and_the_music_control_needs_sound()
    {
        Assert.False(SpecialKey.Offered(SpecialKey.GoalHome, sound: true, clock: true, vmix: false));
        Assert.True(SpecialKey.Offered(SpecialKey.GoalHome, sound: false, clock: false, vmix: true));
        Assert.False(SpecialKey.Offered(SpecialKey.MusicFade, sound: false, clock: true, vmix: true));
        Assert.True(SpecialKey.Offered(SpecialKey.MusicFade, sound: true, clock: false, vmix: false));
    }

    [Fact]
    public void A_hall_with_sound_only_keeps_its_music_keys_and_hides_the_rest()
    {
        var deck = Deck(
            new DeckKey(2, KeyKinds.Music, "Spielunterbruch", MusicId: "tor"),
            new DeckKey(3, KeyKinds.Special, "Automodus", Special: SpecialKey.AutoBreakMusic),
            new DeckKey(4, KeyKinds.Special, "Stopp", Special: SpecialKey.MusicStop),
            new DeckKey(5, KeyKinds.Special, "Tor", Special: SpecialKey.GoalHome),
            new DeckKey(6, KeyKinds.Live, "Stand Heim", Live: LiveValues.HomeScore));

        var view = GetDeckView.Handler.Local(deck, [Piece], "", SoundOnly);

        Assert.Equal("Spielunterbruch", KeyOf(view, 2).Label);
        Assert.Equal("", KeyOf(view, 3).Label);
        Assert.Equal("Stopp", KeyOf(view, 4).Label);
        Assert.Equal("", KeyOf(view, 5).Label);
        Assert.Equal("", KeyOf(view, 6).Label);
        Assert.False(KeyOf(view, 3).Pressable);
        Assert.False(KeyOf(view, 5).Pressable);
    }

    [Fact]
    public void A_sequence_stays_as_long_as_one_of_its_steps_runs_in_this_hall()
    {
        var both = new DeckKey(2, KeyKinds.Steps, "Tor mit Jingle", Steps:
        [
            new KeyStep(KeyActions.MusicPlay, "tor"),
            new KeyStep(KeyActions.VmixShow, "title:goal"),
        ]);
        var onlyVmix = new DeckKey(3, KeyKinds.Steps, "Opener", Steps: [new KeyStep(KeyActions.VmixShow, "title:opener")]);
        var onlyLed = new DeckKey(4, KeyKinds.Steps, "LED Pause", Steps: [new KeyStep(KeyActions.LedStart, "led:pause")]);

        var view = GetDeckView.Handler.Local(Deck(both, onlyVmix, onlyLed), [Piece], "", SoundOnly);

        Assert.Equal("Tor mit Jingle", KeyOf(view, 2).Label);
        Assert.Equal("", KeyOf(view, 3).Label);
        Assert.Equal("", KeyOf(view, 4).Label);
    }

    [Fact]
    public void A_hall_with_everything_shows_every_key()
    {
        var deck = Deck(
            new DeckKey(2, KeyKinds.Special, "Automodus", Special: SpecialKey.AutoBreakMusic),
            new DeckKey(3, KeyKinds.Special, "Tor", Special: SpecialKey.GoalHome),
            new DeckKey(4, KeyKinds.Live, "Stand Heim", Live: LiveValues.HomeScore));

        var view = GetDeckView.Handler.Local(deck, [Piece]);

        Assert.Equal("Automodus", KeyOf(view, 2).Label);
        Assert.Equal("Tor", KeyOf(view, 3).Label);
        Assert.Equal("Stand Heim", KeyOf(view, 4).Label);
    }
}
