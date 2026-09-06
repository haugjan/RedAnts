using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Infrastructure.Show;

namespace RedAnts.Features.Show;

[Route("show")]
public sealed class ShowController(IShowProfileStore store, IConfiguration config) : Controller
{
    [HttpGet("companion")]
    public async Task<IActionResult> Companion(string? profile, int cols = 8, int rows = 4)
    {
        cols = Math.Clamp(cols, 1, 32);
        rows = Math.Clamp(rows, 1, 32);
        var profiles = await store.GetAllAsync();
        var sel = profiles.FirstOrDefault(p => p.Id == profile) ?? profiles.FirstOrDefault();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var key = config["Show:ApiKey"] ?? config["Show:BoardPassword"] ?? "";

        var tiles = sel?.Root.Where(b => !b.Panic && !b.IsFolder).ToList() ?? new List<ShowButton>();

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"de\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Companion-Konfiguration</title><style>");
        sb.Append("body{margin:0;background:#141418;color:#f1f1f4;font-family:'Segoe UI',sans-serif;padding:20px}");
        sb.Append("h1{font-size:1.3rem}a.btn,button{display:inline-block;background:#E11330;color:#fff;border:none;border-radius:10px;padding:.7rem 1.1rem;font-weight:700;cursor:pointer;text-decoration:none}");
        sb.Append("label{display:inline-flex;flex-direction:column;gap:4px;margin:0 14px 14px 0;font-size:.8rem;color:#9b9ba6;font-weight:700}");
        sb.Append("input,select{background:#0f0f13;color:#fff;border:1px solid #333;border-radius:8px;padding:.5rem}");
        sb.Append("table{border-collapse:collapse;margin-top:18px;width:100%}td,th{border:1px solid #333;padding:.4rem .6rem;text-align:left;font-size:.85rem}");
        sb.Append("code{background:#0f0f13;padding:.1rem .4rem;border-radius:5px;word-break:break-all}a{color:#ff9aa8}</style></head><body>");
        sb.Append("<h1>Companion / Streamdeck-Konfiguration</h1>");
        sb.Append("<form method=\"get\" action=\"/show/companion\">");
        sb.Append("<label>Profil<select name=\"profile\">");
        foreach (var p in profiles)
            sb.Append($"<option value=\"{Enc(p.Id)}\"{(p.Id == sel?.Id ? " selected" : "")}>{Enc(p.Name)}</option>");
        sb.Append("</select></label>");
        sb.Append($"<label>Spalten (horizontal)<input type=\"number\" name=\"cols\" min=\"1\" max=\"32\" value=\"{cols}\"></label>");
        sb.Append($"<label>Zeilen (vertikal)<input type=\"number\" name=\"rows\" min=\"1\" max=\"32\" value=\"{rows}\"></label>");
        sb.Append("<button type=\"submit\">Aktualisieren</button></form>");

        var dl = $"/show/companion/download?profile={Uri.EscapeDataString(sel?.Id ?? "")}&cols={cols}&rows={rows}";
        sb.Append($"<p style=\"margin-top:16px\"><a class=\"btn\" href=\"{Enc(dl)}\">⤓ Companion-Konfiguration herunterladen (.companionconfig)</a></p>");
        sb.Append($"<p style=\"color:#9b9ba6;font-size:.85rem\">Server: <code>{Enc(baseUrl)}</code> · API-Key ist eingebettet. Passt für ein {cols}×{rows}-Raster ({cols * rows} Tasten). {tiles.Count} Kacheln im Profil.</p>");

        sb.Append("<table><tr><th>#</th><th>Kachel</th><th>URL (GET) für Companion HTTP</th></tr>");
        for (var i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            var url = $"{baseUrl}/api/show/play/{t.Id}?key={Uri.EscapeDataString(key)}";
            sb.Append($"<tr><td>{i + 1}</td><td>{Enc(t.Label)}</td><td><code>{Enc(url)}</code></td></tr>");
        }
        sb.Append($"<tr><td>–</td><td>Stopp</td><td><code>{Enc($"{baseUrl}/api/show/stop?key={Uri.EscapeDataString(key)}")}</code></td></tr>");
        sb.Append("</table>");
        sb.Append("<p style=\"color:#9b9ba6;font-size:.8rem;margin-top:16px\">Import in Companion: Buttons → Import → einzelne Seite. Modul „Generic HTTP\". Falls der Import hakt, sag mir deine Companion-Version, dann passe ich das Format an. Alternativ die URLs oben manuell je Taste als „HTTP GET\" hinterlegen.</p>");
        sb.Append("</body></html>");
        return Content(sb.ToString(), "text/html; charset=utf-8");
    }

    [HttpGet("companion/download")]
    public async Task<IActionResult> CompanionDownload(string? profile, int cols = 8, int rows = 4)
    {
        cols = Math.Clamp(cols, 1, 32);
        rows = Math.Clamp(rows, 1, 32);
        var profiles = await store.GetAllAsync();
        var sel = profiles.FirstOrDefault(p => p.Id == profile) ?? profiles.FirstOrDefault();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var key = config["Show:ApiKey"] ?? config["Show:BoardPassword"] ?? "";
        var tiles = sel?.Root.Where(b => !b.Panic && !b.IsFolder).ToList() ?? new List<ShowButton>();

        var controls = new Dictionary<string, object>();
        var idx = 0;
        for (var row = 0; row < rows && idx < tiles.Count; row++)
        {
            for (var col = 0; col < cols && idx < tiles.Count; col++)
            {
                var t = tiles[idx++];
                var url = $"{baseUrl}/api/show/play/{t.Id}?key={Uri.EscapeDataString(key)}";
                controls[$"{row}/{col}"] = Button(t.Label, ColorInt(t.Color), url);
            }
        }
        // Letzte Taste unten rechts: Stopp.
        controls[$"{rows - 1}/{cols - 1}"] = Button("STOP", 0xC8102E, $"{baseUrl}/api/show/stop?key={Uri.EscapeDataString(key)}");

        var doc = new
        {
            version = 4,
            type = "page",
            page = new { name = sel?.Name ?? "RedAnts" },
            instances = new Dictionary<string, object>
            {
                ["redants_http"] = new { instance_type = "generic-http", label = "RedAnts", enabled = true },
            },
            controls,
        };
        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"redants-{sel?.Id ?? "board"}.companionconfig");
    }

    private static object Button(string label, int bgColor, string url) => new
    {
        type = "button",
        style = new { text = label, size = "auto", alignment = "center:center", color = 0xFFFFFF, bgcolor = bgColor, show_topbar = false },
        feedbacks = Array.Empty<object>(),
        steps = new Dictionary<string, object>
        {
            ["0"] = new
            {
                action_sets = new Dictionary<string, object>
                {
                    ["down"] = new object[]
                    {
                        new { instance = "redants_http", action = "url_get", options = new { url } },
                    },
                    ["up"] = Array.Empty<object>(),
                },
            },
        },
    };

    private static int ColorInt(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return 0x555555;
        hex = hex.TrimStart('#');
        return int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : 0x555555;
    }

    private static string Enc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("~/Features/Show/Views/Index.cshtml");
}
