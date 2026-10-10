namespace RedAnts.DJ.Features.Sounds;

public sealed class DJStorageOptions
{
    public const string SectionName = "Show:Storage";

    public string? AccountUrl { get; set; }
    public string? ConnectionString { get; set; }
    public string Container { get; set; } = "show";
}
