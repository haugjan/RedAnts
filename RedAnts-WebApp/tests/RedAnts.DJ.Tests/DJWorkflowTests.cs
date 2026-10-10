using RedAnts.Domain;
using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Admin;
using RedAnts.DJ.Features.Board;
using RedAnts.DJ.Features.Remote;
using Xunit;

namespace RedAnts.DJ.Tests;

public class DJWorkflowTests
{
    [Fact]
    public async Task SaveDJProfiles_rejects_duplicate_ids_without_saving()
    {
        var profiles = new RecordingProfiles();
        var handler = new SaveDJProfiles.Handler(profiles);
        var command = new SaveDJProfiles.Command([new DJProfile("a", "A", null, []), new DJProfile("a", "B", null, [])]);

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Null(profiles.Saved);
    }

    [Fact]
    public async Task SaveDJProfiles_stores_valid_profiles()
    {
        var profiles = new RecordingProfiles();
        var list = new List<DJProfile> { new("a", "A", null, []), new("b", "B", null, []) };

        await new SaveDJProfiles.Handler(profiles).HandleAsync(new SaveDJProfiles.Command(list));

        Assert.Same(list, profiles.Saved);
    }

    [Fact]
    public async Task GetDJProfiles_returns_the_stored_profiles()
    {
        var profiles = new RecordingProfiles { Stored = [new DJProfile("x", "X", null, [])] };

        var result = await new GetDJProfiles.Handler(profiles).HandleAsync(new GetDJProfiles.Query());

        Assert.Single(result);
        Assert.Equal("x", result[0].Id);
    }

    [Fact]
    public async Task DispatchDJCommand_returns_how_many_boards_were_reached()
    {
        var remote = new CountingRemote { Reach = 3 };
        var handler = new DispatchDJCommand.Handler(remote);

        var reached = await handler.HandleAsync(new DispatchDJCommand.Command(new DJCommand("play", TileId: "goal")));

        Assert.Equal(3, reached);
        Assert.Equal("play", remote.Last?.Action);
    }

    [Fact]
    public async Task DispatchDJCommand_rejects_a_blank_action()
    {
        var handler = new DispatchDJCommand.Handler(new CountingRemote());

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new DispatchDJCommand.Command(new DJCommand(" "))));
    }

    [Fact]
    public async Task SetDJSetting_trims_the_key_and_rejects_blank_keys()
    {
        var settings = new RecordingSettings();
        var handler = new SetDJSetting.Handler(settings);

        await handler.HandleAsync(new SetDJSetting.Command(" Spotify:ClientId ", "id"));
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new SetDJSetting.Command(" ", "x")));

        Assert.Equal("id", settings.Values["Spotify:ClientId"]);
    }

    private sealed class RecordingProfiles : IDJProfiles
    {
        public IReadOnlyList<DJProfile> Stored { get; set; } = [];
        public IReadOnlyList<DJProfile>? Saved { get; private set; }

        public Task<IReadOnlyList<DJProfile>> GetAllAsync() => Task.FromResult(Stored);

        public Task SaveAllAsync(IReadOnlyList<DJProfile> profiles)
        {
            Saved = profiles;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingRemote : IDJRemote
    {
        public int Reach { get; set; }
        public DJCommand? Last { get; private set; }

        public IDisposable Register(string? room, Func<DJCommand, Task> handler) => new Registration();

        public Task<int> DispatchAsync(DJCommand command)
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

    private sealed class RecordingSettings : IDJSettings
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
