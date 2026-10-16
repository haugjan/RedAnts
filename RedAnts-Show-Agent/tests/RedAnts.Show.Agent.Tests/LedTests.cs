using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class LedTests
{
    static readonly GameTeam Home = GameTeam.Empty with
    {
        LongName = "Red Ants",
        ShortName = "RA",
        Players = [new Player(7, "Nina Muster")],
    };

    static readonly Game Match = Game.None with { Id = "1", Home = Home };

    static KeyRunner Build(int port, bool led = true)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with
        {
            Id = "win4",
            Offered = new HallCapabilities(Sound: true, Clock: false, Vmix: false, Led: led),
            Devices = new HallDevices(SednaHost: "127.0.0.1", SednaPort: port),
            LedTemplates = [new LedTemplate("spielerin", "start playlist -n \"Spielerin {team.short} {player.number}\" -o")],
        };
        store.Apply(new AgentPayload(hall, Match, DateTimeOffset.Now, []));
        var live = new LiveGame();
        return new KeyRunner(store, live, new ShowStates(NullLogger<ShowStates>.Instance),
            new VmixLink(store, live, NullLogger<VmixLink>.Instance),
            new SednaLink(store, NullLogger<SednaLink>.Instance),
            NullLogger<KeyRunner>.Instance);
    }

    static async Task<string> HeardAsync(TcpListener listener, CancellationToken stop)
    {
        using var client = await listener.AcceptTcpClientAsync(stop);
        var buffer = new byte[256];
        var read = await client.GetStream().ReadAsync(buffer, stop);
        return Encoding.UTF8.GetString(buffer, 0, read).Trim();
    }

    [Fact]
    public async Task The_chosen_player_goes_to_the_led_wall_with_her_number()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var heard = HeardAsync(listener, stop.Token);

        var runner = Build(port);
        await runner.ShowPlayerOnLedAsync(new KeyContext("home", Home.Players[0], Event: "goal"), stop.Token);

        Assert.Equal("start playlist -n \"Spielerin RA 7\" -o", await heard);
        listener.Stop();
    }

    [Fact]
    public async Task A_hall_without_a_led_wall_sends_nothing()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var runner = Build(port, led: false);
        await runner.ShowPlayerOnLedAsync(new KeyContext("home", Home.Players[0]), CancellationToken.None);

        Assert.False(listener.Pending());
        listener.Stop();
    }

    [Fact]
    public async Task Without_a_player_nothing_goes_out()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var runner = Build(port);
        await runner.ShowPlayerOnLedAsync(new KeyContext("home"), CancellationToken.None);

        Assert.False(listener.Pending());
        listener.Stop();
    }

    [Fact]
    public async Task An_unreachable_led_wall_does_not_break_the_key()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var runner = Build(port);

        await runner.ShowPlayerOnLedAsync(new KeyContext("home", Home.Players[0]), CancellationToken.None);
    }
}
