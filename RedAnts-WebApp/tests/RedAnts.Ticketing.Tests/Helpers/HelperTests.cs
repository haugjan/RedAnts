using RedAnts.Ticketing.Features.Helpers;
using Xunit;

namespace RedAnts.Ticketing.Tests.Helpers;

public class HelperTests
{
    [Fact]
    public async Task AddHelper_returns_the_stored_helper()
    {
        var helpers = new RecordingHelpers();

        var helper = await new AddHelperToSeason.Handler(helpers).HandleAsync(new AddHelperToSeason.Command(3, "Anna", "Muster", "anna@example.ch"));

        Assert.Equal(1, helper.Id);
        Assert.Equal("Anna", helper.FirstName);
        Assert.Single(helpers.Stored);
    }

    [Fact]
    public async Task AddHelper_rejects_invalid_mail_addresses()
    {
        var helpers = new RecordingHelpers();

        await Assert.ThrowsAsync<DomainException>(() => new AddHelperToSeason.Handler(helpers).HandleAsync(new AddHelperToSeason.Command(3, "Anna", "Muster", "keine-adresse")));

        Assert.Empty(helpers.Stored);
    }

    [Fact]
    public async Task Activation_assignment_and_deletion_delegate()
    {
        var helpers = new RecordingHelpers();
        var helper = await helpers.AddAsync(3, "Anna", "Muster", "anna@example.ch");

        await new SetHelperActive.Handler(helpers).HandleAsync(new SetHelperActive.Command(helper.Id, false));
        await new AssignHelperEvents.Handler(helpers).HandleAsync(new AssignHelperEvents.Command(helper.Id, false, [10, 11], true));
        await new RemoveHelperFromSeason.Handler(helpers).HandleAsync(new RemoveHelperFromSeason.Command(helper.Id));

        Assert.Equal((helper.Id, false), Assert.Single(helpers.ActiveChanges));
        var assignment = Assert.Single(helpers.Assignments);
        Assert.Equal(helper.Id, assignment.Id);
        Assert.False(assignment.AllEvents);
        Assert.Equal([10, 11], assignment.EventIds);
        Assert.True(assignment.CanRebook);
        Assert.Equal(helper.Id, Assert.Single(helpers.Deleted));
    }

    [Fact]
    public async Task InviteHelper_sends_the_invitation_with_the_login_link()
    {
        var helpers = new RecordingHelpers();
        var mailer = new RecordingHelperInviteMailer();
        var helper = await helpers.AddAsync(3, "Anna", "Muster", "anna@example.ch");

        var result = await new InviteHelperByMail.Handler(mailer).HandleAsync(new InviteHelperByMail.Command(helper, "Einladung", "Text", "https://scan.redants.ch/scan/login"));

        Assert.True(result.Success);
        var sent = Assert.Single(mailer.Sent);
        Assert.Same(helper, sent.Helper);
        Assert.Equal("https://scan.redants.ch/scan/login", sent.LoginLink);
    }
}
