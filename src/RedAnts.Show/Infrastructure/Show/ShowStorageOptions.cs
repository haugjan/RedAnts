namespace RedAnts.Infrastructure.Show;

public sealed class ShowStorageOptions
{
    public const string SectionName = "Show:Storage";

    public string? ConnectionString { get; set; }
    public string Container { get; set; } = "show";
}
