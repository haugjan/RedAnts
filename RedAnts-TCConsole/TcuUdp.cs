using System.Net.Sockets;
using System.Text;

namespace TcuConsole;

public class TcuUdp
{
    const string Host = "127.0.0.1";
    const int    Port = 7001;

    public void Send(string command)
    {
        using var client = new UdpClient();
        var bytes = Encoding.ASCII.GetBytes(command);
        client.Send(bytes, bytes.Length, Host, Port);
    }
}
