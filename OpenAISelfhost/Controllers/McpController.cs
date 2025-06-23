using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAISelfhost.DataContracts.Response.Common;
using OpenAISelfhost.Service.MCP;

namespace OpenAISelfhost.Controllers
{
    [Route("mcp")]
    [ApiController]
    public class McpController : ApiControllerBase
    {
        private readonly IMCPTransportService mcpTransportService;

        public McpController(IMCPTransportService mcpTransportService)
        {
            this.mcpTransportService = mcpTransportService;
        }

        // websocket connect to frontend
        [Route("transport")]
        public async Task McpTransportController()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var transport = mcpTransportService.AddTransport(webSocket);
                try
                {
                    await transport.WaitForCompletion();
                }catch(Exception ex)
                {
                    Console.WriteLine($"Error in MCP transport: {ex.Message}");
                }
                finally
                {
                   
                }

                Console.WriteLine($"WebSocket connection closed");
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
