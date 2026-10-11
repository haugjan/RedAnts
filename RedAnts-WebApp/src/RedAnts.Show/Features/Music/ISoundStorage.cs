namespace RedAnts.Show.Features.Music;

public interface ISoundStorage
{
    Task<string> SaveAsync(string fileName, Stream content, string? contentType);

    string UrlOf(string reference);
}
