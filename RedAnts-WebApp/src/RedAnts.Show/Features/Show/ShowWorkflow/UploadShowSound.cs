using RedAnts.Features.Show.Ports;

namespace RedAnts.Features.Show.ShowWorkflow;

public static class UploadShowSound
{
    public sealed record Command(string FileName, Stream Content, string? ContentType);

    public sealed class Handler(IShowSoundUploader uploader)
    {
        public Task<string> HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.FileName)) throw new DomainException("Ein Dateiname ist erforderlich.");
            return uploader.UploadAsync(command.FileName, command.Content, command.ContentType);
        }
    }
}
