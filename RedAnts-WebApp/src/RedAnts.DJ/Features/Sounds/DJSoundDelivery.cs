using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace RedAnts.DJ.Features.Sounds;

public static class DJSoundDelivery
{
    public const string BoardBase = "/dj/sound/";
    public const string AdminBase = "/admin/dj/sound/";

    private const string CachePolicy = "private, max-age=31536000, immutable";

    public static async Task<IActionResult> StreamDJSoundAsync(
        this ControllerBase controller, OpenDJSound.Handler sounds, string? path)
    {
        var sound = await sounds.HandleAsync(new OpenDJSound.Query(path));
        if (sound is null) return controller.NotFound();

        controller.Response.Headers.CacheControl = CachePolicy;

        return string.IsNullOrWhiteSpace(sound.ETag)
            ? controller.File(sound.Content, sound.ContentType, enableRangeProcessing: true)
            : controller.File(sound.Content, sound.ContentType, sound.LastModified,
                new EntityTagHeaderValue($"\"{sound.ETag.Trim('"')}\""), enableRangeProcessing: true);
    }
}
