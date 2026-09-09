using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RedAnts.Show.Features.Ports;

namespace RedAnts.Show.Features;

public static class ShowSoundDelivery
{
    public const string BoardBase = "/show/sound/";
    public const string AdminBase = "/admin/show/sound/";

    private const string CachePolicy = "private, max-age=31536000, immutable";

    public static async Task<IActionResult> StreamShowSoundAsync(
        this ControllerBase controller, IShowSoundUploader sounds, string? path)
    {
        if (!IsWithinContainer(path)) return controller.NotFound();

        var sound = await sounds.OpenReadAsync(path!);
        if (sound is null) return controller.NotFound();

        controller.Response.Headers.CacheControl = CachePolicy;

        return string.IsNullOrWhiteSpace(sound.ETag)
            ? controller.File(sound.Content, sound.ContentType, enableRangeProcessing: true)
            : controller.File(sound.Content, sound.ContentType, sound.LastModified,
                new EntityTagHeaderValue($"\"{sound.ETag.Trim('"')}\""), enableRangeProcessing: true);
    }

    private static bool IsWithinContainer(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && !path.StartsWith('/')
        && !path.Contains('\\')
        && !path.Split('/').Any(segment => segment is "." or "..");
}
