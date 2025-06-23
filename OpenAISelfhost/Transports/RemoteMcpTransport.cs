using OpenAISelfhost.DataContracts.Request.User;
using OpenAISelfhost.DataContracts.Response.MCP;
using OpenAISelfhost.Service.Interface;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OpenAISelfhost.Transports
{
    public class RemoteMcpTransport : IDisposable
    {
        public bool Disposed { get; private set; } = false;
        public Pipe ClientToServerPipe { get; private set; }
        public Pipe ServerToClientPipe { get; private set; }

        public event Action? OnDisposing;

        private bool authenticated { get; set; } = false;

        private readonly WebSocket webSocket;
        private readonly string correlationId;

        private DateTime LastMessageReceived { get; set; } = DateTime.UtcNow;
        private readonly Task receiveDataFromWebSocketTask;
        private readonly Task receiveDataFromPipeTask;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public Task WaitForCompletion() => Task.WhenAll([receiveDataFromWebSocketTask, receiveDataFromPipeTask]);
        private readonly IServiceProvider serviceProvider;

        public RemoteMcpTransport(WebSocket webSocket, string correlationId, IServiceProvider serviceProvider)
        {
            this.webSocket = webSocket;
            this.correlationId = correlationId;
            ClientToServerPipe = new Pipe();
            ServerToClientPipe = new Pipe();
            this.serviceProvider = serviceProvider;

            receiveDataFromPipeTask = ReceiveDataFromPipe(cancellationTokenSource.Token);
            receiveDataFromWebSocketTask = ReceiveDataFromWebSocket(cancellationTokenSource.Token);
        }

        private async Task ReceiveDataFromWebSocket(CancellationToken token)
        {
            var buffer = new byte[1024 * 1024];
            try
            {
                while (!token.IsCancellationRequested)
                {

                    Array.Clear(buffer, 0, buffer.Length);
                    var result = await webSocket.ReceiveAsync(buffer, token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // text message preserved for business logics communication.
                        HandleControlMessages(System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        var bufferSegment = new ArraySegment<byte>(buffer, 0, result.Count);
                        HandleMCPProtocolMessage(bufferSegment.ToArray());
                    }
                    LastMessageReceived = DateTime.UtcNow;
                }
            }
            finally
            {
                Dispose();
            }
        }

        public bool IsHealthy()
        {
            if (webSocket.State != WebSocketState.Open)
                return false;
            return (DateTime.UtcNow - LastMessageReceived).TotalSeconds < 60;
        }

        private async Task ReceiveDataFromPipe(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await ClientToServerPipe.Reader.ReadAsync(cancellationToken);
                    if (result.IsCompleted)
                        break;
                    var buffer = result.Buffer;
                    if (buffer.Length > 0)
                    {
                        await webSocket.SendAsync(buffer.ToArray(), WebSocketMessageType.Binary, true, cancellationToken);
                    }
                    ClientToServerPipe.Reader.AdvanceTo(buffer.End);
                }
            }
            finally
            {
                Dispose();
            }
        }

        public void HandleControlMessages(string message)
        {
            if (!authenticated)
            {
                using var scope = serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var authPayload = JsonSerializer.Deserialize<AuthPayload>(message);
                authenticated = userService.ValidateToken(authPayload?.Token ?? string.Empty);
                if (!authenticated)
                {
                    Dispose();
                    return;
                }
            }
            var response = new MCPTransportResponse()
            {
                CorrelationId = correlationId
            };
            webSocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response))),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).Wait();
        }

        public void HandleMCPProtocolMessage(byte[] message)
        {
            if(!authenticated)
            {
                return;
            }
            ServerToClientPipe.Writer.AsStream().Write(message);
        }

        public void Dispose()
        {
            Disposed = true;
            OnDisposing?.Invoke();
            cancellationTokenSource.Cancel();
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None).Wait();
                webSocket.Dispose();
            }
        }
    }
}
