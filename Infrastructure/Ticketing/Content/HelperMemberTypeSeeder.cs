using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace RedAnts.Infrastructure.Ticketing.Content;

public static class HelperAliases
{
    public const string MemberType = "scanHelper";
    public const string Code = "helperCode";
    public const string SeasonId = "helperSeasonId";
    public const string FirstName = "helperFirstName";
    public const string LastName = "helperLastName";
    public const string AllEvents = "helperAllEvents";
    public const string EventIds = "helperEventIds";
}

public sealed class HelperMemberTypeSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, HelperMemberTypeSeeder>();
}

public sealed class HelperMemberTypeSeeder(
    IMemberTypeService memberTypeService,
    IDataTypeService dataTypeService,
    IShortStringHelper shortStringHelper,
    ILogger<HelperMemberTypeSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string Group = "helfer";
    private const string GroupName = "Helfer";

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            if (memberTypeService.Get(HelperAliases.MemberType) is not null) return Task.CompletedTask;

            var all = dataTypeService.GetAll().ToList();
            IDataType ByEditor(string alias) => all.First(d => d.EditorAlias == alias);
            var textBox = ByEditor("Umbraco.TextBox");
            var integer = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.Integer") ?? textBox;
            var boolean = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TrueFalse") ?? textBox;

            var type = new MemberType(shortStringHelper, Constants.System.Root)
            {
                Alias = HelperAliases.MemberType,
                Name = "Scan-Helfer",
                Icon = "icon-user"
            };
            type.AddPropertyType(Prop(textBox, HelperAliases.Code, "Zugangscode"), Group, GroupName);
            type.AddPropertyType(Prop(textBox, HelperAliases.FirstName, "Vorname"), Group, GroupName);
            type.AddPropertyType(Prop(textBox, HelperAliases.LastName, "Nachname"), Group, GroupName);
            type.AddPropertyType(Prop(integer, HelperAliases.SeasonId, "Saison-Id"), Group, GroupName);
            type.AddPropertyType(Prop(boolean, HelperAliases.AllEvents, "Alle Anlässe"), Group, GroupName);
            type.AddPropertyType(Prop(textBox, HelperAliases.EventIds, "Zugewiesene Anlässe"), Group, GroupName);

            memberTypeService.Save(type);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HelperMemberTypeSeeder failed.");
        }
        return Task.CompletedTask;
    }

    private PropertyType Prop(IDataType dataType, string alias, string name) =>
        new(shortStringHelper, dataType, alias) { Name = name };
}
