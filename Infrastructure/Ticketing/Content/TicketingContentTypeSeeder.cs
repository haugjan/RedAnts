using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class TicketingContentTypeSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TicketingContentTypeSeeder>();
}

/// <summary>
/// Code-first creation of the ticketing Document Types (ticketingRoot/venue/season/event) and a
/// sample published content tree, mirroring the reference project's ContentSeeder/MemberTypeSeeder.
/// </summary>
public sealed class TicketingContentTypeSeeder(
    IContentTypeService contentTypeService,
    IContentService contentService,
    IDataTypeService dataTypeService,
    IFileService fileService,
    IWebHostEnvironment hostEnvironment,
    IShortStringHelper shortStringHelper,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer serializer,
    IPublishedUrlProvider urlProvider,
    IUmbracoContextFactory umbracoContextFactory,
    ILogger<TicketingContentTypeSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const int SuperUser = Constants.Security.SuperUserId;
    private const string Group = "content";
    private const string GroupName = "Inhalt";

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            EnsureContentTypes();
            EnsureSampleContent();
            RefreshAccessLinks();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TicketingContentTypeSeeder failed.");
        }
        return Task.CompletedTask;
    }

    // Recompute the public/internal link display fields for existing event + season nodes from their
    // official Umbraco content URL. Runs every boot (best-effort: skips nodes whose URL is not yet
    // available), so links created with the old sqid scheme are updated without a manual re-save.
    private void RefreshAccessLinks()
    {
        using var ctxRef = umbracoContextFactory.EnsureUmbracoContext();
        foreach (var typeAlias in new[] { A.EventType, A.SeasonType })
        {
            var ct = contentTypeService.Get(typeAlias);
            if (ct is null) continue;

            foreach (var item in contentService.GetPagedOfType(ct.Id, 0, 1000, out _, null))
            {
                if (!TicketingLinks.TryApply(item, urlProvider)) continue;
                contentService.Save(item, SuperUser);
                logger.LogInformation("TicketingContentTypeSeeder: refreshed access links for '{Name}' -> {Link}",
                    item.Name, item.GetValue<string>(A.PublicLink));
            }
        }
    }

    private void EnsureContentTypes()
    {
        if (contentTypeService.Get(A.RootType) is not null) return; // already created

        var all = dataTypeService.GetAll().ToList();
        IDataType ByEditor(string alias) => all.First(d => d.EditorAlias == alias);

        var textBox = ByEditor("Umbraco.TextBox");
        var richText = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.RichText") ?? textBox;
        var mediaPicker = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.MediaPicker3") ?? textBox;
        var contentPicker = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.ContentPicker") ?? textBox;
        var labelType = EnsureLabel(all);
        var statusDropdown = EnsureStatusDropdown(all);
        // Swiss-format date pickers (dd.MM.yyyy and dd.MM.yyyy HH:mm) for the backoffice editor.
        var dateCh = EnsureDatePicker(all, "Ticketing Datum (CH)", "DD.MM.YYYY");
        var dateTimeCh = EnsureDatePicker(all, "Ticketing Datum + Zeit (CH)", "DD.MM.YYYY HH:mm");

        // Templates (each entity node renders as a page). Prefixed so they do not collide with the
        // MVC purchase views (Views/Tickets/Event.cshtml etc.) via Umbraco's root view-location expander.
        var venueTpl = EnsureTemplate("TicketVenue");
        var eventTpl = EnsureTemplate("TicketEvent");
        var seasonTpl = EnsureTemplate("TicketSeason");

        // venue
        var venue = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.VenueType, Name = "Ort", Icon = "icon-map-location"
        };
        venue.AddPropertyType(Prop(textBox, A.VenueGoogleGeoId, "Google Geo ID"), Group, GroupName);
        venue.AddPropertyType(Prop(mediaPicker, A.VenueImage, "Bild"), Group, GroupName);
        venue.AddPropertyType(Prop(richText, A.VenueDescription, "Beschreibung"), Group, GroupName);
        AssignTemplate(venue, venueTpl);
        contentTypeService.Save(venue, SuperUser);

        // event
        var evt = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.EventType, Name = "Anlass", Icon = "icon-calendar"
        };
        evt.AddPropertyType(Prop(richText, A.EventText, "Text"), Group, GroupName);
        evt.AddPropertyType(Prop(dateTimeCh, A.EventStart, "Beginn"), Group, GroupName);
        evt.AddPropertyType(Prop(contentPicker, A.EventVenue, "Ort"), Group, GroupName);
        evt.AddPropertyType(Prop(statusDropdown, A.EventStatus, "Status"), Group, GroupName);
        evt.AddPropertyType(Prop(mediaPicker, A.EventImage, "Eventbild"), "media", "Bilder");
        evt.AddPropertyType(Prop(mediaPicker, A.EventHomeTeamLogo, "Logo Heimteam"), "media", "Bilder");
        evt.AddPropertyType(Prop(mediaPicker, A.EventAwayTeamLogo, "Logo Auswärtsteam"), "media", "Bilder");
        evt.AddPropertyType(PropWithHint(labelType, A.PublicLink, "Öffentlicher Link", "Automatisch generiert (ohne Geheimnis)."), "access", "Zugriff");
        evt.AddPropertyType(PropWithHint(labelType, A.InternLink, "Interner Link (mit Geheimnis)", "Automatisch generiert; nur mit diesem Link aufrufbar."), "access", "Zugriff");
        AssignTemplate(evt, eventTpl);
        contentTypeService.Save(evt, SuperUser);

        // season (allows event children)
        var season = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.SeasonType, Name = "Saison", Icon = "icon-time"
        };
        season.AddPropertyType(Prop(dateCh, A.SeasonStartDate, "Start"), Group, GroupName);
        season.AddPropertyType(Prop(dateCh, A.SeasonEndDate, "Ende"), Group, GroupName);
        season.AddPropertyType(Prop(statusDropdown, A.SeasonStatus, "Status"), Group, GroupName);
        season.AddPropertyType(Prop(mediaPicker, A.SeasonImage, "Bild"), Group, GroupName);
        season.AddPropertyType(PropWithHint(labelType, A.PublicLink, "Öffentlicher Link", "Automatisch generiert (ohne Geheimnis)."), "access", "Zugriff");
        season.AddPropertyType(PropWithHint(labelType, A.InternLink, "Interner Link (mit Geheimnis)", "Automatisch generiert; nur mit diesem Link aufrufbar."), "access", "Zugriff");
        season.AllowedContentTypes = new[] { new ContentTypeSort(evt.Key, 0, evt.Alias) };
        AssignTemplate(season, seasonTpl);
        contentTypeService.Save(season, SuperUser);

        // folders (containers with list view) grouping seasons and venues
        var seasonsFolder = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.SeasonsFolderType, Name = "Saisons", Icon = "icon-folder"
        };
        seasonsFolder.AllowedContentTypes = new[] { new ContentTypeSort(season.Key, 0, season.Alias) };
        contentTypeService.Save(seasonsFolder, SuperUser);

        var venuesFolder = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.VenuesFolderType, Name = "Orte", Icon = "icon-folder"
        };
        venuesFolder.AllowedContentTypes = new[] { new ContentTypeSort(venue.Key, 0, venue.Alias) };
        contentTypeService.Save(venuesFolder, SuperUser);

        // ticketingRoot (allowed at root, allows the two folders)
        var root = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.RootType, Name = "Ticketing", Icon = "icon-tickets", AllowedAsRoot = true
        };
        root.AllowedContentTypes = new[]
        {
            new ContentTypeSort(seasonsFolder.Key, 0, seasonsFolder.Alias),
            new ContentTypeSort(venuesFolder.Key, 1, venuesFolder.Alias)
        };
        contentTypeService.Save(root, SuperUser);

        logger.LogInformation("TicketingContentTypeSeeder: created document types.");
    }

    private void EnsureSampleContent()
    {
        if (contentService.GetRootContent().Any(c => c.ContentType.Alias == A.RootType)) return;

        var root = contentService.Create("Ticketing", Constants.System.Root, A.RootType);
        Publish(root);

        var seasonsFolder = contentService.Create("Saisons", root.Id, A.SeasonsFolderType);
        Publish(seasonsFolder);

        var venuesFolder = contentService.Create("Orte", root.Id, A.VenuesFolderType);
        Publish(venuesFolder);

        var venue = contentService.Create("Deutweg Halle", venuesFolder.Id, A.VenueType);
        venue.SetValue(A.VenueGoogleGeoId, "ChIJSeedGeoId");
        venue.SetValue(A.VenueDescription, "<p>Heimhalle der Red Ants.</p>");
        Publish(venue);

        var season = contentService.Create("Saison 2026/27", seasonsFolder.Id, A.SeasonType);
        season.SetValue(A.SeasonStartDate, DateTime.Today);
        season.SetValue(A.SeasonEndDate, DateTime.Today.AddMonths(8));
        season.SetValue(A.SeasonStatus, Status("Open"));
        Publish(season);

        var openEvent = contentService.Create("Red Ants vs. Rivals", season.Id, A.EventType);
        openEvent.SetValue(A.EventText, "<p>Grosses Heimspiel.</p>");
        openEvent.SetValue(A.EventStart, DateTime.Today.AddDays(21).AddHours(19).AddMinutes(30));
        openEvent.SetValue(A.EventVenue, venue.GetUdi().ToString());
        openEvent.SetValue(A.EventStatus, Status("Open"));
        Publish(openEvent);

        var internEvent = contentService.Create("Intern-Anlass (versteckt)", season.Id, A.EventType);
        internEvent.SetValue(A.EventStart, DateTime.Today.AddDays(21).AddHours(20));
        internEvent.SetValue(A.EventVenue, venue.GetUdi().ToString());
        internEvent.SetValue(A.EventStatus, Status("Intern"));
        Publish(internEvent);
        logger.LogInformation("Seed intern event secret = {Secret}", internEvent.Key.ToString().Split('-')[0]);

        var closedEvent = contentService.Create("Vergangenes Spiel (Closed)", season.Id, A.EventType);
        closedEvent.SetValue(A.EventStart, DateTime.Today.AddDays(-7).AddHours(19));
        closedEvent.SetValue(A.EventVenue, venue.GetUdi().ToString());
        closedEvent.SetValue(A.EventStatus, Status("Closed"));
        Publish(closedEvent);

        // An Intern season: not listed publicly, only buyable with its secret.
        var internSeason = contentService.Create("Intern-Saison 2027/28", seasonsFolder.Id, A.SeasonType);
        internSeason.SetValue(A.SeasonStartDate, DateTime.Today.AddMonths(9));
        internSeason.SetValue(A.SeasonEndDate, DateTime.Today.AddMonths(17));
        internSeason.SetValue(A.SeasonStatus, Status("Intern"));
        Publish(internSeason);
        logger.LogInformation("Seed intern season secret = {Secret}", internSeason.Key.ToString().Split('-')[0]);

        logger.LogInformation("TicketingContentTypeSeeder: seeded sample content tree.");
    }

    private PropertyType Prop(IDataType dataType, string alias, string name) =>
        new(shortStringHelper, dataType, alias) { Name = name };

    private PropertyType PropWithHint(IDataType dataType, string alias, string name, string description) =>
        new(shortStringHelper, dataType, alias) { Name = name, Description = description };

    // Umbraco.DropDown.Flexible persists its value as a JSON array of selected items.
    private static string Status(string value) =>
        System.Text.Json.JsonSerializer.Serialize(new[] { value });

    private void Publish(IContent content)
    {
        contentService.Save(content, SuperUser);
        contentService.Publish(content, new[] { "*" }, SuperUser);
    }

    // Registers a template from its Views/{alias}.cshtml file (pattern: reference ContentSeeder.EnsureTemplate).
    private ITemplate? EnsureTemplate(string alias)
    {
        var existing = fileService.GetTemplate(alias);
        if (existing is not null) return existing;

        var viewPath = Path.Combine(hostEnvironment.ContentRootPath, "Views", $"{alias}.cshtml");
        if (!System.IO.File.Exists(viewPath))
        {
            logger.LogWarning("Template view {Path} not found; skipping template {Alias}.", viewPath, alias);
            return null;
        }
        var content = System.IO.File.ReadAllText(viewPath);
        return fileService.CreateTemplateWithIdentity(alias, alias, content, null, SuperUser);
    }

    private static void AssignTemplate(IContentType type, ITemplate? tpl)
    {
        if (tpl is null) return;
        type.AllowedTemplates = new[] { tpl };
        type.SetDefaultTemplate(tpl);
    }

    // A DateTime data type with a Swiss display/edit format (moment-style tokens, e.g. DD.MM.YYYY).
    private IDataType EnsureDatePicker(List<IDataType> all, string name, string format)
    {
        var existing = all.FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.DateTime", out var editor))
            throw new InvalidOperationException("Umbraco.DateTime property editor not found.");

        var dt = new DataType(editor, serializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.DatePicker",
            DatabaseType = ValueStorageType.Date,
            ConfigurationData = new Dictionary<string, object> { ["format"] = format }
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

    // Read-only display editor (Umbraco.Label) for the generated links.
    // Match by Name, not by EditorAlias: Umbraco ships several built-in Umbraco.Label
    // data types (Label (pixels), Label (integer), ...). A plain EditorAlias match would
    // grab whichever comes first (e.g. "Label (pixels)", which renders "{=value}px" as INT),
    // so a string link value shows up as just "px".
    private IDataType EnsureLabel(List<IDataType> all)
    {
        const string name = "Ticketing Link (Label)";
        var existing = all.FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.Label", out var editor))
            throw new InvalidOperationException("Umbraco.Label property editor not found.");

        var dt = new DataType(editor, serializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.Label",
            DatabaseType = ValueStorageType.Nvarchar,
            ConfigurationData = new Dictionary<string, object> { ["umbracoDataValueType"] = "STRING" }
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

    private IDataType EnsureStatusDropdown(List<IDataType> all)
    {
        const string name = "Ticketing Status";
        var existing = all.FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.DropDown.Flexible", out var editor))
            throw new InvalidOperationException("Umbraco.DropDown.Flexible property editor not found.");

        var config = new Dictionary<string, object>
        {
            ["multiple"] = false,
            ["items"] = new List<string> { "Draft", "Open", "Intern", "Closed" }
        };

        var dt = new DataType(editor, serializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
            DatabaseType = ValueStorageType.Nvarchar,
            ConfigurationData = config
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

}
