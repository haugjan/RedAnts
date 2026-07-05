using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using RedAnts.Infrastructure.Ticketing.Content;
using A = RedAnts.Infrastructure.Website.WebsiteAliases;

namespace RedAnts.Infrastructure.Website;

[ComposeAfter(typeof(TicketingContentTypeSeederComposer))]
public sealed class WebsiteContentTypeSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, WebsiteContentTypeSeeder>();
}

public sealed class WebsiteContentTypeSeeder(
    IContentTypeService contentTypeService,
    IContentService contentService,
    IDataTypeService dataTypeService,
    IFileService fileService,
    IWebHostEnvironment hostEnvironment,
    IShortStringHelper shortStringHelper,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer serializer,
    ILogger<WebsiteContentTypeSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
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
            EnsureHomeIsFirstRoot();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WebsiteContentTypeSeeder failed.");
        }
        return Task.CompletedTask;
    }

    private void EnsureContentTypes()
    {
        var all = dataTypeService.GetAll().ToList();
        IDataType ByEditor(string alias, string fallback = "Umbraco.TextBox") =>
            all.FirstOrDefault(d => d.EditorAlias == alias) ?? all.First(d => d.EditorAlias == fallback);

        var textBox = ByEditor("Umbraco.TextBox");
        var textArea = ByEditor("Umbraco.TextArea");
        var mediaPicker = ByEditor("Umbraco.MediaPicker3");
        var urlPicker = ByEditor("Umbraco.MultiUrlPicker");
        var contentPicker = ByEditor("Umbraco.ContentPicker");

        var flexTpl = EnsureTemplate("FlexPage");

        var hero = EnsureElementType(A.HeroElementType, "Hero Element", "icon-medal color-red", h =>
        {
            h.AddPropertyType(Prop(mediaPicker, A.HeroImage, "Hero Bild", "Grossflächiges Hintergrundbild"), Group, GroupName);
            h.AddPropertyType(Prop(textBox, A.HeroTitle, "Titel"), Group, GroupName);
            h.AddPropertyType(Prop(textArea, A.HeroSubtitle, "Untertitel"), Group, GroupName);
            h.AddPropertyType(Prop(textBox, A.HeroTags, "Schlagwörter", "Kommagetrennte Tags in der Hero-Bar (z.B. Winterthur,Unihockey)"), Group, GroupName);
            h.AddPropertyType(Prop(textBox, A.HeroCtaText, "Button-Text"), Group, GroupName);
            h.AddPropertyType(Prop(urlPicker, A.HeroCtaLink, "Button-Link"), Group, GroupName);
        });

        var header = EnsureElementType(A.HeaderElementType, "Header Element", "icon-navigation-top color-red", h =>
        {
            h.AddPropertyType(Prop(textBox, A.HeaderTitle, "Titel"), Group, GroupName);
            h.AddPropertyType(Prop(mediaPicker, A.HeaderImage, "Bild", "Optionales Hintergrundbild"), Group, GroupName);
        });

        var eventList = EnsureElementType(A.EventListElementType, "Event-Liste", "icon-list color-red", h =>
        {
            h.AddPropertyType(Prop(contentPicker, A.EventListSeason, "Saison",
                "Alle offenen, zukünftigen Anlässe dieser Saison werden automatisch aufgelistet."), Group, GroupName);
        });

        var blockList = EnsureBlockList(hero.Key, header.Key, eventList.Key);

        if (contentTypeService.Get(A.FlexPageType) is null)
        {
            var flex = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = A.FlexPageType, Name = "Flex Page", Icon = "icon-layout", AllowedAsRoot = true
            };
            flex.AddPropertyType(Prop(blockList, A.FlexContent, "Inhalt", "Block List Inhalt der Seite"), Group, GroupName);
            AssignTemplate(flex, flexTpl);
            contentTypeService.Save(flex, SuperUser);

            flex.AllowedContentTypes = new[] { new ContentTypeSort(flex.Key, 0, flex.Alias) };
            contentTypeService.Save(flex, SuperUser);

            logger.LogInformation("WebsiteContentTypeSeeder: created flexPage document type.");
        }
    }

    private IContentType EnsureElementType(string alias, string name, string icon, Action<ContentType> addProps)
    {
        if (contentTypeService.Get(alias) is { } existing) return existing;

        var type = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = alias, Name = name, Icon = icon, IsElement = true
        };
        addProps(type);
        contentTypeService.Save(type, SuperUser);
        logger.LogInformation("WebsiteContentTypeSeeder: created element type {Alias}.", alias);
        return type;
    }

    private void EnsureSampleContent()
    {
        if (contentService.GetRootContent().Any(c => c.ContentType.Alias == A.FlexPageType)) return;

        var heroKey = contentTypeService.Get(A.HeroElementType)?.Key;
        var eventListKey = contentTypeService.Get(A.EventListElementType)?.Key;
        if (heroKey is null || eventListKey is null) return;

        var seasonUdi = FindFirstSeason()?.GetUdi().ToString();

        var home = contentService.Create("Startseite", Constants.System.Root, A.FlexPageType);
        home.SetValue(A.FlexContent, BuildHomepageJson(heroKey.Value, eventListKey.Value, seasonUdi));
        contentService.Save(home, SuperUser);
        contentService.Publish(home, new[] { "*" }, SuperUser);

        logger.LogInformation("WebsiteContentTypeSeeder: seeded 'Startseite' flexPage with Hero + Event-Liste blocks.");
    }

    private IContent? FindFirstSeason()
    {
        var root = contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == "ticketingRoot");
        if (root is null) return null;
        var saisons = contentService.GetPagedChildren(root.Id, 0, 100, out _)
            .FirstOrDefault(c => c.ContentType.Alias == "seasonsFolder");
        if (saisons is null) return null;
        return contentService.GetPagedChildren(saisons.Id, 0, 100, out _)
            .FirstOrDefault(c => c.ContentType.Alias == "season");
    }

    private void EnsureHomeIsFirstRoot()
    {
        var roots = contentService.GetRootContent().OrderBy(c => c.SortOrder).ToList();
        if (roots.Count < 2) return;

        var home = roots.FirstOrDefault(c => c.ContentType.Alias == A.FlexPageType);
        if (home is null || roots[0].Id == home.Id) return;

        var ordered = new[] { home }.Concat(roots.Where(c => c.Id != home.Id));
        contentService.Sort(ordered, SuperUser);
        logger.LogInformation("WebsiteContentTypeSeeder: moved '{Name}' to first root position (served at /).", home.Name);
    }

    private static string BuildHomepageJson(Guid heroTypeKey, Guid eventListTypeKey, string? seasonUdi)
    {
        object Val(string alias, object? value) =>
            new { alias, culture = (string?)null, segment = (string?)null, value };

        var heroContentKey = Guid.NewGuid().ToString();
        var listContentKey = Guid.NewGuid().ToString();

        var ctaLink = new object[]
        {
            new { name = "Tickets", target = (string?)null, unique = (string?)null,
                  type = "EXTERNAL", udi = (string?)null, url = "/tickets",
                  queryString = (string?)null, culture = (string?)null }
        };

        var contentData = new List<object>
        {
            new
            {
                contentTypeKey = heroTypeKey.ToString(),
                key = heroContentKey,
                values = new[]
                {
                    Val(A.HeroTitle, "Red Ants Winterthur"),
                    Val(A.HeroSubtitle, "Unihockey aus Winterthur. Heimspiele, Saisonkarten und alles rund um den Verein."),
                    Val(A.HeroTags, "Winterthur,Unihockey,NLA Damen"),
                    Val(A.HeroCtaText, "Zu den Tickets"),
                    Val(A.HeroCtaLink, ctaLink)
                }
            }
        };
        var layout = new List<object>
        {
            new { contentKey = heroContentKey, contentUdi = (string?)null, settingsKey = (string?)null, settingsUdi = (string?)null }
        };
        var expose = new List<object>
        {
            new { contentKey = heroContentKey, culture = (string?)null, segment = (string?)null }
        };

        if (seasonUdi is not null)
        {
            contentData.Add(new
            {
                contentTypeKey = eventListTypeKey.ToString(),
                key = listContentKey,
                values = new[] { Val(A.EventListSeason, seasonUdi) }
            });
            layout.Add(new { contentKey = listContentKey, contentUdi = (string?)null, settingsKey = (string?)null, settingsUdi = (string?)null });
            expose.Add(new { contentKey = listContentKey, culture = (string?)null, segment = (string?)null });
        }

        var value = new
        {
            contentData,
            settingsData = Array.Empty<object>(),
            expose,
            layout = new Dictionary<string, object> { ["Umbraco.BlockList"] = layout }
        };

        return JsonSerializer.Serialize(value);
    }

    private IDataType EnsureBlockList(Guid heroTypeKey, Guid headerTypeKey, Guid eventListTypeKey)
    {
        const string name = "Website Content Blocks";
        var existing = dataTypeService.GetAll().FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.BlockList", out var editor))
            throw new InvalidOperationException("Umbraco.BlockList property editor not found.");

        Dictionary<string, object?> Block(Guid typeKey, string label) => new()
        {
            ["contentElementTypeKey"] = typeKey.ToString(),
            ["settingsElementTypeKey"] = null,
            ["label"] = label,
            ["editorSize"] = "medium",
            ["forceHideContentEditorInOverlay"] = false,
            ["iconColor"] = "#C8102E",
            ["backgroundColor"] = null,
            ["thumbnail"] = null
        };

        var config = new Dictionary<string, object>
        {
            ["blocks"] = new object[]
            {
                Block(heroTypeKey, "Hero"),
                Block(headerTypeKey, "Header"),
                Block(eventListTypeKey, "Event-Liste")
            },
            ["useInlineEditingAsDefault"] = false,
            ["useLiveEditing"] = false,
            ["useSingleBlockMode"] = false
        };

        var dt = new DataType(editor, serializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.BlockList",
            DatabaseType = ValueStorageType.Ntext,
            ConfigurationData = config
        };
        dataTypeService.Save(dt);
        return dt;
    }

    private PropertyType Prop(IDataType dataType, string alias, string name, string? description = null) =>
        new(shortStringHelper, dataType, alias) { Name = name, Description = description ?? string.Empty };

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
}
