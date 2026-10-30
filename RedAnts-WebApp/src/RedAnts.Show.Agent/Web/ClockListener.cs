using System.Net;
using System.Net.Sockets;

namespace RedAnts.Show.Agent.Web;

public static class ClockListener
{
    public static bool IsFree(string host, int port)
    {
        var address = host is "localhost" ? IPAddress.Loopback : IPAddress.TryParse(host, out var parsed) ? parsed : IPAddress.Any;
        try
        {
            var listener = new TcpListener(address, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static void UseClockRoot(this WebApplication app, int port) =>
        app.Use(async (context, next) =>
        {
            if (context.Connection.LocalPort == port && context.Request.Path == "/")
            {
                context.Response.Redirect("/uhr");
                return;
            }
            await next();
        });
}
