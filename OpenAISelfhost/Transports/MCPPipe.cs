using System.IO.Pipelines;

namespace OpenAISelfhost.Transports
{
    public class MCPPipe
    {
        public Pipe ClientToServerPipe { get; private set; }
        public Pipe ServerToClientPipe { get; private set; }

        public MCPPipe()
        {
            ClientToServerPipe = new Pipe();
            ServerToClientPipe = new Pipe();
        }
    }
}