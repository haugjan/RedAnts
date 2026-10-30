using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Music;

public sealed class MusicCache(AgentConfig config, PayloadStore payload, ILogger<MusicCache> log) : BackgroundService
{
    static readonly TimeSpan Idle = TimeSpan.FromMinutes(5);

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(2) };
    readonly SemaphoreSlim _one = new(1, 1);

    TaskCompletionSource _wake = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static string Folder => Path.Combine(AgentConfig.DataDir, "musik");

    public int Cached { get; private set; }

    public string? Fault { get; private set; }

    public string? Local(string reference)
    {
        var file = PathOf(reference);
        return file is not null && File.Exists(file) ? file : null;
    }

    public async Task<string?> EnsureAsync(string reference, CancellationToken stop)
    {
        if (Local(reference) is { } ready) return ready;
        var file = PathOf(reference);
        if (file is null) return null;
        await _one.WaitAsync(stop);
        try
        {
            if (File.Exists(file)) return file;
            var path = string.Join('/', reference.Split('/').Select(Uri.EscapeDataString));
            var url = $"{config.Cloud}/dj/sound/{path}";
            using var answer = await _http.GetAsync(url, stop);
            if (!answer.IsSuccessStatusCode)
            {
                Fault = $"{reference}: {(int)answer.StatusCode}";
                log.LogWarning("Musik-Zwischenspeicher: {Reference} antwortet mit {Status}", reference, (int)answer.StatusCode);
                return null;
            }
            var kind = answer.Content.Headers.ContentType?.MediaType ?? "";
            if (!kind.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                && !kind.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                && !kind.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                Fault = $"{reference}: Antwort ist {kind}, kein Ton";
                log.LogWarning("Musik-Zwischenspeicher: {Reference} kam als {Kind} zurück, nicht als Ton", reference, kind);
                return null;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var temp = file + ".teil";
            await using (var target = File.Create(temp))
                await answer.Content.CopyToAsync(target, stop);
            File.Move(temp, file, overwrite: true);
            Fault = null;
            return file;
        }
        catch (Exception e) when (e is HttpRequestException or IOException or TaskCanceledException)
        {
            Fault = $"{reference}: {e.Message}";
            log.LogWarning("Musik-Zwischenspeicher: {Reference} gescheitert: {Message}", reference, e.Message);
            return null;
        }
        finally { _one.Release(); }
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        payload.Changed += Wake;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                try { await FillAsync(stop); }
                catch (OperationCanceledException) { break; }
                catch (Exception e) { log.LogDebug("Musik-Zwischenspeicher: {Message}", e.Message); }
                try { await _wake.Task.WaitAsync(Idle, stop); }
                catch (TimeoutException) { }
                catch (OperationCanceledException) { break; }
                _wake = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
        finally { payload.Changed -= Wake; }
    }

    void Wake() => _wake.TrySetResult();

    async Task FillAsync(CancellationToken stop)
    {
        var wanted = References().ToList();
        var missing = wanted.Where(r => Local(r) is null).ToList();
        if (missing.Count > 0)
            log.LogInformation("Musik-Zwischenspeicher: {Missing} von {Total} Stücken fehlen noch", missing.Count, wanted.Count);
        foreach (var reference in missing)
        {
            if (stop.IsCancellationRequested) return;
            await EnsureAsync(reference, stop);
        }
        Cached = wanted.Count(r => Local(r) is not null);
        if (missing.Count > 0)
            log.LogInformation("Musik-Zwischenspeicher: {Cached} von {Total} Stücken liegen lokal", Cached, wanted.Count);
    }

    IEnumerable<string> References()
    {
        foreach (var item in payload.Music)
            foreach (var reference in MusicSongs.References(item.SongsJson))
                yield return reference;
    }

    static string? PathOf(string reference)
    {
        if (reference.Length == 0 || reference.Contains("..", StringComparison.Ordinal)) return null;
        var clean = reference.Replace('\\', '/').TrimStart('/');
        if (Path.IsPathRooted(clean)) return null;
        return Path.Combine(Folder, clean.Replace('/', Path.DirectorySeparatorChar));
    }
}
