using System.Reflection;
using OpenAISelfhost.Transports;

namespace OpenAISelfhost.Service.OpenAI
{
    public class LocalMCPService : IDisposable
    {
        private IHost host;
        public LocalMCPService(MCPPipe pipe)
        {
            var builder = Host.CreateApplicationBuilder();
            var mcpBuilder = builder.Services
                        .AddMcpServer()
                        .WithStreamServerTransport(pipe.ClientToServerPipe.Reader.AsStream(), pipe.ServerToClientPipe.Writer.AsStream());
            Assembly? toolsAssembly;
            try
            {
                toolsAssembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "tools", "MCPTools.dll"));
            }
            catch (Exception)
            {
                toolsAssembly = null;
            }
            if (toolsAssembly != null)
            {
                mcpBuilder.WithToolsFromAssembly(toolsAssembly);
            }
            host = builder.Build();
        }
        
        public async Task StartAsync()
        {
            await host.StartAsync();
        }

        public async Task StopAsync()
        {
            await host.StopAsync();
        }

        public void Dispose()
        {
            try
            {
                host.StopAsync().Wait();
            }catch (Exception)
            {
                // ignored
            }
            
            host.Dispose();
        }
    }
}
