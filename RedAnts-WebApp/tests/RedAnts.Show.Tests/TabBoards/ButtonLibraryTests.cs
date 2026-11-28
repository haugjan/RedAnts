using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;
using Xunit;

namespace RedAnts.Show.Tests.TabBoards;

public class ButtonLibraryTests
{
    private static BoardButton Button(string id, string label) =>
        new(id, label, DeckKey.DefaultColor, new ButtonAction(KeyKinds.Music, MusicId: id));

    private static readonly BoardButton[] Buttons =
    [
        Button("tor-heim", "Tor Heim"),
        Button("einlauf", "Einlaufmusik"),
        Button("mobiliar", "Mobiliar"),
    ];

    [Fact]
    public void Without_search_lists_every_button_alphabetically()
    {
        var found = ButtonLibrary.Find(Buttons, "");

        Assert.Equal(["Einlaufmusik", "Mobiliar", "Tor Heim"], found.Select(b => b.Label));
    }

    [Fact]
    public void Search_matches_label_ignoring_case()
    {
        var found = ButtonLibrary.Find(Buttons, "  heim ");

        Assert.Equal("tor-heim", Assert.Single(found).Id);
    }

    [Fact]
    public void Search_matches_id()
    {
        var found = ButtonLibrary.Find(Buttons, "einl");

        Assert.Equal("einlauf", Assert.Single(found).Id);
    }

    [Theory]
    [InlineData("#2b3038", true)]
    [InlineData("#ABCDEF", true)]
    [InlineData("2b3038", false)]
    [InlineData("#2b30", false)]
    [InlineData("#zzzzzz", false)]
    public void Accepts_only_six_digit_hex_colours(string color, bool valid) =>
        Assert.Equal(valid, ButtonLibrary.IsColor(color));

    [Fact]
    public void Normalizing_turns_empty_league_choices_into_all_leagues()
    {
        var button = Button("lidl", " LIDL ") with
        {
            Leagues = [],
            PerLeague = new Dictionary<string, ButtonAction>(),
            State = "",
        };

        var normalized = ButtonLibrary.Normalized(button);

        Assert.Equal("LIDL", normalized.Label);
        Assert.Null(normalized.Leagues);
        Assert.Null(normalized.PerLeague);
        Assert.Null(normalized.State);
    }

    [Fact]
    public void Normalizing_keeps_chosen_leagues_and_variants()
    {
        var variant = new ButtonAction(KeyKinds.Music, MusicId: "einlauf-u21");
        var button = Button("einlauf", "Einlauf") with
        {
            Leagues = ["lupl"],
            PerLeague = new Dictionary<string, ButtonAction> { ["u21"] = variant },
            State = DefaultStates.Music,
        };

        var normalized = ButtonLibrary.Normalized(button);

        Assert.Equal(["lupl"], normalized.Leagues!);
        Assert.Same(variant, normalized.PerLeague!["u21"]);
        Assert.Equal(DefaultStates.Music, normalized.State);
    }
}
