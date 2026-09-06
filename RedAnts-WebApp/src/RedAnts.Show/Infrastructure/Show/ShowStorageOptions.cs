namespace RedAnts.Infrastructure.Show;

public sealed class ShowStorageOptions
{
    public const string SectionName = "Show:Storage";

    public string? AccountUrl { get; set; }
    public string? ConnectionString { get; set; }
    public string Container { get; set; } = "show";
    public string? PublicBaseUrl { get; set; }
}
