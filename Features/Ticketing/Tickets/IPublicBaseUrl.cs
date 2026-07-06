using Microsoft.AspNetCore.Http;

namespace RedAnts.Features.Ticketing.Tickets;

public interface IPublicBaseUrl
{
    string Resolve(HttpRequest request);
}
