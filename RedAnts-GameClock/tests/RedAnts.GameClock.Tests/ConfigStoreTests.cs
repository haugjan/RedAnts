using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RedAnts.GameClock.Configuration;
using RedAnts.GameClock.Vmix;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class ConfigStoreTests : IDisposable
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"gameclock-{Guid.NewGuid():n}");

    [Fact]
    public void SeedsFromAppSettingsWhenNothingIsStoredYet()
    {
        var store = Store(Seed());

        Assert.False(store.Current.Configured);
        Assert.Equal(50085, store.Current.Clock.Port);
        Assert.Equal("172.20.1.143", store.Current.Clock.SourceIp);
        Assert.Equal("Red Ants", store.Current.Teams.Home.Name);
        Assert.Equal("UH Berner Oberland", store.Current.Teams.ByAbbreviation["BEO"].Name);
    }

    [Fact]
    public async Task KeepsTheSavedSettingsForTheNextStart()
    {
        var first = Store(Seed());
        var config = first.Current.Copy();
        config.Clock.Port = 50099;
        config.Clock.Protocol = "delimited";
        config.Vmix.Enabled = true;
        config.Vmix.Host = "10.0.0.5";
        config.Teams.ByAbbreviation["RED"] = new TeamEntry { Name = "Red Ants Winterthur", Logo = "/teamlogo/team-1.png" };
        await first.SaveAsync(config);

        var second = Store(Seed());

        Assert.True(second.Current.Configured);
        Assert.Equal(50099, second.Current.Clock.Port);
        Assert.Equal("delimited", second.Current.Clock.Protocol);
        Assert.True(second.Current.Vmix.Enabled);
        Assert.Equal("10.0.0.5", second.Current.Vmix.Host);
        Assert.Equal("Red Ants Winterthur", second.Current.Teams.ByAbbreviation["RED"].Name);
    }

    [Fact]
    public async Task RaisesChangedSoTheFeedRebinds()
    {
        var store = Store(Seed());
        var raised = 0;
        store.Changed += () => raised++;

        await store.SaveAsync(store.Current.Copy());

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task FallsBackToTheSeedWhenTheFileIsBroken()
    {
        var store = Store(Seed());
        await File.WriteAllTextAsync(store.File, "{ this is not json");

        var reopened = Store(Seed());

        Assert.False(reopened.Current.Configured);
        Assert.Equal(50085, reopened.Current.Clock.Port);
    }

    [Fact]
    public void ShipsTheLuplMappingSoVmixWorksWithoutTyping()
    {
        var store = Store(Seed());

        Assert.Equal(VmixSlots.LuplInput, store.Current.Vmix.Input);
        Assert.Equal("TxtClockTime{0}.Text", store.Current.Vmix.Fields[VmixSlots.Time]);
        Assert.False(store.Current.Vmix.Enabled);
    }

    [Fact]
    public void CopyDoesNotShareTheTeamTable()
    {
        var store = Store(Seed());
        var copy = store.Current.Copy();
        copy.Teams.ByAbbreviation["XYZ"] = new TeamEntry { Name = "Neu" };
        copy.Teams.Home.Name = "Anders";

        Assert.False(store.Current.Teams.ByAbbreviation.ContainsKey("XYZ"));
        Assert.Equal("Red Ants", store.Current.Teams.Home.Name);
    }

    ConfigStore Store(GameClockOptions seed)
    {
        var options = Options.Create(seed);
        return new ConfigStore(options, new DataFolder(options, new TestEnvironment()), NullLogger<ConfigStore>.Instance);
    }

    GameClockOptions Seed() => new()
    {
        Port = 50085,
        Ip = "172.20.1.143",
        DataDir = _folder,
        Home = new TeamInfo { Name = "Red Ants", Logo = "logos/redants.png" },
        Guests = { ["BEO"] = new TeamInfo { Name = "UH Berner Oberland", Logo = "logos/beo.webp" } },
    };

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "RedAnts.GameClock.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
