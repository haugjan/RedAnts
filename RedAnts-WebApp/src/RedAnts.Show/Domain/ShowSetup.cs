namespace RedAnts.Show.Domain;

public sealed record ShowSetup(IReadOnlyList<OverlayTitle> Titles, IReadOnlyList<LedTemplate> Led)
{
    public static ShowSetup Default => new(Hall.DefaultOverlayTitles, Hall.DefaultLedTemplates);

    public static readonly ShowSetup Empty = new([], []);

    public bool IsEmpty => Titles.Count == 0 && Led.Count == 0;
}
