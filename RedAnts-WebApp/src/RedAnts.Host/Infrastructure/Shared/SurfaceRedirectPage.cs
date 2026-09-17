using System.Net;
using System.Text.Json;

namespace RedAnts.Infrastructure.Shared;

public static class SurfaceRedirectPage
{
    public static string Html(string targetUrl, string hostLabel, int seconds = 5)
    {
        var url = WebUtility.HtmlEncode(targetUrl);
        var label = WebUtility.HtmlEncode(hostLabel);
        var urlJs = JsonSerializer.Serialize(targetUrl);
        return $$"""
            <!doctype html>
            <html lang="de">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="robots" content="noindex,nofollow">
            <meta http-equiv="refresh" content="{{seconds}};url={{url}}">
            <title>Weiterleitung …</title>
            <style>
              :root { --red: #C8102E; }
              html, body { height: 100%; margin: 0; }
              body { display: flex; align-items: center; justify-content: center; background: #101013; color: #e9edf1; font: 16px/1.6 system-ui, 'Segoe UI', Roboto, sans-serif; }
              .card { max-width: 34rem; padding: 2.5rem 2rem; text-align: center; }
              .logo { font-weight: 800; letter-spacing: 0.12em; color: var(--red); font-size: 1rem; margin-bottom: 1.4rem; }
              h1 { font-size: 1.4rem; margin: 0 0 0.6rem; }
              p { color: #aeb6bf; margin: 0.4rem 0; }
              .btn { display: inline-block; margin: 1.3rem 0 0.6rem; padding: 0.7rem 1.4rem; border-radius: 10px; background: var(--red); color: #fff; text-decoration: none; font-weight: 600; }
              .u { color: #7f8790; font-size: 0.85rem; word-break: break-all; }
              .n { color: #fff; font-weight: 700; }
            </style>
            </head>
            <body>
            <div class="card">
              <div class="logo">RED ANTS</div>
              <h1>Diese Adresse ist umgezogen</h1>
              <p>Weiterleitung in <span class="n" id="s">{{seconds}}</span> Sekunden zu <strong>{{label}}</strong>.</p>
              <a class="btn" href="{{url}}">Jetzt weiter</a>
              <p class="u">{{url}}</p>
            </div>
            <script>
            (function () { var n = {{seconds}}, e = document.getElementById('s'); var t = setInterval(function () { n--; if (e) e.textContent = n < 0 ? 0 : n; if (n <= 0) { clearInterval(t); location.replace({{urlJs}}); } }, 1000); })();
            </script>
            </body>
            </html>
            """;
    }
}
