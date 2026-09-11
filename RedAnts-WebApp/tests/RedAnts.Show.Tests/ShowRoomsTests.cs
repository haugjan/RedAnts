using RedAnts.Domain;
using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Remote;
using RedAnts.Show.Features.Remote.Infrastructure;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowRoomsTests
{
    [Fact]
    public async Task Recording_stores_the_normalized_room_once()
    {
        var settings = new CountingSettings();
        var rooms = new ShowRooms(settings);

        await rooms.RecordAsync(" HalleA ");
        await rooms.RecordAsync("hallea");

        var room = Assert.Single(rooms.All());
        Assert.Equal("hallea", room.Name);
        Assert.NotNull(room.LastSeen);
        Assert.Equal(1, settings.Writes);
    }

    [Fact]
    public async Task A_deleted_room_comes_back_with_the_next_command()
    {
        var rooms = new ShowRooms(new CountingSettings());
        await rooms.RecordAsync("hallea");
        await rooms.RecordAsync("halleb");

        await rooms.DeleteAsync("HalleA");
        Assert.Equal(["halleb"], rooms.All().Select(r => r.Name));

        await rooms.RecordAsync("hallea");
        Assert.Equal(["hallea", "halleb"], rooms.All().Select(r => r.Name));
    }

    [Fact]
    public async Task Rooms_survive_a_restart_through_the_settings()
    {
        var settings = new CountingSettings();
        await new ShowRooms(settings).RecordAsync("hallea");

        var room = Assert.Single(new ShowRooms(settings).All());

        Assert.Equal("hallea", room.Name);
        Assert.Null(room.LastSeen);
    }

    [Fact]
    public async Task Overlong_room_names_are_ignored()
    {
        var rooms = new ShowRooms(new CountingSettings());

        await rooms.RecordAsync(new string('x', 41));

        Assert.Empty(rooms.All());
    }

    [Fact]
    public async Task DeleteShowRoom_rejects_a_blank_room()
    {
        var handler = new DeleteShowRoom.Handler(new ShowRooms(new CountingSettings()));

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new DeleteShowRoom.Command(" ")));
    }

    private sealed class CountingSettings : IShowSettings
    {
        private readonly Dictionary<string, string?> _values = new();

        public int Writes { get; private set; }

        public string? Get(string key) => _values.GetValueOrDefault(key);

        public Task SetAsync(string key, string? value)
        {
            _values[key] = value;
            Writes++;
            return Task.CompletedTask;
        }

        public Task LoadAsync() => Task.CompletedTask;
    }
}
