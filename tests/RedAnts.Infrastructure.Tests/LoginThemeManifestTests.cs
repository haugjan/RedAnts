using System.Text.Json;
using RedAnts.Infrastructure.Shared;
using Xunit;

namespace RedAnts.Infrastructure.Tests;

public class LoginThemeManifestTests
{
    private readonly LoginThemeManifestReader _reader = new();

    [Fact]
    public async Task ReturnsExactlyOneManifest()
    {
        var manifests = await _reader.ReadPackageManifestsAsync();
        Assert.Single(manifests);
    }

    [Fact]
    public async Task ManifestIsPublic()
    {
        var manifest = (await _reader.ReadPackageManifestsAsync()).Single();
        Assert.True(manifest.AllowPublicAccess);
    }

    [Fact]
    public async Task ManifestHasExpectedName()
    {
        var manifest = (await _reader.ReadPackageManifestsAsync()).Single();
        Assert.Equal("RedAnts.LoginTheme", manifest.Name);
    }

    [Fact]
    public async Task ManifestHasExactlyOneExtension()
    {
        var manifest = (await _reader.ReadPackageManifestsAsync()).Single();
        Assert.Single(manifest.Extensions!);
    }

    [Fact]
    public async Task ExtensionHasCorrectProperties()
    {
        var manifest = (await _reader.ReadPackageManifestsAsync()).Single();
        var json = JsonSerializer.Serialize(manifest.Extensions);
        using var doc = JsonDocument.Parse(json);
        var ext = doc.RootElement[0];

        Assert.Equal("appEntryPoint", ext.GetProperty("type").GetString());
        Assert.Equal("RedAnts.Login.Theme", ext.GetProperty("alias").GetString());
        Assert.Equal("Red Ants Login Theme", ext.GetProperty("name").GetString());
        Assert.Equal("/App_Plugins/RedAntsLogin/login-theme.js", ext.GetProperty("js").GetString());
    }
}
