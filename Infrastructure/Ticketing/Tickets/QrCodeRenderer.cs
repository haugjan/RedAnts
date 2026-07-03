using QRCoder;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

/// <summary>QR rendering via QRCoder's managed encoders (SvgQRCode / PngByteQRCode) — no
/// System.Drawing, so it works on the Linux App Service. ECC level Q (~25% recovery) tolerates a
/// little screen glare/print wear while keeping the code reasonably small.</summary>
public sealed class QrCodeRenderer : IQrCodeRenderer
{
    public string RenderSvg(string content, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new SvgQRCode(data).GetGraphic(pixelsPerModule);
    }

    public string RenderPngDataUri(string content, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(pixelsPerModule);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }
}
