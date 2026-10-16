using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class TitleTests : IAsyncDisposable
{
    private readonly TcpListener _vmix = new(IPAddress.Loopback, 0);
    private readonly List<string> _heard = [];
    private VmixLink? _link;

    private async Task<KeyRunner> StandAsync()
    {
        _vmix.Start();
        _ = AcceptAsync();
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with
        {
            Id = "win4",
            Offered = new HallCapabilities(Sound: false, Clock: false, Vmix: true, Led: false),
            Devices = new HallDevices(VmixHost: "127.0.0.1", VmixPort: ((IPEndPoint)_vmix.LocalEndpoint).Port),
            OverlayTitles =
            [
                new OverlayTitle("result", "board.gtzip", 2, new Dictionary<string, string> { ["TxtTop.Text"] = "RESULTAT" }),
                new OverlayTitle("opener", "board.gtzip", 2, new Dictionary<string, string> { ["TxtTop.Text"] = "OPENER" }),
            ],
        };
        store.Apply(new AgentPayload(hall, Game.None, DateTimeOffset.Now, []));
        var live = new LiveGame();
        _link = new VmixLink(store, live, NullLogger<VmixLink>.Instance);
        await _link.StartAsync(CancellationToken.None);
        var waited = 0;
        while (!_link.Connected && waited++ < 100) await Task.Delay(50);
        Assert.True(_link.Connected, "vMix-Ersatz wurde nicht verbunden");
        return new KeyRunner(store, live, new ShowStates(NullLogger<ShowStates>.Instance), _link,
            new SednaLink(store, NullLogger<SednaLink>.Instance), NullLogger<KeyRunner>.Instance);
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (true)
            {
                var client = await _vmix.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    using var reader = new StreamReader(client.GetStream());
                    while (await reader.ReadLineAsync() is { } line)
                        lock (_heard) _heard.Add(line);
                    client.Dispose();
                });
            }
        }
        catch (Exception) { }
    }

    private IReadOnlyList<string> Overlays()
    {
        lock (_heard) return _heard.Where(l => l.Contains("Overlay", StringComparison.Ordinal)).ToList();
    }

    private async Task<int> OverlaysAsync(int wanted)
    {
        for (var waited = 0; waited < 100 && Overlays().Count < wanted; waited++) await Task.Delay(20);
        await Task.Delay(100);
        return Overlays().Count;
    }

    [Fact]
    public async Task A_second_insert_keeps_the_first_timer_from_hiding_it()
    {
        var runner = await StandAsync();

        var first = await runner.ShowTitleAsync("result", KeyContext.None, CancellationToken.None);
        var second = await runner.ShowTitleAsync("opener", KeyContext.None, CancellationToken.None);

        await runner.HideTitleAsync("result", first, CancellationToken.None);

        Assert.Equal(2, await OverlaysAsync(2));
        Assert.Equal("opener", runner.Shown);

        await runner.HideTitleAsync("opener", second, CancellationToken.None);

        Assert.Equal(3, await OverlaysAsync(3));
        Assert.Equal("", runner.Shown);
    }

    [Fact]
    public async Task The_same_insert_pressed_twice_only_hides_at_the_end_of_the_second_run()
    {
        var runner = await StandAsync();

        var first = await runner.ShowTitleAsync("result", KeyContext.None, CancellationToken.None);
        var second = await runner.ShowTitleAsync("result", KeyContext.None, CancellationToken.None);

        await runner.HideTitleAsync("result", first, CancellationToken.None);
        Assert.Equal(2, await OverlaysAsync(2));

        await runner.HideTitleAsync("result", second, CancellationToken.None);
        Assert.Equal(3, await OverlaysAsync(3));
    }

    [Fact]
    public async Task A_hide_without_a_token_always_goes_out()
    {
        var runner = await StandAsync();
        await runner.ShowTitleAsync("result", KeyContext.None, CancellationToken.None);
        await runner.ShowTitleAsync("opener", KeyContext.None, CancellationToken.None);

        await runner.HideTitleAsync("result", CancellationToken.None);

        Assert.Equal(3, await OverlaysAsync(3));
    }

    public async ValueTask DisposeAsync()
    {
        if (_link is not null) await _link.StopAsync(CancellationToken.None);
        _vmix.Stop();
    }
}
