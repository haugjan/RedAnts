using RedAnts.Features.Ticketing.Admin;
using Xunit;

namespace RedAnts.Features.Tests.Admin;

public class AdminFormatTests
{
    // --- TicketNo ---

    [Fact]
    public void TicketNo_EmptyGuid_ReturnsDash()
    {
        Assert.Equal("—", AdminFormat.TicketNo(Guid.Empty));
    }

    [Fact]
    public void TicketNo_RealGuid_ReturnsFirst8CharsOfNFormat()
    {
        var guid = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        Assert.Equal("A1B2C3D4", AdminFormat.TicketNo(guid));
    }

    [Fact]
    public void TicketNo_AlwaysExactlyEightChars()
    {
        for (var i = 0; i < 20; i++)
            Assert.Equal(8, AdminFormat.TicketNo(Guid.NewGuid()).Length);
    }

    [Fact]
    public void TicketNo_ResultIsUppercase()
    {
        var result = AdminFormat.TicketNo(Guid.NewGuid());
        Assert.Equal(result.ToUpperInvariant(), result);
    }

    // --- Initials ---

    [Fact]
    public void Initials_Null_ReturnsQuestionMark()
    {
        Assert.Equal("?", AdminFormat.Initials(null));
    }

    [Fact]
    public void Initials_Empty_ReturnsQuestionMark()
    {
        Assert.Equal("?", AdminFormat.Initials(""));
    }

    [Fact]
    public void Initials_WhitespaceOnly_ReturnsQuestionMark()
    {
        Assert.Equal("?", AdminFormat.Initials("   "));
    }

    [Fact]
    public void Initials_SingleWord_ReturnsTwoCharsUppercase()
    {
        Assert.Equal("AN", AdminFormat.Initials("Anna"));
    }

    [Fact]
    public void Initials_SingleChar_ReturnsSingleCharUppercase()
    {
        Assert.Equal("A", AdminFormat.Initials("a"));
    }

    [Fact]
    public void Initials_SingleWordLowercase_ConvertsToUppercase()
    {
        Assert.Equal("JA", AdminFormat.Initials("jan"));
    }

    [Fact]
    public void Initials_TwoWordsBySpace_ReturnsFirstAndLastInitial()
    {
        Assert.Equal("AM", AdminFormat.Initials("Anna Muster"));
    }

    [Fact]
    public void Initials_ThreeWords_ReturnsFirstAndLastInitial()
    {
        Assert.Equal("AM", AdminFormat.Initials("Anna Maria Muster"));
    }

    [Fact]
    public void Initials_DotSeparated_SplitsOnDot()
    {
        Assert.Equal("AM", AdminFormat.Initials("anna.muster"));
    }

    [Fact]
    public void Initials_DashSeparated_SplitsOnDash()
    {
        Assert.Equal("AM", AdminFormat.Initials("anna-muster"));
    }

    [Fact]
    public void Initials_UnderscoreSeparated_SplitsOnUnderscore()
    {
        Assert.Equal("AM", AdminFormat.Initials("anna_muster"));
    }

    [Fact]
    public void Initials_Email_StripsAtDomainThenSplitsByDot()
    {
        Assert.Equal("JH", AdminFormat.Initials("jan.haug@example.ch"));
    }

    [Fact]
    public void Initials_EmailWithSingleLocalPart_ReturnsTwoChars()
    {
        Assert.Equal("JA", AdminFormat.Initials("jan@example.com"));
    }

    [Fact]
    public void Initials_AlwaysUppercase()
    {
        var result = AdminFormat.Initials("anna muster");
        Assert.Equal(result.ToUpperInvariant(), result);
    }

    [Fact]
    public void Initials_MixedSeparators_SplitsOnAll()
    {
        Assert.Equal("AM", AdminFormat.Initials("anna_maria.muster"));
    }

    // --- CreatedShort ---

    [Fact]
    public void CreatedShort_WithOrderId_ShowsOnline()
    {
        var utc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var expectedDate = utc.ToLocalTime().ToString("dd.MM.yy");

        Assert.Equal($"{expectedDate} / online", AdminFormat.CreatedShort(utc, null, orderId: 42));
    }

    [Fact]
    public void CreatedShort_NoOrderIdNoName_ShowsDash()
    {
        var utc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var expectedDate = utc.ToLocalTime().ToString("dd.MM.yy");

        Assert.Equal($"{expectedDate} / —", AdminFormat.CreatedShort(utc, createdByName: null, orderId: null));
    }

    [Fact]
    public void CreatedShort_WithName_ShowsInitials()
    {
        var utc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var expectedDate = utc.ToLocalTime().ToString("dd.MM.yy");

        Assert.Equal($"{expectedDate} / JH", AdminFormat.CreatedShort(utc, "jan.haug", orderId: null));
    }

    [Fact]
    public void CreatedShort_OrderIdTakesPrecedenceOverName()
    {
        var utc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var result = AdminFormat.CreatedShort(utc, "Jan Haug", orderId: 99);

        Assert.Contains("online", result);
        Assert.DoesNotContain("JH", result);
    }

    [Fact]
    public void CreatedShort_DateUsesLocalTime()
    {
        var utc = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var expectedDate = utc.ToLocalTime().ToString("dd.MM.yy");
        var result = AdminFormat.CreatedShort(utc, null, null);

        Assert.StartsWith(expectedDate, result);
    }

    // --- CreatedTooltip ---

    [Fact]
    public void CreatedTooltip_WithOrderId_ShowsOnlinekauf()
    {
        var utc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var expectedStamp = utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        Assert.Equal($"{expectedStamp} — Onlinekauf",
            AdminFormat.CreatedTooltip(utc, null, null, orderId: 42));
    }

    [Fact]
    public void CreatedTooltip_NoOrderIdNoWho_ShowsStampOnly()
    {
        var utc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var expectedStamp = utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        Assert.Equal(expectedStamp,
            AdminFormat.CreatedTooltip(utc, null, null, orderId: null));
    }

    [Fact]
    public void CreatedTooltip_WithEmail_AppendEmail()
    {
        var utc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var expectedStamp = utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        Assert.Equal($"{expectedStamp} — test@example.ch",
            AdminFormat.CreatedTooltip(utc, "Jan Haug", "test@example.ch", orderId: null));
    }

    [Fact]
    public void CreatedTooltip_NameButNoEmail_AppendName()
    {
        var utc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var expectedStamp = utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        Assert.Equal($"{expectedStamp} — Jan Haug",
            AdminFormat.CreatedTooltip(utc, "Jan Haug", null, orderId: null));
    }

    [Fact]
    public void CreatedTooltip_EmailTakesPrecedenceOverName()
    {
        var utc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var result = AdminFormat.CreatedTooltip(utc, "Jan Haug", "jan@example.ch", orderId: null);

        Assert.Contains("jan@example.ch", result);
        Assert.DoesNotContain("Jan Haug", result);
    }

    [Fact]
    public void CreatedTooltip_OrderIdTakesPrecedenceOverEverything()
    {
        var utc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var result = AdminFormat.CreatedTooltip(utc, "Jan Haug", "jan@example.ch", orderId: 5);

        Assert.Contains("Onlinekauf", result);
        Assert.DoesNotContain("jan@example.ch", result);
        Assert.DoesNotContain("Jan Haug", result);
    }

    [Fact]
    public void CreatedTooltip_DateFormatIncludesTime()
    {
        var utc = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var result = AdminFormat.CreatedTooltip(utc, null, null, null);

        Assert.Matches(@"\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}", result);
    }
}
