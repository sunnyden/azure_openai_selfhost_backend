using OpenAISelfhost.DataContracts.Response.MCP;
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

        private readonly WebSocket webSocket;
        private readonly string correlationId;

        private DateTime LastMessageReceived { get; set; } = DateTime.UtcNow;
        private readonly Timer heartBeatTimer;
        private readonly Task receiveDataFromWebSocketTask;
        private readonly Task receiveDataFromPipeTask;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public Task WaitForCompletion() => Task.WhenAll([receiveDataFromWebSocketTask, receiveDataFromPipeTask]);

        public RemoteMcpTransport(WebSocket webSocket, string correlationId)
        {
            this.webSocket = webSocket;
            this.correlationId = correlationId;
            ClientToServerPipe = new Pipe();
            ServerToClientPipe = new Pipe();

            receiveDataFromPipeTask = ReceiveDataFromPipe(cancellationTokenSource.Token);
            receiveDataFromWebSocketTask = ReceiveDataFromWebSocket(cancellationTokenSource.Token);
            heartBeatTimer = new Timer(SendHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        private void SendHeartbeat(object? _)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var heartbeatMessage = new MCPTransportResponse()
                {
                    CorrelationId = correlationId,
                };
                _ = webSocket.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(heartbeatMessage))),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            else
            {
                Dispose();
            }
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
                        // binary preserved for MCP communication.
                        HandleMCPProtocolMessage(buffer);
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
            ServerToClientPipe.Writer.Write(message);
        }

        public void Dispose()
        {
            Disposed = true;
            this.heartBeatTimer.Dispose();
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
