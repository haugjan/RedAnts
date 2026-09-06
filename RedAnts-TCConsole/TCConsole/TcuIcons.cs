using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TcuConsole;

/// <summary>
/// Liefert die Button-Bilder als Base64 für die Companion-Config.
///
/// Companion nimmt style.png als Base64 entgegen und ergänzt das
/// "data:image/png;base64,"-Präfix beim Import selbst (Funktion P$r in
/// main.js) — beide Formen sind zulässig. Hier wird bewusst nur der nackte
/// Base64-Text geschrieben, das hält die Config kleiner.
///
/// Die Dateien liegen neben der Exe unter Icons\ (siehe csproj). Fehlt eine,
/// bleibt der Button ohne Bild statt den ganzen Export scheitern zu lassen —
/// die Beschriftung trägt die Bedeutung ohnehin mit.
/// </summary>
public static class TcuIcons
{
    // ── Namen der Bilddateien (ohne .png) ────────────────────────────────────
    public const string Tor            = "ball";
    public const string Coach          = "coach";
    public const string Timeout        = "timeout";
    public const string Aufstellung    = "aufstellung";
    public const string Spieler        = "spieler";
    public const string BestPlayer     = "bestplayer";
    public const string Starting6      = "starting6";
    public const string Strafe         = "strafe";
    public const string Meldung        = "meldungen";
    public const string Drittel        = "drittel";
    public const string Resultat       = "resultat";
    public const string Highlight      = "highlights";
    public const string Opener         = "opener";
    public const string Kommentar      = "kommentar";
    public const string Schiedsrichter = "schiedsrichter";
    public const string EinblenderEin  = "einblender-ein";
    public const string EinblenderAus  = "einblender-aus";
    public const string Werbung        = "werbung";
    // Headset — für die Sprechverbindung. Bewusst die zweite Kommentar-Variante,
    // damit sie sich vom Kommentar-Knopf unterscheidet.
    public const string Sprech         = "kommentar-b";

    static readonly Dictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);
    static readonly object _lock = new();

    static string Dir => Path.Combine(AppContext.BaseDirectory, "Icons");

    // Aufteilung der Knopffläche.
    //
    // Companion skaliert das Bild immer auf die ganze Taste und lässt die
    // Textfläche nicht begrenzen: mit size "auto" wächst die Beschriftung über
    // den ganzen Knopf und bricht um. Deshalb wird hier BEIDES ins Bild
    // gerechnet — Beschriftung oben, Motiv darunter — und der Knopf selbst
    // trägt keinen Text mehr. Nur so sitzt die Schrift zuverlässig im oberen
    // Drittel und bleibt einzeilig.
    const int Canvas   = 144;
    const int TextArea = Canvas / 3;        // 48 px: oberes Drittel für die Schrift
    const int IconArea = Canvas - TextArea; // 96 px: untere zwei Drittel für das Motiv
    const int SidePad  = 4;

    /// <summary>
    /// Base64 des fertigen Knopfbildes: einzeilige Beschriftung im oberen
    /// Drittel, Motiv in den unteren zwei Dritteln. null, wenn die Bilddatei
    /// fehlt.
    /// </summary>
    /// <param name="label">Beschriftung; leer lässt das obere Drittel frei.</param>
    /// <param name="argbText">Schriftfarbe als 0xRRGGBB — muss zum
    /// Knopfhintergrund passen, den Companion dahinter zeichnet.</param>
    public static string? Get(string name, string label = "", int argbText = 0x1A1A1A)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var key = $"{name}|{label}|{argbText:X6}";
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;

            string? data = null;
            var file = Path.Combine(Dir, name + ".png");
            try
            {
                if (File.Exists(file)) data = Compose(file, label, argbText);
            }
            catch (Exception)
            {
                // Aufbereitung fehlgeschlagen: lieber das Rohbild als keins.
                try { if (File.Exists(file)) data = Convert.ToBase64String(File.ReadAllBytes(file)); }
                catch { }
            }

            _cache[key] = data;
            return data;
        }
    }

    /// <summary>Zeichnet das Bild seitenverhältnistreu in die unteren zwei
    /// Drittel einer quadratischen, sonst durchsichtigen Leinwand.
    ///
    /// Läuft zwingend auf einem STA-Thread: RenderTargetBitmap liefert im MTA
    /// still ein leeres Bild, ohne Fehler zu melden. Eine Konsolenanwendung ist
    /// standardmässig MTA, deshalb wird hier ein eigener Thread aufgesetzt.
    /// </summary>
    static string Compose(string file, string label, int argbText)
    {
        string? result = null;
        Exception? failure = null;

        var t = new Thread(() =>
        {
            try { result = ComposeCore(file, label, argbText); }
            catch (Exception ex) { failure = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();

        if (failure is not null) throw failure;
        return result ?? throw new InvalidOperationException("Bildaufbereitung lieferte nichts");
    }

    static string ComposeCore(string file, string label, int argbText)
    {
        var src = new BitmapImage();
        src.BeginInit();
        src.UriSource   = new Uri(file);
        src.CacheOption = BitmapCacheOption.OnLoad;   // Datei sofort freigeben
        src.EndInit();

        // Die Vorlagen haben einen breiten durchsichtigen Rand. Ohne Beschnitt
        // füllt das Motiv nur etwa die Hälfte der Fläche, obwohl ihm zwei
        // Drittel zustehen — deshalb zuerst auf den tatsächlich gezeichneten
        // Bereich zurechtschneiden.
        var trimmed = TrimTransparent(src);

        // Beide Brüche ausdrücklich als double: mit Ganzzahldivision wäre
        // 136/144 = 0 und das Bild bliebe leer.
        double maxW = Canvas - 2 * SidePad;
        var scale = Math.Min(maxW / trimmed.PixelWidth, (double)IconArea / trimmed.PixelHeight);
        var w = trimmed.PixelWidth * scale;
        var h = trimmed.PixelHeight * scale;
        var x = (Canvas - w) / 2;
        var y = TextArea + (IconArea - h) / 2;   // unter dem Textband

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(trimmed, new Rect(x, y, w, h));

            if (label.Length > 0)
            {
                var text = FitText(label, argbText);
                // Im oberen Drittel senkrecht zentriert
                var tx = (Canvas - text.Width) / 2;
                var ty = (TextArea - text.Height) / 2;
                dc.DrawText(text, new Point(tx, ty));
            }
        }

        var target = new RenderTargetBitmap(Canvas, Canvas, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>Einzeilige Beschriftung, so gross wie sie ins untere Drittel
    /// passt. Bricht bewusst nie um — bei Bedarf wird verkleinert und erst als
    /// letztes Mittel gekürzt.</summary>
    static FormattedText FitText(string label, int argbText)
    {
        var brush = new SolidColorBrush(Color.FromRgb(
            (byte)((argbText >> 16) & 0xFF), (byte)((argbText >> 8) & 0xFF), (byte)(argbText & 0xFF)));
        var face  = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var maxW  = Canvas - 2 * SidePad;

        FormattedText Make(string s, double size) => new(
            s, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            face, size, brush, 1.0) { MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };

        for (var size = 30.0; size >= 12.0; size -= 1.0)
        {
            var t = Make(label, size);
            if (t.Width <= maxW && t.Height <= TextArea) return t;
        }

        // Passt selbst klein nicht: auf die Breite beschneiden lassen.
        var last = Make(label, 12.0);
        last.MaxTextWidth = maxW;
        return last;
    }

    /// <summary>Schneidet den vollständig durchsichtigen Rand ab.</summary>
    static BitmapSource TrimTransparent(BitmapSource src)
    {
        var bgra = src.Format == PixelFormats.Bgra32 ? src : new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
        int w = bgra.PixelWidth, h = bgra.PixelHeight, stride = w * 4;
        var px = new byte[stride * h];
        bgra.CopyPixels(px, stride, 0);

        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (var yy = 0; yy < h; yy++)
            for (var xx = 0; xx < w; xx++)
                if (px[yy * stride + xx * 4 + 3] > 8)
                {
                    if (xx < minX) minX = xx;
                    if (xx > maxX) maxX = xx;
                    if (yy < minY) minY = yy;
                    if (yy > maxY) maxY = yy;
                }

        if (maxX < minX || maxY < minY) return bgra;   // komplett leer
        return new CroppedBitmap(bgra, new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
    }

    /// <summary>Welche Icons fehlen? Für die Startmeldung.</summary>
    public static (int found, int missing, List<string> missingNames) Check()
    {
        string[] all = [
            Tor, Coach, Timeout, Aufstellung, Spieler, BestPlayer, Starting6, Strafe,
            Meldung, Drittel, Resultat, Highlight, Opener, Kommentar, Schiedsrichter,
            EinblenderEin, EinblenderAus, Werbung, Sprech,
        ];
        var missing = all.Where(n => Get(n) is null).ToList();
        return (all.Length - missing.Count, missing.Count, missing);
    }
}
