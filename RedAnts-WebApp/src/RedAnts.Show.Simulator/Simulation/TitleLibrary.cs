using System.Text.Json;

namespace RedAnts.Show.Simulator.Simulation;

public sealed record TitleElement(
    string Kind = "",
    string Name = "",
    double X = 0,
    double Y = 0,
    double W = 0,
    double H = 0,
    double R = 0,
    string? Fill = null,
    string? Stroke = null,
    string? Text = null,
    double Size = 20,
    string? Weight = null,
    string? Align = null,
    string? Valign = null,
    string? Src = null);

public sealed record TitleLayout(string Name = "", double Width = 1920, double Height = 1080, IReadOnlyList<TitleElement>? Elements = null)
{
    public IReadOnlyList<TitleElement> All => Elements ?? [];

    public IEnumerable<string> TextFields => All.Where(e => e.Kind == "text").Select(e => e.Name + ".Text");

    public IEnumerable<string> ImageFields => All.Where(e => e.Kind == "image").Select(e => e.Name + ".Source");
}

public sealed class TitleLibrary
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, TitleLayout> _layouts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _logos = new(StringComparer.OrdinalIgnoreCase);

    public TitleLibrary(IWebHostEnvironment environment, EventLog log)
    {
        var root = Path.Combine(environment.WebRootPath ?? "wwwroot", "titles");
        if (!Directory.Exists(root))
        {
            log.Add("vMix", $"Keine Einblender gefunden unter {root}");
            return;
        }
        foreach (var file in Directory.GetFiles(root, "*.json").Where(f => !f.EndsWith("index.json", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                if (JsonSerializer.Deserialize<TitleLayout>(File.ReadAllText(file), Json) is { } layout && layout.All.Count > 0)
                    _layouts[Path.GetFileNameWithoutExtension(file) + ".gtzip"] = layout;
            }
            catch (JsonException e) { log.Add("vMix", $"Einblender {Path.GetFileName(file)}: {e.Message}"); }
        }
        var logos = Path.Combine(root, "logos");
        if (Directory.Exists(logos))
            foreach (var logo in Directory.GetFiles(logos, "*.png"))
                _logos.Add(Path.GetFileName(logo));
        log.Add("vMix", $"{_layouts.Count} Einblender und {_logos.Count} Teamlogos geladen");
    }

    public IReadOnlyList<string> Titles => _layouts.Keys.OrderBy(k => k).ToList();

    public TitleLayout? Of(string? title) =>
        title is { Length: > 0 } && _layouts.TryGetValue(title, out var layout) ? layout : null;

    public string? Image(string? designSource, string? liveValue)
    {
        if (liveValue is { Length: > 0 })
        {
            var name = Path.GetFileName(liveValue.Replace('\\', '/'));
            if (_logos.Contains(name)) return "titles/logos/" + name;
            if (name.Length > 0 && File.Exists(Path.Combine("wwwroot", "titles", "img", name))) return "titles/img/" + name;
        }
        return designSource is { Length: > 0 } ? "titles/img/" + designSource : null;
    }
}
