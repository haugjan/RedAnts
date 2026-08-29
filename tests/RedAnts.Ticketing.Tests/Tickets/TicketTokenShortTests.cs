using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Infrastructure.Ticketing.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class TicketTokenShortTests
{
    private static TicketTokenService NewService(string secret = "unit-test-qr-secret-please-change")
    {
        var config = new StubConfig(new Dictionary<string, string?> { ["Tickets:QrSecret"] = secret });
        return new TicketTokenService(config, new StubEnv(), NullLogger<TicketTokenService>.Instance);
    }

    [Fact]
    public void Short_token_round_trips_to_the_printed_code()
    {
        var svc = NewService();
        var uuid = Guid.NewGuid();
        var expected = uuid.ToString("N")[..8];

        Assert.True(svc.TryVerifyShort(svc.CreateShort(uuid), out var code));
        Assert.Equal(expected, code);
    }

    [Fact]
    public void Short_token_is_far_shorter_than_the_full_token()
    {
        var svc = NewService();
        var uuid = Guid.NewGuid();
        Assert.True(svc.CreateShort(uuid).Length * 2 < svc.Create(TicketType.EventTicket, uuid, 123).Length);
    }

    [Fact]
    public void Tampered_short_token_is_rejected()
    {
        var svc = NewService();
        var arr = svc.CreateShort(Guid.NewGuid()).ToCharArray();
        var i = arr.Length / 2;
        arr[i] = arr[i] == 'A' ? 'B' : 'A';
        Assert.False(svc.TryVerifyShort(new string(arr), out _));
    }

    [Fact]
    public void Short_token_from_a_different_key_is_rejected()
    {
        var token = NewService("secret-one").CreateShort(Guid.NewGuid());
        Assert.False(NewService("secret-two").TryVerifyShort(token, out _));
    }

    [Fact]
    public void Legacy_and_short_verification_do_not_cross_accept()
    {
        var svc = NewService();
        var uuid = Guid.NewGuid();

        Assert.False(svc.TryVerifyShort(svc.Create(TicketType.SeasonPass, uuid, 5), out _));
        Assert.False(svc.TryVerify(svc.CreateShort(uuid), out _));
    }

    private sealed class StubConfig(Dictionary<string, string?> values) : IConfiguration
    {
        public string? this[string key]
        {
            get => values.GetValueOrDefault(key);
            set => values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];
        public IChangeToken GetReloadToken() => throw new NotSupportedException();
        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }

    private sealed class StubEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
