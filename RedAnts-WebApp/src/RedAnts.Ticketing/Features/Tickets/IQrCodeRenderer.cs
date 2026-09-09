namespace RedAnts.Ticketing.Features.Tickets;

public interface IQrCodeRenderer
{
    string RenderSvg(string content, int pixelsPerModule = 6);

    string RenderPngDataUri(string content, int pixelsPerModule = 6);

    byte[] RenderPng(string content, int pixelsPerModule = 6);
}
