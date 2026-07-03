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
    private const string CategoryPickerName = "Ticketing Kategorie-Auswahl";
    private const string VenuePickerName = "Ticketing Ort-Auswahl";

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            EnsureContentTypes();
            EnsureSampleContent();
            RemoveObsoleteEventProperties();
            MigrateSalesPrices();
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

    // Idempotent cleanup: drop the obsolete free-text "Interner Link" (internalLink) property from the
    // event type on databases created before it was removed. Runs every boot; only saves when present.
    private void RemoveObsoleteEventProperties()
    {
        var eventType = contentTypeService.Get(A.EventType);
        if (eventType is null || !eventType.PropertyTypeExists("internalLink")) return;

        eventType.RemovePropertyType("internalLink");
        contentTypeService.Save(eventType, SuperUser);
        logger.LogInformation("TicketingContentTypeSeeder: removed obsolete 'internalLink' property from the event type.");
    }

    // Idempotent Stage 1 migration for databases created before the "Preise Verkauf" Block List existed.
    // Creates the ticketCategory content type + folder, the salesPrice element type and its Block List,
    // the restricted category/venue pickers, adds the salesPrices property to event and season, drops the
    // obsolete fixed decimal price fields, and seeds the category nodes. Runs every boot; only changes
    // what is missing, so it is a no-op once applied (and on fresh installs handled by EnsureContentTypes).
    private void MigrateSalesPrices()
    {
        var eventType = contentTypeService.Get(A.EventType);
        var seasonType = contentTypeService.Get(A.SeasonType);
        if (eventType is null || seasonType is null) return; // nothing seeded yet

        var all = dataTypeService.GetAll().ToList();
        var textBox = all.First(d => d.EditorAlias == "Umbraco.TextBox");
        var decimalType = EnsureDecimal(all);
        var integerType = EnsureInteger(all);
        var booleanType = EnsureBoolean(all);
        var categoryPicker = EnsureContentPicker(all, CategoryPickerName);
        var venuePicker = EnsureContentPicker(all, VenuePickerName);

        var ticketCategoryType = contentTypeService.Get(A.TicketCategoryType);
        if (ticketCategoryType is null)
        {
            ticketCategoryType = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = A.TicketCategoryType, Name = "Ticketkategorie", Icon = "icon-tag"
            };
            ticketCategoryType.AddPropertyType(Prop(textBox, A.CategoryCode, "Code"), Group, GroupName);
            ticketCategoryType.AddPropertyType(Prop(decimalType, A.CategoryDefaultPrice, "Standardpreis"), Group, GroupName);
            contentTypeService.Save(ticketCategoryType, SuperUser);
        }

        var categoriesFolderType = contentTypeService.Get(A.CategoriesFolderType);
        if (categoriesFolderType is null)
        {
            categoriesFolderType = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = A.CategoriesFolderType, Name = "Ticketkategorien", Icon = "icon-folder"
            };
            categoriesFolderType.AllowedContentTypes = new[] { new ContentTypeSort(ticketCategoryType.Key, 0, ticketCategoryType.Alias) };
            contentTypeService.Save(categoriesFolderType, SuperUser);
        }

        var salesPriceElement = contentTypeService.Get(A.SalesPriceElement);
        if (salesPriceElement is null)
        {
            salesPriceElement = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = A.SalesPriceElement, Name = "Verkaufspreis", Icon = "icon-tag", IsElement = true
            };
            salesPriceElement.AddPropertyType(Prop(categoryPicker, A.SalesPriceCategory, "Kategorie"), Group, GroupName);
            salesPriceElement.AddPropertyType(PropWithHint(booleanType, A.SalesPriceUseDefault, "Standardpreis",
                "Wenn gesetzt, gilt der Standardpreis der Kategorie; sonst der Preis unten."), Group, GroupName);
            salesPriceElement.AddPropertyType(Prop(decimalType, A.SalesPricePrice, "Preis"), Group, GroupName);
            salesPriceElement.AddPropertyType(Prop(integerType, A.SalesPriceContingent, "Verkaufskontingent"), Group, GroupName);
            contentTypeService.Save(salesPriceElement, SuperUser);
        }

        var blockList = EnsureBlockList(all, "Ticketing Preise Verkauf (Block List)", salesPriceElement.Key);

        // Event: add salesPrices, re-point venue to the restricted picker, drop the old decimal price fields.
        var eventChanged = false;
        if (!eventType.PropertyTypeExists(A.SalesPrices))
        {
            eventType.AddPropertyType(Prop(blockList, A.SalesPrices, "Preise Verkauf"), "prices", "Preise");
            eventChanged = true;
        }
        var venueProp = eventType.PropertyTypes.FirstOrDefault(p => p.Alias == A.EventVenue);
        if (venueProp is not null && venueProp.DataTypeKey != venuePicker.Key)
        {
            venueProp.DataTypeId = venuePicker.Id;
            venueProp.DataTypeKey = venuePicker.Key;
            eventChanged = true;
        }
        foreach (var oldAlias in new[] { "priceChild", "priceYouth", "priceAdult" })
        {
            if (!eventType.PropertyTypeExists(oldAlias)) continue;
            eventType.RemovePropertyType(oldAlias);
            eventChanged = true;
        }
        if (eventChanged) contentTypeService.Save(eventType, SuperUser);

        // Season: add salesPrices.
        if (!seasonType.PropertyTypeExists(A.SalesPrices))
        {
            seasonType.AddPropertyType(Prop(blockList, A.SalesPrices, "Preise Verkauf"), "prices", "Preise");
            contentTypeService.Save(seasonType, SuperUser);
        }

        // Allow the categories folder under the ticketing root.
        var rootType = contentTypeService.Get(A.RootType);
        if (rootType is not null && rootType.AllowedContentTypes?.All(s => s.Alias != A.CategoriesFolderType) == true)
        {
            var allowed = rootType.AllowedContentTypes.ToList();
            allowed.Add(new ContentTypeSort(categoriesFolderType.Key, allowed.Count, A.CategoriesFolderType));
            rootType.AllowedContentTypes = allowed;
            contentTypeService.Save(rootType, SuperUser);
        }

        EnsureCategoryContent();
    }

    // Idempotent: ensures the categories folder node and the six ticket categories exist, and points
    // the category/venue pickers at their folders. Adds only missing categories (matched by code).
    private void EnsureCategoryContent()
    {
        var root = contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == A.RootType);
        if (root is null) return;

        var children = contentService.GetPagedChildren(root.Id, 0, 100, out _).ToList();
        var folder = children.FirstOrDefault(c => c.ContentType.Alias == A.CategoriesFolderType);
        if (folder is null)
        {
            folder = contentService.Create("Ticketkategorien", root.Id, A.CategoriesFolderType);
            Publish(folder);
        }

        var existingCodes = contentService.GetPagedChildren(folder.Id, 0, 100, out _)
            .Where(c => c.ContentType.Alias == A.TicketCategoryType)
            .Select(c => c.GetValue<string>(A.CategoryCode))
            .Where(code => !string.IsNullOrEmpty(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        void Category(string name, string code, decimal defaultPrice)
        {
            if (existingCodes.Contains(code)) return;
            SeedCategory(folder.Id, name, code, defaultPrice);
        }
        Category("Kind", "child", 10m);
        Category("Jugend", "youth", 15m);
        Category("Erwachsen", "adult", 25m);
        Category("Kind reduziert", "child-reduced", 8m);
        Category("Jugend reduziert", "youth-reduced", 12m);
        Category("Erwachsen reduziert", "adult-reduced", 20m);

        SetPickerStartNode(CategoryPickerName, folder);
        var venuesFolder = children.FirstOrDefault(c => c.ContentType.Alias == A.VenuesFolderType);
        if (venuesFolder is not null) SetPickerStartNode(VenuePickerName, venuesFolder);
    }

    private void EnsureContentTypes()
    {
        if (contentTypeService.Get(A.RootType) is not null) return; // already created

        var all = dataTypeService.GetAll().ToList();
        IDataType ByEditor(string alias) => all.First(d => d.EditorAlias == alias);

        var textBox = ByEditor("Umbraco.TextBox");
        var richText = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.RichText") ?? textBox;
        var mediaPicker = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.MediaPicker3") ?? textBox;
        var decimalType = EnsureDecimal(all);
        var integerType = EnsureInteger(all);
        var booleanType = EnsureBoolean(all);
        var labelType = EnsureLabel(all);
        var statusDropdown = EnsureStatusDropdown(all);
        // Content pickers scoped to their folders (start node set once the folders exist in EnsureSampleContent).
        var categoryPicker = EnsureContentPicker(all, CategoryPickerName);
        var venuePicker = EnsureContentPicker(all, VenuePickerName);
        // Swiss-format date pickers (dd.MM.yyyy and dd.MM.yyyy HH:mm) for the backoffice editor.
        var dateCh = EnsureDatePicker(all, "Ticketing Datum (CH)", "DD.MM.YYYY");
        var dateTimeCh = EnsureDatePicker(all, "Ticketing Datum + Zeit (CH)", "DD.MM.YYYY HH:mm");

        // Ticket category (central content list) + the salesPrice element type and its Block List.
        var ticketCategoryType = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.TicketCategoryType, Name = "Ticketkategorie", Icon = "icon-tag"
        };
        ticketCategoryType.AddPropertyType(Prop(textBox, A.CategoryCode, "Code"), Group, GroupName);
        ticketCategoryType.AddPropertyType(Prop(decimalType, A.CategoryDefaultPrice, "Standardpreis"), Group, GroupName);
        contentTypeService.Save(ticketCategoryType, SuperUser);

        var categoriesFolder = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.CategoriesFolderType, Name = "Ticketkategorien", Icon = "icon-folder"
        };
        categoriesFolder.AllowedContentTypes = new[] { new ContentTypeSort(ticketCategoryType.Key, 0, ticketCategoryType.Alias) };
        contentTypeService.Save(categoriesFolder, SuperUser);

        // salesPrice element type: one price row (category + optional default-price flag + price + contingent).
        var salesPriceElement = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.SalesPriceElement, Name = "Verkaufspreis", Icon = "icon-tag", IsElement = true
        };
        salesPriceElement.AddPropertyType(Prop(categoryPicker, A.SalesPriceCategory, "Kategorie"), Group, GroupName);
        salesPriceElement.AddPropertyType(PropWithHint(booleanType, A.SalesPriceUseDefault, "Standardpreis",
            "Wenn gesetzt, gilt der Standardpreis der Kategorie; sonst der Preis unten."), Group, GroupName);
        salesPriceElement.AddPropertyType(Prop(decimalType, A.SalesPricePrice, "Preis"), Group, GroupName);
        salesPriceElement.AddPropertyType(Prop(integerType, A.SalesPriceContingent, "Verkaufskontingent"), Group, GroupName);
        contentTypeService.Save(salesPriceElement, SuperUser);

        var salesPricesBlockList = EnsureBlockList(all, "Ticketing Preise Verkauf (Block List)", salesPriceElement.Key);

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
        evt.AddPropertyType(Prop(venuePicker, A.EventVenue, "Ort"), Group, GroupName);
        evt.AddPropertyType(Prop(statusDropdown, A.EventStatus, "Status"), Group, GroupName);
        evt.AddPropertyType(Prop(mediaPicker, A.EventImage, "Eventbild"), "media", "Bilder");
        evt.AddPropertyType(Prop(mediaPicker, A.EventHomeTeamLogo, "Logo Heimteam"), "media", "Bilder");
        evt.AddPropertyType(Prop(mediaPicker, A.EventAwayTeamLogo, "Logo Auswärtsteam"), "media", "Bilder");
        evt.AddPropertyType(Prop(salesPricesBlockList, A.SalesPrices, "Preise Verkauf"), "prices", "Preise");
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
        season.AddPropertyType(Prop(salesPricesBlockList, A.SalesPrices, "Preise Verkauf"), "prices", "Preise");
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
            new ContentTypeSort(venuesFolder.Key, 1, venuesFolder.Alias),
            new ContentTypeSort(categoriesFolder.Key, 2, categoriesFolder.Alias)
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

        // Ticket categories (central list picked in the "Preise Verkauf" editor).
        // Display names are public-facing (German); the code is the stable English identifier.
        var categoriesFolder = contentService.Create("Ticketkategorien", root.Id, A.CategoriesFolderType);
        Publish(categoriesFolder);
        SeedCategory(categoriesFolder.Id, "Kind", "child", 10m);
        SeedCategory(categoriesFolder.Id, "Jugend", "youth", 15m);
        SeedCategory(categoriesFolder.Id, "Erwachsen", "adult", 25m);
        SeedCategory(categoriesFolder.Id, "Kind reduziert", "child-reduced", 8m);
        SeedCategory(categoriesFolder.Id, "Jugend reduziert", "youth-reduced", 12m);
        SeedCategory(categoriesFolder.Id, "Erwachsen reduziert", "adult-reduced", 20m);

        // Restrict the content pickers to their folders now that the folder nodes exist.
        SetPickerStartNode(CategoryPickerName, categoriesFolder);
        SetPickerStartNode(VenuePickerName, venuesFolder);

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
        // Sales prices are added via the new "Preise Verkauf" Block List editor in the backoffice.
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

    private void SeedCategory(int parentId, string name, string code, decimal defaultPrice)
    {
        var node = contentService.Create(name, parentId, A.TicketCategoryType);
        node.SetValue(A.CategoryCode, code);
        node.SetValue(A.CategoryDefaultPrice, defaultPrice);
        Publish(node);
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

    private IDataType EnsureDecimal(List<IDataType> all)
    {
        var existing = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.Decimal");
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.Decimal", out var editor))
            throw new InvalidOperationException("Umbraco.Decimal property editor not found.");

        var dt = new DataType(editor, serializer)
        {
            Name = "Ticketing Preis (Decimal)",
            EditorUiAlias = "Umb.PropertyEditorUi.Decimal",
            DatabaseType = ValueStorageType.Decimal
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

    private IDataType EnsureInteger(List<IDataType> all)
    {
        var existing = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.Integer");
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.Integer", out var editor))
            throw new InvalidOperationException("Umbraco.Integer property editor not found.");

        var dt = new DataType(editor, serializer)
        {
            Name = "Ticketing Kontingent (Integer)",
            EditorUiAlias = "Umb.PropertyEditorUi.Integer",
            DatabaseType = ValueStorageType.Integer
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

    private IDataType EnsureBoolean(List<IDataType> all)
    {
        var existing = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TrueFalse");
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.TrueFalse", out var editor))
            throw new InvalidOperationException("Umbraco.TrueFalse property editor not found.");

        var dt = new DataType(editor, serializer)
        {
            Name = "Ticketing Ja/Nein (Toggle)",
            EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
            DatabaseType = ValueStorageType.Integer
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

    // A dedicated Content Picker scoped to a start node (subtree) so the editor only offers the
    // intended nodes. The start node is configured later (SetPickerStartNode), once the target
    // folder content node exists. This is how we limit the venue/category pickers to their folders.
    private IDataType EnsureContentPicker(List<IDataType> all, string name)
    {
        var existing = all.FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.ContentPicker", out var editor))
            throw new InvalidOperationException("Umbraco.ContentPicker property editor not found.");

        var dt = new DataType(editor, serializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.ContentPicker",
            DatabaseType = ValueStorageType.Nvarchar,
            ConfigurationData = new Dictionary<string, object> { ["ignoreUserStartNodes"] = false }
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

    // Block List holding the salesPrice element type (category + price + contingent) rows.
    private IDataType EnsureBlockList(List<IDataType> all, string name, Guid elementTypeKey)
    {
        var existing = all.FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.BlockList", out var editor))
            throw new InvalidOperationException("Umbraco.BlockList property editor not found.");

        var config = new Dictionary<string, object>
        {
            ["useInlineEditingAsDefault"] = true,
            ["blocks"] = new[]
            {
                new Dictionary<string, object> { ["contentElementTypeKey"] = elementTypeKey.ToString() }
            }
        };

        var dt = new DataType(editor, serializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.BlockList",
            DatabaseType = ValueStorageType.Ntext,
            ConfigurationData = config
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

    // Points a Content Picker data type at a start node (subtree root) so only that node's
    // descendants are selectable. Called after the folder content node has been created.
    private void SetPickerStartNode(string dataTypeName, IContent startNode)
    {
        var dt = dataTypeService.GetAll().FirstOrDefault(d => d.Name == dataTypeName);
        if (dt is null) return;

        var config = dt.ConfigurationData is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(dt.ConfigurationData);
        // v13-style UDI start node; the new backoffice still honours this key for the content picker.
        config["startNodeId"] = Udi.Create(Constants.UdiEntityType.Document, startNode.Key).ToString();
        dt.ConfigurationData = config;
        dataTypeService.Save(dt);
    }
}
