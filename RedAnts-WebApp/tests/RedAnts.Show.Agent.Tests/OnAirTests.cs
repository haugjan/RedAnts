using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class OnAirTests
{
    static readonly Player Nina = new(7, "Nina Muster");

    static (KeyRunner Runner, VmixLink Vmix) Build(int vmixPort)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with
        {
            Id = "win4",
            Offered = new HallCapabilities(Sound: true, Clock: true, Vmix: true, Led: false),
            Devices = new HallDevices(VmixHost: "127.0.0.1", VmixPort: vmixPort),
            OverlayTitles = [new OverlayTitle("player_event", "event.gtzip", 1, new Dictionary<string, string> { ["Headline.Text"] = "{player.name}" })],
        };
        var team = GameTeam.Empty with { ShortName = "RA", Players = [Nina] };
        store.Apply(new AgentPayload(hall with { }, Game.None with { Id = "1", Home = team }, DateTimeOffset.Now, []));
        var live = new LiveGame();
        var vmix = new VmixLink(store, live, NullLogger<VmixLink>.Instance);
        return (new KeyRunner(store, live, new ShowStates(NullLogger<ShowStates>.Instance), vmix,
            new SednaLink(store, NullLogger<SednaLink>.Instance),
            NullLogger<KeyRunner>.Instance), vmix);
    }

    static async Task<(KeyRunner Runner, TcpListener Vmix, VmixLink Link)> StandAsync()
    {
        var stand = new TcpListener(IPAddress.Loopback, 0);
        stand.Start();
        _ = AcceptAsync(stand);
        var (runner, link) = Build(((IPEndPoint)stand.LocalEndpoint).Port);
        await link.StartAsync(CancellationToken.None);
        var waited = 0;
        while (!link.Connected && waited++ < 100) await Task.Delay(50);
        Assert.True(link.Connected, "vMix-Ersatz wurde nicht verbunden");
        return (runner, stand, link);
    }

    static async Task AcceptAsync(TcpListener vmix)
    {
        try
        {
            while (true)
            {
                var client = await vmix.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    var buffer = new byte[512];
                    try { while (await client.GetStream().ReadAsync(buffer) > 0) { } }
                    catch (Exception) { }
                    client.Dispose();
                });
            }
        }
        catch (Exception) { }
    }

    [Fact]
    public async Task A_goal_puts_the_chosen_player_on_air()
    {
        var (runner, vmix, link) = await StandAsync();

        await runner.ShowTitleAsync("player_event", new KeyContext("home", Nina, Event: "goal"), CancellationToken.None);

        Assert.Equal("Tor\n7 Nina Muster", runner.OnAir);
    }

    [Fact]
    public async Task An_own_goal_says_so()
    {
        var (runner, vmix, link) = await StandAsync();

        await runner.ShowTitleAsync("player_event", new KeyContext("home", Nina, Event: "owngoal"), CancellationToken.None);

        Assert.StartsWith("Eigentor", runner.OnAir);
        await link.StopAsync(CancellationToken.None);
        vmix.Stop();
    }

    [Fact]
    public async Task Hiding_the_insert_clears_what_is_on_air()
    {
        var (runner, vmix, link) = await StandAsync();
        await runner.ShowTitleAsync("player_event", new KeyContext("home", Nina, Event: "goal"), CancellationToken.None);

        await runner.HideTitleAsync("player_event", CancellationToken.None);

        Assert.Equal("", runner.OnAir);
        await link.StopAsync(CancellationToken.None);
        vmix.Stop();
    }

    [Fact]
    public async Task Panic_clears_what_is_on_air()
    {
        var (runner, vmix, link) = await StandAsync();
        await runner.ShowTitleAsync("player_event", new KeyContext("home", Nina, Event: "goal"), CancellationToken.None);

        await runner.HideAllAsync(CancellationToken.None);

        Assert.Equal("", runner.OnAir);
        await link.StopAsync(CancellationToken.None);
        vmix.Stop();
    }
}
