using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using QuestPDF.Infrastructure;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Ticketing.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests;

public class TicketPdfRendererTests
{
    static TicketPdfRendererTests() => QuestPDF.Settings.License = LicenseType.Community;

    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M8AAAMBAQDJ/pLvAAAAAElFTkSuQmCC");

    private static TicketPdfModel Model(string holder, string scope) => new(
        TypeLabel: "Spielticket",
        ScopeName: scope,
        DateText: "01.01.2026",
        CategoryLabel: "Erwachsen",
        HolderName: holder,
        TicketRef: "AABBCCDD",
        AccentHex: "#D02D38",
        QrPng: MinimalPng,
        Kicker: "Einzelspiel",
        VenueName: "Sporthalle Rennweg",
        Admissions: 1);

    [Fact]
    public void Render_ShortValues_Works()
    {
        var pdf = new TicketPdfRenderer(new FakeEnv()).Render(Model("Anna Muster", "Red Ants vs. Gegner"));
        Assert.True(pdf.Length > 0);
    }

    [Fact]
    public void Render_LongUnbreakableValues_DoesNotThrow()
    {
        var longToken = new string('W', 80);
        var pdf = new TicketPdfRenderer(new FakeEnv())
            .Render(Model(longToken + " GmbH", "Red " + longToken + " Ants"));
        Assert.True(pdf.Length > 0);
    }

    [Fact]
    public void Render_WithEmojiOrExoticGlyph_DoesNotThrow()
    {
        var pdf = new TicketPdfRenderer(new FakeEnv())
            .Render(Model("Anna 😀 Müller ✓ 木村", "Red Ants 🐜 vs. Gegner"));
        Assert.True(pdf.Length > 0);
    }

    [Fact]
    public void Render_WithRealLogo_DoesNotThrow()
    {
        var wwwroot = @"C:\development\RedAnts-s1\src\RedAnts.Host\wwwroot";
        if (!File.Exists(Path.Combine(wwwroot, "img", "logo-badge.png"))) return;
        var pdf = new TicketPdfRenderer(new FakeEnv { WebRootPath = wwwroot })
            .Render(Model("Anna Muster", "Red Ants vs. Gegner"));
        Assert.True(pdf.Length > 0);
    }

    private sealed class FakeEnv : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "redants-tests-nowebroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
