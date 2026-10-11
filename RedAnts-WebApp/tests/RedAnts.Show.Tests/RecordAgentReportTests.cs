using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;
using Xunit;

namespace RedAnts.Show.Tests;

public class RecordAgentReportTests
{
    private sealed class Reports : IAgentReports
    {
        public List<(string Hall, AgentReport Report)> Recorded { get; } = [];
        public Task RecordAsync(string hallId, AgentReport report) { Recorded.Add((hallId, report)); return Task.CompletedTask; }
        public Task<IReadOnlyList<AgentReport>> LatestAsync(string hallId) =>
            Task.FromResult<IReadOnlyList<AgentReport>>(Recorded.Where(r => r.Hall == hallId).Select(r => r.Report).ToList());
    }

    private sealed class Halls(Hall? hall) : IHallRepository
    {
        public Task<Hall?> GetByIdAsync(string id) => Task.FromResult(hall?.Id == id ? hall : null);
        public Task SaveAsync(Hall hall) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
    }

    private sealed class Sounds : ISoundPlayer
    {
        public List<string> Calls { get; } = [];
        public Task PlayAsync(string? room, string tileId, string? profileId) { Calls.Add($"play {room ?? "-"} {profileId ?? "-"}/{tileId}"); return Task.CompletedTask; }
        public Task StopAsync(string? room) { Calls.Add($"stop {room ?? "-"}"); return Task.CompletedTask; }
        public Task FadeAsync(string? room) { Calls.Add($"fade {room ?? "-"}"); return Task.CompletedTask; }
    }

    private static Hall HallWithRoom(string room) =>
        Hall.Create("win4", "Win4") with { Devices = new HallDevices(DjRoom: room) };

    [Fact]
    public async Task Sound_reports_reach_the_dj_board_of_the_hall()
    {
        var reports = new Reports();
        var sounds = new Sounds();
        var handler = new RecordAgentReport.Handler(reports, new Halls(HallWithRoom("Halle A")), sounds);

        await handler.HandleAsync(new RecordAgentReport.Command("win4", "sound", """{"action":"sound.play","target":"tile:heim/t1","scene":"tor-heim"}"""));
        await handler.HandleAsync(new RecordAgentReport.Command("win4", "sound", """{"action":"sound.fade","target":"","scene":"pause-ende"}"""));
        await handler.HandleAsync(new RecordAgentReport.Command("win4", "Sound", """{"action":"sound.stop"}"""));

        Assert.Equal(["play Halle A heim/t1", "fade Halle A", "stop Halle A"], sounds.Calls);
        Assert.All(reports.Recorded, r => Assert.Equal("sound", r.Report.Kind));
    }

    [Fact]
    public async Task Other_reports_are_only_recorded()
    {
        var reports = new Reports();
        var sounds = new Sounds();
        var handler = new RecordAgentReport.Handler(reports, new Halls(HallWithRoom("")), sounds);

        await handler.HandleAsync(new RecordAgentReport.Command("win4", "score", """{"home":"1","away":"0"}"""));
        await handler.HandleAsync(new RecordAgentReport.Command("win4", "sound", """{"action":"led.start","target":"tile:x"}"""));

        Assert.Empty(sounds.Calls);
        Assert.Equal(2, reports.Recorded.Count);
    }

    [Fact]
    public async Task A_tile_without_profile_plays_in_every_board_when_no_room_is_set()
    {
        var sounds = new Sounds();
        var handler = new RecordAgentReport.Handler(new Reports(), new Halls(HallWithRoom("")), sounds);

        await handler.HandleAsync(new RecordAgentReport.Command("win4", "sound", """{"action":"sound.play","target":"tile:t9"}"""));

        Assert.Equal(["play - -/t9"], sounds.Calls);
    }

    [Theory]
    [InlineData("tile:heim/t1", "heim", "t1")]
    [InlineData("tile:t1", null, "t1")]
    [InlineData("led:tor-heim", null, null)]
    [InlineData("tile:", null, null)]
    public void Targets_split_into_profile_and_tile(string target, string? profile, string? tile)
    {
        Assert.Equal((profile, tile), SoundRequest.SplitTarget(target));
    }
}
