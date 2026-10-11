using RedAnts.DJ.Features.Sounds;

namespace RedAnts.Show.Features.Music.Infrastructure;

public sealed class DJSoundStorage(UploadDJSound.Handler upload) : ISoundStorage
{
    public Task<string> SaveAsync(string fileName, Stream content, string? contentType) =>
        upload.HandleAsync(new UploadDJSound.Command(fileName, content, contentType));

    public string UrlOf(string reference) => SoundMedia.File(reference);
}
