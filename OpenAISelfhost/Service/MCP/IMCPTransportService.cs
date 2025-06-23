using OpenAISelfhost.Transports;
using System.Net.WebSockets;

namespace OpenAISelfhost.Service.MCP
{
    public interface IMCPTransportService
    {
        RemoteMcpTransport AddTransport(WebSocket socket);
        RemoteMcpTransport? GetTransport(string correlationId);
    }
}