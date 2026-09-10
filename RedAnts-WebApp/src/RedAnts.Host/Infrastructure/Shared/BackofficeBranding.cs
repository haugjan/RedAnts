using System.Text;
using System.Text.RegularExpressions;

namespace RedAnts.Infrastructure.Shared;

public static partial class BackofficeBrandingExtensions
{
    private const string PageTitle = "Red Ants - Admin";
    private const string FaviconLink = "<link rel=\"icon\" href=\"/favicons/favicon.ico\" />";

    [GeneratedRegex("<link[^>]*rel=\"icon\"[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex IconLink();

    [GeneratedRegex("<title>[^<]*</title>", RegexOptions.IgnoreCase)]
    private static partial Regex Title();

    public static WebApplication UseBackofficeBranding(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (!HttpMethods.IsGet(context.Request.Method)
                || !path.StartsWithSegments("/umbraco")
                || path.StartsWithSegments("/umbraco/backoffice")
                || path.StartsWithSegments("/umbraco/management")
                || path.StartsWithSegments("/umbraco/swagger")
                || context.WebSockets.IsWebSocketRequest)
            {
                await next();
                return;
            }

            var original = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;
            try
            {
                await next();
                context.Response.Body = original;
                var contentType = context.Response.ContentType ?? "";
                var compressed = context.Response.Headers.ContentEncoding.Count > 0;
                buffer.Seek(0, SeekOrigin.Begin);
                if (!context.Response.HasStarted && !compressed
                    && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(buffer, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    var branded = Title().Replace(body, $"<title>{PageTitle}</title>", 1);
                    branded = IconLink().Replace(branded, FaviconLink, 1);
                    var bytes = Encoding.UTF8.GetBytes(branded);
                    context.Response.ContentLength = bytes.Length;
                    await original.WriteAsync(bytes);
                }
                else
                {
                    await buffer.CopyToAsync(original);
                }
            }
            finally
            {
                context.Response.Body = original;
            }
        });

        return app;
    }
}
