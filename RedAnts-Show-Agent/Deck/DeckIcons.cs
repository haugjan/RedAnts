using SkiaSharp;

namespace RedAnts.Show.Agent.Deck;

public static class DeckIcons
{
    public const string Tor = "ball";
    public const string Coach = "coach";
    public const string Timeout = "timeout";
    public const string Aufstellung = "aufstellung";
    public const string Spieler = "spieler";
    public const string BestPlayer = "bestplayer";
    public const string Starting6 = "starting6";
    public const string Strafe = "strafe";
    public const string Meldung = "meldungen";
    public const string Drittel = "drittel";
    public const string Resultat = "resultat";
    public const string Highlight = "highlights";
    public const string Opener = "opener";
    public const string Kommentar = "kommentar";
    public const string Schiedsrichter = "schiedsrichter";
    public const string EinblenderEin = "einblender-ein";
    public const string Werbung = "werbung";

    const int Canvas = 144;
    const int TextArea = Canvas / 3;
    const int IconArea = Canvas - TextArea;
    const int SidePad = 4;

    static readonly Dictionary<string, string?> Cache = new(StringComparer.OrdinalIgnoreCase);
    static readonly object Lock = new();

    public static string Dir => Path.Combine(AppContext.BaseDirectory, "Icons");

    public static string Key(string icon, string label, int textColor) => $"{icon}|{label}|{textColor:X6}";

    public static string? FromKey(string key)
    {
        var parts = key.Split('|');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out var fg)) return null;
        return Get(parts[0], parts[1], fg);
    }

    public static string? Get(string name, string label, int textColor)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = Key(name, label, textColor);
        lock (Lock)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var file = Path.Combine(Dir, name + ".png");
            string? data = null;
            if (File.Exists(file))
            {
                try { data = Compose(file, label, textColor); }
                catch (Exception) { data = Convert.ToBase64String(File.ReadAllBytes(file)); }
            }
            Cache[key] = data;
            return data;
        }
    }

    static string Compose(string file, string label, int textColor)
    {
        using var source = SKBitmap.Decode(file) ?? throw new InvalidOperationException($"{file} ist kein Bild");
        using var trimmed = TrimTransparent(source);
        using var surface = SKSurface.Create(new SKImageInfo(Canvas, Canvas, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var scale = Math.Min((Canvas - 2.0 * SidePad) / trimmed.Width, (double)IconArea / trimmed.Height);
        var width = (float)(trimmed.Width * scale);
        var height = (float)(trimmed.Height * scale);
        var x = (Canvas - width) / 2;
        var y = TextArea + (IconArea - height) / 2;
        using var image = SKImage.FromBitmap(trimmed);
        using var sampling = new SKPaint { IsAntialias = true };
        canvas.DrawImage(image, new SKRect(x, y, x + width, y + height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), sampling);

        if (label.Length > 0) DrawLabel(canvas, label, textColor);

        using var snapshot = surface.Snapshot();
        using var png = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(png.ToArray());
    }

    static void DrawLabel(SKCanvas canvas, string label, int textColor)
    {
        using var typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                             ?? SKTypeface.Default;
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor((byte)((textColor >> 16) & 0xFF), (byte)((textColor >> 8) & 0xFF), (byte)(textColor & 0xFF)),
        };
        var maxWidth = Canvas - 2 * SidePad;
        var text = label.Replace('\n', ' ');
        for (var size = 30f; size >= 12f; size -= 1f)
        {
            using var font = new SKFont(typeface, size);
            if (font.MeasureText(text) > maxWidth) continue;
            Draw(canvas, font, paint, text);
            return;
        }
        using var smallest = new SKFont(typeface, 12f);
        while (text.Length > 1 && smallest.MeasureText(text + "…") > maxWidth) text = text[..^1];
        Draw(canvas, smallest, paint, text + "…");
    }

    static void Draw(SKCanvas canvas, SKFont font, SKPaint paint, string text)
    {
        var width = font.MeasureText(text);
        var metrics = font.Metrics;
        var x = (Canvas - width) / 2;
        var y = (TextArea - (metrics.Descent - metrics.Ascent)) / 2 - metrics.Ascent;
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    static SKBitmap TrimTransparent(SKBitmap source)
    {
        int minX = source.Width, minY = source.Height, maxX = -1, maxY = -1;
        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
                if (source.GetPixel(x, y).Alpha > 8)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
        if (maxX < minX || maxY < minY) return source.Copy();
        var cropped = new SKBitmap(maxX - minX + 1, maxY - minY + 1);
        source.ExtractSubset(cropped, new SKRectI(minX, minY, maxX + 1, maxY + 1));
        return cropped.Copy();
    }
}
