namespace RedAnts.Features.Show;

public enum NodeKind { Sound, Folder, Random }

public sealed class EditSound
{
    public SoundKind Kind { get; set; } = SoundKind.Local;
    public string Ref { get; set; } = "";
    public double StartSec { get; set; }
    public double? DurationSec { get; set; }
    public bool Shuffle { get; set; }

    public EditSound Clone() => new() { Kind = Kind, Ref = Ref, StartSec = StartSec, DurationSec = DurationSec, Shuffle = Shuffle };
    public ShowSound ToModel() => new(Kind, Ref, StartSec, DurationSec, Shuffle);
    public static EditSound From(ShowSound s) => new() { Kind = s.Kind, Ref = s.Ref, StartSec = s.StartSec, DurationSec = s.DurationSec, Shuffle = s.Shuffle };
}

public sealed class EditButton
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Label { get; set; } = "Neu";
    public string? Subtitle { get; set; }
    public string? Icon { get; set; }
    public string Color { get; set; } = "#C8102E";
    public TileSize Size { get; set; } = TileSize.Normal;
    public int X { get; set; } = -1;
    public int Y { get; set; } = -1;
    public int W { get; set; }
    public int H { get; set; }
    public NodeKind Kind { get; set; } = NodeKind.Sound;
    public List<EditSound> Songs { get; set; } = new() { new EditSound() };
    public bool SongsRandom { get; set; }
    public List<EditButton> Children { get; set; } = new();
    public bool Panic { get; set; }

    public EditSound FirstSong => Songs.Count > 0 ? Songs[0] : Songs.AddAndReturn(new EditSound());

    public static EditButton From(ShowButton b)
    {
        var kind = b.IsFolder ? NodeKind.Folder : NodeKind.Sound;
        var songs = b.Songs is { Count: > 0 } ? b.Songs.Select(EditSound.From).ToList()
            : b.Sound is { } s ? new List<EditSound> { EditSound.From(s) }
            : b.Pool is { Count: > 0 } ? b.Pool.Select(EditSound.From).ToList()
            : new List<EditSound> { new EditSound() };
        return new EditButton
        {
            Id = b.Id,
            Label = b.Label,
            Subtitle = b.Subtitle,
            Icon = b.Icon,
            Color = b.Color ?? "#C8102E",
            Size = b.Size,
            X = b.X, Y = b.Y, W = b.W, H = b.H,
            Kind = kind,
            Panic = b.Panic,
            Songs = songs,
            SongsRandom = b.SongsRandom || (b.Songs is null && b.Pool is { Count: > 0 }),
            Children = (b.Children ?? []).Select(From).ToList(),
        };
    }

    public ShowButton ToModel() => Kind switch
    {
        _ when Panic => new ShowButton(Id, Label, Icon, Color, Size, null, null, Subtitle, null, X, Y, W, H, true),
        NodeKind.Folder => new ShowButton(Id, Label, Icon, Color, Size, Children.Select(c => c.ToModel()).ToList(), null, Subtitle, null, X, Y, W, H),
        _ => new ShowButton(Id, Label, Icon, Color, Size, null,
            Songs.Count == 1 ? Songs[0].ToModel() : null, Subtitle, null, X, Y, W, H, false,
            Songs.Select(s => s.ToModel()).ToList(), SongsRandom),
    };
}

internal static class EditSoundListExtensions
{
    public static EditSound AddAndReturn(this List<EditSound> list, EditSound s) { list.Add(s); return s; }
}

public sealed class EditProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..6];
    public string Name { get; set; } = "Neues Profil";
    public string Color { get; set; } = "#C8102E";
    public List<EditButton> Root { get; set; } = new();

    public static EditProfile From(ShowProfile p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Color = p.Color ?? "#C8102E",
        Root = p.Root.Select(EditButton.From).ToList(),
    };

    public ShowProfile ToModel() => new(Id, Name, Color, Root.Select(b => b.ToModel()).ToList());
}
