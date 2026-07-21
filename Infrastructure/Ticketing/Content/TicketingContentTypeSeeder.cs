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
            EnsurePublicPageTypes();
            EnsureEventExtraProperties();
            EnsureContentStructure();
            ConsolidateSaisonsNode();
            EnsureVenuePicker();
            RefreshAccessLinks();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TicketingContentTypeSeeder failed.");
        }
        return Task.CompletedTask;
    }

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
        if (contentTypeService.Get(A.RootType) is not null) return;

        var all = dataTypeService.GetAll().ToList();
        IDataType ByEditor(string alias) => all.First(d => d.EditorAlias == alias);

        var textBox = ByEditor("Umbraco.TextBox");
        var richText = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.RichText") ?? textBox;
        var mediaPicker = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.MediaPicker3") ?? textBox;
        var contentPicker = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.ContentPicker") ?? textBox;
        var labelType = EnsureLabel(all);
        var statusDropdown = EnsureStatusDropdown(all);
        var boolean = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TrueFalse") ?? EnsureToggle(all);
        var dateCh = EnsureDatePicker(all, "Ticketing Datum (CH)", "DD.MM.YYYY");
        var dateTimeCh = EnsureDatePicker(all, "Ticketing Datum + Zeit (CH)", "DD.MM.YYYY HH:mm");

        var venueTpl = EnsureTemplate("TicketVenue");
        var eventTpl = EnsureTemplate("TicketEvent");
        var seasonTpl = EnsureTemplate("TicketSeason");

        var venue = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.VenueType, Name = "Ort", Icon = "icon-map-location"
        };
        venue.AddPropertyType(Prop(textBox, A.VenueGoogleGeoId, "Google Geo ID"), Group, GroupName);
        venue.AddPropertyType(Prop(mediaPicker, A.VenueImage, "Bild"), Group, GroupName);
        venue.AddPropertyType(Prop(richText, A.VenueDescription, "Beschreibung"), Group, GroupName);
        AssignTemplate(venue, venueTpl);
        contentTypeService.Save(venue, SuperUser);

        var evt = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.EventType, Name = "Anlass", Icon = "icon-calendar"
        };
        evt.AddPropertyType(Prop(richText, A.EventText, "Text"), Group, GroupName);
        evt.AddPropertyType(Prop(dateTimeCh, A.EventStart, "Beginn"), Group, GroupName);
        evt.AddPropertyType(PropWithHint(boolean, A.EventTimeUnknown, "Zeit noch unbekannt", "Wenn aktiviert, wird überall nur das Datum angezeigt."), Group, GroupName);
        evt.AddPropertyType(Prop(contentPicker, A.EventVenue, "Ort"), Group, GroupName);
        evt.AddPropertyType(Prop(statusDropdown, A.EventStatus, "Status"), Group, GroupName);
        evt.AddPropertyType(Prop(mediaPicker, A.EventImage, "Eventbild"), "media", "Bilder");
        evt.AddPropertyType(Prop(mediaPicker, A.EventHomeTeamLogo, "Logo Heimteam"), "media", "Bilder");
        evt.AddPropertyType(Prop(mediaPicker, A.EventAwayTeamLogo, "Logo Auswärtsteam"), "media", "Bilder");
        evt.AddPropertyType(PropWithHint(labelType, A.PublicLink, "Öffentlicher Link", "Automatisch generiert (ohne Geheimnis)."), "access", "Zugriff");
        evt.AddPropertyType(PropWithHint(labelType, A.InternLink, "Interner Link (mit Geheimnis)", "Automatisch generiert; nur mit diesem Link aufrufbar."), "access", "Zugriff");
        AssignTemplate(evt, eventTpl);
        contentTypeService.Save(evt, SuperUser);

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

    private void EnsureContentStructure()
    {
        if (contentService.GetRootContent().Any(c => c.ContentType.Alias == A.RootType)) return;

        var root = contentService.Create("Ticketing", Constants.System.Root, A.RootType);
        Publish(root);

        var seasonsFolder = contentService.Create("Saisons", root.Id, A.SeasonsFolderType);
        Publish(seasonsFolder);

        var venuesFolder = contentService.Create("Orte", root.Id, A.VenuesFolderType);
        Publish(venuesFolder);

        logger.LogInformation("TicketingContentTypeSeeder: created content structure (root + folders).");
    }

    private void EnsurePublicPageTypes()
    {
        var all = dataTypeService.GetAll().ToList();
        var textBox = all.First(d => d.EditorAlias == "Umbraco.TextBox");
        var richText = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.RichText") ?? textBox;
        var mediaPicker = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.MediaPicker3") ?? textBox;

        var root = contentTypeService.Get(A.RootType);
        if (root is not null)
        {
            var rootChanged = false;
            if (!root.PropertyTypeExists(A.HeaderText))
            {
                root.AddPropertyType(Prop(textBox, A.HeaderText, "Headertext"), Group, GroupName);
                rootChanged = true;
            }
            if (!root.PropertyTypeExists(A.HeaderImage))
            {
                root.AddPropertyType(Prop(mediaPicker, A.HeaderImage, "Headerbild"), Group, GroupName);
                rootChanged = true;
            }
            if (root.DefaultTemplate is null)
            {
                AssignTemplate(root, EnsureTemplate("TicketingHome"));
                rootChanged = true;
            }
            if (rootChanged) contentTypeService.Save(root, SuperUser);
        }

        var seasonsFolder = contentTypeService.Get(A.SeasonsFolderType);
        if (seasonsFolder is not null)
        {
            var folderChanged = false;
            if (!seasonsFolder.PropertyTypeExists(A.HeaderText))
            {
                seasonsFolder.AddPropertyType(Prop(textBox, A.HeaderText, "Header"), Group, GroupName);
                folderChanged = true;
            }
            if (!seasonsFolder.PropertyTypeExists(A.HeaderImage))
            {
                seasonsFolder.AddPropertyType(Prop(mediaPicker, A.HeaderImage, "Headerbild"), Group, GroupName);
                folderChanged = true;
            }
            if (!seasonsFolder.PropertyTypeExists(A.PromoBodyText))
            {
                seasonsFolder.AddPropertyType(Prop(richText, A.PromoBodyText, "Text"), Group, GroupName);
                folderChanged = true;
            }
            var promoTemplate = EnsureTemplate("SaisonsPromo");
            if (promoTemplate is not null && seasonsFolder.DefaultTemplate is null)
            {
                AssignTemplate(seasonsFolder, promoTemplate);
                folderChanged = true;
            }
            if (folderChanged) contentTypeService.Save(seasonsFolder, SuperUser);
        }
    }

    private void EnsureEventExtraProperties()
    {
        var evt = contentTypeService.Get(A.EventType);
        if (evt is null || evt.PropertyTypeExists(A.EventTimeUnknown)) return;

        var all = dataTypeService.GetAll().ToList();
        var boolean = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TrueFalse") ?? EnsureToggle(all);
        evt.AddPropertyType(PropWithHint(boolean, A.EventTimeUnknown, "Zeit noch unbekannt", "Wenn aktiviert, wird überall nur das Datum angezeigt."), Group, GroupName);
        contentTypeService.Save(evt, SuperUser);
        logger.LogInformation("TicketingContentTypeSeeder: added '{Alias}' to the event type.", A.EventTimeUnknown);
    }

    private const string ObsoleteSaisonsPromoType = "saisonsPromo";

    private void ConsolidateSaisonsNode()
    {
        EnsureNodeTemplate(A.RootType, "TicketingHome");

        var seasonsFolder = FindSeasonsFolderNode();
        if (seasonsFolder is not null)
        {
            var promoTemplate = fileService.GetTemplate("SaisonsPromo");
            var folderChanged = false;

            var promoNode = contentService.GetRootContent()
                .FirstOrDefault(c => c.ContentType.Alias == ObsoleteSaisonsPromoType);
            if (promoNode is not null)
            {
                folderChanged |= CopyIfEmpty(promoNode, seasonsFolder, A.HeaderText);
                folderChanged |= CopyIfEmpty(promoNode, seasonsFolder, A.HeaderImage);
                folderChanged |= CopyIfEmpty(promoNode, seasonsFolder, A.PromoBodyText);
            }

            if (string.IsNullOrWhiteSpace(seasonsFolder.GetValue<string>(A.HeaderText)))
            {
                seasonsFolder.SetValue(A.HeaderText, "Saisonkarten");
                folderChanged = true;
            }
            if (promoTemplate is not null && seasonsFolder.TemplateId != promoTemplate.Id)
            {
                seasonsFolder.TemplateId = promoTemplate.Id;
                folderChanged = true;
            }
            if (folderChanged) Publish(seasonsFolder);

            if (promoNode is not null)
            {
                contentService.Delete(promoNode, SuperUser);
                logger.LogInformation("TicketingContentTypeSeeder: removed redundant Saisons promo root node.");
            }
        }

        var obsoleteType = contentTypeService.Get(ObsoleteSaisonsPromoType);
        if (obsoleteType is not null
            && !contentService.GetRootContent().Any(c => c.ContentType.Alias == ObsoleteSaisonsPromoType))
        {
            contentTypeService.Delete(obsoleteType, SuperUser);
            logger.LogInformation("TicketingContentTypeSeeder: removed obsolete saisonsPromo document type.");
        }
    }

    private IContent? FindSeasonsFolderNode() => FindFolderNode(A.SeasonsFolderType);

    private IContent? FindFolderNode(string folderTypeAlias)
    {
        var root = contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == A.RootType);
        if (root is null) return null;
        return contentService.GetPagedChildren(root.Id, 0, 100, out _)
            .FirstOrDefault(c => c.ContentType.Alias == folderTypeAlias);
    }

    private void EnsureVenuePicker()
    {
        var venuesFolder = FindFolderNode(A.VenuesFolderType);
        if (venuesFolder is null) return;

        var startNode = venuesFolder.Key.ToString();

        const string name = "Ticketing Ort (Venues)";
        var picker = dataTypeService.GetAll().FirstOrDefault(d => d.Name == name);
        if (picker is null)
        {
            if (!propertyEditors.TryGet("Umbraco.ContentPicker", out var editor)) return;
            picker = new DataType(editor, serializer)
            {
                Name = name,
                EditorUiAlias = "Umb.PropertyEditorUi.DocumentPicker",
                DatabaseType = ValueStorageType.Nvarchar,
                ConfigurationData = new Dictionary<string, object> { ["startNodeId"] = startNode }
            };
            dataTypeService.Save(picker);
        }
        else
        {
            var current = picker.ConfigurationData.TryGetValue("startNodeId", out var v) ? v as string : null;
            if (current != startNode)
            {
                picker.ConfigurationData = new Dictionary<string, object> { ["startNodeId"] = startNode };
                dataTypeService.Save(picker);
            }
        }

        var evt = contentTypeService.Get(A.EventType);
        var venueProp = evt?.PropertyTypes.FirstOrDefault(p => p.Alias == A.EventVenue);
        if (evt is not null && venueProp is not null && venueProp.DataTypeKey != picker.Key)
        {
            venueProp.DataTypeId = picker.Id;
            venueProp.DataTypeKey = picker.Key;
            contentTypeService.Save(evt, SuperUser);
            logger.LogInformation("TicketingContentTypeSeeder: restricted the event venue picker to the Orte folder.");
        }
    }

    private static bool CopyIfEmpty(IContent from, IContent to, string alias)
    {
        if (!string.IsNullOrWhiteSpace(to.GetValue<string>(alias))) return false;
        var value = from.GetValue<string>(alias);
        if (string.IsNullOrWhiteSpace(value)) return false;
        to.SetValue(alias, value);
        return true;
    }

    private void EnsureNodeTemplate(string contentTypeAlias, string templateAlias)
    {
        var node = contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == contentTypeAlias);
        if (node is null || node.TemplateId is not null) return;

        var template = fileService.GetTemplate(templateAlias);
        if (template is null) return;

        node.TemplateId = template.Id;
        Publish(node);
        logger.LogInformation("TicketingContentTypeSeeder: assigned template {Template} to '{Node}'.",
            templateAlias, node.Name);
    }

    private PropertyType Prop(IDataType dataType, string alias, string name) =>
        new(shortStringHelper, dataType, alias) { Name = name };

    private PropertyType PropWithHint(IDataType dataType, string alias, string name, string description) =>
        new(shortStringHelper, dataType, alias) { Name = name, Description = description };

    private void Publish(IContent content)
    {
        contentService.Save(content, SuperUser);
        contentService.Publish(content, new[] { "*" }, SuperUser);
    }

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

    private IDataType EnsureToggle(List<IDataType> all)
    {
        const string name = "Ticketing Ja/Nein";
        var existing = all.FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.TrueFalse", out var editor))
            throw new InvalidOperationException("Umbraco.TrueFalse property editor not found.");

        var dt = new DataType(editor, serializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
            DatabaseType = ValueStorageType.Integer
        };
        dataTypeService.Save(dt);
        all.Add(dt);
        return dt;
    }

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
