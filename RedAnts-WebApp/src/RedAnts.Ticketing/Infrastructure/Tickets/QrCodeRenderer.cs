using QRCoder;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Infrastructure.Tickets;

public sealed class QrCodeRenderer : IQrCodeRenderer
{
    public string RenderSvg(string content, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(pixelsPerModule);
    }

    public string RenderPngDataUri(string content, int pixelsPerModule = 6) =>
        "data:image/png;base64," + Convert.ToBase64String(RenderPng(content, pixelsPerModule));

    public byte[] RenderPng(string content, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
    }
}
