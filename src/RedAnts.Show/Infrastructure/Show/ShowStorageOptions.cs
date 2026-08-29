namespace RedAnts.Infrastructure.Show;

public sealed class ShowStorageOptions
{
    public const string SectionName = "Show:Storage";

    public string? ConnectionString { get; set; }
    public string Container { get; set; } = "show";

    // Public base URL of the blob container that holds the sound files, e.g.
    // https://stredantsdev.blob.core.windows.net/show . Sound refs are relative
    // to this (e.g. "sounds/17_Boxing-Gong.mp3").
    public string? PublicBaseUrl { get; set; }
}
