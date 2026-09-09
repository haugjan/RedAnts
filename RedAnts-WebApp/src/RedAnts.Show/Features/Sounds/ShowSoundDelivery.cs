using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace RedAnts.Show.Features.Sounds;

public static class ShowSoundDelivery
{
    public const string BoardBase = "/show/sound/";
    public const string AdminBase = "/admin/show/sound/";

    private const string CachePolicy = "private, max-age=31536000, immutable";

    public static async Task<IActionResult> StreamShowSoundAsync(
        this ControllerBase controller, OpenShowSound.Handler sounds, string? path)
    {
        var sound = await sounds.HandleAsync(new OpenShowSound.Query(path));
        if (sound is null) return controller.NotFound();

        controller.Response.Headers.CacheControl = CachePolicy;

        return string.IsNullOrWhiteSpace(sound.ETag)
            ? controller.File(sound.Content, sound.ContentType, enableRangeProcessing: true)
            : controller.File(sound.Content, sound.ContentType, sound.LastModified,
                new EntityTagHeaderValue($"\"{sound.ETag.Trim('"')}\""), enableRangeProcessing: true);
    }
}
