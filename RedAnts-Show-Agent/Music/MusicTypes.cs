namespace RedAnts.Show.Agent.Music;

public static class MusicTypes
{
    public static string Of(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".mp3" => "audio/mpeg",
        ".m4a" or ".mp4" or ".aac" => "audio/mp4",
        ".ogg" or ".oga" => "audio/ogg",
        ".wav" => "audio/wav",
        ".flac" => "audio/flac",
        ".opus" => "audio/opus",
        _ => "application/octet-stream",
    };
}
