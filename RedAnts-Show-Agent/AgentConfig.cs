using System.Reflection;
using System.Text.Json;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent;

public sealed record AgentConfig(string CloudUrl = "", string HallId = "", string Key = "", int DeckPort = 5150, string Bind = "localhost", string BoardPassword = "")
{
    public static string DataDir => Path.Combine(AppContext.BaseDirectory, "data");

    public static string File => Path.Combine(DataDir, "agent.json");

    public static string Version =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";

    public bool IsComplete => CloudUrl.Length > 0 && HallId.Length > 0 && Key.Length > 0;

    public string HubUrl => CloudUrl.TrimEnd('/') + "/hubs/halle";

    public string Host => Bind.Length > 0 ? Bind : "localhost";

    public bool OnNetwork => Host is not ("localhost" or "127.0.0.1");

    public string? Warning => OnNetwork && BoardPassword.Length == 0
        ? "Der Agent hört im Netz, aber das Bord hat kein Passwort (agent.json: boardPassword)."
        : null;

    public string BoardUrl => $"http://{(OnNetwork ? Environment.MachineName.ToLowerInvariant() : "localhost")}:{DeckPort}";

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
            }
        }
        return config;
    }

    public AgentConfig Prompted()
    {
        if (IsComplete || Console.IsInputRedirected) return this;
        Console.WriteLine("Einrichtung des Hallen-Agenten (Werte stehen im Backoffice unter Show > Hallen):");
        var cloud = Ask("Cloud-URL", CloudUrl.Length > 0 ? CloudUrl : "https://show.redants.ch");
        var hall = Ask("Hallen-Kennung", HallId).ToLowerInvariant();
        var key = Ask("Agent-Schlüssel", Key);
        return this with { CloudUrl = cloud, HallId = hall, Key = key };
    }

    static string Ask(string label, string current)
    {
        Console.Write(current.Length > 0 ? $"{label} [{current}]: " : $"{label}: ");
        var input = Console.ReadLine()?.Trim() ?? "";
        return input.Length > 0 ? input : current;
    }
}
