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
            EnsureLegalContent();
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
        var richText = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.RichText") ?? textBox;
        EnsureRichTextToolbar(richText);

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

        var legalTpl = EnsureTemplate("LegalPage");
        if (contentTypeService.Get(A.LegalPageType) is null)
        {
            var legal = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = A.LegalPageType, Name = "Rechtliche Seite", Icon = "icon-document", AllowedAsRoot = true
            };
            legal.AddPropertyType(Prop(textBox, A.LegalTitle, "Titel"), Group, GroupName);
            legal.AddPropertyType(Prop(textBox, A.LegalSlug, "Pfad (Slug)", "Fester URL-Pfad, z. B. impressum oder datenschutz. Bitte nicht ändern."), Group, GroupName);
            legal.AddPropertyType(Prop(mediaPicker, A.LegalImage, "Headerbild", "Optionales Bild im Seitenkopf"), Group, GroupName);
            legal.AddPropertyType(Prop(richText, A.LegalBodyText, "Inhalt", "Überschriften und Texte der Seite"), Group, GroupName);
            AssignTemplate(legal, legalTpl);
            contentTypeService.Save(legal, SuperUser);
            logger.LogInformation("WebsiteContentTypeSeeder: created legalPage document type.");
        }

        if (contentTypeService.Get(A.FooterFolderType) is null)
        {
            var footer = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = A.FooterFolderType, Name = "Footer", Icon = "icon-folder", AllowedAsRoot = true
            };
            contentTypeService.Save(footer, SuperUser);

            if (contentTypeService.Get(A.LegalPageType) is { } legalType)
            {
                footer.AllowedContentTypes = new[] { new ContentTypeSort(legalType.Key, 0, legalType.Alias) };
                contentTypeService.Save(footer, SuperUser);
            }
            logger.LogInformation("WebsiteContentTypeSeeder: created Footer folder document type.");
        }
    }

    private void EnsureLegalContent()
    {
        if (contentTypeService.Get(A.LegalPageType) is null) return;
        if (contentTypeService.Get(A.FooterFolderType) is null) return;

        var footerId = EnsureFooterFolder();
        MoveRootLegalPagesUnder(footerId);
        EnsureLegalPage(footerId, "impressum", "Impressum", ImpressumBody());
        EnsureLegalPage(footerId, "datenschutz", "Datenschutzerklärung", DatenschutzBody());
        EnsureLegalPage(footerId, "agb", "AGB", AgbBody());
        PublishFooterLegalPages(footerId);
    }

    private void PublishFooterLegalPages(int footerId)
    {
        foreach (var node in contentService.GetPagedChildren(footerId, 0, 100, out _))
        {
            if (node.ContentType.Alias == A.LegalPageType && !node.Published)
            {
                contentService.Publish(node, new[] { "*" }, SuperUser);
                logger.LogInformation("WebsiteContentTypeSeeder: republished legal page {Name} under Footer.", node.Name);
            }
        }
    }

    private int EnsureFooterFolder()
    {
        var existing = contentService.GetRootContent()
            .FirstOrDefault(c => c.ContentType.Alias == A.FooterFolderType);
        if (existing is not null) return existing.Id;

        var node = contentService.Create("Footer", Constants.System.Root, A.FooterFolderType);
        contentService.Save(node, SuperUser);
        contentService.Publish(node, new[] { "*" }, SuperUser);
        logger.LogInformation("WebsiteContentTypeSeeder: created Footer folder.");
        return node.Id;
    }

    private void MoveRootLegalPagesUnder(int footerId)
    {
        var rootLegal = contentService.GetRootContent()
            .Where(c => c.ContentType.Alias == A.LegalPageType)
            .ToList();
        foreach (var node in rootLegal)
        {
            contentService.Move(node, footerId, SuperUser);
            logger.LogInformation("WebsiteContentTypeSeeder: moved legal page {Name} under Footer.", node.Name);
        }
    }

    private void EnsureLegalPage(int parentId, string slug, string title, string bodyHtml)
    {
        var exists = contentService.GetPagedChildren(parentId, 0, 100, out _).Any(c =>
            c.ContentType.Alias == A.LegalPageType
            && string.Equals(c.GetValue<string>(A.LegalSlug), slug, StringComparison.OrdinalIgnoreCase));
        if (exists) return;

        var node = contentService.Create(title, parentId, A.LegalPageType);
        node.SetValue(A.LegalTitle, title);
        node.SetValue(A.LegalSlug, slug);
        node.SetValue(A.LegalBodyText, Rte(bodyHtml));
        contentService.Save(node, SuperUser);
        contentService.Publish(node, new[] { "*" }, SuperUser);
        logger.LogInformation("WebsiteContentTypeSeeder: seeded legal page {Slug}.", slug);
    }

    private static string Rte(string html) =>
        JsonSerializer.Serialize(new
        {
            markup = html,
            blocks = new
            {
                layout = new Dictionary<string, object>(),
                contentData = Array.Empty<object>(),
                settingsData = Array.Empty<object>(),
                expose = Array.Empty<object>()
            }
        });

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
                  type = "EXTERNAL", udi = (string?)null, url = "/ticketing/",
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

    private void EnsureRichTextToolbar(IDataType richText)
    {
        if (richText.EditorAlias != "Umbraco.RichText") return;

        var existing = richText.ConfigurationData ?? new Dictionary<string, object>();
        if (serializer.Serialize(existing).Contains("Umb.Tiptap.Toolbar.Heading2", StringComparison.Ordinal))
            return;

        var config = new Dictionary<string, object>(existing)
        {
            ["toolbar"] = new object[]
            {
                new object[]
                {
                    new[] { "Umb.Tiptap.Toolbar.SourceEditor" },
                    new[] { "Umb.Tiptap.Toolbar.Heading2", "Umb.Tiptap.Toolbar.Heading3" },
                    new[] { "Umb.Tiptap.Toolbar.Bold", "Umb.Tiptap.Toolbar.Italic", "Umb.Tiptap.Toolbar.Underline", "Umb.Tiptap.Toolbar.Strike" },
                    new[] { "Umb.Tiptap.Toolbar.TextAlignLeft", "Umb.Tiptap.Toolbar.TextAlignCenter", "Umb.Tiptap.Toolbar.TextAlignRight" },
                    new[] { "Umb.Tiptap.Toolbar.BulletList", "Umb.Tiptap.Toolbar.OrderedList" },
                    new[] { "Umb.Tiptap.Toolbar.Blockquote", "Umb.Tiptap.Toolbar.HorizontalRule" },
                    new[] { "Umb.Tiptap.Toolbar.Link", "Umb.Tiptap.Toolbar.Unlink" },
                    new[] { "Umb.Tiptap.Toolbar.MediaPicker", "Umb.Tiptap.Toolbar.EmbeddedMedia" },
                    new[] { "Umb.Tiptap.Toolbar.ClearFormatting" }
                }
            }
        };

        richText.ConfigurationData = config;
        dataTypeService.Save(richText);
        logger.LogInformation("WebsiteContentTypeSeeder: enriched Rich Text toolbar with headings and clear formatting.");
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

    private static string ImpressumBody() =>
        """
        <p>Angaben gemäss den Vorgaben des schweizerischen Rechts (insbesondere Art. 3 Abs. 1 lit. s UWG).</p>
        <h2>Verein</h2>
        <p>Sportverein Red Ants Rychenberg Winterthur<br>Verein im Sinne von Art. 60 ff. ZGB</p>
        <h2>Adresse</h2>
        <p>Geschäftsstelle<br>Ruhtalstrasse 20<br>8400 Winterthur<br>Schweiz</p>
        <h2>Kontakt</h2>
        <p>E-Mail: <a href="mailto:sekretariat@redants.ch">sekretariat@redants.ch</a><br>Ticketing: <a href="mailto:tickets@redants.ch">tickets@redants.ch</a></p>
        <h2>Vertretungsberechtigte Person</h2>
        <p>[Name der Präsidentin oder des Präsidenten einsetzen]</p>
        <h2>Mehrwertsteuer</h2>
        <p>Der Verein ist nicht mehrwertsteuerpflichtig. Auf Käufen wird keine Mehrwertsteuer erhoben oder ausgewiesen.</p>
        <h2>Haftungsausschluss</h2>
        <p>Die Inhalte dieser Website werden mit grösstmöglicher Sorgfalt erstellt. Für die Richtigkeit, Vollständigkeit und Aktualität der Inhalte wird jedoch keine Gewähr übernommen. Für Inhalte externer Links sind ausschliesslich deren Betreiber verantwortlich.</p>
        <h2>Urheberrecht</h2>
        <p>Die auf dieser Website veröffentlichten Inhalte unterliegen dem schweizerischen Urheberrecht. Jede Verwendung, die nicht ausdrücklich vom Urheberrecht zugelassen ist, bedarf der vorherigen schriftlichen Zustimmung des Vereins.</p>
        """;

    private static string DatenschutzBody() =>
        """
        <p>Der verantwortungsvolle Umgang mit Ihren Daten ist dem Sportverein Red Ants Rychenberg Winterthur ein grosses Anliegen. In dieser Datenschutzerklärung informieren wir Sie darüber, wie wir Personendaten erheben und bearbeiten, wenn Sie unsere Website besuchen, Tickets oder Saisonkarten erwerben oder unseren Newsletter abonnieren.</p>
        <p>Unsere Datenbearbeitung richtet sich nach dem Schweizer Datenschutzgesetz (DSG).</p>
        <h2>1. Verantwortliche Stelle</h2>
        <p>Verantwortlich für die in dieser Datenschutzerklärung beschriebenen Datenbearbeitungen ist:</p>
        <p>Sportverein Red Ants Rychenberg Winterthur<br>Geschäftsstelle<br>Ruhtalstrasse 20<br>8400 Winterthur<br>E-Mail: <a href="mailto:sekretariat@redants.ch">sekretariat@redants.ch</a></p>
        <h2>2. Hosting und Bereitstellung der Website</h2>
        <p>Unsere Website wird auf Servern von Microsoft Azure gehostet. Beim Zugriff auf unsere Website werden automatisch Daten erhoben und in sogenannten Server-Logfiles gespeichert, die Ihr Browser automatisch an uns übermittelt. Dazu gehören IP-Adresse des anfragenden Endgeräts, Datum und Uhrzeit des Zugriffs, Name und URL der abgerufenen Datei, die Referrer-URL sowie der verwendete Browser und das Betriebssystem. Diese Daten sind technisch erforderlich, um Ihnen unsere Website stabil und sicher anzuzeigen.</p>
        <h2>3. Verwendung von Cookies</h2>
        <p>Wir verwenden auf unserer Website ausschliesslich technisch notwendige Cookies. Diese dienen dazu, den Betrieb, die Navigation und die grundlegenden Funktionen der Website zu gewährleisten. Wir setzen keine Cookies zu Marketing- oder Analysezwecken ein.</p>
        <h2>4. Ticket- und Saisonkartenkauf</h2>
        <p>Wenn Sie über unsere Homepage Tickets oder Saisonkarten erwerben, erheben wir die für die Abwicklung des Kaufs und den Einlass erforderlichen Daten (insbesondere Name, Vorname, E-Mail-Adresse und Postadresse).</p>
        <h3>4.1 Zahlungsabwicklung via Payrexx</h3>
        <p>Für die Abwicklung von Zahlungen im Onlineshop arbeiten wir mit dem Zahlungsdienstleister Payrexx (Payrexx AG, Burgstrasse 20, 3600 Thun) zusammen. Ihre Zahlungsdaten werden direkt an Payrexx übermittelt und dort verarbeitet. Die Red Ants Rychenberg Winterthur haben keinen Zugriff auf Ihre vollständigen Kreditkarten- oder Bankdaten. Es gelten die Datenschutzbestimmungen von Payrexx.</p>
        <h3>4.2 Aufbewahrung von Saisonkartendaten</h3>
        <p>Die Daten von Käuferinnen und Käufern von Saisonkarten werden separat von der regulären Mitgliederverwaltung des Vereins gespeichert und ausschliesslich für die Verwaltung und Gültigkeitsprüfung der Saisonkarten verwendet.</p>
        <h2>5. Newsletter (Fairgate)</h2>
        <p>Sie haben die Möglichkeit, auf unserer Website unseren Newsletter zu abonnieren. Wenn Sie sich für den Newsletter anmelden, verwenden wir die von Ihnen eingegebenen Daten ausschliesslich für diesen Zweck.</p>
        <p>Für die Verwaltung und den Versand des Newsletters nutzen wir die Vereinssoftware Fairgate (Fairgate AG, Brühlstrasse 41, 8400 Winterthur). Die Daten werden gemäss den Datenschutzstandards von Fairgate verarbeitet. Sie können sich jederzeit über den Abmeldelink im Newsletter oder durch eine Mitteilung an unsere Geschäftsstelle vom Newsletter abmelden.</p>
        <h2>6. Bild- und Tonaufnahmen bei Spielen</h2>
        <p>Wir weisen darauf hin, dass bei Heimspielen der Red Ants Rychenberg Winterthur Fotos und Videos für die Vereinsberichterstattung, die Website, Social-Media-Kanäle sowie für Printmedien gemacht werden. Mit dem Besuch der Spiele beziehungsweise dem Erwerb eines Tickets nehmen Sie zur Kenntnis, dass Sie auf diesen Aufnahmen sichtbar sein können. Sollten Sie im Einzelfall nicht mit einer Veröffentlichung einverstanden sein, wenden Sie sich bitte direkt an das Aufnahmepersonal vor Ort oder an unsere Geschäftsstelle.</p>
        <h2>7. Ihre Rechte</h2>
        <p>Sie haben im Rahmen des geltenden Datenschutzrechts das Recht auf Auskunft über Ihre von uns verarbeiteten Personendaten, das Recht auf Berichtigung unrichtiger Daten sowie das Recht auf Löschung Ihrer Daten, sofern keine gesetzlichen Aufbewahrungspflichten oder berechtigten Interessen unsererseits entgegenstehen. Bitte wenden Sie sich zur Ausübung Ihrer Rechte direkt an unsere Geschäftsstelle.</p>
        <h2>8. Änderungen dieser Datenschutzerklärung</h2>
        <p>Wir behalten uns vor, diese Datenschutzerklärung jederzeit anzupassen, damit sie den aktuellen rechtlichen Anforderungen entspricht oder um Änderungen unserer Dienstleistungen umzusetzen.</p>
        """;

    private static string AgbBody() =>
        """
        <p>Die Allgemeinen Geschäftsbedingungen werden hier ergänzt.</p>
        """;

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
