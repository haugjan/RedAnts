using RedAnts.Domain;
using RedAnts.DJ.Features.Admin;
using RedAnts.DJ.Features.Remote;
using RedAnts.DJ.Features.Remote.Infrastructure;
using Xunit;

namespace RedAnts.DJ.Tests;

public class DJRoomsTests
{
    [Fact]
    public async Task Recording_stores_the_normalized_room_once()
    {
        var settings = new CountingSettings();
        var rooms = new DJRooms(settings);

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
        var rooms = new DJRooms(new CountingSettings());
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
        await new DJRooms(settings).RecordAsync("hallea");

        var room = Assert.Single(new DJRooms(settings).All());

        Assert.Equal("hallea", room.Name);
        Assert.Null(room.LastSeen);
    }

    [Fact]
    public async Task Overlong_room_names_are_ignored()
    {
        var rooms = new DJRooms(new CountingSettings());

        await rooms.RecordAsync(new string('x', 41));

        Assert.Empty(rooms.All());
    }

    [Fact]
    public async Task DeleteDJRoom_rejects_a_blank_room()
    {
        var handler = new DeleteDJRoom.Handler(new DJRooms(new CountingSettings()));

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new DeleteDJRoom.Command(" ")));
    }

    private sealed class CountingSettings : IDJSettings
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
