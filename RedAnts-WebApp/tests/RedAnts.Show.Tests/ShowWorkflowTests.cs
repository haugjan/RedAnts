using RedAnts.Domain;
using RedAnts.Domain.Show;
using RedAnts.Features.Show.Ports;
using RedAnts.Features.Show.ShowWorkflow;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowWorkflowTests
{
    [Fact]
    public async Task SaveShowProfiles_rejects_duplicate_ids_without_saving()
    {
        var profiles = new RecordingProfiles();
        var handler = new SaveShowProfiles.Handler(profiles);
        var command = new SaveShowProfiles.Command([new ShowProfile("a", "A", null, []), new ShowProfile("a", "B", null, [])]);

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Null(profiles.Saved);
    }

    [Fact]
    public async Task SaveShowProfiles_stores_valid_profiles()
    {
        var profiles = new RecordingProfiles();
        var list = new List<ShowProfile> { new("a", "A", null, []), new("b", "B", null, []) };

        await new SaveShowProfiles.Handler(profiles).HandleAsync(new SaveShowProfiles.Command(list));

        Assert.Same(list, profiles.Saved);
    }

    [Fact]
    public async Task GetShowProfiles_returns_the_stored_profiles()
    {
        var profiles = new RecordingProfiles { Stored = [new ShowProfile("x", "X", null, [])] };

        var result = await new GetShowProfiles.Handler(profiles).HandleAsync(new GetShowProfiles.Query());

        Assert.Single(result);
        Assert.Equal("x", result[0].Id);
    }

    [Fact]
    public async Task DispatchShowCommand_returns_how_many_boards_were_reached()
    {
        var remote = new CountingRemote { Reach = 3 };
        var handler = new DispatchShowCommand.Handler(remote);

        var reached = await handler.HandleAsync(new DispatchShowCommand.Command(new ShowCommand("play", TileId: "goal")));

        Assert.Equal(3, reached);
        Assert.Equal("play", remote.Last?.Action);
    }

    [Fact]
    public async Task DispatchShowCommand_rejects_a_blank_action()
    {
        var handler = new DispatchShowCommand.Handler(new CountingRemote());

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new DispatchShowCommand.Command(new ShowCommand(" "))));
    }

    [Fact]
    public async Task SetShowSetting_trims_the_key_and_rejects_blank_keys()
    {
        var settings = new RecordingSettings();
        var handler = new SetShowSetting.Handler(settings);

        await handler.HandleAsync(new SetShowSetting.Command(" Spotify:ClientId ", "id"));
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new SetShowSetting.Command(" ", "x")));

        Assert.Equal("id", settings.Values["Spotify:ClientId"]);
    }

    private sealed class RecordingProfiles : IShowProfiles
    {
        public IReadOnlyList<ShowProfile> Stored { get; set; } = [];
        public IReadOnlyList<ShowProfile>? Saved { get; private set; }

        public Task<IReadOnlyList<ShowProfile>> GetAllAsync() => Task.FromResult(Stored);

        public Task SaveAllAsync(IReadOnlyList<ShowProfile> profiles)
        {
            Saved = profiles;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingRemote : IShowRemote
    {
        public int Reach { get; set; }
        public ShowCommand? Last { get; private set; }

        public IDisposable Register(string? room, Func<ShowCommand, Task> handler) => new Registration();

        public Task<int> DispatchAsync(ShowCommand command)
        {
            Last = command;
            return Task.FromResult(Reach);
        }

        private sealed class Registration : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class RecordingSettings : IShowSettings
    {
        public Dictionary<string, string?> Values { get; } = new();

        public string? Get(string key) => Values.GetValueOrDefault(key);

        public Task SetAsync(string key, string? value)
        {
            Values[key] = value;
            return Task.CompletedTask;
        }

        public Task LoadAsync() => Task.CompletedTask;
    }
}
