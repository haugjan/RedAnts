using Microsoft.AspNetCore.SignalR;
using RedAnts.Show.Features.Board;

namespace RedAnts.Show.Features.Agents;

public sealed class AgentHub(
    RegisterAgent.Handler register,
    UnregisterAgent.Handler unregister,
    RecordAgentReport.Handler report,
    GetAgentPayload.Handler payload,
    RecordDeckView.Handler deck) : Hub
{
    public const string Route = "/hubs/halle";
    private const string HallItem = "hall";

    public static string GroupOf(string hallId) => "hall:" + hallId;

    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var hall = http?.Request.Query["hall"].ToString().Trim().ToLowerInvariant() ?? "";
        var key = http?.Request.Query["key"].ToString();
        var version = http?.Request.Query["version"].ToString() ?? "";
        if (!await register.HandleAsync(new RegisterAgent.Command(hall, key, Context.ConnectionId, version)))
        {
            Context.Abort();
            return;
        }
        Context.Items[HallItem] = hall;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(hall));
        if (await payload.HandleAsync(new GetAgentPayload.Query(hall)) is { } current)
            await Clients.Caller.SendAsync("Sync", current);
        await base.OnConnectedAsync();
    }

    public Task Deck(string deckId, string json) =>
        Context.Items[HallItem] is string hall
            ? deck.HandleAsync(new RecordDeckView.Command(hall, deckId, json))
            : Task.CompletedTask;

    public Task Live(string json) =>
        Context.Items[HallItem] is string hall
            ? deck.HandleAsync(new RecordDeckView.Command(hall, "", json))
            : Task.CompletedTask;

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items[HallItem] is string hall) await deck.HandleAsync(new RecordDeckView.Command(hall, "", ""));
        await unregister.HandleAsync(new UnregisterAgent.Command(Context.ConnectionId));
        await base.OnDisconnectedAsync(exception);
    }

    public Task Report(string kind, string json) =>
        Context.Items[HallItem] is string hall
            ? report.HandleAsync(new RecordAgentReport.Command(hall, kind, json))
            : Task.CompletedTask;
}
