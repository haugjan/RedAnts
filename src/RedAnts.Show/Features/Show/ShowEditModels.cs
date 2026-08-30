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
    public EditSound Sound { get; set; } = new();
    public List<EditButton> Children { get; set; } = new();
    public List<EditSound> Pool { get; set; } = new();
    public bool Panic { get; set; }

    public static EditButton From(ShowButton b)
    {
        var kind = b.IsFolder ? NodeKind.Folder : b.IsRandom ? NodeKind.Random : NodeKind.Sound;
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
            Sound = b.Sound is { } s ? EditSound.From(s) : new EditSound(),
            Children = (b.Children ?? []).Select(From).ToList(),
            Pool = (b.Pool ?? []).Select(EditSound.From).ToList(),
        };
    }

    public ShowButton ToModel() => Kind switch
    {
        _ when Panic => new ShowButton(Id, Label, Icon, Color, Size, null, null, Subtitle, null, X, Y, W, H, true),
        NodeKind.Folder => new ShowButton(Id, Label, Icon, Color, Size, Children.Select(c => c.ToModel()).ToList(), null, Subtitle, null, X, Y, W, H),
        NodeKind.Random => new ShowButton(Id, Label, Icon, Color, Size, null, null, Subtitle, Pool.Select(p => p.ToModel()).ToList(), X, Y, W, H),
        _ => new ShowButton(Id, Label, Icon, Color, Size, null, Sound.ToModel(), Subtitle, null, X, Y, W, H),
    };
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
