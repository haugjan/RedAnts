using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using Xunit;

namespace RedAnts.Show.Tests.Board;

public class WebKeyTests
{
    [Fact]
    public void A_music_key_plays_in_the_browser() =>
        Assert.Equal("bully", WebKey.Of(new ButtonAction(KeyKinds.Music, MusicId: "bully")).MusicId);

    [Fact]
    public void Starting_six_plays_its_music_on_the_web_and_skips_the_player_choice() =>
        Assert.Equal("s6", WebKey.Of(new ButtonAction(KeyKinds.Special, Special: SpecialKey.Starting6Home,
            Steps: [new KeyStep(KeyActions.MusicPlay, "s6")])).MusicId);

    [Theory]
    [InlineData(SpecialKey.MusicStop, "stop")]
    [InlineData(SpecialKey.MusicPause, "pause")]
    [InlineData(SpecialKey.MusicNext, "next")]
    [InlineData(SpecialKey.MusicPrevious, "previous")]
    [InlineData(SpecialKey.MusicFade, "fade")]
    public void The_music_controls_work_on_the_web(string special, string control) =>
        Assert.Equal(control, WebKey.Of(new ButtonAction(KeyKinds.Special, Special: special)).Control);

    [Fact]
    public void A_key_for_the_led_wall_does_nothing_on_the_web() =>
        Assert.False(WebKey.Of(new ButtonAction(KeyKinds.Steps, Steps: [new KeyStep(KeyActions.LedStart, "led:pause")])).Works);

    [Fact]
    public void A_goal_without_music_does_nothing_on_the_web() =>
        Assert.False(WebKey.Of(new ButtonAction(KeyKinds.Special, Special: SpecialKey.GoalHome)).Works);
}
