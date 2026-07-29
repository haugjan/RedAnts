using Microsoft.Extensions.Configuration;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class EmailTransportSelector(IEnumerable<IEmailTransport> transports, IConfiguration config)
{
    public IReadOnlyList<IEmailTransport> Active()
    {
        var configured = (config["Email:Transports"] ?? "Graph")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var active = new List<IEmailTransport>();
        foreach (var name in configured)
        {
            var transport = transports.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (transport is not null && !active.Contains(transport))
                active.Add(transport);
        }

        if (active.Count == 0)
        {
            var fallback = transports.FirstOrDefault(t => t.Name == "Graph");
            if (fallback is not null) active.Add(fallback);
        }

        return active;
    }
}
