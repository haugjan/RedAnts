using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Purchase;

namespace RedAnts.Features.Ticketing.Public;

public sealed class TicketsController(
    IEvents events,
    ISeasons seasons,
    IVenues venues,
    ISingleTickets singleTickets,
    ISeasonTickets seasonTickets,
    ISeasonTicketPricing pricing,
    ISqidEncoder sqids,
    ICaptcha captcha,
    StartSingleTicketPurchase startSingle,
    StartSeasonTicketPurchase startSeason,
    IConfiguration config) : Controller
{
    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";
    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? TurnstileSiteKey => string.IsNullOrWhiteSpace(config["Turnstile:SiteKey"]) ? null : config["Turnstile:SiteKey"];

    // GET /tickets/event/{sqid} — single ticket purchase page. Intern requires the matching ?secret.
    [HttpGet("/tickets/event/{sqid}")]
    public async Task<IActionResult> Event(string sqid, [FromQuery] string? secret)
    {
        var evt = await ResolveEventAsync(sqid);
        if (evt is null || !AccessGate.EventOk(evt.Status, evt.AccessSecret, secret)) return NotFound();
        return View(await BuildEventModelAsync(evt, sqid, secret, null));
    }

    // POST /tickets/event/{sqid}/kauf — start a single ticket purchase.
    [HttpPost("/tickets/event/{sqid}/kauf")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyEvent(string sqid, [FromForm] SinglePurchaseForm form)
    {
        var evt = await ResolveEventAsync(sqid);
        if (evt is null || !AccessGate.EventOk(evt.Status, evt.AccessSecret, form.Secret)) return NotFound();

        if (!await captcha.VerifyAsync(form.CaptchaToken, RemoteIp))
            return View("Event", await BuildEventModelAsync(evt, sqid, form.Secret, "Captcha-Prüfung fehlgeschlagen."));

        try
        {
            var cmd = new StartSinglePurchaseCommand(evt.Id, form.PriceCategory, form.ToBilling());
            var result = await startSingle.ExecuteAsync(cmd, BaseUrl);
            return Redirect(result.RedirectUrl);
        }
        catch (DomainException ex)
        {
            return View("Event", await BuildEventModelAsync(evt, sqid, form.Secret, ex.Message));
        }
    }

    // GET /saisonkarten/{sqid} — season ticket purchase page. Intern requires the matching ?secret.
    [HttpGet("/saisonkarten/{sqid}")]
    public async Task<IActionResult> Season(string sqid, [FromQuery] string? secret)
    {
        var season = await ResolveSeasonAsync(sqid);
        if (season is null || !AccessGate.SeasonOk(season.Status, season.AccessSecret, secret)) return NotFound();
        return View(BuildSeasonModel(season, sqid, secret, null));
    }

    // POST /saisonkarten/{sqid}/kauf — start a season ticket purchase.
    [HttpPost("/saisonkarten/{sqid}/kauf")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuySeason(string sqid, [FromForm] SeasonPurchaseForm form)
    {
        var season = await ResolveSeasonAsync(sqid);
        if (season is null || !AccessGate.SeasonOk(season.Status, season.AccessSecret, form.Secret)) return NotFound();

        if (!await captcha.VerifyAsync(form.CaptchaToken, RemoteIp))
            return View("Season", BuildSeasonModel(season, sqid, form.Secret, "Captcha-Prüfung fehlgeschlagen."));

        try
        {
            var cmd = new StartSeasonPurchaseCommand(season.Id, form.Category, form.AgeGroup, form.ToBilling());
            var result = await startSeason.ExecuteAsync(cmd, BaseUrl);
            return Redirect(result.RedirectUrl);
        }
        catch (DomainException ex)
        {
            return View("Season", BuildSeasonModel(season, sqid, form.Secret, ex.Message));
        }
    }

    // GET /tickets/bestaetigung/{ref} — confirmation page (reachable via the secret ticket ref).
    [HttpGet("/tickets/bestaetigung/{reference:guid}")]
    public async Task<IActionResult> Confirmation(Guid reference)
    {
        var single = await singleTickets.FindByTicketGuidAsync(reference);
        if (single is not null)
        {
            var evt = await events.FindByIdAsync(single.EventId);
            return View(new ConfirmationModel
            {
                Found = true,
                Paid = single.PayStatus == PayStatus.Paid,
                Title = "Ticket",
                TicketRef = reference,
                Summary = $"{evt?.Name} · {Labels.PriceCategory(single.PriceCategory)} · CHF {single.Price:N2}"
            });
        }

        var seasonTicket = await seasonTickets.FindByTicketGuidAsync(reference);
        if (seasonTicket is not null)
        {
            var season = await seasons.FindByIdAsync(seasonTicket.SeasonId);
            return View(new ConfirmationModel
            {
                Found = true,
                Paid = seasonTicket.PayStatus == PayStatus.Paid,
                Title = "Saisonkarte",
                TicketRef = reference,
                Summary = $"{season?.Name} · {Labels.SeasonTicketCategory(seasonTicket.Category)} · {Labels.AgeGroup(seasonTicket.AgeGroup)} · CHF {seasonTicket.Price:N2}"
            });
        }

        return View(new ConfirmationModel { Found = false, Paid = false, Title = "Unbekannt" });
    }

    // Resolve by sqid only; status/secret gating is applied by the caller via AccessGate.
    private async Task<Event?> ResolveEventAsync(string sqid)
    {
        var id = sqids.Decode(sqid);
        return id is null ? null : await events.FindByIdAsync(id.Value);
    }

    private async Task<Season?> ResolveSeasonAsync(string sqid)
    {
        var id = sqids.Decode(sqid);
        return id is null ? null : await seasons.FindByIdAsync(id.Value);
    }

    private async Task<EventPurchaseModel> BuildEventModelAsync(Event evt, string sqid, string? secret, string? error)
    {
        var allSeasons = await seasons.GetAllAsync();
        var venue = await venues.FindByIdAsync(evt.VenueId);
        return new EventPurchaseModel
        {
            Event = evt,
            Sqid = sqid,
            Secret = secret,
            SeasonName = allSeasons.FirstOrDefault(s => s.Id == evt.SeasonId)?.Name ?? "",
            Venue = venue,
            TurnstileSiteKey = TurnstileSiteKey,
            Error = error
        };
    }

    private SeasonPurchaseModel BuildSeasonModel(Season season, string sqid, string? secret, string? error) => new()
    {
        Season = season,
        Sqid = sqid,
        Secret = secret,
        Prices = pricing.All(),
        TurnstileSiteKey = TurnstileSiteKey,
        Error = error
    };
}

public sealed class SinglePurchaseForm
{
    public PriceCategory PriceCategory { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Street { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string? Country { get; set; }
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? CaptchaToken { get; set; }
    public string? Secret { get; set; }

    public BillingInput ToBilling() => new(FirstName, LastName, Street, AddressLine2, PostalCode, City, Country, Email, Phone);
}

public sealed class SeasonPurchaseForm
{
    public SeasonTicketCategory Category { get; set; }
    public AgeGroup AgeGroup { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Street { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string? Country { get; set; }
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? CaptchaToken { get; set; }
    public string? Secret { get; set; }

    public BillingInput ToBilling() => new(FirstName, LastName, Street, AddressLine2, PostalCode, City, Country, Email, Phone);
}
