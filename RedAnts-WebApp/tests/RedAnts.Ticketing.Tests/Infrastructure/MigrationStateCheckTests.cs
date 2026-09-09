using Microsoft.Extensions.Logging;
using RedAnts.Ticketing.Infrastructure;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Xunit;

namespace RedAnts.Ticketing.Tests.Infrastructure;

public class MigrationStateCheckTests
{
    private const string Database = "sqldb-redants-agent";

    private readonly Upgrader _upgrader = new(new TicketingMigrationPlan());
    private readonly FakeKeyValues _keyValues = new();
    private readonly CapturingLogger _logger = new();

    private MigrationStateCheck Check => new(_keyValues, _logger);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ticketing-schema")]
    [InlineData("order-payment-columns")]
    public void An_empty_or_known_stored_state_passes(string? stored)
    {
        _keyValues.Stored = stored;

        Check.EnsureStoredStateIsKnown(_upgrader, Database);

        Assert.Empty(_logger.Lines);
    }

    [Fact]
    public void The_final_state_of_the_plan_passes()
    {
        _keyValues.Stored = _upgrader.Plan.FinalState;

        Check.EnsureStoredStateIsKnown(_upgrader, Database);

        Assert.Empty(_logger.Lines);
    }

    [Fact]
    public void The_state_is_read_from_the_upgrader_key()
    {
        Check.EnsureStoredStateIsKnown(_upgrader, Database);

        Assert.Equal(["Umbraco.Core.Upgrader.State+Ticketing"], _keyValues.Reads);
    }

    [Fact]
    public void An_unknown_stored_state_logs_one_line_and_throws_with_the_same_text()
    {
        _keyValues.Stored = "ticketing-from-another-branch";

        var exception = Assert.Throws<DomainException>(() => Check.EnsureStoredStateIsKnown(_upgrader, "sqldb-redants-dev"));

        var expected =
            "Ticketing migration plan does not know the stored state \"ticketing-from-another-branch\" of database \"sqldb-redants-dev\"; " +
            $"last known step is \"{_upgrader.Plan.FinalState}\". Reset the state or deploy the branch that owns it.";
        Assert.Equal(expected, exception.Message);
        var (level, line) = Assert.Single(_logger.Lines);
        Assert.Equal(LogLevel.Error, level);
        Assert.Equal(expected, line);
    }

    private sealed class FakeKeyValues : IKeyValueService
    {
        public string? Stored { get; set; }
        public List<string> Reads { get; } = [];

        public string? GetValue(string key)
        {
            Reads.Add(key);
            return Stored;
        }

        public IReadOnlyDictionary<string, string?>? FindByKeyPrefix(string keyPrefix) => null;
        public void SetValue(string key, string value) => Stored = value;
        public void SetValue(string key, string originValue, string newValue) => Stored = newValue;
        public bool TrySetValue(string key, string originValue, string newValue) => true;
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Line)> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add((logLevel, formatter(state, exception)));
    }
}
