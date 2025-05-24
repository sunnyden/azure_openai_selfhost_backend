using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using OpenAISelfhost.DatabaseContext;
using OpenAISelfhost.DataContracts.Utils.Serialization.Chat;
using OpenAISelfhost.Exceptions;
using OpenAISelfhost.Service;
using OpenAISelfhost.Service.Billing;
using OpenAISelfhost.Service.Interface;
using OpenAISelfhost.Service.OpenAI;
using System.Text;
using Microsoft.AspNetCore.ResponseCompression;
using OpenAISelfhost.Middleware;
using OpenAISelfhost.Transports;
using System.Reflection;
var pipe = new MCPPipe();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddExceptionHandler<ExceptionHandler>();

builder.Services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonChatRoleConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonChatContentTypeConverter());
                });
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IChatService, ChatService>();
builder.Services.AddTransient<ITransactionService, TransactionService>();
builder.Services.AddTransient<IModelService, ModelService>();
builder.Services.AddSingleton(pipe);
// try to load all assemblies from ./tools directory
Assembly? toolsAssembly;
try
{
    toolsAssembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "tools", "MCPTools.dll"));
}catch (Exception)
{
    toolsAssembly = null;
}

var mcpBuilder = builder.Services.AddMcpServer()
                        .WithStreamServerTransport(pipe.ClientToServerPipe.Reader.AsStream(), pipe.ServerToClientPipe.Writer.AsStream());
if (toolsAssembly != null)
{
    mcpBuilder.WithToolsFromAssembly(toolsAssembly);
}
                
builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JWT:ValidAudience"],
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
                    };
                });
var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
builder.Services.AddDbContext<ServiceDatabaseContext>(options => options.UseMySql(builder.Configuration.GetConnectionString("DBConn"), serverVersion));

// Add response compression with exception for SSE content type
builder.Services.AddResponseCompression(options =>
{
    // Only compress specific mime types (exclude text/event-stream)
    options.EnableForHttps = true;
    options.MimeTypes = new[] 
    {
        "application/json",
        "application/xml",
        "text/plain",
        "text/html"
    };
    // Explicitly exclude SSE content type
    options.ExcludedMimeTypes = new[] { "text/event-stream" };
});

var app = builder.Build();

// Our custom middleware to disable compression for SSE endpoints
app.UseDisableCompressionForSSE();

app.UseResponseCompression();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseExceptionHandler(options => { });
app.Run();
