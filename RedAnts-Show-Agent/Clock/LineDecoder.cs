using System.Text;

namespace RedAnts.Show.Agent.Clock;

public static class LineDecoder
{
    public static string Decode(byte[] buffer) =>
        Encoder(Detect(buffer)).GetString(buffer).Trim('\0', '\r', '\n').Trim();

    static Encoding Encoder(string encoding) => encoding switch
    {
        "utf-32be" => new UTF32Encoding(bigEndian: true, byteOrderMark: false),
        "utf-16be" => new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
        "latin1" => Encoding.Latin1,
        _ => new UTF8Encoding(false),
    };

    static string Detect(byte[] buffer)
    {
        if (buffer.Length == 0) return "utf-8";
        if (buffer.Length % 4 == 0 && IsPadded(buffer, 4, 3)) return "utf-32be";
        if (buffer.Length % 2 == 0 && IsPadded(buffer, 2, 1)) return "utf-16be";
        return IsValidUtf8(buffer) ? "utf-8" : "latin1";
    }

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
