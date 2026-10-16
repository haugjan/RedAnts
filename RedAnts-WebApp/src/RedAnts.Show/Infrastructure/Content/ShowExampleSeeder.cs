using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace RedAnts.Show.Infrastructure.Content;

public sealed class ShowExampleSeeder(
    IHallRepository halls,
    IMusicLibrary music,
    ILogger<ShowExampleSeeder> log) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string HallId = "win4";
    public const string HallName = "WIN4 Stratos";

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken stop)
    {
        try
        {
            if (await halls.GetByIdAsync(HallId) is not null) return;
            var library = await music.AllAsync();
            var hall = Example(library);
            await halls.SaveAsync(hall);
            log.LogInformation("Beispielhalle {Hall} angelegt: {Decks} Decks, {Music} Musiktasten",
                hall.Name, hall.DeckLayouts.Count, library.Count == 0 ? 0 : MusicKeys.Length);
        }
        catch (Exception e)
        {
            log.LogWarning("Beispielhalle konnte nicht angelegt werden: {Message}", e.Message);
        }
    }

    public static readonly string[] MusicKeys =
    [
        "Aufwärmmusik", "Wartesong", "Einlaufmusik", "Torbeschwörung", "Bully",
        "Starting Six Gegner", "Starting Six Red Ants", "Spielunterbruch", "Penalty",
    ];

    public static Hall Example(IReadOnlyList<MusicItem> library)
    {
        var hall = Hall.Create(HallId, HallName) with
        {
            Devices = new HallDevices(VmixHost: "127.0.0.1", SednaHost: "127.0.0.1", ClockType: "iCast", ClockPort: 50086),
            Offered = new HallCapabilities(Sound: true, Clock: true, Vmix: true, Led: true),
            BreakMusicId = Find(library, "Spielunterbruch")?.Id ?? "",
        };
        return hall with { Decks = [Field(library), Music(library)] };
    }

    private static Deck Field(IReadOnlyList<MusicItem> library)
    {
        var deck = DefaultDecks.Field();
        var home = deck.Home;
        home = WithMusic(home, 1, Find(library, "Tor Red Ants"));
        home = WithMusic(home, 8, Find(library, "Tor Gegner"));
        home = WithMusic(home, 9, Find(library, "Strafe Red Ants"));
        home = WithMusic(home, 16, Find(library, "Strafe Gegner"));
        return deck with { Pages = deck.Pages.Select(p => p.IsHome ? home : p).ToList() };
    }

    private static Deck Music(IReadOnlyList<MusicItem> library)
    {
        var deck = DefaultDecks.Music();
        var home = deck.Home;
        for (var i = 0; i < MusicKeys.Length; i++)
        {
            if (Find(library, MusicKeys[i]) is not { } item) continue;
            home = home.With(new DeckKey(i + 1, KeyKinds.Music, Short(MusicKeys[i]), "#2f855a", MusicId: item.Id));
        }
        return deck with { Pages = deck.Pages.Select(p => p.IsHome ? home : p).ToList() };
    }

    private static DeckPage WithMusic(DeckPage page, int number, MusicItem? item)
    {
        if (item is null) return page;
        var key = page.KeyAt(number);
        var steps = new List<KeyStep> { new(KeyActions.MusicPlay, item.Id) };
        steps.AddRange(key.Sequence.Where(s => s.Action != KeyActions.MusicPlay));
        return page.With(key with { Steps = steps });
    }

    private static MusicItem? Find(IReadOnlyList<MusicItem> library, string label) =>
        library.FirstOrDefault(m => m.Name.Contains(label, StringComparison.OrdinalIgnoreCase))
        ?? library.FirstOrDefault(m => m.Display.Contains(label, StringComparison.OrdinalIgnoreCase));

    private static string Short(string label) => label.Replace("Starting Six ", "S6 ").Replace("musik", "");
}
