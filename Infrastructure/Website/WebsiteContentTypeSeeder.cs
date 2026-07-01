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
using A = RedAnts.Infrastructure.Website.WebsiteAliases;

namespace RedAnts.Infrastructure.Website;

public sealed class WebsiteContentTypeSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, WebsiteContentTypeSeeder>();
}

/// <summary>
/// Code-first creation of the public website Document Types (flexPage + heroElement block, wired
/// through a Block List data type) and a sample "Startseite" flexPage carrying one Hero block.
/// Mirrors the pattern of <see cref="Ticketing.Content.TicketingContentTypeSeeder"/>.
/// </summary>
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WebsiteContentTypeSeeder failed.");
        }
        return Task.CompletedTask;
    }

    private void EnsureContentTypes()
    {
        if (contentTypeService.Get(A.FlexPageType) is not null) return; // already created

        var all = dataTypeService.GetAll().ToList();
        IDataType ByEditor(string alias, string fallback = "Umbraco.TextBox") =>
            all.FirstOrDefault(d => d.EditorAlias == alias) ?? all.First(d => d.EditorAlias == fallback);

        var textBox = ByEditor("Umbraco.TextBox");
        var textArea = ByEditor("Umbraco.TextArea");
        var mediaPicker = ByEditor("Umbraco.MediaPicker3");
        var urlPicker = ByEditor("Umbraco.MultiUrlPicker");

        var flexTpl = EnsureTemplate("FlexPage");

        // heroElement (block element type, IsElement = true)
        var hero = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.HeroElementType, Name = "Hero Element", Icon = "icon-medal color-red", IsElement = true
        };
        hero.AddPropertyType(Prop(mediaPicker, A.HeroImage, "Hero Bild", "Grossflächiges Hintergrundbild"), Group, GroupName);
        hero.AddPropertyType(Prop(textBox, A.HeroTitle, "Titel"), Group, GroupName);
        hero.AddPropertyType(Prop(textArea, A.HeroSubtitle, "Untertitel"), Group, GroupName);
        hero.AddPropertyType(Prop(textBox, A.HeroTags, "Schlagwörter", "Kommagetrennte Tags in der Hero-Bar (z.B. Winterthur,Unihockey)"), Group, GroupName);
        hero.AddPropertyType(Prop(textBox, A.HeroCtaText, "Button-Text"), Group, GroupName);
        hero.AddPropertyType(Prop(urlPicker, A.HeroCtaLink, "Button-Link"), Group, GroupName);
        contentTypeService.Save(hero, SuperUser);

        // Block List data type offering the Hero block.
        var blockList = EnsureBlockList(hero.Key);

        // flexPage (allowed at root, holds a Block List; allows flexPage sub-pages)
        var flex = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = A.FlexPageType, Name = "Flex Page", Icon = "icon-layout", AllowedAsRoot = true
        };
        flex.AddPropertyType(Prop(blockList, A.FlexContent, "Inhalt", "Block List Inhalt der Seite"), Group, GroupName);
        AssignTemplate(flex, flexTpl);
        contentTypeService.Save(flex, SuperUser);

        // Allow flexPage under flexPage (sub-pages) now that its Key exists.
        flex.AllowedContentTypes = new[] { new ContentTypeSort(flex.Key, 0, flex.Alias) };
        contentTypeService.Save(flex, SuperUser);

        logger.LogInformation("WebsiteContentTypeSeeder: created flexPage + heroElement document types.");
    }

    private void EnsureSampleContent()
    {
        if (contentService.GetRootContent().Any(c => c.ContentType.Alias == A.FlexPageType)) return;

        var heroKey = contentTypeService.Get(A.HeroElementType)?.Key;
        if (heroKey is null) return;

        var home = contentService.Create("Startseite", Constants.System.Root, A.FlexPageType);
        home.SetValue(A.FlexContent, BuildHeroBlockJson(heroKey.Value));
        contentService.Save(home, SuperUser);
        contentService.Publish(home, new[] { "*" }, SuperUser);

        // Make the new homepage the first root node so it is served at "/".
        contentService.Sort(new[] { home }, SuperUser);

        logger.LogInformation("WebsiteContentTypeSeeder: seeded sample 'Startseite' flexPage with a Hero block.");
    }

    // Builds the Umbraco 17 Block List property value (contentData / layout / expose) for one Hero block.
    private static string BuildHeroBlockJson(Guid heroTypeKey)
    {
        var contentKey = Guid.NewGuid().ToString();
        var ctaLink = new object[]
        {
            new { name = "Tickets", target = (string?)null, unique = (string?)null,
                  type = "EXTERNAL", udi = (string?)null, url = "/tickets",
                  queryString = (string?)null, culture = (string?)null }
        };

        object Val(string alias, object value) =>
            new { alias, culture = (string?)null, segment = (string?)null, value };

        var value = new
        {
            contentData = new object[]
            {
                new
                {
                    contentTypeKey = heroTypeKey.ToString(),
                    key = contentKey,
                    values = new[]
                    {
                        Val(A.HeroTitle, "Red Ants Winterthur"),
                        Val(A.HeroSubtitle, "Unihockey aus Winterthur. Heimspiele, Saisonkarten und alles rund um den Verein."),
                        Val(A.HeroTags, "Winterthur,Unihockey,NLA Damen"),
                        Val(A.HeroCtaText, "Zu den Tickets"),
                        Val(A.HeroCtaLink, ctaLink)
                    }
                }
            },
            settingsData = Array.Empty<object>(),
            expose = new object[]
            {
                new { contentKey, culture = (string?)null, segment = (string?)null }
            },
            layout = new Dictionary<string, object>
            {
                ["Umbraco.BlockList"] = new object[]
                {
                    new { contentKey, contentUdi = (string?)null, settingsKey = (string?)null, settingsUdi = (string?)null }
                }
            }
        };

        return JsonSerializer.Serialize(value);
    }

    private IDataType EnsureBlockList(Guid heroTypeKey)
    {
        const string name = "Website Content Blocks";
        var existing = dataTypeService.GetAll().FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.BlockList", out var editor))
            throw new InvalidOperationException("Umbraco.BlockList property editor not found.");

        var config = new Dictionary<string, object>
        {
            ["blocks"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["contentElementTypeKey"] = heroTypeKey.ToString(),
                    ["settingsElementTypeKey"] = null,
                    ["label"] = "Hero",
                    ["editorSize"] = "medium",
                    ["forceHideContentEditorInOverlay"] = false,
                    ["iconColor"] = "#C8102E",
                    ["backgroundColor"] = null,
                    ["thumbnail"] = null
                }
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
