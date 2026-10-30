using System.Reflection;
using System.Text.Json;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent;

public sealed record AgentConfig(string CloudUrl = "", string HallId = "", string Key = "", int DeckPort = 5150, string Bind = "localhost", string BoardPassword = "", string CloudPassword = "", string HallName = "", int ClockPort = 80)
{
    public const string DefaultCloud = "https://show.redants.ch";

    public const string ChooseHall = "--choose-hall";

    public string Cloud => CloudUrl.Length > 0 ? CloudUrl.TrimEnd('/') : DefaultCloud;

    public static string DataDir => Path.Combine(AppContext.BaseDirectory, "data");

    public static string File => Path.Combine(DataDir, "agent.json");

    public static string Version =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";

    public bool IsComplete => HallId.Length > 0 && Key.Length > 0;

    public string HubUrl => Cloud + "/hubs/halle";

    public string Wanted => Bind.Length > 0 ? Bind : "localhost";

    public bool Protected => BoardPassword.Length > 0;

    public bool WantsNetwork => Wanted is not ("localhost" or "127.0.0.1");

    public string Host => WantsNetwork && !Protected ? "localhost" : Wanted;

    public bool OnNetwork => Host is not ("localhost" or "127.0.0.1");

    public string? Warning => WantsNetwork && !Protected
        ? $"Das Bord soll im Netz hören ({Wanted}), hat aber kein Passwort (agent.json: boardPassword). Der Agent bleibt deshalb auf localhost."
        : null;

    public bool WantsClockPort => ClockPort > 0 && ClockPort != DeckPort;

    public string ClockUrl => ClockPort == 80
        ? $"http://{(OnNetwork ? Environment.MachineName.ToLowerInvariant() : "localhost")}/uhr"
        : $"http://{(OnNetwork ? Environment.MachineName.ToLowerInvariant() : "localhost")}:{ClockPort}/uhr";

    public string BoardUrl =>$"http://{(OnNetwork ? Environment.MachineName.ToLowerInvariant() : "localhost")}:{DeckPort}";

    public static AgentConfig Load()
    {
        if (!System.IO.File.Exists(File)) return new AgentConfig();
        try
        {
            return JsonSerializer.Deserialize<AgentConfig>(System.IO.File.ReadAllText(File), PayloadJson.Options) ?? new AgentConfig();
        }
        catch (JsonException) { return new AgentConfig(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this, PayloadJson.Pretty));
    }

    public AgentConfig WithArgs(string[] args)
    {
        var config = this;
        for (var i = 0; i + 1 < args.Length; i++)
        {
            switch (args[i])
            {
                case "--cloud": config = config with { CloudUrl = args[++i].Trim() }; break;
                case "--hall": config = config with { HallId = args[++i].Trim().ToLowerInvariant() }; break;
                case "--key": config = config with { Key = args[++i].Trim() }; break;
                case "--deck-port": config = config with { DeckPort = int.Parse(args[++i]) }; break;
                case "--bind": config = config with { Bind = args[++i].Trim() }; break;
                case "--board-password": config = config with { BoardPassword = args[++i].Trim() }; break;
                case "--cloud-password": config = config with { CloudPassword = args[++i].Trim() }; break;
                case "--clock-port": config = config with { ClockPort = int.Parse(args[++i]) }; break;
            }
        }
        return config;
    }
}
