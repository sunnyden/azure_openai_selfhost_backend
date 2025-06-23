using OpenAISelfhost.Transports;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace OpenAISelfhost.Service.MCP
{
    public class MCPTransportService : IMCPTransportService
    {
        private readonly ConcurrentDictionary<string, RemoteMcpTransport> transports = new ConcurrentDictionary<string, RemoteMcpTransport>();
        private readonly IServiceProvider serviceProvider;

        public MCPTransportService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public RemoteMcpTransport? GetTransport(string correlationId)
        {
            if (transports.TryGetValue(correlationId, out var transport) && transport.IsHealthy())
            {
                return transport;
            }
            return null;
        }

        public RemoteMcpTransport AddTransport(WebSocket socket)
        {
            var correlationId = GenerateGuid();
            var transport = new RemoteMcpTransport(socket, correlationId, serviceProvider);
            transport.OnDisposing += () =>
            {
                transports.TryRemove(correlationId, out _);
            };
            transports[correlationId] = transport;
            return transport;
        }

        private string GenerateGuid()
        {
            while (true)
            {
                var guid = Guid.NewGuid();
                if (!transports.ContainsKey(guid.ToString()))
                {
                    return guid.ToString();
                }
            }
        }
    }
}
