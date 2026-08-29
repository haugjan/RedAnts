namespace RedAnts.Features.Show;

public enum NodeKind { Sound, Folder, Random }

public sealed class EditSound
{
    public SoundKind Kind { get; set; } = SoundKind.Local;
    public string Ref { get; set; } = "";
    public int StartSec { get; set; }
    public int? DurationSec { get; set; }

    public EditSound Clone() => new() { Kind = Kind, Ref = Ref, StartSec = StartSec, DurationSec = DurationSec };
    public ShowSound ToModel() => new(Kind, Ref, StartSec, DurationSec);
    public static EditSound From(ShowSound s) => new() { Kind = s.Kind, Ref = s.Ref, StartSec = s.StartSec, DurationSec = s.DurationSec };
}

public sealed class EditButton
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Label { get; set; } = "Neu";
    public string? Subtitle { get; set; }
    public string? Icon { get; set; }
    public string Color { get; set; } = "#C8102E";
    public TileSize Size { get; set; } = TileSize.Normal;
    public NodeKind Kind { get; set; } = NodeKind.Sound;
    public EditSound Sound { get; set; } = new();
    public List<EditButton> Children { get; set; } = new();
    public List<EditSound> Pool { get; set; } = new();

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
            Kind = kind,
            Sound = b.Sound is { } s ? EditSound.From(s) : new EditSound(),
            Children = (b.Children ?? []).Select(From).ToList(),
            Pool = (b.Pool ?? []).Select(EditSound.From).ToList(),
        };
    }

    public ShowButton ToModel() => Kind switch
    {
        NodeKind.Folder => new ShowButton(Id, Label, Icon, Color, Size, Children.Select(c => c.ToModel()).ToList(), null, Subtitle),
        NodeKind.Random => new ShowButton(Id, Label, Icon, Color, Size, null, null, Subtitle, Pool.Select(p => p.ToModel()).ToList()),
        _ => new ShowButton(Id, Label, Icon, Color, Size, null, Sound.ToModel(), Subtitle),
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
