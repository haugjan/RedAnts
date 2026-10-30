using System.Net;
using System.Text;

namespace RedAnts.Show.Agent.Web;

public static class HallChooser
{
    public const string Path = "/bord/halle";
    public const string SwitchPath = "/bord/halle/wechseln";
    public const int SwitchExitCode = 3;

    const string Style =
        "body{margin:0;min-height:100vh;background:#0f0f13;color:#e9edf1;font-family:'Segoe UI',Verdana,sans-serif}" +
        "main{max-width:960px;margin:0 auto;padding:40px 16px}" +
        "h1{font-size:2rem;margin:0 0 6px}" +
        "p{color:#9aa3ad;margin:0 0 24px}" +
        ".grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:16px}" +
        ".grid form{margin:0}" +
        ".hall{width:100%;min-height:120px;border:2px solid #343442;border-radius:12px;background:#1d1d26;color:#fff;font:inherit;font-size:1.6rem;font-weight:700;cursor:pointer;padding:16px;text-align:left}" +
        ".hall:hover,.hall:focus{border-color:#d62d38;background:#262631}" +
        ".hall.last{border-color:#d62d38}" +
        ".hall small{display:block;font-size:.85rem;font-weight:400;color:#9aa3ad;margin-top:8px}" +
        ".box{background:#16161d;border:1px solid #272730;border-radius:12px;padding:24px;max-width:420px}" +
        "input{width:100%;padding:.8rem;border:1px solid #2f2f3a;border-radius:8px;background:#0f0f13;color:#fff;font-size:1.1rem;box-sizing:border-box}" +
        ".go{margin-top:14px;width:100%;padding:.8rem;border:0;border-radius:8px;background:#d62d38;color:#fff;font-weight:700;font-size:1.1rem;cursor:pointer}" +
        ".quiet{margin-top:10px;background:#262631}" +
        ".err{color:#ff9b9b}";

    public static async Task<AgentConfig?> RunAsync(AgentConfig config, string[] args)
    {
        var current = config;
        var chosen = new TaskCompletionSource<AgentConfig?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var directory = new HallDirectory();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseUrls($"http://{config.Host}:{config.DeckPort}");
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        app.UseBoardGate(config.BoardPassword, "Red Ants Board");

        app.MapGet("/info", () => Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
        app.MapGet("/deck/{**rest}", () => Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
        app.MapGet("/favicon.ico", () => Results.File(System.IO.Path.Combine(AppContext.BaseDirectory, "board.ico"), "image/x-icon"));

        app.MapGet(Path, async (string? fehler, CancellationToken stop) =>
        {
            if (current.CloudPassword.Length == 0) return Html(PasswordPage(current, false));
            var list = await directory.ListAsync(current, stop);
            return list.Outcome switch
            {
                HallListOutcome.WrongPassword => Html(PasswordPage(current, true)),
                HallListOutcome.Unreachable => Html(OfflinePage(current, list.Message)),
                _ => Html(HallsPage(current, list.Halls, fehler)),
            };
        });

        app.MapPost(Path + "/passwort", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            current = current with { CloudPassword = form["password"].ToString().Trim() };
            current.Save();
            return Results.Redirect(Path);
        });

        app.MapPost(Path + "/zuletzt", () =>
        {
            if (!current.IsComplete) return Results.Redirect(Path);
            chosen.TrySetResult(current);
            return Html(ConnectingPage(current.HallName.Length > 0 ? current.HallName : current.HallId));
        });

        app.MapPost(Path + "/{id}", async (string id, CancellationToken stop) =>
        {
            var access = await directory.AccessAsync(current, id, stop);
            if (access is null) return Results.Redirect(Path + "?fehler=" + Uri.EscapeDataString(id));
            current = current with { HallId = access.Id.ToLowerInvariant(), Key = access.Key, HallName = access.Name };
            current.Save();
            chosen.TrySetResult(current);
            return Html(ConnectingPage(access.Name));
        });

        app.MapFallback(() => Results.Redirect(Path));

        app.Lifetime.ApplicationStopping.Register(() => chosen.TrySetResult(null));
        await app.StartAsync();
        Console.WriteLine($"Red Ants Board {AgentConfig.Version}: Halle wählen auf {config.BoardUrl}{Path}");

        var result = await chosen.Task;
        await Task.Delay(TimeSpan.FromMilliseconds(600));
        await app.StopAsync();
        return result;
    }

    static IResult Html(string body) => Results.Content(body, "text/html; charset=utf-8", Encoding.UTF8);

    static string Page(string title, string body, string script = "") =>
        "<!DOCTYPE html><html lang=\"de\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
        $"<title>{E(title)}</title><link rel=\"icon\" href=\"/favicon.ico\"><style>{Style}</style></head>" +
        $"<body><main>{body}</main>{script}</body></html>";

    static string HallsPage(AgentConfig config, IReadOnlyList<HallChoice> halls, string? failed)
    {
        var body = new StringBuilder("<h1>Welche Halle?</h1><p>Ein Klick verbindet das Board mit der Halle.</p>");
        if (failed is { Length: > 0 })
            body.Append($"<p class=\"err\">Die Halle „{E(failed)}“ konnte nicht verbunden werden. Bitte nochmals versuchen.</p>");
        if (halls.Count == 0) body.Append("<p>In der Cloud ist noch keine Halle erfasst (Backoffice → Show → Hallen).</p>");
        body.Append("<div class=\"grid\">");
        foreach (var hall in halls)
        {
            var last = hall.Id == config.HallId;
            var note = last ? "zuletzt benutzt" : hall.AgentOnline ? "dort läuft schon ein Board" : "";
            body.Append($"<form method=\"post\" action=\"{Path}/{E(Uri.EscapeDataString(hall.Id))}\">")
                .Append($"<button class=\"hall{(last ? " last" : "")}\" type=\"submit\"{(last ? " autofocus" : "")}>{E(hall.Name)}")
                .Append(note.Length > 0 ? $"<small>{E(note)}</small>" : "")
                .Append("</button></form>");
        }
        body.Append("</div>");
        return Page("Welche Halle?", body.ToString());
    }

    static string PasswordPage(AgentConfig config, bool wrong) =>
        Page("Red Ants Board", "<h1>Red Ants Board</h1><p>Einmal das Bord-Passwort eingeben, dann kommen die Hallen aus der Cloud.</p>" +
            $"<form class=\"box\" method=\"post\" action=\"{Path}/passwort\">" +
            "<input type=\"password\" name=\"password\" placeholder=\"Bord-Passwort\" autofocus autocomplete=\"current-password\">" +
            "<button class=\"go\" type=\"submit\">Weiter</button>" +
            (wrong ? "<p class=\"err\">Das Passwort passt nicht.</p>" : "") +
            $"<p>Dasselbe Passwort wie für {E(config.Cloud)}/bord.</p></form>");

    static string OfflinePage(AgentConfig config, string message)
    {
        var body = new StringBuilder("<h1>Keine Verbindung zur Cloud</h1>")
            .Append($"<p>{E(config.Cloud)} antwortet nicht ({E(message)}).</p>")
            .Append("<div class=\"box\">")
            .Append($"<form method=\"get\" action=\"{Path}\"><button class=\"go\" type=\"submit\">Nochmals versuchen</button></form>");
        if (config.IsComplete)
        {
            var name = config.HallName.Length > 0 ? config.HallName : config.HallId;
            body.Append($"<form method=\"post\" action=\"{Path}/zuletzt\"><button class=\"go quiet\" type=\"submit\">Ohne Internet in {E(name)} starten</button></form>");
        }
        body.Append("</div>");
        return Page("Keine Verbindung", body.ToString());
    }

    static string ConnectingPage(string hall) =>
        Page("Verbinde …", $"<h1>{E(hall)}</h1><p>Das Board startet, gleich geht es weiter …</p>",
            "<script>(function poll(){fetch('/info',{cache:'no-store'}).then(function(r){if(r.ok){location.replace('/bord')}else{setTimeout(poll,700)}}).catch(function(){setTimeout(poll,700)})})()</script>");

    public static string SwitchingPage() =>
        Page("Halle wechseln", "<h1>Halle wechseln</h1><p>Das Board startet neu, gleich kommt die Hallenwahl …</p>",
            "<script>setTimeout(function poll(){fetch('/info',{cache:'no-store'}).then(function(r){if(r.status===503){location.replace('" + Path + "')}else{setTimeout(poll,700)}}).catch(function(){setTimeout(poll,700)})},1500)</script>");

    static string E(string text) => WebUtility.HtmlEncode(text);
}
