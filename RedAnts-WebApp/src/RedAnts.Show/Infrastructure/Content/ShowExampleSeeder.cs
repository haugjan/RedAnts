using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Setup;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace RedAnts.Show.Infrastructure.Content;

public sealed class ShowExampleSeeder(
    IHallRepository halls,
    IMusicLibrary music,
    IDJProfiles profiles,
    IShowSetup setup,
    AgentSync sync,
    ILogger<ShowExampleSeeder> log) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string HallId = "win4";
    public const string HallName = "WIN4 Stratos";
    public const string SoundHallId = "win4-sound";
    public const string StreamHallId = "win4-livestream";
    public const string MusicHallId = "oberseen";
    public const string MusicHallName = "Oberseen iPad";
    public const string NotebookHallId = "oberseen-notebook";
    public const string RennwegHallId = "rennweg-ipad";

    private static readonly int[] TimeoutKeys = [3, 6];

    private const int HighlightKey = 22;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken stop)
    {
        try
        {
            await CarryTitlesOverAsync();
            await FlattenLibraryAsync();
            var library = await music.AllAsync();
            if (await halls.GetByIdAsync(HallId) is not { } known)
            {
                var hall = Example(library);
                await halls.SaveAsync(hall);
                log.LogInformation("Beispielhalle {Hall} angelegt: {Decks} Decks", hall.Name, hall.DeckLayouts.Count);
            }
            else if (Refreshed(known, library) is { } updated)
            {
                await halls.SaveAsync(updated);
                await sync.PushAsync(updated.Id);
                log.LogInformation("Decks der Halle {Hall} nachgeführt", updated.Name);
            }
            foreach (var (id, build) in Others)
            {
                if (await halls.GetByIdAsync(id) is { } other)
                {
                    if (Refreshed(other, library) is not { } fresh) continue;
                    await halls.SaveAsync(fresh);
                    await sync.PushAsync(fresh.Id);
                    log.LogInformation("Decks der Halle {Hall} nachgeführt", fresh.Name);
                    continue;
                }
                var hall = build(library);
                await halls.SaveAsync(hall);
                log.LogInformation("Halle {Hall} angelegt: {Decks} Decks", hall.Name, hall.DeckLayouts.Count);
            }
        }
        catch (Exception e)
        {
            log.LogWarning("Beispielhalle konnte nicht angelegt werden: {Message}", e.Message);
        }
    }

    public static Hall? Refreshed(Hall hall, IReadOnlyList<MusicItem> library)
    {
        var decks = hall.DeckLayouts.ToList();
        var changed = false;

        if (decks.FirstOrDefault(d => d.Id == DefaultDecks.FieldDeck) is { } field)
        {
            var home = field.Home;
            var fresh = home;
            if (Find(library, "Time Out") is { } timeout)
                foreach (var number in TimeoutKeys)
                    if (!fresh.KeyAt(number).Sequence.Any(step => step.Action == KeyActions.MusicPlay))
                        fresh = WithMusic(fresh, number, timeout);

            var wanted = DefaultDecks.Field().Home.KeyAt(HighlightKey);
            if (fresh.KeyAt(HighlightKey).Sequence.Any(step => step.Target == "title:result"))
                fresh = fresh.With(wanted);

            foreach (var key in fresh.Keys.Where(Timed).ToList())
                fresh = fresh.With(key with { Steps = key.Sequence.Where(Shows).ToList() });

            if (fresh != home)
            {
                decks[decks.IndexOf(field)] = field with { Pages = field.Pages.Select(p => p.IsHome ? fresh : p).ToList() };
                changed = true;
            }
        }

        var name = hall.Id == MusicHallId && hall.Name == "Oberseen" ? MusicHallName : hall.Name;
        if (name != hall.Name) changed = true;

        return changed ? hall with { Name = name, Decks = decks } : null;
    }

    private async Task FlattenLibraryAsync()
    {
        var stored = await profiles.GetAllAsync();
        if (stored.Count == 1 && stored[0].Id == GetMusicLibrary.LibraryId
            && stored[0].Root.All(button => !button.IsFolder && !button.IsControl && !button.Panic))
            return;
        var flat = GetMusicLibrary.Flatten(stored);
        await profiles.SaveAllAsync([MusicLibraryWriter.Library(flat)]);
        log.LogInformation("Musik ist neu eine Liste: {Count} Einträge", flat.Count);
    }

    private async Task CarryTitlesOverAsync()
    {
        var stored = await setup.GetAsync();
        if (stored.IsEmpty)
        {
            var configured = await halls.GetByIdAsync(HallId);
            var titles = configured?.OverlayTitles is { Count: > 0 } known ? known : ShowSetup.Default.Titles;
            var led = configured?.LedTemplates is { Count: > 0 } templates ? templates : ShowSetup.Default.Led;
            await setup.SaveAsync(new ShowSetup(titles, led));
            log.LogInformation("vMix-Titel und LED-Vorlagen gelten neu für alle Hallen: {Titles} Titel, {Led} Vorlagen",
                titles.Count, led.Count);
            return;
        }

        var complete = stored.Titles.ToList();
        var added = 0;
        foreach (var title in ShowSetup.Default.Titles)
        {
            if (complete.Any(known => known.Kind.Equals(title.Kind, StringComparison.OrdinalIgnoreCase))) continue;
            complete.Add(title);
            added++;
        }
        if (added == 0) return;
        await setup.SaveAsync(stored with { Titles = complete });
        log.LogInformation("{Count} fehlende vMix-Titel ergänzt", added);
    }

    public static Hall Example(IReadOnlyList<MusicItem> library)
    {
        var hall = Hall.Create(HallId, HallName) with
        {
            Devices = new HallDevices(VmixHost: "127.0.0.1", SednaHost: "127.0.0.1", ClockType: "iCast", ClockPort: 50086),
            Offered = new HallCapabilities(Sound: true, Clock: true, Vmix: true, Led: true),
            BreakMusicId = Find(library, "Spielunterbruch")?.Id ?? "",
        };
        return hall with { Decks = [Field(library), DefaultDecks.Music()] };
    }

    private static readonly IReadOnlyList<(string Id, Func<IReadOnlyList<MusicItem>, Hall> Build)> Others =
    [
        (SoundHallId, library => SoundOnly(SoundHallId, "WIN4 nur Sound", library)),
        (StreamHallId, StreamOnly),
        (MusicHallId, library => SoundOnly(MusicHallId, MusicHallName, library)),
        (NotebookHallId, Notebook),
        (RennwegHallId, library => SoundOnly(RennwegHallId, "Rennweg iPad", library)),
    ];

    public static Hall MusicHall(IReadOnlyList<MusicItem> library) => SoundOnly(MusicHallId, MusicHallName, library);

    public static Hall SoundOnly(string id, string name, IReadOnlyList<MusicItem> library)
    {
        var hall = Hall.Create(id, name) with
        {
            Offered = new HallCapabilities(Sound: true, Clock: false, Vmix: false, Led: false),
            BreakMusicId = Find(library, "Spielunterbruch")?.Id ?? "",
        };
        return hall with { Decks = [DefaultDecks.Music() with { Name = "Bord" }] };
    }

    public static Hall Notebook(IReadOnlyList<MusicItem> library)
    {
        var hall = Hall.Create(NotebookHallId, "Oberseen Notebook") with
        {
            Devices = new HallDevices(ClockType: "Bodet", ClockPort: 4001),
            Offered = new HallCapabilities(Sound: true, Clock: true, Vmix: false, Led: false),
            BreakMusicId = Find(library, "Spielunterbruch")?.Id ?? "",
        };
        return hall with { Decks = [DefaultDecks.Music() with { Name = "Bord" }] };
    }

    public static Hall StreamOnly(IReadOnlyList<MusicItem> library)
    {
        var hall = Hall.Create(StreamHallId, "WIN4 nur Livestream") with
        {
            Devices = new HallDevices(VmixHost: "127.0.0.1", ClockType: "iCast", ClockPort: 50086),
            Offered = new HallCapabilities(Sound: false, Clock: true, Vmix: true, Led: false),
        };
        return hall with { Decks = [DefaultDecks.Field()] };
    }

    private static bool Timed(DeckKey key) =>
        key.Sequence.Any(step => step.Action == KeyActions.VmixShow)
        && key.Sequence.Any(step => step.Action == KeyActions.Wait);

    private static bool Shows(KeyStep step) =>
        step.Action is not (KeyActions.Wait or KeyActions.VmixHide);

    private static bool Clipped(OverlayTitle title) =>
        title.Fields.TryGetValue("TxtBottom.Text", out var bottom)
        && (bottom.Contains("referee2", StringComparison.OrdinalIgnoreCase)
            || bottom.Contains("commentator2", StringComparison.OrdinalIgnoreCase));

    private static Deck Field(IReadOnlyList<MusicItem> library)
    {
        var deck = DefaultDecks.Field();
        var home = deck.Home;
        home = WithMusic(home, 1, Find(library, "Tor Red Ants"));
        home = WithMusic(home, 8, Find(library, "Tor Gegner"));
        home = WithMusic(home, 9, Find(library, "Strafe Red Ants"));
        home = WithMusic(home, 16, Find(library, "Strafe Gegner"));
        foreach (var number in TimeoutKeys) home = WithMusic(home, number, Find(library, "Time Out"));
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
        library.FirstOrDefault(item => item.Name.Equals(label, StringComparison.OrdinalIgnoreCase))
        ?? library.FirstOrDefault(item => Last(item.Name).Equals(label, StringComparison.OrdinalIgnoreCase))
        ?? library.FirstOrDefault(item => item.Name.Contains(label, StringComparison.OrdinalIgnoreCase));

    private static string Last(string name)
    {
        var cut = name.LastIndexOf('·');
        return cut >= 0 ? name[(cut + 1)..].Trim() : name;
    }
}
