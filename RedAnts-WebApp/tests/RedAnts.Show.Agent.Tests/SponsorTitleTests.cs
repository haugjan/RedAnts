using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class SponsorTitleTests : IAsyncDisposable
{
    private const string Folder = @"C:\Livestream\PersonalSponsors";
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
            Sponsors = new PersonalSponsors(Folder, "", new Dictionary<string, int> { ["41"] = 2, ["00"] = 1 }),
            OverlayTitles =
            [
                new OverlayTitle("player_event", "event.gtzip", 1, new Dictionary<string, string>
                {
                    ["TxtNumber.Text"] = "{player.number}",
                    ["ImgSponsor.Source"] = "{player.sponsor}",
                    ["ImgTopscorer.Visible"] = "{player.topscorer}",
                }),
            ],
        };
        var game = Game.None with
        {
            Home = GameTeam.Empty with
            {
                ShortName = "RAW",
                Players = [new Player(41, "Livia Werz"), new Player(7, "Nora Stark"), new Player(9, "Ohne Logo")],
                TopScorer = 7,
            },
        };
        store.Apply(new AgentPayload(hall, game, DateTimeOffset.Now, []));
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

    private async Task<IReadOnlyList<string>> HeardAsync(string needle, int wanted)
    {
        for (var waited = 0; waited < 100; waited++)
        {
            lock (_heard) if (_heard.Count(l => l.Contains(needle, StringComparison.Ordinal)) >= wanted) break;
            await Task.Delay(20);
        }
        lock (_heard) return _heard.Where(l => l.Contains(needle, StringComparison.Ordinal)).ToList();
    }

    private static string Logo(string key, int index) => Uri.EscapeDataString($@"{Folder}\{key}_{index}.png");

    [Fact]
    public async Task A_player_with_two_logos_shows_the_first_and_the_key_cycles_through_the_rest()
    {
        var runner = await StandAsync();

        await runner.ShowTitleAsync("player_event", new KeyContext("home", new Player(41, "Livia Werz")), CancellationToken.None);

        var sponsor = await HeardAsync("ImgSponsor.Source", 1);
        Assert.Contains(sponsor, l => l.StartsWith("FUNCTION SetImage ", StringComparison.Ordinal) && l.EndsWith("&Value=" + Logo("41", 1), StringComparison.Ordinal));
        Assert.Contains(await HeardAsync("ImgTopscorer.Source", 1), l => l.StartsWith("FUNCTION SetImageVisibleOff Input=event.gtzip&SelectedName=ImgTopscorer.Source", StringComparison.Ordinal));
        Assert.Equal(2, runner.SponsorCount);
        Assert.Equal(1, runner.SponsorIndex);

        await runner.NextSponsorAsync(CancellationToken.None);
        Assert.Contains(await HeardAsync("ImgSponsor.Source", 2), l => l.EndsWith("&Value=" + Logo("41", 2), StringComparison.Ordinal));
        Assert.Equal(2, runner.SponsorIndex);

        await runner.NextSponsorAsync(CancellationToken.None);
        Assert.Equal(2, (await HeardAsync("ImgSponsor.Source", 3)).Count(l => l.EndsWith("&Value=" + Logo("41", 1), StringComparison.Ordinal)));
        Assert.Equal(1, runner.SponsorIndex);
    }

    [Fact]
    public async Task The_topscorer_is_sent_as_00_with_the_icon_switched_on()
    {
        var runner = await StandAsync();

        await runner.ShowTitleAsync("player_event", new KeyContext("home", new Player(7, "Nora Stark")), CancellationToken.None);

        Assert.Contains(await HeardAsync("TxtNumber.Text", 1), l => l.EndsWith("&SelectedName=TxtNumber.Text&Value=00", StringComparison.Ordinal));
        Assert.Contains(await HeardAsync("ImgTopscorer.Source", 1), l => l.StartsWith("FUNCTION SetImageVisibleOn Input=event.gtzip&SelectedName=ImgTopscorer.Source", StringComparison.Ordinal));
        Assert.Contains(await HeardAsync("ImgSponsor.Source", 1), l => l.EndsWith("&Value=" + Logo("00", 1), StringComparison.Ordinal));
        Assert.Equal(1, runner.SponsorCount);

        await runner.NextSponsorAsync(CancellationToken.None);
        await Task.Delay(100);
        Assert.Single(await HeardAsync("ImgSponsor.Source", 1));
    }

    [Fact]
    public async Task A_player_without_a_logo_leaves_the_box_empty_and_the_key_dead()
    {
        var runner = await StandAsync();

        await runner.ShowTitleAsync("player_event", new KeyContext("home", new Player(9, "Ohne Logo")), CancellationToken.None);

        Assert.Contains(await HeardAsync("ImgSponsor.Source", 1), l => l.EndsWith("&SelectedName=ImgSponsor.Source&Value=", StringComparison.Ordinal));
        Assert.Equal(0, runner.SponsorCount);

        await runner.HideAllAsync(CancellationToken.None);
        Assert.Equal(0, runner.SponsorIndex);
    }

    public async ValueTask DisposeAsync()
    {
        if (_link is not null) await _link.StopAsync(CancellationToken.None);
        _vmix.Stop();
    }
}
