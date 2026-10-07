using System.Text;

namespace RedAnts.GameClock.Clocks;

public static class LineDecoder
{
    public const string Auto = "auto";
    public const string Utf32Be = "utf-32be";
    public const string Utf16Be = "utf-16be";
    public const string Utf8 = "utf-8";
    public const string Latin1 = "latin1";

    public static readonly string[] All = [Auto, Utf32Be, Utf16Be, Utf8, Latin1];

    public static string Decode(byte[] buffer, string encoding) =>
        Encoder(encoding == Auto ? Detect(buffer) : encoding)
            .GetString(buffer)
            .Trim('\0', '\r', '\n')
            .Trim();

    public static string Detect(byte[] buffer)
    {
        if (buffer.Length == 0) return Utf8;
        if (buffer.Length % 4 == 0 && IsPadded(buffer, 4, 3)) return Utf32Be;
        if (buffer.Length % 2 == 0 && IsPadded(buffer, 2, 1)) return Utf16Be;
        return IsValidUtf8(buffer) ? Utf8 : Latin1;
    }

    static Encoding Encoder(string encoding) => encoding switch
    {
        Utf32Be => new UTF32Encoding(bigEndian: true, byteOrderMark: false),
        Utf16Be => new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
        Latin1 => Encoding.Latin1,
        _ => new UTF8Encoding(false),
    };

    static bool IsPadded(byte[] buffer, int width, int zeros)
    {
        for (var i = 0; i < buffer.Length; i += width)
            for (var pad = 0; pad < zeros; pad++)
                if (buffer[i + pad] != 0) return false;
        return true;
    }

    static bool IsValidUtf8(byte[] buffer)
    {
        try
        {
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(buffer);
            return true;
        }
        catch (DecoderFallbackException) { return false; }
    }
}
